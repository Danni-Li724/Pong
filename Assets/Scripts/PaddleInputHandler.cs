using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PaddleInputHandler : NetworkBehaviour
{
    private PlayerInput playerInput;
    private bool isHorizontal;
    private Vector2 moveInput;

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

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    void MoveRequest_ServerRpc(float inputX, float inputY)
    {
        GetComponent<PaddleMovement>().SetMoveInput(new Vector2(inputX, inputY));
    }
}
