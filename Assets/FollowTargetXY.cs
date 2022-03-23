using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowTargetXY : MonoBehaviour
{
    public Transform target;
    //public float lerpSpeed = 50f;
    public float speedMultiplier = 1f;
    public Vector2 tolerance = new Vector2(0.25f, 0.25f);

    // Update is called once per frame
    void Update()
    {
        Vector3 camPos = this.transform.position;
        Vector3 tarPos = target.transform.position; 
        if (Mathf.Abs(camPos.x - tarPos.x) > tolerance.x || Mathf.Abs(camPos.y - tarPos.y) > tolerance.y)
        {
            float x = Mathf.Lerp(camPos.x, tarPos.x, Time.deltaTime*speedMultiplier);
            float y = Mathf.Lerp(camPos.y, tarPos.y, Time.deltaTime*speedMultiplier);
            this.transform.position = new Vector3(x, y, camPos.z);
        }
    }
}
