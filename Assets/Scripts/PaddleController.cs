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
    
    public float topLimit = 4f;
    public float bottomLimit = -4f;

   
    private void Awake()
    {
        playerInput = new PlayerInput();
        playerInput.Player.Move.performed += ctx =>
        {
            moveInput = ctx.ReadValue<Vector2>();
            MoveRequest_ServerRpc(moveInput.y);
        };
        playerInput.Player.Move.canceled += ctx => 
        { 
            moveInput = Vector2.zero;
            MoveRequest_ServerRpc(0);
        };
    }
   
    private void OnEnable()
    {
        if (IsOwner)
        {
            playerInput.Enable();
        }
    }
   
    private void OnDisable()
    {
        if (IsOwner)
        {
            playerInput.Enable();
        }
    }
   
    //void Update()
    //{
        //if (!IsOwner) return;
        //Vector2 movement = new Vector2(0, moveInput.y); 
        //transform.Translate(movement * moveSpeed * Time.deltaTime);
           
       // Vector3 pos = transform.position;
        //pos.y = Mathf.Clamp(pos.y, bottomLimit, topLimit);
        //transform.position = pos;
    //}

    [ServerRpc(RequireOwnership = false)]
    private void MoveRequest_ServerRpc(float directionY) 
    {
        if (!IsServer) return;

        float delta = directionY * moveSpeed * Time.fixedDeltaTime;
        Vector3 newPos = transform.position + new Vector3(0, delta, 0);
        newPos.y = Mathf.Clamp(newPos.y, bottomLimit, topLimit);
        transform.position = newPos;
        
        MoveUpdate_ClientRpc(newPos); // Sync to other clients
    }
    
    // if no network transform
    [ClientRpc]
    private void MoveUpdate_ClientRpc(Vector3 newPos) 
    {
        if (IsOwner) return; // Owner already sees it
        transform.position = newPos;
    }
}
