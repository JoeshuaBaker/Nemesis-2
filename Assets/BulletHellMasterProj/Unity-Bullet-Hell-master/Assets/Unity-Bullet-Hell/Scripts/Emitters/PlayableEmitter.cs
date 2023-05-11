using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BulletHell;
using UnityEditor;

[ExecuteAlways]
public class PlayableEmitter : ProjectileEmitterAdvanced
{
    public struct TimelineProperties
    {
        public EmitterProperties clipProperties;
#if UNITY_EDITOR
        public EditorCurveBinding[] propertiesCurves;
#endif
        public AnimationClip animationClip;
    }

    public List<TimelineProperties> timelineProperties;
    public List<EmitterShootBehaviour> shootBehaviours;
    private EmitterProperties baseProps = null;
    public AnimationCurve defaultLinearCurve;
    [SerializeField] public List<EmitterProperties> shootEvents;
    private float lastTime = 0f;

    public override void Initialize(int size)
    {
        if(shootEvents == null)
        {
            base.Initialize(size);
            return;
        }
        
        int numBulletsNeeded = 1;
        foreach(var e in shootEvents)
        {
            EmitterProperties prop = e;
            numBulletsNeeded += (prop.GroupCount * prop.SpokeCount);
        }
        base.Initialize(numBulletsNeeded);
        Debug.Log("Init finished on " + this.name);
    }

    public override void UpdateEmitter(float tick)
    {
        
    }

    
    public void Build()
    {
        //only build state in edit mode because unity's timeline curves are a fucking nightmare i guess
        if(!Application.isEditor)
            return;

        if(Camera == null)
            Camera = Camera.main;

        if(baseProps == null)
            baseProps = props;

        if(shootEvents == null)
            shootEvents = new List<EmitterProperties>();
        else
            shootEvents.Clear();

        int emitterId = 0;
        
        foreach(var shoot in shootBehaviours)
        {
            int shots = shoot.numShots;
            double nextShot = shoot.start;

            //Process all shots this clip will make into ShootEvents
            while(nextShot < shoot.end && shots > 0)
            {
                int propIndex = GetPropertiesIndex(nextShot);
                if(propIndex == -1)
                {
                    EmitterProperties snapshotProps = baseProps.Copy();
                    snapshotProps.id = emitterId++;
                    snapshotProps.spawnTime = nextShot;
                    shootEvents.Add(snapshotProps);
                    nextShot += snapshotProps.CoolOffTime;
                }
                else
                {
                    TimelineProperties timelineProp = timelineProperties[propIndex];
                    EmitterProperties snapshotProps = timelineProp.clipProperties.Copy();

#if UNITY_EDITOR
                    if(timelineProp.propertiesCurves != null && timelineProp.animationClip != null)
                    {
                        EditorCurveBinding[] curveBindings = timelineProp.propertiesCurves;
                        AnimationClip animClip = timelineProp.animationClip;

                        foreach(EditorCurveBinding curve in curveBindings)
                        {
                            AnimationCurve animCurve = AnimationUtility.GetEditorCurve(animClip, curve);
                            float f = animCurve.Evaluate(Mathf.Max((float)(nextShot - snapshotProps.start), 0));
                            //simple value type
                            if(!curve.propertyName.Contains("."))
                            {
                                FieldInfo field = snapshotProps.GetType().GetField(curve.propertyName);
                                if (field != null)
                                {
                                    SetFieldSafe(field, snapshotProps, f);
                                }
                                else
                                {
                                    Debug.Log("Field not found for curve property name: " + curve.propertyName);
                                }
                            }
                            //Complex object value -- only go one layer deep for simplicity's sake
                            else
                            {
                                string[] pieces = curve.propertyName.Split('.');
                                if(pieces.Length > 2)
                                {
                                    Debug.Log("Cannot reflect upon complex property: " + curve.propertyName);
                                    continue;
                                }
                                FieldInfo field = snapshotProps.GetType().GetField(pieces[0]);
                                object vector = field.GetValue(snapshotProps);
                                FieldInfo subfield = vector.GetType().GetField(pieces[1]);
                                SetFieldSafe(subfield, vector, f);
                                field.SetValue(snapshotProps, vector);
                            }
                        }
                    }
#endif

                    snapshotProps.id = emitterId++;
                    snapshotProps.spawnTime = nextShot;
                    shootEvents.Add(snapshotProps);
                    nextShot += snapshotProps.CoolOffTime;
                    shots--;
                }
            }
        }

        Awake();
        defaultLinearCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        Debug.Log($"Build Finished.");
    }

