using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PaddleController : NetworkBehaviour
{
    private int playerId;
    public float moveSpeed;
    
    private Rigidbody2D rb;
    private PlayerInput playerInput;
    private Vector2 moveInput;
    
    public float topLimit;
    public float bottomLimit;
    public float leftLimit;
    public float rightLimit;

    public bool isHorizontalPaddle;
    private NetworkVariable<int> playerIdVar = new NetworkVariable<int>();
    
    public GameObject playerTargetUIPrefab;

    public void SetPlayerId(int id)
    {
        playerIdVar.Value = id;
        Debug.Log(id);
        isHorizontalPaddle = (id == 3 || id == 4); // top and bottom players
    }
    
    public int GetPlayerId() => playerIdVar.Value;
    
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            Debug.Log($"NOT setting up input for paddle owned by client {OwnerClientId} (I am client {NetworkManager.Singleton.LocalClientId})");
            return;
        }
        Debug.Log($"Waiting for playerIdVar to be set...");
        
        playerIdVar.OnValueChanged += (_, newId) =>
        {
            playerId = newId;
            isHorizontalPaddle = (playerId == 3 || playerId == 4);
            Debug.Log($"Received playerId {playerId} on client {OwnerClientId}, isHorizontal: {isHorizontalPaddle}");
            SetUpInput();
        };
        if (playerIdVar.Value != 0)
        {
            playerId = playerIdVar.Value;
            isHorizontalPaddle = (playerId == 3 || playerId == 4);
            Debug.Log($"playerIdVar was already available: {playerId}");
            SetUpInput();
        }
    }

    public void SpawnPlayerSelectionUI()
    {
        if (!IsOwner) return;

        NetworkGameManager manager = FindObjectOfType<NetworkGameManager>();
        List<PlayerInfo> otherPlayers = manager.GetOtherPlayers(NetworkManager.Singleton.LocalClientId);

        GameObject ui = Instantiate(playerTargetUIPrefab);
        PlayerSelectionUI uiScript = ui.GetComponent<PlayerSelectionUI>();
        uiScript.InitializeUI(otherPlayers, NetworkManager.Singleton.LocalClientId, isHorizontalPaddle);
    }

    private void SetUpInput()
    {
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
    
    private void OnDisable()
    {
        if (IsOwner && playerInput != null)
        {
            playerInput.Disable();
        }
    }

    public void OnReadyButtonPressed()
    {
        if (IsOwner)
            NotifyReadyServerRpc();
    }

    [Rpc(SendTo.Server)]
    void NotifyReadyServerRpc()
    {
        NetworkGameManager manager = FindObjectOfType<NetworkGameManager>();
        manager.MarkPlayerReady(OwnerClientId);
    }
   
    void Update()
    {
        if (!IsServer) return;

        Vector3 movementDirection = Vector3.zero;

        if (isHorizontalPaddle)
        {
            // Use paddle's local right
            movementDirection = transform.right* -moveInput.x;
        }
        else
        {
            // Use paddle's local up
            movementDirection = transform.up * moveInput.y;
        }

        transform.Translate(movementDirection * moveSpeed * Time.deltaTime);

        // Clamp based on world space position and not local
        Vector3 pos = transform.position;

        if (isHorizontalPaddle)
        {
            pos.x = Mathf.Clamp(pos.x, leftLimit, rightLimit);
        }
        else
        {
            pos.y = Mathf.Clamp(pos.y, bottomLimit, topLimit);
        }

        transform.position = pos;
    }
    
    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    void MoveRequest_ServerRpc(float inputX, float inputY)
    {
        moveInput = new Vector2(inputX, inputY);
    }
}
