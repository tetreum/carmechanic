using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public Camera fpCamera;
    public Camera tpCamera;

    public void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        fpCamera.enabled = true;
        Movement.playerCamera = fpCamera;
        tpCamera.enabled = false;
    }

    private void Update()
    {
        if (fpCamera.enabled)
            Movement.playerCamera = fpCamera;
        else
            Movement.playerCamera = tpCamera;
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