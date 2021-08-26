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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (fpCamera.enabled)
                ThirdPersonCamera();
            else
                FirstPersonCamera();
        }
    }

    private void FirstPersonCamera()
    {
        fpCamera.enabled = true;
        tpCamera.enabled = false;
    }

    private void ThirdPersonCamera()
    {
        fpCamera.enabled = false;
        tpCamera.enabled = true;
    }
}