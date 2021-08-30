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
    [SerializeField] private float movementSpeed = 5f;
    private InputAction movement;
    
    [BurstCompile]
    private void Start()
    {
        Cursor.visible = false;
    }
    
    [BurstCompile]
    private void Update()
    { 
        Vector3 movement = movementSpeed * (
            inputMovement.x * Vector3.forward + inputMovement.y * Vector3.right
        );
        transform.position += movement;
        inputMovement = Vector2.zero;
    }
    
    [BurstCompile]
    public void Move(InputAction.CallbackContext ctx)
    {
        inputMovement += ctx.ReadValue<Vector2>();
    }
}