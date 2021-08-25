using System.Collections;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Lift : MonoBehaviour
{
    [SerializeField] public Transform arms;
    public float speed = 0.01f;
    public float maxHeight = 1.22f;
    public float minHeight = 0;
    private bool isElevated;
    private bool isMoving;
    private Material mat;
    private GameObject carobj;
    private EventInstance liftInstance;

    public void Start()
    {
        liftInstance = RuntimeManager.CreateInstance("event:/HydraulicLift");
    }

    private void OnMouseDown()
    {
        if (isMoving) return;
        liftInstance.start();
        isMoving = true;
        StartCoroutine(nameof(StartAscension));
    }

    private void OnDestroy()
    {
        liftInstance.release();
    }
    
    private IEnumerator StartAscension()
    {
        var armsPos = arms.position;

        if (isElevated){
            armsPos.y = minHeight;
        }else{
            armsPos.y = maxHeight;
        }
        while (AudioUtils.IsPlaying(liftInstance))
        {
            arms.position = Vector3.Lerp(arms.position, armsPos, speed);
            yield return new WaitForSeconds(0.03f);
        }

        isElevated = !isElevated;
        isMoving = false;
    }
}