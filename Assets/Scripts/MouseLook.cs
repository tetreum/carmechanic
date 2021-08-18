using UnityEngine;

public class MouseLook : MonoBehaviour
{
    float yRotation;
    float xRotation;
    float lookSensitivity = 5;
    float currentXRotation;
    float currentYRotation;
    float yRotationV;
    float xRotationV;
    float lookSmoothnes = 0.1f; 
 
    void Update ()
    {
        yRotation += Input.GetAxis("Mouse X") * lookSensitivity;
        xRotation -= Input.GetAxis("Mouse Y") * lookSensitivity;
        xRotation = Mathf.Clamp(xRotation, -80, 100);
        currentXRotation = Mathf.SmoothDamp(currentXRotation, xRotation, ref xRotationV, lookSmoothnes);
        currentYRotation = Mathf.SmoothDamp(currentYRotation, yRotation, ref yRotationV, lookSmoothnes);
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
    }
}
