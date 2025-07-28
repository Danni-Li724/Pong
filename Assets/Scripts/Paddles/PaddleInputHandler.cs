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
            GetComponent<PaddleController>().UpdateSpaceshipRotation(input);
        };
        spaceshipInput.Spaceship.Move.canceled += _ =>
        {
            MoveRequest_ServerRpc(0, 0);
            //GetComponent<PaddleController>().UpdateSpaceshipRotation(Vector2.zero);
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
}
