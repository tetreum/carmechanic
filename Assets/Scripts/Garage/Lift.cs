using System;
using System.Collections;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Lift : MonoBehaviour
{
    [SerializeField] public Transform arms;
    public float speed = 0.01f;
    public float maxHeight = 1.3f;
    public float minHeight;
    private GameObject carobj;
    private bool isElevated;
    private bool isMoving;
    private Material mat;

    private void OnMouseDown()
    {
        if (isMoving) return;
        isMoving = true;
        AudioManager.liftInstance.start();
        StartCoroutine(nameof(StartAscension));
    }

    private void OnDestroy()
    {
        AudioManager.liftInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
    }

    private IEnumerator StartAscension()
    {
        var armsPos = arms.position;

        if (isElevated)
            armsPos.y = minHeight;
        else
            armsPos.y = maxHeight;
        while (AudioUtils.IsPlaying(AudioManager.liftInstance))
        {
            arms.position = Vector3.Lerp(arms.position, armsPos, speed);
            yield return new WaitForSeconds(0.03f);
        }

        isElevated = !isElevated;
        isMoving = false;
    }
}