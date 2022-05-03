using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BulletHell;
using UnityEditor;

public class PlayableEmitter : ProjectileEmitterAdvanced
{   
    public struct TimelineProperties
    {
        public EmitterProperties clipProperties;
        public EditorCurveBinding[] propertiesCurves;
        public AnimationClip animationClip;
    }

    public List<TimelineProperties> timelineProperties;
    public List<EmitterShootBehaviour> shootBehaviours;
    private EmitterProperties baseProps = null;
    private List<(double, EmitterProperties)> shootEvents;

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
            EmitterProperties prop = e.Item2;
            numBulletsNeeded += (prop.GroupCount * prop.SpokeCount);
        }
        base.Initialize(numBulletsNeeded);
    }

    public override void UpdateEmitter(float tick)
    {
        
    }

    public void Build()
    {
        //only build state in edit mode because unity's timeline curves are a fucking nightmare i guess
        if(EditorApplication.isPlaying)
            return;

        if(baseProps == null)
            baseProps = props;

        if(shootEvents == null)
            shootEvents = new List<(double, EmitterProperties)>();
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
                    shootEvents.Add((nextShot, snapshotProps));
                    nextShot += snapshotProps.CoolOffTime;
                }
                else
                {
                    TimelineProperties timelineProp = timelineProperties[propIndex];
                    EmitterProperties snapshotProps = timelineProp.clipProperties;

                    if(timelineProp.propertiesCurves != null && timelineProp.animationClip != null)
                    {
                        snapshotProps = snapshotProps.Copy();
                        EditorCurveBinding[] curveBindings = timelineProp.propertiesCurves;
                        AnimationClip animClip = timelineProp.animationClip;

                        foreach(EditorCurveBinding curve in curveBindings)
                        {
                            AnimationCurve animCurve = AnimationUtility.GetEditorCurve(animClip, curve);
                            float f = animCurve.Evaluate(Mathf.Max((float)(nextShot - snapshotProps.start), 0));
                            FieldInfo field = snapshotProps.GetType().GetField(curve.propertyName);
                            if(field != null)
                            {
                                SetFieldSafe(field, snapshotProps, f);
                            }
                        }
                    }
                    
                    snapshotProps.id = emitterId++;
                    shootEvents.Add((nextShot, snapshotProps));
                    nextShot += snapshotProps.CoolOffTime;
                    shots--;
                }
            }
        }
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

        foreach(var shot in shootEvents)
        {
            double spawnTime = shot.Item1;
            EmitterProperties props = shot.Item2;
            if(!activeProjectileIds.Contains(props.id) && IsProjectileAlive(props, spawnTime, time))
            {

            }
        }

        // Set end point of array so we know when to stop looping
        ActiveProjectileIndexes[ActiveProjectileIndexesPosition] = -1;

        // Overwrite old previous active projectile index array
        System.Array.Copy(ActiveProjectileIndexes, PreviousActiveProjectileIndexes, Mathf.Max(ActiveProjectileIndexesPosition, previousIndexCount));
    }

    //Copied from ProjectileEmitterAdvanced. Called from UpdateProjectiles. Rewrite to use time.
    protected override void UpdateProjectile(ref Pool<ProjectileData>.Node node, float time)
    {          
        // Projectile is active
        if (IsProjectileAlive(node.Item, time))
        {
            UpdateProjectileNodePulse(time, ref node.Item);

            // apply acceleration
            node.Item.Velocity *= (1 + node.Item.Acceleration * time);

            // follow target
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

            // calculate where projectile will be at the end of this frame
            Vector2 deltaPosition = node.Item.Velocity * time;
            float distance = deltaPosition.magnitude;

            // If flag set - return projectiles that are no longer in view 
            if (props.CullProjectilesOutsideCameraBounds)
            {
                Bounds bounds = new Bounds(node.Item.Position, new Vector3(node.Item.Scale, node.Item.Scale, node.Item.Scale));
                if (!GeometryUtility.TestPlanesAABB(Planes, bounds))
                {
                    ReturnNode(node);
                    return;
                }
            }

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
                }
                else
                {
                    ReturnNode(node);
                }
            }
            else
            {
                //No collision -move projectile
                node.Item.Position += deltaPosition;
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

    public Pool<ProjectileData>.Node FireProjectile(EmitterProperties props, Vector2 direction, double spawnTime)
    {
        Pool<ProjectileData>.Node node = new Pool<ProjectileData>.Node();

        props.Direction = direction;
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

                    //UpdateProjectile(ref node, 0);

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

    public void PlayableUpdate(double t, double start, double end)
    {
        UpdateProjectiles((float)t);
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

    private void SetFieldSafe(FieldInfo field, EmitterProperties snapshotProps, float f)
    {
        Type type = field.FieldType;
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
}
