using UnityEngine;

public class MouseOrbit : MonoBehaviour
{
    public static MouseOrbit Instance;
    public Transform target; // Inspector> Assign the LookAt Camera Target Object
    public float distance = 10.0f; // distance of the camera from the Target Object

    public float xSpeed = 250.0f; // Speed of x rotation
    public float ySpeed = 120.0f; // Speed of y rotation
    private readonly int yMaxLimit = 80; // y maximum rotation limit

    private readonly int yMinLimit = -20; // y minimum rotation limit
    private float oldScrollValue;

    private float smooth;

    private float x;
    private float y;

    private void Start()
    {
        Instance = this;
        var angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        // Make the rigid body not change rotation
        //if (rigidbody)
        //	rigidbody.freezeRotation = true;
    }

    private void LateUpdate()
    {
        if (!target) return;

        if (Input.GetMouseButton(1))
        {
            x += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
            y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
        }

        y = ClampAngle(y, yMinLimit, yMaxLimit);

        var rotation = Quaternion.Euler(y, x, 0);
        var position = rotation * new Vector3(0.0f, 0.5f, -distance) + target.position;

        transform.rotation = rotation;
        transform.position = position;

        if (Input.GetAxis("Mouse ScrollWheel") != oldScrollValue)
        {
            oldScrollValue = Input.GetAxis("Mouse ScrollWheel");
            smooth += -Input.GetAxis("Mouse ScrollWheel");
        }

        distance += smooth;
        if (distance < 1) // la Camera non si avvicina più di 1 unità
            distance = 1;
        if (distance > 6) // la Camera non si allontana più di 6 unità
            distance = 6;
        if (smooth != 0)
            smooth /= 1.2f;
    }

    public float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360)
            angle += 360;
        if (angle > 360)
            angle -= 360;
        return Mathf.Clamp(angle, min, max);
    }
}