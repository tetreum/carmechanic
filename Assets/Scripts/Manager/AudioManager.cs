using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

public class AudioManager : MonoBehaviour
{
    public static EventInstance footsteps;
    
    void Awake()
    {
        footsteps = RuntimeManager.CreateInstance("event:/footsteps");
    }
    
    void Update()
    {
        
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.A) ||
            Input.GetKeyDown(KeyCode.D))
        {
            footsteps.start();
            RuntimeManager.AttachInstanceToGameObject(footsteps, transform);
        }

        if (Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.A) ||
            Input.GetKeyUp(KeyCode.D)) footsteps.stop(STOP_MODE.ALLOWFADEOUT);
    }
}
