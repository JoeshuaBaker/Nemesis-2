﻿using UnityEngine;

namespace BulletHell
{   
    public class ProjectileData
    {
        public int emitterId;
        public Vector2 Velocity;
        public float Acceleration;
        public Vector2 Gravity;
        public Vector2 InitialPosition;
        public Vector2 Position;
        public float Rotation;
        public Color Color;
        public float Scale;
        public float TimeSpawned;
        public float TimeToLive;
        public float Speed;
        public float SpeedFloor;
        public float SpeedCeil;
        public AnimationCurve[] SpeedCurves;
        public float SpeedCurveSelector;
        public float TurnFloor;
        public float TurnCeil;
        public AnimationCurve[] TurningCurves;
        public float TurningCurvesSelector;

        public ColorPulse Pulse;
        public ColorPulse OutlinePulse;

        public Transform Target;
        public bool FollowTarget;
        public float FollowIntensity;

        // Stores the pooled node that is used to draw the shadow for this projectile
        public Pool<ProjectileData>.Node Outline;
    }
}