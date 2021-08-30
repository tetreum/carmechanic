using FMODUnity;
using UnityEngine;
using UnityEngine.InputSystem;
using STOP_MODE = FMOD.Studio.STOP_MODE;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class Player : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput = null;
    public float walkingSpeed = 7.5f;
    public float runningSpeed = 11.5f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;
    public static Camera playerCamera;
    public float lookSpeed = 2.0f;
    public float lookXLimit = 45.0f;

    public static bool canMove = true;

    private CharacterController controller;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX;
    private Vector3 inputMovement;
    public PlayerInput PlayerInput => playerInput;
    [SerializeField] private float movementSpeed = 5f;
    public float mouseSensitivity = 100.0f;
    public float clampAngle = 80.0f;
 
    private float rotY = 0.0f; 
    private float rotX = 0.0f;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.visible = false;
        Vector3 rot = transform.localRotation.eulerAngles;
        rotY = rot.y;
        rotX = rot.x;
    }

    private void Update()
    {
        var finalMovement = inputMovement;
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = -Input.GetAxis("Mouse Y");

        finalMovement *= movementSpeed * Time.deltaTime;

        controller.Move(finalMovement);

        Vector3 velocity = controller.velocity;
        velocity.y = 0f;

        rotY += mouseX * mouseSensitivity * Time.deltaTime;
        rotX += mouseY * mouseSensitivity * Time.deltaTime;
        rotX = Mathf.Clamp(rotX, -clampAngle, clampAngle);
 
        Quaternion localRotation = Quaternion.Euler(rotX, rotY, 0.0f);
        transform.rotation = localRotation;
    }
    
    public void Move(InputAction.CallbackContext ctx)
    {
        var inputValue = ctx.ReadValue<Vector2>();
        inputMovement = new Vector3(inputValue.x, 0f, inputValue.y);
    }
}