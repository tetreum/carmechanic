using System;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static EventInstance footsteps;

    private void Awake()
    {
        footsteps = RuntimeManager.CreateInstance("event:/footsteps");
    }

    private void Update()
    {
        if (Player.canMove)
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.A) ||
                Input.GetKeyDown(KeyCode.D))
            {
                footsteps.start();
                RuntimeManager.AttachInstanceToGameObject(footsteps, transform);
            }

            if (Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.A) ||
                Input.GetKeyUp(KeyCode.D)) footsteps.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }
    }

    private void OnDestroy()
    {
        footsteps.release();
    }
}