using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BulletHell;

public class PlayableEmitter : ProjectileEmitterAdvanced
{
    public void PlayableUpdate(double t, double start, double end)
    {
        Debug.Log($"t: {t}, start: {start}, end: {end}");
    }
}
