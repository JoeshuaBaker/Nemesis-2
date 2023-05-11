[System.Serializable]
public class Keyframe1D
{
    public float a;
    public float b;
    public float c;
    public float d;
    public UnityEngine.Vector2 range;
    public float width { 
        get
        {
            return range.y - range.x;
        }
    }

    public float GetControlPosition(int id)
    {
        if (id <= 0 || id > 2)
            return -1;

        if(id == 1)
        {
            return range.x + (range.y - range.x) / 3f;
        }
        else
        {
            return range.x + 2 * (range.y - range.x) / 3f;
        }
    }

    public Keyframe1D(float a, float b, float c, float d, UnityEngine.Vector2 range)
    {
        this.a = a;
        this.b = b;
        this.c = c;
        this.d = d;
        this.range = range;
    }
}
