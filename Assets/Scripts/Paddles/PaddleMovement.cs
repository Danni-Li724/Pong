using Unity.Netcode;
using UnityEngine;
/// <summary>
/// Server side paddle movement logic
/// </summary>
public class PaddleMovement : NetworkBehaviour
{
    private Vector2 moveInput;
    private Vector2 spaceshipInput;
    public float moveSpeed;
    public float topLimit, bottomLimit, leftLimit, rightLimit;

    private PaddleController controller;
    private Rigidbody2D rb;
    
    [Header("Spaceship Mode Things")]
    [SerializeField] private Rect spaceshipBounds = new Rect(0f, 0f, 0f, 0f);
    private bool spaceshipModeBounds = false;
    // spaceship mode physics
    public float spaceshipThrust = 10f;
    public float spaceshipTorque = 150f;
    public float maxVelocity = 8f;

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

    public void SetSpaceshipInput(Vector2 input)
    {
        spaceshipInput = input;
    }
    public void EnableSpaceshipBounds(bool enable)
    {
        spaceshipModeBounds = enable;
    }
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
        // Apply tilt or spaceship rotation always based on networked value:
        float tiltAngle = controller.IsHorizontal ? 90f + controller.networkTiltAngle.Value : controller.networkTiltAngle.Value;
        rb.MoveRotation(tiltAngle);

    }
}
