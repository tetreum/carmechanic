using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class Player : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    public static Camera playerCamera;
    public static bool canMove = true;
    [SerializeField]private CharacterController controller;
    private float rotationX;
    private Vector2 inputMovement;
    public PlayerInput PlayerInput => playerInput;
    [SerializeField] private float moveSpeed;
    private InputAction movement;
    
    private void Start()
    {
        Cursor.visible = false;
        moveSpeed = 0.5f;
    }
    
    private void Update()
    {
        Movement();
    }

    [BurstCompile]
    private void Movement()
    {
        Vector3 movement = moveSpeed * (
            inputMovement.x * Vector3.forward + inputMovement.y * Vector3.left
        );
        transform.position += movement;
        inputMovement = Vector2.zero;
    }
    
    public void Move(InputAction.CallbackContext ctx)
    {
        inputMovement += ctx.ReadValue<Vector2>();
    }
}