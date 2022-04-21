using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BulletHell;
using Sirenix.OdinInspector;
using UnityEngine.Playables;

[System.Serializable]
public class EmitterProperties : EmitterTrackBehaviour
{
    public ProjectilePrefab ProjectilePrefab;
    
    //General
    [FoldoutGroup("General")] [Range(0.01f, 2f)] 
    public float Scale = 0.05f;
    [FoldoutGroup("General")] 
    public float TimeToLive = 5;
    [FoldoutGroup("General")] [Range(0.01f, 5f)] 
    public float CoolOffTime = 0.1f;
    [FoldoutGroup("General")] 
    public bool AutoFire = true;
    [FoldoutGroup("General")] 
    public Vector2 Direction = Vector2.up;        
    [FoldoutGroup("General")] [Range(0.001f, 10f)] 
    public float Speed = 1;
    [FoldoutGroup("General")] [Range(1f, 100f)] 
    public float MaxSpeed = 100;        
    [FoldoutGroup("General")] 
    public float RotationSpeed = 0;        
    [FoldoutGroup("General")] 
    public CollisionDetectionType CollisionDetection = CollisionDetectionType.CircleCast;
    [FoldoutGroup("General")] 
    public bool BounceOffSurfaces = true;        
    [FoldoutGroup("General")] 
    public bool CullProjectilesOutsideCameraBounds = true;
    [FoldoutGroup("General")] 
    public bool IsFixedTimestep = true;
    [FoldoutGroup("General")] [ConditionalField(nameof(IsFixedTimestep)), Range(0.01f, 0.02f)] 
    public float FixedTimestepRate = 0.01f;     
    
    //Outline 
    [FoldoutGroup("Outline")] 
    public bool DrawOutlines;
    [FoldoutGroup("Outline")] [ConditionalField(nameof(DrawOutlines)), Range(0.0f, 1f)] 
    public float OutlineSize;
    [FoldoutGroup("Outline")] [ConditionalField(nameof(DrawOutlines))] 
    public Gradient OutlineColor;
    [FoldoutGroup("Outline")] [SerializeField] 
    public bool UseOutlineColorPulse;
    [FoldoutGroup("Outline")] [ConditionalField(nameof(UseOutlineColorPulse)), SerializeField] 
    public float OutlinePulseSpeed;
    [FoldoutGroup("Outline")] [ConditionalField(nameof(UseOutlineColorPulse)), SerializeField] 
    public bool UseOutlineStaticPulse;
    
    //Modifiers
    [FoldoutGroup("Modifiers")] 
    public Vector2 Gravity = Vector2.zero;
    [FoldoutGroup("Modifiers")] [Range(0.0f, 1f)] 
    public float BounceAbsorbtionY;
    [FoldoutGroup("Modifiers")] [Range(0.0f, 1f)] 
    public float BounceAbsorbtionX;                  
    [FoldoutGroup("Modifiers")] [Range(-10f, 10f)] 
    public float Acceleration = 0;
    [FoldoutGroup("Modifiers")] [SerializeField] 
    public bool UseFollowTarget;       
    [FoldoutGroup("Modifiers")] [ConditionalField(nameof(UseFollowTarget))] 
    public Transform Target;
    [FoldoutGroup("Modifiers")] [ConditionalField(nameof(UseFollowTarget))] 
    public FollowTargetType FollowTargetType = FollowTargetType.Homing;
    [FoldoutGroup("Modifiers")] [ConditionalField(nameof(UseFollowTarget)), Range(0, 5)] 
    public float FollowIntensity;

    //Appearance
    [Foldout("Appearance", true)]
    [FoldoutGroup("Appearance")] 
    public Gradient Color;
    [FoldoutGroup("Appearance")] 
    public bool UseColorPulse;
    [FoldoutGroup("Appearance")] [ConditionalField(nameof(UseColorPulse)), SerializeField] 
    public float PulseSpeed;
    [FoldoutGroup("Appearance")] [ConditionalField(nameof(UseColorPulse)), SerializeField] 
    public bool UseStaticPulse;

    //Spokes
    [Foldout("Spokes", true)]
    [FoldoutGroup("Spokes")] [Range(1, 10), SerializeField] 
    public int GroupCount = 1;
    [FoldoutGroup("Spokes")] [Range(0, 1), SerializeField] 
    public float GroupSpacing = 1;
    [FoldoutGroup("Spokes")] [Range(1, 10), SerializeField] 
    public int SpokeCount = 3;
    [FoldoutGroup("Spokes")] [Range(0, 100), SerializeField] 
    public float SpokeSpacing = 25;
    [FoldoutGroup("Spokes")] [SerializeField] 
    public bool MirrorPairRotation;                                                     
    [FoldoutGroup("Spokes")] [ConditionalField(nameof(MirrorPairRotation)), SerializeField] 
    public bool PairGroupDirection;
    
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        (playerData as PlayableEmitter).props = this;
        base.ProcessFrame(playable, info, playerData);
    }

    public EmitterProperties Copy()
    {
        var copy = (EmitterProperties) this.MemberwiseClone();
        copy.Gravity = new Vector2(Gravity.x, Gravity.y);
        copy.Direction = new Vector2(Direction.x, Direction.y);
        return copy;
    }
    
}