    //Copied from ProjectileEmitterAdvanced. Update to seek to any time given.
    protected override void UpdateProjectiles(float time)
    {
        ActiveProjectileCount = 0;
        ActiveOutlineCount = 0;

        //Update camera planes if needed
        if (props.CullProjectilesOutsideCameraBounds)
        {
            GeometryUtility.CalculateFrustumPlanes(Camera, Planes);
        }

        int previousIndexCount = ActiveProjectileIndexesPosition;
        ActiveProjectileIndexesPosition = 0;
        HashSet<int> activeProjectileIds = new HashSet<int>();

        // Only loop through currently active projectiles
        for (int i = 0; i < PreviousActiveProjectileIndexes.Length - 1; i++)
        {
            // End of array is set to -1
            if (PreviousActiveProjectileIndexes[i] == -1)
                break;

            Pool<ProjectileData>.Node node = Projectiles.GetActive(PreviousActiveProjectileIndexes[i]);
            UpdateProjectile(ref node, time);

            // If still active store in our active projectile collection
            if (node.Active)
            {
                ActiveProjectileIndexes[ActiveProjectileIndexesPosition] = node.NodeIndex;
                ActiveProjectileIndexesPosition++;
                activeProjectileIds.Add(node.Item.emitterId);
            }
        }

        // Set end point of array so we know when to stop looping
        ActiveProjectileIndexes[ActiveProjectileIndexesPosition] = -1;

        // Overwrite old previous active projectile index array
        System.Array.Copy(ActiveProjectileIndexes, PreviousActiveProjectileIndexes, Mathf.Max(ActiveProjectileIndexesPosition, previousIndexCount));

        foreach(var shot in shootEvents)
        {
            EmitterProperties props = shot;
            //Debug.Log("Area under speedCurve: " + AreaUnderCurve(props.SpeedCurve, 1.0f, 1.0f));
            double spawnTime = props.spawnTime;
            if(IsProjectileAlive(props, spawnTime, time) && !activeProjectileIds.Contains(props.id))
            {
                FireProjectile(props, props.Direction, spawnTime, time);
            }
        }
    }
    

