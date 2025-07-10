using Unity.Netcode;
using UnityEngine;
/// <summary>
/// Server side paddle movement logic
/// </summary>
public class PaddleMovement : NetworkBehaviour
{
    private Vector2 moveInput;
    private Rigidbody2D rb;

    public float moveSpeed;
    public float topLimit, bottomLimit, leftLimit, rightLimit;

    private PaddleController controller;

    private void Awake()
    {
        controller = GetComponent<PaddleController>();
        rb = GetComponent<Rigidbody2D>();
    }

    // Called by server when it receives move input from client via InputHandler
    public void SetMoveInput(Vector2 input)
    {
        moveInput = input;
    }

    private void FixedUpdate()
    {
        if (!IsServer) return; // Only server moves paddles

        // Vector3 movementDirection = new Vector3(moveInput.x, moveInput.y, 0).normalized;
        // transform.Translate(movementDirection * moveSpeed * Time.deltaTime, Space.World);
        Vector2 movementDirection = moveInput.normalized;
        Vector2 targetPosition = rb.position + movementDirection * moveSpeed * Time.fixedDeltaTime;


        if (controller == null || !controller.isInSpaceshipMode())
        {
            Vector3 pos = transform.position;

            if (controller.IsHorizontal)
                pos.x = Mathf.Clamp(pos.x, leftLimit, rightLimit);
            else
                pos.y = Mathf.Clamp(pos.y, bottomLimit, topLimit);
        }
        // transform.position = pos;
        rb.MovePosition(targetPosition);
    }
}
