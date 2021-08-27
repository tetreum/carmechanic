using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public Camera fpCamera;
    public Camera tpCamera;

    public void Start()
    {
        fpCamera.enabled = true;
        Player.playerCamera = fpCamera;
        tpCamera.enabled = false;
    }

    private void Update()
    {
        if (fpCamera.enabled)
        {
            Player.playerCamera = fpCamera;
        }else
        {
            Player.playerCamera = tpCamera;
        }
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