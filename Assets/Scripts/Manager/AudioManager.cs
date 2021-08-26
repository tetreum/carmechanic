using System;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static EventInstance liftInstance;
    public static EventInstance footsteps;
    
    private void Awake()
    {
        footsteps = RuntimeManager.CreateInstance("event:/footsteps");
        liftInstance = RuntimeManager.CreateInstance("event:/HydraulicLift");
    }

    private void OnDestroy()
    {
        footsteps.release();
        liftInstance.release();
    }
}
