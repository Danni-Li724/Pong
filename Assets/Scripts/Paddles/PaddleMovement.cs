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
    public float spaceshipThrust = 1000f;
    public float spaceshipTorque = 2500f;
    public float maxVelocity = 50f;

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
        // if (!IsServer) return;
        // Vector2 desiredVelocity = moveInput.normalized * moveSpeed;
        // rb.linearVelocity = desiredVelocity;
        // Vector3 pos = rb.position;
        // if (spaceshipModeBounds)
        // {
        //     pos.x = Mathf.Clamp(pos.x, spaceshipBounds.xMin, spaceshipBounds.xMax);
        //     pos.y = Mathf.Clamp(pos.y, spaceshipBounds.yMin, spaceshipBounds.yMax);
        // }
        // else
        // {
        //     if(controller.IsHorizontal)
        //         pos.x = Mathf.Clamp(pos.x, leftLimit, rightLimit);
        //     else
        //         pos.y = Mathf.Clamp(pos.y, bottomLimit, topLimit);
        // }
        // // Apply tilt or spaceship rotation always based on networked value:
        // float tiltAngle = controller.IsHorizontal ? 90f + controller.networkTiltAngle.Value : controller.networkTiltAngle.Value;
        // rb.MoveRotation(tiltAngle);
        
        /// New Aesteroid Physics:
        if (!IsServer) return;

        if (controller.isInSpaceshipMode())
        {
            // Spaceship Mode now apply thrust and torque
            // spaceshipInput.y = forward/backward thrust (W/S or up/down)
            // spaceshipInput.x = left/right steering (A/D or left/right)
            // only apply thrust in the direction the paddle is facing (its current rotation)
            
            // Clamp input to avoid small undetectable inputs
            float thrustInput = Mathf.Abs(spaceshipInput.y) < 0.1f ? 0 : Mathf.Sign(spaceshipInput.y);
            float turnInput = Mathf.Abs(spaceshipInput.x) < 0.1f ? 0 : Mathf.Sign(spaceshipInput.x);
            // thrust
            float thrust = spaceshipInput.y;
            Vector2 force = transform.up * (thrust * spaceshipThrust);
            rb.AddForce(force);
            // clamp velocity to max
            if (rb.linearVelocity.magnitude > maxVelocity)
                rb.linearVelocity = rb.linearVelocity.normalized * maxVelocity;
            // Torque
            float turn = -spaceshipInput.x; // Negative for "A=left", "D=right"
            rb.AddTorque(turn * spaceshipTorque);
            // Clamp position in bounds
            Vector2 pos = rb.position;
            if (spaceshipModeBounds)
            {
                pos.x = Mathf.Clamp(pos.x, spaceshipBounds.xMin, spaceshipBounds.xMax);
                pos.y = Mathf.Clamp(pos.y, spaceshipBounds.yMin, spaceshipBounds.yMax);
                rb.position = pos;
            }
            return;
        }
        
        // Normal Pong Movement:
        Vector2 desiredVelocity = moveInput.normalized * moveSpeed;
        rb.linearVelocity = desiredVelocity;
        Vector2 pongPos = rb.position;
        if (spaceshipModeBounds)
        {
            pongPos.x = Mathf.Clamp(pongPos.x, spaceshipBounds.xMin, spaceshipBounds.xMax);
            pongPos.y = Mathf.Clamp(pongPos.y, spaceshipBounds.yMin, spaceshipBounds.yMax);
        }
        else
        {
            if(controller.IsHorizontal)
                pongPos.x = Mathf.Clamp(pongPos.x, leftLimit, rightLimit);
            else
                pongPos.y = Mathf.Clamp(pongPos.y, bottomLimit, topLimit);
        }
        rb.position = pongPos;

        // Apply paddle tilt rotation (no longer spaceship rotation)
        float tiltAngle = controller.IsHorizontal ? 90f + controller.networkTiltAngle.Value : controller.networkTiltAngle.Value;
        rb.MoveRotation(tiltAngle);
    }
}
