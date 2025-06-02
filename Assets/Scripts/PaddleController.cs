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
                playerInput.PlayerHorizontal.Move.performed += ctx =>
                {
                    moveInput = ctx.ReadValue<Vector2>();
                    MoveRequest_ServerRpc(moveInput.x); // Use X axis for horizontal movement
                };
                playerInput.PlayerHorizontal.Move.canceled += ctx =>
                {
                    moveInput = Vector2.zero;
                    MoveRequest_ServerRpc(0);
                };
            }
            else
            {
                // for left/right paddles
                playerInput.Player.Move.performed += ctx =>
                {
                    moveInput = ctx.ReadValue<Vector2>();
                    MoveRequest_ServerRpc(moveInput.y); // Use Y axis for vertical movement
                };
                playerInput.Player.Move.canceled += ctx =>
                {
                    moveInput = Vector2.zero;
                    MoveRequest_ServerRpc(0);
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
        // only the server moves the paddle based on received input
        if (!IsServer) return;
        Vector2 verticalMovement = new Vector2(0, moveInput.y);
        transform.Translate(verticalMovement * moveSpeed * Time.deltaTime);
        Vector3 pos = transform.position;
        pos.y = Mathf.Clamp(pos.y, bottomLimit, topLimit);
        transform.position = pos;
        
        if (isHorizontalPaddle)
        {
            Vector2 horizontalMovement = new Vector2(0, moveInput.x);
            transform.Translate(horizontalMovement * moveSpeed * Time.deltaTime);
            //Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, -leftLimit, rightLimit);
            transform.position = pos;
        }
    }
    
    [ServerRpc]
    void MoveRequest_ServerRpc(float inputY)
    {
        // server receives input and updates moveInput
        moveInput = new Vector2(0, inputY);
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
