using UnityEngine;
using UnityEngine.InputSystem; // Necesario para el nuevo Input System

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 3f;
    private Vector2 movement;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        var gamepad = Gamepad.current;
        var keyboard = Keyboard.current;

        // Usa WASD o flechas con el teclado
        if (keyboard != null)
        {
            movement = Vector2.zero;

            // Movimiento vertical
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                movement.y += 1;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                movement.y -= 1;

            // Movimiento horizontal
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                movement.x -= 1;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                movement.x += 1;

            // Normalizar para evitar velocidad mayor al moverse en diagonal
            movement = movement.normalized;
        }


        // Alternativamente, usa joystick si se detecta un gamepad
        if (gamepad != null)
        {
            movement = gamepad.leftStick.ReadValue();
        }
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}