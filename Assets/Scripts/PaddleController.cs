using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PaddleController : NetworkBehaviour
{
    public int playerId = 1;
    public float moveSpeed;
    
    private Rigidbody2D rb;
    private PlayerInput playerInput;
    private Vector2 moveInput;
    
    public float topLimit;
    public float bottomLimit;
    public float leftLimit;
    public float rightLimit;

    public bool isHorizontalPaddle;
    
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Debug.Log($"Setting up input for paddle owned by client {OwnerClientId}");

            playerInput = new PlayerInput();

            if (isHorizontalPaddle)
            {
                // for top/bottom paddles
                playerInput.Player.Move.performed += ctx =>
                {
                    moveInput = ctx.ReadValue<Vector2>();
                    MoveRequest_ServerRpc(moveInput.x, 0); // send x only
                };
                playerInput.Player.Move.canceled += ctx =>
                {
                    moveInput = Vector2.zero;
                    MoveRequest_ServerRpc(0, 0);
                };
            }
            else
            {
                // for left/right paddles
                playerInput.Player.Move.performed += ctx =>
                {
                    moveInput = ctx.ReadValue<Vector2>();
                    MoveRequest_ServerRpc(0, moveInput.y); // send y only
                };
                playerInput.Player.Move.canceled += ctx =>
                {
                    moveInput = Vector2.zero;
                    MoveRequest_ServerRpc(0, 0);
                };
            }

            playerInput.Enable();
        }
        else
        {
            Debug.Log($"NOT setting up input for paddle owned by client {OwnerClientId} (I am client {NetworkManager.Singleton.LocalClientId})");
        }
    }

   
    // private void Awake()
    // {
    //     playerInput = new PlayerInput();
    //     playerInput.Player.Move.performed += ctx =>
    //     {
    //         moveInput = ctx.ReadValue<Vector2>();
    //         // Send input to server if this is your paddle
    //         if (IsOwner)
    //         {
    //             MoveRequest_ServerRpc(moveInput.y);
    //         }
    //     };
    //     playerInput.Player.Move.canceled += ctx => 
    //     { 
    //         moveInput = Vector2.zero;
    //         if (IsOwner)
    //         {
    //             MoveRequest_ServerRpc(0);
    //         }
    //     };
    // }
   
    // private void OnEnable()
    // {
    //     playerInput.Enable();
    // }
    
    private void OnDisable()
    {
        if (IsOwner && playerInput != null)
        {
            playerInput.Disable();
        }
    }
   
    void Update()
    {
        if (!IsServer) return;

        Vector3 pos = transform.position;

        if (isHorizontalPaddle)
        {
            Vector2 horizontalMovement = new Vector2(moveInput.x, 0);
            transform.Translate(horizontalMovement * moveSpeed * Time.deltaTime);
            pos.x = Mathf.Clamp(transform.position.x, leftLimit, rightLimit);
        }
        else
        {
            Vector2 verticalMovement = new Vector2(0, moveInput.y);
            transform.Translate(verticalMovement * moveSpeed * Time.deltaTime);
            pos.y = Mathf.Clamp(transform.position.y, bottomLimit, topLimit);
        }

        transform.position = pos;
    }
    
    [ServerRpc]
    void MoveRequest_ServerRpc(float inputX, float inputY)
    {
        moveInput = new Vector2(inputX, inputY);
    }

    // [ServerRpc(RequireOwnership = false)]
    // private void MoveRequest_ServerRpc(float directionY) 
    // {
    //     if (!IsServer) return;
    //
    //     float delta = directionY * moveSpeed * Time.fixedDeltaTime;
    //     Vector3 newPos = transform.position + new Vector3(0, delta, 0);
    //     newPos.y = Mathf.Clamp(newPos.y, bottomLimit, topLimit);
    //     transform.position = newPos;
    //     
    //     MoveUpdate_ClientRpc(newPos); // Sync to other clients
    // }
    //
    // // if no network transform
    // [ClientRpc]
    // private void MoveUpdate_ClientRpc(Vector3 newPos) 
    // {
    //     if (IsOwner) return; // Owner already sees it
    //     transform.position = newPos;
    // }
}
