using UnityEngine.Playables;

[System.Serializable]
public abstract class EmitterTrackBehaviour : PlayableBehaviour
{
    public double start;
    public double end;
    public int id;
}
