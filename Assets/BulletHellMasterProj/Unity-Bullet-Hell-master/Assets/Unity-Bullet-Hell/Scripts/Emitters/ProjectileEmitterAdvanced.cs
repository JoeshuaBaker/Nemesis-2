using UnityEngine;

namespace BulletHell
{
    public enum FollowTargetType
    {
        Homing,
        LockOnShot
    };

    public class ProjectileEmitterAdvanced : ProjectileEmitterBase
    {
        ColorPulse StaticOutlinePulse;
        ColorPulse StaticPulse;

        protected EmitterGroup[] Groups;
        protected int LastGroupCountPoll = -1;
        protected bool PreviousMirrorPairRotation = false;
        protected bool PreviousPairGroupDirection = false;

        public new void Awake()
        {
            base.Awake();

            Groups = new EmitterGroup[10];
            RefreshGroups();
        }

        void Start()
        {
            // To allow for the enable / disable checkbox in Inspector
        }

        protected void RefreshGroups()
        {
            if (props.GroupCount > 10)
            {
                Debug.Log("Max Group Count is set to 10.  You attempted to set it to " + props.GroupCount.ToString() + ".");
                return;
            }

            bool mirror = false;
            if (Groups == null || LastGroupCountPoll != props.GroupCount || PreviousMirrorPairRotation != props.MirrorPairRotation || PreviousPairGroupDirection != props.PairGroupDirection)
            {               
                // Refresh the groups, they were changed
                float rotation = 0;
                for (int n = 0; n < Groups.Length; n++)
                {
                    if (n < props.GroupCount && Groups[n] == null)
                    {
                        Groups[n] = new EmitterGroup(Rotate(props.Direction, rotation).normalized, props.SpokeCount, props.SpokeSpacing, mirror);
                    }
                    else if (n < props.GroupCount)
                    {
                        Groups[n].Set(Rotate(props.Direction, rotation).normalized, props.SpokeCount, props.SpokeSpacing, mirror);
                    }
                    else
                    {
                        //n is greater than GroupCount -- ensure we clear the rest of the buffer
                        Groups[n] = null;
                    }

                    // invert the mirror flag if needed
                    if (props.MirrorPairRotation)
                        mirror = !mirror;

                    // sets the starting direction of all the groups so we divide by 360 to evenly distribute their direction
                    // Could reduce the scope of the directions here
                    rotation = CalculateGroupRotation(n, rotation);
                }
                LastGroupCountPoll = props.GroupCount;
                PreviousMirrorPairRotation = props.MirrorPairRotation;
                PreviousPairGroupDirection = props.PairGroupDirection;
            }
            else if (props.RotationSpeed == 0)
            {
                float rotation = 0;
                // If rotation speed is locked, then allow to update Direction of groups
                for (int n = 0; n < Groups.Length; n++)
                {
                    if (Groups[n] != null)
                    {
                        Groups[n].Direction = Rotate(props.Direction, rotation).normalized;
                    }

                    rotation = CalculateGroupRotation(n, rotation);
                }
            }
        }

        public override Pool<ProjectileData>.Node FireProjectile(Vector2 direction, float leakedTime)
        {
            Pool<ProjectileData>.Node node = new Pool<ProjectileData>.Node();

            props.Direction = direction;
            RefreshGroups();

            if (!props.AutoFire)
            {
                if (Interval > 0) return node;
                else Interval = props.CoolOffTime;
            }

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

                        UpdateProjectile(ref node, leakedTime);

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

        public void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, props.Scale);

            Gizmos.color = UnityEngine.Color.yellow;

            float rotation = 0;

            for (int n = 0; n < props.GroupCount; n++)
            {
                Vector2 direction = Rotate(props.Direction, rotation).normalized * (props.Scale + 0.2f);
                Gizmos.DrawLine(transform.position, new Vector2(transform.position.x, transform.position.y) + direction);

                rotation = CalculateGroupRotation(n, rotation);
            }

            Gizmos.color = UnityEngine.Color.red;
            rotation = 0;
            float spokeRotation = 0;
            bool left = true;
            for (int n = 0; n < props.GroupCount; n++)
            {
                Vector2 groupDirection = Rotate(props.Direction, rotation).normalized;
                spokeRotation = 0;
                left = true;

                for (int m = 0; m < props.SpokeCount; m++)
                {
                    Vector2 direction = Vector2.zero;
                    if (left)
                    {
                        direction = Rotate(groupDirection, spokeRotation).normalized * (props.Scale + 0.15f);
                        spokeRotation += props.SpokeSpacing;
                    }
                    else
                    {
                        direction = Rotate(groupDirection, -spokeRotation).normalized * (props.Scale + 0.15f);
                    }
                    Gizmos.DrawLine(transform.position, new Vector2(transform.position.x, transform.position.y) + direction);

                    left = !left;
                }

                rotation = CalculateGroupRotation(n, rotation);
            }
        }

