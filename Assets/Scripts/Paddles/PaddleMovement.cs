using Unity.Netcode;
using UnityEngine;
/// <summary>
/// Server side paddle movement logic
/// </summary>
public class PaddleMovement : NetworkBehaviour
{
    private Vector2 moveInput;
    public float moveSpeed;
    public float topLimit, bottomLimit, leftLimit, rightLimit;

    private PaddleController controller;
    private Rigidbody2D rb;
    
    [SerializeField] private Rect spaceshipBounds = new Rect(0f, 0f, 0f, 0f);
    private bool spaceshipModeBounds = false;

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

    public void EnableSpaceshipBounds(bool enable)
    {
        spaceshipModeBounds = enable;
    }

    // private void FixedUpdate()
    // {
    //     if (!IsServer) return; // Only server moves paddles
    //     // Calculate movement vector
    //     Vector3 movement = new Vector3(moveInput.x, moveInput.y, 0).normalized * moveSpeed * Time.fixedDeltaTime;
    //     // Clamp the position according to paddle orientation
    //     Vector3 pos = transform.position;
    //     if (controller == null || !controller.isInSpaceshipMode())
    //     {
    //         if (controller.IsHorizontal)
    //             pos.x = Mathf.Clamp(pos.x, leftLimit, rightLimit);
    //         else
    //             pos.y = Mathf.Clamp(pos.y, bottomLimit, topLimit);
    //
    //         // Assign the clamped position back
    //         transform.position = pos;
    //     }
    //     // manually setting spaceship bounds now that I am not using rb
    //     if (controller.isInSpaceshipMode())
    //     {
    //         pos.x = Mathf.Clamp(pos.x, spaceshipBounds.xMin, spaceshipBounds.xMax);
    //         pos.y = Mathf.Clamp(pos.y, spaceshipBounds.yMin, spaceshipBounds.yMax);
    //     }
    //     // Move using Translate now
    //     transform.Translate(movement, Space.World);
    //     if(rb) rb.linearVelocity = Vector2.zero;
    // }

    void FixedUpdate()
    {
        if (!IsServer) return;
        Vector2 desiredVelocity = moveInput.normalized * moveSpeed;
        rb.linearVelocity = desiredVelocity;
        Vector3 pos = rb.position;
        if (spaceshipModeBounds)
        {
            pos.x = Mathf.Clamp(pos.x, spaceshipBounds.xMin, spaceshipBounds.xMax);
            pos.y = Mathf.Clamp(pos.y, spaceshipBounds.yMin, spaceshipBounds.yMax);
        }
        else
        {
            if(controller.IsHorizontal)
                pos.x = Mathf.Clamp(pos.x, leftLimit, rightLimit);
            else
                pos.y = Mathf.Clamp(pos.y, bottomLimit, topLimit);
        }
    }
}
