using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
/// <summary>
/// Paddle Input: handles its initializing and switching to spaceship input if needed.
/// It also sends input (move requests) to the server where movement logic exists.
/// </summary>
public class PaddleInputHandler : NetworkBehaviour
{
    private PlayerInput playerInput;
    private bool isHorizontal;
    private Vector2 moveInput;
    private Vector2 lastMoveDirection; // Track last movement direction

    private PlayerInput spaceshipInput;

    public void InitializeInput(bool isHorizontalPaddle)
    {
        isHorizontal = isHorizontalPaddle;
        playerInput = new PlayerInput();
        
        if (isHorizontal)
        {
            playerInput.Player.Move.performed += ctx =>
            {
                moveInput = ctx.ReadValue<Vector2>();
                MoveRequest_ServerRpc(moveInput.x, 0);
            };
            playerInput.Player.Move.canceled += _ =>
            {
                moveInput = Vector2.zero;
                MoveRequest_ServerRpc(0, 0);
            };
        }
        else
        {
            playerInput.Player.Move.performed += ctx =>
            {
                moveInput = ctx.ReadValue<Vector2>();
                MoveRequest_ServerRpc(0, moveInput.y);
            };
            playerInput.Player.Move.canceled += _ =>
            {
                moveInput = Vector2.zero;
                MoveRequest_ServerRpc(0, 0);
            };
        }

        playerInput.Enable();
    }

    private void OnDisable()
    {
        if (IsOwner && playerInput != null)
            playerInput.Disable();
    }

    // Sent by local client to move their paddle because server owns the movement logic.
    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    void MoveRequest_ServerRpc(float inputX, float inputY)
    {
        GetComponent<PaddleMovement>().SetMoveInput(new Vector2(inputX, inputY));
    }

    #region Spaceship Input
    public void EnableSpaceshipControls()
    {
        // disabling normal paddle input first
        if (playerInput != null)
        {
            playerInput.Disable();
        }
        
        spaceshipInput = new PlayerInput();
        spaceshipInput.Spaceship.Move.performed += ctx =>
        {
            Vector2 input = ctx.ReadValue<Vector2>();
            
            // store the movement direction for rotation
            if (input.magnitude > 0.1f) // only update if there's significant input 
            {
                lastMoveDirection = input.normalized;
                // send rotation request to server
                RotateSpaceship_ServerRpc(lastMoveDirection);
            }
            
            MoveRequest_ServerRpc(input.x, input.y);
        };
        spaceshipInput.Spaceship.Move.canceled += _ =>
        {
            MoveRequest_ServerRpc(0, 0);
            // don't reset rotation when movement stops - keep facing last direction
        };
        spaceshipInput.Spaceship.Fire.performed += _ =>
        {
            Vector2 mouse = Mouse.current.position.ReadValue();
            Vector2 worldPos = Camera.main.ScreenToWorldPoint(mouse);
            GetComponent<PaddleController>().TryFire(worldPos);
        };
        spaceshipInput.Enable();
    }
    
    public void DisableSpaceshipControls()
    {
        spaceshipInput?.Disable();
        spaceshipInput = null;
        
        // Re-enable normal paddle input
        if (playerInput != null)
        {
            playerInput.Enable();
        }
    }
    
    // server RPC to handle spaceship rotation
    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    void RotateSpaceship_ServerRpc(Vector2 direction)
    {
        PaddleController controller = GetComponent<PaddleController>();
        if (controller != null && controller.isInSpaceshipMode())
        {
            // calculate rotation angle from direction
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            // apply rotation to the visual child object
            PaddleVisuals visuals = GetComponentInChildren<PaddleVisuals>();
            if (visuals != null)
            {
                visuals.transform.localRotation = Quaternion.Euler(0, 0, angle);
            }
            
            // sync rotation to all clients
            SyncSpaceshipRotation_ClientRpc(angle);
        }
    }
    
    // client RPC to sync rotation across all clients
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    void SyncSpaceshipRotation_ClientRpc(float angle)
    {
        PaddleVisuals visuals = GetComponentInChildren<PaddleVisuals>();
        if (visuals != null)
        {
            visuals.transform.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }
    #endregion
}