    //Copied from ProjectileEmitterAdvanced. Called from UpdateProjectiles. Rewrite to use time.
    protected override void UpdateProjectile(ref Pool<ProjectileData>.Node node, float time)
    {
        // Projectile is active
        if (IsProjectileAlive(node.Item, time))
        {
            UpdateProjectileNodePulse(time, ref node.Item);
            float dt = time - node.Item.TimeSpawned;

            Vector2 startPos = node.Item.Position;
            //s = ut + (1/2)a t^2
            //s = position, u = velocity at t(0), t = time, a = constant accelleration
            //base velocity

            //Debug.Log($"time: {time}, timeSpawned: {node.Item.TimeSpawned}, timeToLive: {node.Item.TimeToLive}");
            float t1 = Mathf.Max(time - node.Item.TimeSpawned, 0)/node.Item.TimeToLive;
            //Debug.Log($"t1: {t1}");

            float turn = MinMaxArea(node.Item.TurningCurvesSelector, node.Item.TurningCurves, 0f, t1);
            Vector2 baseDirection = node.Item.Velocity;

            //float ceilRadians = node.Item.TurnCeil * Mathf.Deg2Rad;
            //float floorRadians = node.Item.TurnFloor * Mathf.Deg2Rad;
            //float a = Mathf.Atan2(Mathf.Sin(floorRadians - ceilRadians), Mathf.Cos(floorRadians - ceilRadians));
            //a *= Mathf.Rad2Deg;
            float degrees = node.Item.TurnCeil * turn * node.Item.TimeToLive;
            Quaternion rotation = Quaternion.AngleAxis(degrees, Vector3.forward);
            Vector2 rotatedDirection = rotation * baseDirection;

            float move = MinMaxArea(node.Item.SpeedCurveSelector, node.Item.SpeedCurves, 0f, t1);
            //Debug.Log($"area under curve scalar: {move}");
            node.Item.Position = node.Item.InitialPosition + (node.Item.SpeedCeil - node.Item.SpeedFloor) * node.Item.TimeToLive * move * rotatedDirection;
            //node.Item.Position = node.Item.Velocity*dt;
            //velocity change due to constant acceleration
            //node.Item.Position += new Vector2(0.5f * node.Item.Acceleration * (dt * dt), 0.5f * node.Item.Acceleration * (dt * dt))*node.Item.Velocity.normalized;
            //velocity change due to gravity
            //node.Item.Position += new Vector2(0.5f*node.Item.Gravity.x*(dt*dt), 0.5f*node.Item.Gravity.y*(dt*dt));

            // follow target
            /*
            if (props.FollowTargetType == FollowTargetType.Homing && node.Item.FollowTarget && node.Item.Target != null)
            {
                node.Item.Speed += props.Acceleration * time;
                node.Item.Speed = Mathf.Clamp(node.Item.Speed, -props.MaxSpeed, props.MaxSpeed);

                Vector2 desiredVelocity = (new Vector2(props.Target.transform.position.x, props.Target.transform.position.y) - node.Item.Position).normalized;
                desiredVelocity *= node.Item.Speed;

                Vector2 steer = desiredVelocity - node.Item.Velocity;
                node.Item.Velocity = Vector2.ClampMagnitude(node.Item.Velocity + steer * node.Item.FollowIntensity * time, node.Item.Speed);
            }
            else
            {
                // apply gravity
                node.Item.Velocity += node.Item.Gravity * time;
            }
            */

            Vector2 deltaPosition = node.Item.Position - startPos;
            float distance = deltaPosition.magnitude;
            float radius = node.Item.Scale / 2f;
            
            // Update foreground and outline color data
            UpdateProjectileColor(ref node.Item);

            int result = -1;
            if (props.CollisionDetection == CollisionDetectionType.Raycast)
            {
                result = Physics2D.Raycast(node.Item.Position, deltaPosition, ContactFilter, RaycastHitBuffer, distance);
            }
            else if (props.CollisionDetection == CollisionDetectionType.CircleCast)
            {
                result = Physics2D.CircleCast(node.Item.Position, radius, deltaPosition, ContactFilter, RaycastHitBuffer, distance);
            }

            if (result > 0)
            {
                // Put whatever hit code you want here such as damage events
                Player p = null;
                if((p = RaycastHitBuffer[0].transform.GetComponent<Player>()) != null)
                {
                    p.updateHp(-10);
                    ReturnNode(node);
                }

                // Collision was detected, should we bounce off or destroy the projectile?
                if (props.BounceOffSurfaces)
                {
                    /* projectile bouncing unsupported yet
                    // Calculate the position the projectile is bouncing off the wall at
                    Vector2 projectedNewPosition = node.Item.Position + (deltaPosition * RaycastHitBuffer[0].fraction);
                    Vector2 directionOfHitFromCenter = RaycastHitBuffer[0].point - projectedNewPosition;
                    float distanceToContact = (RaycastHitBuffer[0].point - projectedNewPosition).magnitude;
                    float remainder = radius - distanceToContact;

                    // reposition projectile to the point of impact 
                    node.Item.Position = projectedNewPosition - (directionOfHitFromCenter.normalized * remainder);

                    // reflect the velocity for a bounce effect -- will work well on static surfaces
                    node.Item.Velocity = Vector2.Reflect(node.Item.Velocity, RaycastHitBuffer[0].normal);

                    // calculate remaining distance after bounce
                    deltaPosition = node.Item.Velocity * time * (1 - RaycastHitBuffer[0].fraction);

                    // When gravity is applied, the positional change here is actually parabolic
                    node.Item.Position += deltaPosition;

                    // Absorbs energy from bounce
                    node.Item.Velocity = new Vector2(node.Item.Velocity.x * (1 - props.BounceAbsorbtionX), node.Item.Velocity.y * (1 - props.BounceAbsorbtionY));

                    //handle outline
                    if (node.Item.Outline.Item != null)
                    {
                        node.Item.Outline.Item.Position = node.Item.Position;
                    }
                    */          
                }
                else
                {
                    ReturnNode(node);
                }
            }
            else
            {
                //no collision
                UpdateProjectileColor(ref node.Item);

                // Update outline position
                if (node.Item.Outline.Item != null)
                {
                    node.Item.Outline.Item.Position = node.Item.Position;
                }                   
            }
        }
        else
        {
            // End of life - return to pool
            ReturnNode(node);
        }
    }

