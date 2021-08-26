using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public Camera fpCamera;
    public Camera tpCamera;

    private void Start()
    {
        fpCamera.enabled = true;
        tpCamera.enabled = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (fpCamera.enabled == true)
            {
                ThirdPersonCamera();
            }else
            {
                FirstPersonCamera();
            }
        }
    }

    void FirstPersonCamera()
    {
        fpCamera.enabled = true;
        tpCamera.enabled = false;
    }

    void ThirdPersonCamera()
    {
        fpCamera.enabled = false;
        tpCamera.enabled = true;
    }
}
