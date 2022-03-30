using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

public class FollowBezierCurve : MonoBehaviour, ITimeControl
{
    public BezierCurve path;
    public AnimationCurve motionCurve;
    public bool animated = false;
    public float timeToComplete = 5f;
    public float percentageDone = 0f;
    private float aliveTime;
    public bool loop;
    // Start is called before the first frame update
    void Start()
    {
        aliveTime = 0;
        if (timeToComplete <= 0)
        {
            Debug.Log("Set TimeToComplete to a positive value on GameObject: " + name);
            timeToComplete = 5f;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(!animated)
        {
            if(loop)
            {
                aliveTime = (aliveTime + Time.deltaTime) % timeToComplete;
            }
            else {
                aliveTime = Time.deltaTime;
            }

            if(aliveTime < timeToComplete)
            {
                percentageDone = aliveTime/timeToComplete;
            }
            else {
                percentageDone = 1f;
            }
        }
        
        float curveTime = motionCurve.Evaluate(percentageDone);
        this.transform.position = path.GetPointAt(curveTime);
    }

    public void OnControlTimeStart(){
        aliveTime = 0;
        this.gameObject.SetActive(true);
    }

    public void OnControlTimeStop() {

    }

    public void SetTime(double time) {
        percentageDone = (float)time/timeToComplete;
        float curveTime = motionCurve.Evaluate(percentageDone);
        this.transform.position = path.GetPointAt(curveTime);
    }
}