    public float MinMaxArea(float selector, AnimationCurve[] curves, float t0, float t1)
    {
        float area;

        if (curves == null || curves.Length == 0 || Mathf.RoundToInt(selector) >= curves.Length)
        {
            area = AreaUnderCurve(defaultLinearCurve, t0, t1);
        }
        else
        {
            float floor = Mathf.FloorToInt(selector);
            float ceil = Mathf.CeilToInt(selector);
            float tolerance = 0.01f;

            if (Mathf.Abs(floor - ceil) < tolerance || Mathf.Abs(selector - floor) < tolerance)
            {
                area = AreaUnderCurve(curves[(int)floor], t0, t1);
            }
            else if (Mathf.Abs(selector - ceil) < 0.01f)
            {
                area = AreaUnderCurve(curves[(int)ceil], t0, t1);
            }
            else
            {
                float area1 = AreaUnderCurve(curves[(int)floor], t0, t1);
                float area2 = AreaUnderCurve(curves[(int)ceil], t0, t1);
                area = (area1 + area2) / 2.0f;
            }
        }
        return area;
    }

    public Pool<ProjectileData>.Node FireProjectile(EmitterProperties props, Vector2 direction, double spawnTime, float time)
    {
        Pool<ProjectileData>.Node node = new Pool<ProjectileData>.Node();
        this.props.Direction = direction;
        RefreshGroups();

        for (int g = 0; g < props.GroupCount; g++)
        {
            if (Projectiles.AvailableCount >= props.SpokeCount)
            {
                float rotation = 0;
                bool left = true;

                for (int n = 0; n < props.SpokeCount; n++)
                {
                    node = Projectiles.Get();

                    node.Item.Position = transform.position;
                    node.Item.InitialPosition = transform.position;
                    node.Item.Speed = props.Speed;
                    node.Item.Scale = props.Scale;
                    node.Item.TimeToLive = props.TimeToLive;
                    node.Item.Gravity = props.Gravity;
                    if (props.UseFollowTarget && props.FollowTargetType == FollowTargetType.LockOnShot && props.Target != null)
                    {
                        Groups[g].Direction = (props.Target.transform.position - transform.position).normalized;
                    }
                    node.Item.Color = props.Color.Evaluate(0);
                    node.Item.Acceleration = props.Acceleration;
                    node.Item.FollowTarget = props.UseFollowTarget;
                    node.Item.FollowIntensity = props.FollowIntensity;
                    node.Item.Target = props.Target;
                    node.Item.TimeSpawned = (float)spawnTime;
                    node.Item.emitterId = props.id;
                    node.Item.SpeedFloor = props.SpeedFloor;
                    node.Item.SpeedCeil = props.SpeedCeil;
                    node.Item.SpeedCurves = props.SpeedCurves;
                    node.Item.SpeedCurveSelector = props.speedCurveSelector;
                    node.Item.TurnFloor = props.TurnFloor;
                    node.Item.TurnCeil = props.TurnCeil;
                    node.Item.TurningCurves = props.TurningCurves;
                    node.Item.TurningCurvesSelector = props.TurningCurveSelector;

                    if (left)
                    {
                        node.Item.Velocity = props.Speed * Rotate(Groups[g].Direction, rotation).normalized;
                        rotation += props.SpokeSpacing;
                    }
                    else
                    {
                        node.Item.Velocity = props.Speed * Rotate(Groups[g].Direction, -rotation).normalized;
                    }

                    // Setup outline if we have one
                    if (props.ProjectilePrefab.Outline != null && props.DrawOutlines)
                    {
                        Pool<ProjectileData>.Node outlineNode = ProjectileOutlines.Get();

                        outlineNode.Item.Position = node.Item.Position;
                        outlineNode.Item.Scale = node.Item.Scale + props.OutlineSize;
                        outlineNode.Item.Color = props.OutlineColor.Evaluate(0);
                        
                        node.Item.Outline = outlineNode;
                    }

                    // Keep track of active projectiles                       
                    PreviousActiveProjectileIndexes[ActiveProjectileIndexesPosition] = node.NodeIndex;
                    ActiveProjectileIndexesPosition++;
                    if (ActiveProjectileIndexesPosition < ActiveProjectileIndexes.Length)
                    {
                        PreviousActiveProjectileIndexes[ActiveProjectileIndexesPosition] = -1;
                    }
                    else
                    {
                        Debug.Log("Error: Projectile was fired before list of active projectiles was refreshed.");
                    }

                    UpdateProjectile(ref node, time);

                    left = !left;
                }

                if (Groups[g].InvertRotation)
                    Groups[g].Direction = Rotate(Groups[g].Direction, -props.RotationSpeed);
                else
                    Groups[g].Direction = Rotate(Groups[g].Direction, props.RotationSpeed);
            }
        }
        return node;
    }

