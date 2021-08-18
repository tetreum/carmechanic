using UnityEngine;

public class MouseLook : MonoBehaviour
{
    private float currentXRotation;
    private float currentYRotation;
    private readonly float lookSensitivity = 5;
    private readonly float lookSmoothnes = 0.1f;
    private float xRotation;
    private float xRotationV;
    private float yRotation;
    private float yRotationV;

    private void Update()
    {
        yRotation += Input.GetAxis("Mouse X") * lookSensitivity;
        xRotation -= Input.GetAxis("Mouse Y") * lookSensitivity;
        xRotation = Mathf.Clamp(xRotation, -80, 100);
        currentXRotation = Mathf.SmoothDamp(currentXRotation, xRotation, ref xRotationV, lookSmoothnes);
        currentYRotation = Mathf.SmoothDamp(currentYRotation, yRotation, ref yRotationV, lookSmoothnes);
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
    }
}