        private float CalculateGroupRotation(int index, float currentRotation)
        {
            if (props.PairGroupDirection)
            {
                if (index % 2 == 1)
                    currentRotation += 360 * props.GroupSpacing * 2f / props.GroupCount;
            }
            else
            {
                currentRotation += 360 * props.GroupSpacing / props.GroupCount;
            }
            return currentRotation;
        }

        protected override void UpdateProjectiles(float tick)
        {
            UpdateStaticPulses(tick);
            base.UpdateProjectiles(tick);
        }

        protected override void UpdateProjectile(ref Pool<ProjectileData>.Node node, float tick)
        {          
            if (node.Active)
            {
                node.Item.TimeToLive -= tick;
                               
                // Projectile is active
                if (node.Item.TimeToLive > 0)
                {
                    UpdateProjectileNodePulse(tick, ref node.Item);

                    // apply acceleration
                    node.Item.Velocity *= (1 + node.Item.Acceleration * tick);

                    // follow target
                    if (props.FollowTargetType == FollowTargetType.Homing && node.Item.FollowTarget && node.Item.Target != null)
                    {
                        node.Item.Speed += props.Acceleration * tick;
                        node.Item.Speed = Mathf.Clamp(node.Item.Speed, -props.MaxSpeed, props.MaxSpeed);

                        Vector2 desiredVelocity = (new Vector2(props.Target.transform.position.x, props.Target.transform.position.y) - node.Item.Position).normalized;
                        desiredVelocity *= node.Item.Speed;

                        Vector2 steer = desiredVelocity - node.Item.Velocity;
                        node.Item.Velocity = Vector2.ClampMagnitude(node.Item.Velocity + steer * node.Item.FollowIntensity * tick, node.Item.Speed);
                    }
                    else
                    {
                        // apply gravity
                        node.Item.Velocity += node.Item.Gravity * tick;
                    }

                    // calculate where projectile will be at the end of this frame
                    Vector2 deltaPosition = node.Item.Velocity * tick;
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
                            deltaPosition = node.Item.Velocity * tick * (1 - RaycastHitBuffer[0].fraction);

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
        }

        protected void UpdateProjectileNodePulse(float tick, ref ProjectileData data)
        {
            if (props.UseColorPulse && !props.UseStaticPulse)
            {
                data.Pulse.Update(tick, props.PulseSpeed);
            }

            if (props.UseOutlineColorPulse && !props.UseOutlineStaticPulse)
            {
                data.OutlinePulse.Update(tick, props.OutlinePulseSpeed);
            }
        }

        private void UpdateStaticPulses(float tick)
        {
            //projectile pulse
            if (props.UseColorPulse && props.UseStaticPulse)
            {
                StaticPulse.Update(tick, props.PulseSpeed);
            }

            //outline pulse
            if (props.UseOutlineColorPulse && props.UseOutlineStaticPulse)
            {
                StaticOutlinePulse.Update(tick, props.OutlinePulseSpeed);
            }
        }

        protected override void UpdateProjectileColor(ref ProjectileData data)
        {
            // foreground
            if (props.UseColorPulse)
            {
                if (props.UseStaticPulse)
                {
                    data.Color = props.Color.Evaluate(StaticPulse.Fraction);
                }
                else
                {
                    data.Color = props.Color.Evaluate(data.Pulse.Fraction);
                }
            }
            else
            {
                data.Color = props.Color.Evaluate(1 - data.TimeToLive / props.TimeToLive);
            }

            //outline
            if (data.Outline.Item != null)
            {
                if (props.UseOutlineColorPulse)
                {
                    if (props.UseOutlineStaticPulse)
                    {
                        data.Outline.Item.Color = props.OutlineColor.Evaluate(StaticOutlinePulse.Fraction);
                    }
                    else
                    {
                        data.Outline.Item.Color = props.OutlineColor.Evaluate(data.OutlinePulse.Fraction);
                    }
                }
                else
                {
                    data.Outline.Item.Color = props.OutlineColor.Evaluate(1 - data.TimeToLive / props.TimeToLive);
                }
            }
        }

    }
}