    protected override void UpdateBuffers(float time)
    {
        ActiveProjectileCount = 0;
        ActiveOutlineCount = 0;

        for (int i = 0; i < PreviousActiveProjectileIndexes.Length; i++)
        {
            // End of array is set to -1
            if (PreviousActiveProjectileIndexes[i] == -1)
                break;

            Pool<ProjectileData>.Node node = Projectiles.GetActive(PreviousActiveProjectileIndexes[i]);
            ProjectileManager.UpdateBufferData(props.ProjectilePrefab, node.Item);
            ActiveProjectileCount++;
        }

        // faster to do two loops than to update the outlines at the same time due to the renderer swapping
        for (int i = 0; i < PreviousActiveProjectileIndexes.Length; i++)
        {
            // End of array is set to -1
            if (PreviousActiveProjectileIndexes[i] == -1)
                break;

            Pool<ProjectileData>.Node node = Projectiles.GetActive(PreviousActiveProjectileIndexes[i]);

            //handle outline
            if (node.Item.Outline.Item != null)
            {
                ProjectileManager.UpdateBufferData(props.ProjectilePrefab.Outline, node.Item.Outline.Item);
                ActiveOutlineCount++;
            }
        }
    }

    public void PlayableUpdate(double t, double start, double end)
    {
        UpdateProjectiles((float)t);
        if(t < lastTime)
        {
            UpdateProjectiles((float)t);
        }
        UpdateBuffers(0);
        lastTime = (float)t;
    }

    private void Update() 
    {
        UpdateBuffers(0);
    }

    private int GetPropertiesIndex(double time)
    {
        time += 0.000000001;
        for(int i = 0; i < timelineProperties.Count; i++)
        {
            if(time >= timelineProperties[i].clipProperties.start && time < timelineProperties[i].clipProperties.end)
            {
                return i;
            }
        }
        return -1;
    }

