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

    private void OnDestroy()
    {
        footsteps.release();
    }
}