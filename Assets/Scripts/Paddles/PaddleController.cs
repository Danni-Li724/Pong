using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
/// <summary>
/// Player identity and set up; also handles player specific UI spawning.
/// Now handles spaceship mode switching and bullet firing. Ideally, these should be seperate scripts
public class PaddleController : NetworkBehaviour
{
   [Header("Pong Settings")]
    private NetworkVariable<int> playerIdVar = new NetworkVariable<int>(); // synced player IDs
    public int PlayerId { get; private set; }
    public GameObject playerSelectionUIPrefab;
    public bool IsHorizontal => (PlayerId == 3 || PlayerId == 4);
    
    [Header("Spaceship Mode Settings")]
    private bool inSpaceshipMode = false;
    [SerializeField] GameObject bulletPrefab;
    private float bulletSpeed;
    private float fireCooldown;
    private float lastFireTime;
    
    private PaddleVisuals paddleVisuals;
    
    public bool isInSpaceshipMode() => inSpaceshipMode;
    
    private void Awake()
    {
        paddleVisuals = GetComponentInChildren<PaddleVisuals>();
        
    }
    
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return; // only runs for local player
        
        // when server updated the ID, sync it and initializes input
        playerIdVar.OnValueChanged += (_, newId) =>
        {
            PlayerId = newId;
            Debug.Log($"[PaddleController] PlayerId received: {PlayerId}");
            GetComponent<PaddleInputHandler>().InitializeInput(IsHorizontal);
        };
        // in case ID is already set on spawn, still init input
        if (playerIdVar.Value != 0)
        {
            PlayerId = playerIdVar.Value;
            Debug.Log($"[PaddleController] PlayerId was already set: {PlayerId}");
            GetComponent<PaddleInputHandler>().InitializeInput(IsHorizontal);
        }
        // registering the paddles to the visual manager for syncing background color effects
        if (IsOwner && VisualEventsManager.Instance != null)
        {
            VisualEventsManager.Instance.RegisterPaddle(this);
        }
    }
    
    public int GetPlayerId() => playerIdVar.Value;

    // server sets player ID and rotates if horizontal
    public void SetPlayerId(int id)
    {
        playerIdVar.Value = id;
        PlayerId = id;
        // rotating horizontal paddle
        if (IsHorizontal)
        {
            transform.localRotation = Quaternion.Euler(0, 0, 90);
        }
    }

    public void ReadyUp()
    {
        // called by client to notify server it's ready
        if (IsOwner)
            NotifyReadyServerRpc();
    }
    
    // Server-Rpc - triggered when the client clicks the ready button.
    // It tells the server - who tracks ready players - that 'I' am ready
    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    void NotifyReadyServerRpc()
    {
        NetworkGameManager manager = FindObjectOfType<NetworkGameManager>();
        manager.MarkPlayerReady(OwnerClientId);
    }

    // This is called by the game manager to spawn buttons to represent players who aren't themselves.
    // These buttons will be used later as one of the boon effects.
    public void SpawnPlayerSelectionUI()
    {
        Debug.Log($"[PaddleController] Attempting to SpawnPlayerSelectionUI (IsOwner: {IsOwner})"); // had to debug this system a lot...
        if (!IsOwner) return;
        // grabs list of other connected players to show in UI
        List<PlayerInfo> otherPlayers = NetworkGameManager.Instance.GetOtherPlayers(NetworkManager.Singleton.LocalClientId);
        GameObject ui = Instantiate(playerSelectionUIPrefab); 
        PlayerSelectionUI uiScript = ui.GetComponent<PlayerSelectionUI>();
        uiScript.InitializeUI(otherPlayers, NetworkManager.Singleton.LocalClientId, PlayerId); // passing playerId
    }
    
    #region SPACESHIP MODE
    /// <summary>
    /// Below are functions for Spaceship controls, firing bullets
    /// </summary>
    /// <param name="active"></param>
    /// <param name="rocketSprite"></param>
    /// <param name="bulletPrefab"></param>
    /// <param name="bulletSpeed"></param>
    /// <param name="fireCooldown"></param>
    
    public void SetSpaceshipMode(bool active, Sprite rocketSprite, GameObject bulletPrefab, float bulletSpeed, float fireCooldown)
    {
        inSpaceshipMode = active;
        this.bulletPrefab = bulletPrefab;
        this.bulletSpeed = bulletSpeed;
        this.fireCooldown = fireCooldown;

        if (active)
        {
            paddleVisuals.SetSpaceshipSprite(rocketSprite);
            transform.localRotation = Quaternion.identity;
            paddleVisuals.transform.localRotation = Quaternion.identity;
            // give player spaceship controls if local
            if (IsOwner)
            {
                GetComponent<PaddleInputHandler>().EnableSpaceshipControls();
            }
        }
        else
        {
            paddleVisuals.RestoreDefaultSprite();
        
            // Restore original paddle rotation if it was horizontal
            if (IsHorizontal)
            {
                transform.localRotation = Quaternion.Euler(0, 0, 90);
            }
            else
            {
                transform.localRotation = Quaternion.identity;
            }
            paddleVisuals.transform.localRotation = Quaternion.identity;

            if (IsOwner)
                GetComponent<PaddleInputHandler>().DisableSpaceshipControls();
        }
    }
    
    // Client-Rpc - Server tells clients to switch sprite and start rocket behaviour.
    // This is called by the GameManager on all clients when entering spaceship mode.
    // Client uses this function to load the right sprites + bullet prefab locally.  
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    public void SetSpaceshipModeRpc(bool active, string rocketSpriteName, float bulletSpeed, float fireCooldown)
    {
        Debug.Log($"[PaddleController] SetSpaceshipModeRpc called for Player {PlayerId}: active={active}, sprite={rocketSpriteName}");
    
        if (active)
        {
            // Load the rocket sprite
            Sprite rocketSprite = Resources.Load<Sprite>($"Sprites/{rocketSpriteName}");
            GameObject bulletPrefab = Resources.Load<GameObject>("Prefabs/Bullet");

            if (rocketSprite == null)
            {
                Debug.LogWarning($"[PaddleController] Missing rocket sprite: {rocketSpriteName}");
                rocketSprite = Resources.Load<Sprite>("Sprites/rocket_default");
            }

            if (bulletPrefab == null)
            {
                Debug.LogError("[PaddleController] Missing bullet prefab");
                return;
            }
            SetSpaceshipMode(true, rocketSprite, bulletPrefab, bulletSpeed, fireCooldown);
        }
        else
        {
            SetSpaceshipMode(false, null, null, 0f, 0f);
        }
    }
    
    public void UpdateSpaceshipRotation(Vector2 input)
    {
        if (!inSpaceshipMode || paddleVisuals == null) return;
        paddleVisuals.RotateInDirection(input);
    }
    
    // Called by GameManager's server-side operations to put a player into spaceship mode
    public void EnterSpaceshipMode(float bulletSpeed, float fireCooldown, string rocketSpriteName)
    {
        Debug.Log($"[PaddleController] EnterSpaceshipMode called for Player {PlayerId}");
        if (IsServer)
        {
            SetSpaceshipModeRpc(true, rocketSpriteName, bulletSpeed, fireCooldown);
        }
    }
    
    // Called locally by owner to try shoot bullets
    public void TryFire(Vector2 targetWorldPos)
    {
        if (!inSpaceshipMode || !IsOwner) return;
        if (Time.time - lastFireTime < fireCooldown) return;
        lastFireTime = Time.time;
        Vector2 direction = (targetWorldPos - (Vector2)transform.position).normalized;
        FireServerRpc(direction);
    }
    
    // Spawns bullet on server side and syncs it to everyone (is called by client owner).
    // Server needs to own the bullet to ensure their visibility to all clients.
    [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable, RequireOwnership = true)]
    private void FireServerRpc(Vector2 direction)
    {
        float bulletSpawnOffset = 1.0f;
        Vector2 spawnPos = (Vector2)transform.position + direction.normalized * bulletSpawnOffset;

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

        // assigns shooter to bullet before anything else
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.SetShooter(GetComponent<NetworkObject>());
        }

        // ignore collision with shooter
        Collider2D playerCol = GetComponent<Collider2D>();
        Collider2D bulletCol = bullet.GetComponent<Collider2D>();
        if (playerCol != null && bulletCol != null)
        {
            Physics2D.IgnoreCollision(bulletCol, playerCol);
        }

        // velocity and rotation
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = direction * bulletSpeed;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        // netSpawn
        NetworkObject netObj = bullet.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
        }
    }
    #endregion
}
