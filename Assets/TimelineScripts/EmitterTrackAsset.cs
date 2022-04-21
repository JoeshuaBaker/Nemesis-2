using UnityEngine.Playables;

public abstract class EmitterTrackAsset : PlayableAsset
{
    public virtual EmitterTrackBehaviour behaviour {get; set;}
}