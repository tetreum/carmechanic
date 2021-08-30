using System;
using System.Collections;
using UnityEditor.Searcher;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(CharacterController))]
public class Movement : MonoBehaviour
{
    public static Camera playerCamera;
    [SerializeField] private CharacterController controller;
    public float moveSpeed;
    public float rotateSpeed;
    private bool m_Charging;
    private Vector2 m_Rotation;
    private Vector2 m_Look;
    private Vector2 m_Move;

    public void OnMove(InputAction.CallbackContext context)
    {
        m_Move = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        m_Look = context.ReadValue<Vector2>();
    }
    
    public void Update()
    {
        Look(m_Look);
        Move(m_Move);
    }

    private void Move(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.01)
            return;
        var scaledMoveSpeed = moveSpeed * Time.deltaTime;

        var move = Quaternion.Euler(0, transform.eulerAngles.y, 0) * new Vector3(direction.x, 0, direction.y);
        transform.position += move * scaledMoveSpeed;
    }

    private void Look(Vector2 rotate)
    {
        if (rotate.sqrMagnitude < 0.01)
            return;
        var scaledRotateSpeed = rotateSpeed * Time.deltaTime;
        m_Rotation.y += rotate.x * scaledRotateSpeed;
        m_Rotation.x = Mathf.Clamp(m_Rotation.x - rotate.y * scaledRotateSpeed, -89, 89);
        transform.localEulerAngles = m_Rotation;
    }
}