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
    
    private PlayerInput spaceshipInput;
    
    public void InitializeInput(bool isHorizontalPaddle)
    {
        isHorizontal = isHorizontalPaddle;
        playerInput = new PlayerInput();
        
        //private PlayerInput spaceshipInput;
    
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
            MoveRequest_ServerRpc(input.x, input.y);
        };
        spaceshipInput.Spaceship.Move.canceled += _ =>
        {
            MoveRequest_ServerRpc(0, 0);
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
    #endregion
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // private PlayerInput playerInput;
    // private bool isHorizontal;
    // private Vector2 moveInput;
    //
    // private PlayerInput spaceshipInput;
    //
    // public void InitializeInput(bool isHorizontalPaddle)
    // {
    //     isHorizontal = isHorizontalPaddle;
    //     playerInput = new PlayerInput();
    //     
    //     if (isHorizontal)
    //     {
    //         playerInput.Player.Move.performed += ctx =>
    //         {
    //             moveInput = ctx.ReadValue<Vector2>();
    //             MoveRequest_ServerRpc(moveInput.x, 0);
    //         };
    //         playerInput.Player.Move.canceled += _ =>
    //         {
    //             moveInput = Vector2.zero;
    //             MoveRequest_ServerRpc(0, 0);
    //         };
    //     }
    //     else
    //     {
    //         playerInput.Player.Move.performed += ctx =>
    //         {
    //             moveInput = ctx.ReadValue<Vector2>();
    //             MoveRequest_ServerRpc(0, moveInput.y);
    //         };
    //         playerInput.Player.Move.canceled += _ =>
    //         {
    //             moveInput = Vector2.zero;
    //             MoveRequest_ServerRpc(0, 0);
    //         };
    //     }
    //
    //     playerInput.Enable();
    // }
    //
    // private void Update()
    // {
    //     // Handle mouse-based rotation for spaceship mode
    //     if (IsOwner && GetComponent<PaddleController>().isInSpaceshipMode())
    //     {
    //         HandleSpaceshipRotation();
    //     }
    // }
    //
    // private void HandleSpaceshipRotation()
    // {
    //     Vector2 mousePos = Mouse.current.position.ReadValue();
    //     Vector2 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
    //     Vector2 direction = (worldPos - (Vector2)transform.position).normalized;
    //     
    //     float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    //     
    //     // Apply rotation locally for immediate response
    //     PaddleVisuals visuals = GetComponentInChildren<PaddleVisuals>();
    //     if (visuals != null)
    //     {
    //         visuals.transform.localRotation = Quaternion.Euler(0, 0, angle);
    //     }
    //     
    //     // Send to server for network sync (less frequently to avoid spam)
    //     if (Time.frameCount % 3 == 0) // Every 3 frames
    //     {
    //         RotateSpaceship_ServerRpc(angle);
    //     }
    // }
    //
    // private void OnDisable()
    // {
    //     if (IsOwner && playerInput != null)
    //         playerInput.Disable();
    // }
    //
    // // Sent by local client to move their paddle because server owns the movement logic.
    // [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    // void MoveRequest_ServerRpc(float inputX, float inputY)
    // {
    //     GetComponent<PaddleMovement>().SetMoveInput(new Vector2(inputX, inputY));
    // }
    //
    // #region Spaceship Input
    // public void EnableSpaceshipControls()
    // {
    //     // disabling normal paddle input first
    //     if (playerInput != null)
    //     {
    //         playerInput.Disable();
    //     }
    //     
    //     spaceshipInput = new PlayerInput();
    //     spaceshipInput.Spaceship.Move.performed += ctx =>
    //     {
    //         Vector2 input = ctx.ReadValue<Vector2>();
    //         MoveRequest_ServerRpc(input.x, input.y);
    //     };
    //     spaceshipInput.Spaceship.Move.canceled += _ =>
    //     {
    //         MoveRequest_ServerRpc(0, 0);
    //     };
    //     spaceshipInput.Spaceship.Fire.performed += _ =>
    //     {
    //         Vector2 mouse = Mouse.current.position.ReadValue();
    //         Vector2 worldPos = Camera.main.ScreenToWorldPoint(mouse);
    //         GetComponent<PaddleController>().TryFire(worldPos);
    //     };
    //     spaceshipInput.Enable();
    // }
    //
    // public void DisableSpaceshipControls()
    // {
    //     spaceshipInput?.Disable();
    //     spaceshipInput = null;
    //     
    //     // Re-enable normal paddle input
    //     if (playerInput != null)
    //     {
    //         playerInput.Enable();
    //     }
    // }
    //
    // // Server RPC to sync spaceship rotation to other clients
    // [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
    // void RotateSpaceship_ServerRpc(float angle)
    // {
    //     PaddleController controller = GetComponent<PaddleController>();
    //     if (controller != null && controller.isInSpaceshipMode())
    //     {
    //         // Sync rotation to all OTHER clients (not back to sender)
    //         SyncSpaceshipRotation_ClientRpc(angle, OwnerClientId);
    //     }
    // }
    //
    // // Client RPC to sync rotation to other clients (excluding the sender)
    // [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Unreliable)]
    // void SyncSpaceshipRotation_ClientRpc(float angle, ulong senderClientId)
    // {
    //     // Don't apply to the sender (they already have it applied locally)
    //     if (NetworkManager.Singleton.LocalClientId != senderClientId)
    //     {
    //         PaddleVisuals visuals = GetComponentInChildren<PaddleVisuals>();
    //         if (visuals != null)
    //         {
    //             visuals.transform.localRotation = Quaternion.Euler(0, 0, angle);
    //         }
    //     }
    // }
    // #endregion
}