    private void SetFieldSafe(FieldInfo field, object snapshotProps, float f)
    {
        System.Type type = field.FieldType;
        if(type == typeof(float))
        {
            field.SetValue(snapshotProps, f);
        }
        else if(type == typeof(double))
        {
            field.SetValue(snapshotProps, (double)f);
        }
        else if(type == typeof(int))
        {
            field.SetValue(snapshotProps, (int)f);
        }
        else if(type == typeof(bool))
        {
            field.SetValue(snapshotProps, (f != 0f));
        }
    }

    private bool IsProjectileAlive(ProjectileData projectile, float time)
    {
        return time > projectile.TimeSpawned && time < projectile.TimeSpawned + projectile.TimeToLive;
    }

    private bool IsProjectileAlive(EmitterProperties projectile, double spawnTime, float time)
    {
        return time > spawnTime && time < spawnTime + projectile.TimeToLive;
    }

    public float AreaUnderCurve(AnimationCurve curve, float t0 = 0.0f, float t1 = 1.0f, float w = 1.0f, float h = 1.0f)
    {
        //Optimization Todo:
        //1. Instead of making a list every frame, make it an array of Keyframes and precompute addition indices
        //2. Pre-compute the coeffecients of the base curve once, rather than doing it every frame (though the added point will need to be calculated at runtime still).
        float areaUnderCurve = 0f;
        var keys = curve.keys;
        List<Keyframe> keyList = new List<Keyframe>(keys);

        bool t0Region;
        bool t1Region;
        int startIndex = -1;
        int endIndex = -1;

        float oneOverThree = 1f / 3f;
        float two = 2.0f;
        float three = 3.0f;
        float four = 4.0f;
        float six = 6.0f;

        //System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
        //timer.Start();
        for (int i = 0; i < keys.Length - 1; i++)
        {
            Keyframe K1 = keys[i];
            Keyframe K2 = keys[i + 1];
            if (t0 == K1.time)
                startIndex = i;
            if (t1 == K2.time)
                endIndex = i + 1;

            t0Region = t0 > K1.time && t0 < K2.time;
            t1Region = t1 > K1.time && t1 < K2.time;

            if (t0Region || t1Region)
            {
                Vector2 A = new Vector2(K1.time * w, K1.value * h);
                Vector2 D = new Vector2(K2.time * w, K2.value * h);
                float e = (D.x - A.x) / three;
                float f = h / w;
                Vector2 B = A + new Vector2(e, e * f * K1.outTangent);
                Vector2 C = D + new Vector2(-e, -e * f * K2.inTangent);

                float a, b, c;
                a = -A.y + three * B.y - three * C.y + D.y;
                b = three * A.y - six * B.y + three * C.y;
                c = -three * A.y + three * B.y;

                bool sameRegion = false;

                if(t0Region)
                {
                    startIndex = i + 1;
                    float slope = (three * a * t0 * t0) + (two * b * t0) + c;
                    keyList.Insert(startIndex, new Keyframe(t0, curve.Evaluate(t0), slope, slope, oneOverThree, oneOverThree));
                    sameRegion = true;
                }
                if (t1Region)
                {
                    endIndex = (sameRegion) ? i + 2 : i + 1;
                    float slope = (three * a * t1 * t1) + (two * b * t1) + c;
                    keyList.Insert(endIndex, new Keyframe(t1, curve.Evaluate(t1), slope, slope, oneOverThree, oneOverThree));
                }
            }
        }
        //timer.Stop();
        //System.TimeSpan timeToAddKeys = timer.Elapsed;

        if(startIndex == -1 || endIndex == -1)
        {
            Debug.LogWarning("When evaluating area under curve the start or end index was not set.\n" + 
                $"t0: {t0}, startIndex: {startIndex}, t1:{t1}, endIndex: {endIndex}");
        }
           
        //timer.Reset();
        //timer.Start();
        for (int i = startIndex; i < endIndex && i < keyList.Count - 1; i++)
        {
            // Calculate the 4 cubic Bezier control points from Unity AnimationCurve (a hermite cubic spline) 
            Keyframe K1 = keyList[i];
            Keyframe K2 = keyList[i + 1];
            Vector2 A = new Vector2(K1.time * w, K1.value * h);
            Vector2 D = new Vector2(K2.time * w, K2.value * h);
            float e = (D.x - A.x) / three;
            float f = h / w;
            Vector2 B = A + new Vector2(e, e * f * K1.outTangent);
            Vector2 C = D + new Vector2(-e, -e * f * K2.inTangent);

            /*
             * The cubic Bezier curve function looks like this:
             * 
             * f(x) = A(1 - x)^3 + 3B(1 - x)^2 x + 3C(1 - x) x^2 + Dx^3
             * 
             * Where A, B, C and D are the control points and, 
             * for the purpose of evaluating an instance of the Bezier curve, 
             * are constants. 
             * 
             * Multiplying everything out and collecting terms yields the expanded polynomial form:
             * f(x) = (-A + 3B -3C + D)x^3 + (3A - 6B + 3C)x^2 + (-3A + 3B)x + A
             * 
             * If we say:
             * a = -A + 3B - 3C + D
             * b = 3A - 6B + 3C
             * c = -3A + 3B
             * d = A
             * 
             * Then we have the expanded polynomal:
             * f(x) = ax^3 + bx^2 + cx + d
             * 
             * Whos indefinite integral is:
             * a/4 x^4 + b/3 x^3 + c/2 x^2 + dx + E
             * Where E is a new constant introduced by integration.
             * 
             * The indefinite integral of the quadratic Bezier curve is:
             * (-A + 3B - 3C + D)/4 x^4 + (A - 2B + C) x^3 + 3/2 (B - A) x^2 + Ax + E
             */

            float a, b, c, d;
            a = -A.y + three * B.y - three * C.y + D.y;
            b = three * A.y - six * B.y + three * C.y;
            c = -three * A.y + three * B.y;
            d = A.y;

            /* 
             * a, b, c, d, now contain the y component from the Bezier control points.
             * In other words - the AnimationCurve Keyframe value * h data!
             * 
             * What about the x component for the Bezier control points - the AnimationCurve
             * time data?  We will need to evaluate the x component when time = 1.
             * 
             * x^4, x^3, X^2, X all equal 1, so we can conveniently drop this coeffiecient.
             * 
             * Lastly, for each segment on the AnimationCurve we get the time difference of the 
             * Keyframes and multiply by w.
             * 
             * Iterate through the segments and add up all the areas for 
             * the total area under the AnimationCurve!
             */

            float t = (K2.time - K1.time) * w;

            // Define A and D...

            float area;  // just renamed area since it also works for negative values

            // If this portion of the curve is constant (i.e. either this key has a right tangent constant,
            // or the next key has a left tangent constant), compute the integral directly as the signed area of a rectangle
            if (float.IsInfinity(K1.outTangent) || float.IsInfinity(K2.inTangent))
            {
                area = A.y * (D.x - A.x);
            }
            else
            {
                // computation for non-constant tangents...
                area = ((a / four) + (b / three) + (c / two) + d) * t;
            }
            areaUnderCurve += area;
        }
        //timer.Stop();
        //System.TimeSpan timeToCompute = timer.Elapsed;

        //timer.Reset();
        //timer.Start();
        //timer.Stop();
        //System.TimeSpan timeToRemove = timer.Elapsed;

        //Debug.Log($"Time to add keys: {timeToAddKeys.TotalMilliseconds} ms.");
        //Debug.Log($"Time to compute: {timeToCompute.TotalMilliseconds} ms.");
        //Debug.Log($"Time to remove keys: {timeToRemove.TotalMilliseconds} ms.");
        return areaUnderCurve;
    }
}
