using System.Collections.Generic;
using UnityEngine;

public class EditorTriggerController : MonoBehaviour
{
    public static EditorTriggerController Instance;
    public List<Collider> triggers;
    public new GameObject camera;

    // Use this for initialization
    private void Start()
    {
        Instance = this;

        if (camera == null)
        {
            camera = GameObject.Find("EngineCamera");

            if (camera == null) camera = Resources.Load("Prefabs/EngineCamera") as GameObject;
        }
    }

    // Init Engine Editor mode camera
    // @ToDo: Move this
    public void init(Vector3 pos, Quaternion rot)
    {
        camera.transform.position = pos;
        camera.transform.rotation = rot;
        camera.SetActive(true);
    }

    public void enableTriggers(bool action)
    {
        foreach (var trigger in triggers) trigger.gameObject.SetActive(action);
    }
}