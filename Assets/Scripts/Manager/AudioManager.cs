using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

public class AudioManager : MonoBehaviour
{
    public static EventInstance Footsteps;
    public static EventInstance Screw;
    public static EventInstance OpenCase;
    private string FootstepsPath;
    public static string ScrewPath;
    public static string OpenCasePath;

    private void Awake()
    {
        FootstepsPath = "event:/footsteps";
        ScrewPath = "event:/Screw";
        OpenCasePath = "event:/OpenCase";
        
        Footsteps = RuntimeManager.CreateInstance(FootstepsPath);
        Screw = RuntimeManager.CreateInstance(ScrewPath);
        OpenCase = RuntimeManager.CreateInstance(OpenCasePath);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.A) ||
            Input.GetKeyDown(KeyCode.D))
        {
            Footsteps.start();
            RuntimeManager.AttachInstanceToGameObject(Footsteps, transform);
        }

        if (Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.A) ||
            Input.GetKeyUp(KeyCode.D)) Footsteps.stop(STOP_MODE.ALLOWFADEOUT);
    }

    private void OnDestroy()
    {
        Footsteps.release();
        Screw.release();
        OpenCase.release();
    }
}