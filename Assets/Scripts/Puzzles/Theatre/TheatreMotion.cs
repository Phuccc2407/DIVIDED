using System;
using UnityEngine;

[Serializable]
public class TheatreMotion
{
    public Transform moving;
    public Transform hiddenPose;
    public Transform shownPose;
    public bool IsValid => moving != null && hiddenPose != null && shownPose != null;

    public void Apply(float amount)
    {
        if (!IsValid) return;
        moving.position = Vector3.Lerp(hiddenPose.position, shownPose.position, amount);
        moving.rotation = Quaternion.Slerp(hiddenPose.rotation, shownPose.rotation, amount);
    }
}
