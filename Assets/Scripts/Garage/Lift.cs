using System.Collections;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

public class Lift : MonoBehaviour
{
    [SerializeField] public Transform arms;
    public float speed = 0.01f;
    public float maxHeight = 1.3f;
    public float minHeight;
    private GameObject carobj;
    private bool isElevated;
    private bool isMoving;
    private EventInstance liftInstance;
    private Material mat;

    private void Awake()
    {
        liftInstance = RuntimeManager.CreateInstance("event:/lift");
    }

    private void OnDestroy()
    {
        liftInstance.release();
    }

    private void OnMouseDown()
    {
        if (isMoving) return;
        isMoving = true;
        liftInstance.start();
        StartCoroutine(nameof(StartAscension));
    }

    private IEnumerator StartAscension()
    {
        var armsPos = arms.position;

        if (isElevated)
            armsPos.y = minHeight;
        else
            armsPos.y = maxHeight;
        while (AudioUtils.IsPlaying(liftInstance))
        {
            arms.position = Vector3.Lerp(arms.position, armsPos, speed);
            yield return new WaitForSeconds(0.03f);
        }

        isElevated = !isElevated;
        isMoving = false;
        liftInstance.stop(STOP_MODE.IMMEDIATE);
    }
}