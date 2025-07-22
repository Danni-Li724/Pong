using System;
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
    private PaddleVisuals paddleVisuals;
    
    [Header("Spaceship Mode Settings")]
    private bool inSpaceshipMode = false;
    [SerializeField] GameObject bulletPrefab;
    private float bulletSpeed;
    private float fireCooldown;
    private float lastFireTime;
    public bool isInSpaceshipMode() => inSpaceshipMode;
    
    [Header("Paddle Tilt Settings")]
    [SerializeField] private float maxTiltAngle = 45f;
    [SerializeField] private float tiltSpeed = 5f;
    private bool tiltActive = false;
    private float tiltDuration = 0f;
    private float tiltTimer = 0f;
    private float currentTiltAngle = 0f;
    // Network variables to sync tilt state across all clients
    private NetworkVariable<float> networkTiltAngle = new NetworkVariable<float>(0f, 
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> networkTiltActive = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private int paddleLayer;
    private int goalLayer;
    
    private void Awake()
    {
        paddleVisuals = GetComponentInChildren<PaddleVisuals>();
        // setting up layers for avoiding collision in certain modes
        paddleLayer = LayerMask.NameToLayer("Paddle");
        Debug.Log("Paddle Layer: " + paddleLayer);
        goalLayer = LayerMask.NameToLayer("Goal");
        Debug.Log("Goal Layer: " + goalLayer); 
        
    }
    
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return; // only runs for local player
        
        networkTiltAngle.OnValueChanged += OnTiltAngleChanged;
        networkTiltActive.OnValueChanged += OnTiltActiveChanged;
        
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
    
    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            networkTiltAngle.OnValueChanged -= OnTiltAngleChanged;
            networkTiltActive.OnValueChanged -= OnTiltActiveChanged;
        }
        base.OnNetworkDespawn();
    }
    public int GetPlayerId() => playerIdVar.Value;
    // server sets player ID and rotates if horizontal
    public void SetPlayerId(int id)
    {
        if (IsServer)
        {
            playerIdVar.Value = id;
            PlayerId = id;
        }
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
    
    private void Update()
    {
        if (!IsOwner) return;
        
        // Handle tilt boon
        if (tiltActive)
        {
            tiltTimer -= Time.deltaTime;
            if (tiltTimer <= 0f)
            {
                DeactivateTilt();
                return;
            }
            
            // Handle tilt input ONLY if not in spaceship mode, it only works for Pong mode
            if (!inSpaceshipMode)
            {
                HandleTiltInput();
            }
        }
        else
        {
            // Return to original position when not tilting
            if (currentTiltAngle != 0f)
            {
                currentTiltAngle = Mathf.Lerp(currentTiltAngle, 0f, Time.deltaTime * tiltSpeed);
                networkTiltAngle.Value = currentTiltAngle;
                
                if (Mathf.Abs(currentTiltAngle) < 0.1f)
                {
                    currentTiltAngle = 0f;
                    networkTiltAngle.Value = 0f;
                }
            }
        }
    }
    
     #region PADDLE TILT METHODS
    
    private void HandleTiltInput()
    {
        float targetAngle = 0f;
        
        if (Mouse.current.leftButton.isPressed)
        {
            targetAngle = -maxTiltAngle;
        }
        else if (Mouse.current.rightButton.isPressed)
        {
            targetAngle = maxTiltAngle;
        }
        
        // Smoothly interpolate to target angle
        currentTiltAngle = Mathf.Lerp(currentTiltAngle, targetAngle, Time.deltaTime * tiltSpeed);
        networkTiltAngle.Value = currentTiltAngle;
    }
    
    public void ActivateTilt(float duration)
    {
        if (!IsOwner) return;
        
        tiltActive = true;
        tiltDuration = duration;
        tiltTimer = duration;
        networkTiltActive.Value = true;
        Physics.IgnoreLayerCollision(paddleLayer, goalLayer, true);
        Debug.Log($"[PaddleController] Paddle tilt activated for {duration} seconds");
    }
    
    public void DeactivateTilt()
    {
        if (!IsOwner) return;
        
        tiltActive = false;
        networkTiltActive.Value = false;
        Physics.IgnoreLayerCollision(paddleLayer, goalLayer, false);
        Debug.Log("[PaddleController] Paddle tilt deactivated");
    }
    
    // Called when network tilt angle changes (for visual sync)
    private void OnTiltAngleChanged(float previousValue, float newValue)
    {
        ApplyTiltRotation(newValue);
    }
    
    // Called when network tilt active state changes
    private void OnTiltActiveChanged(bool previousValue, bool newValue)
    {
        Debug.Log($"[PaddleController] Tilt active state changed to: {newValue}");
    }
    
    private void ApplyTiltRotation(float angle)
    {
        // Apply the tilt rotation in original paddle orientation
        Quaternion baseTiltRotation = Quaternion.Euler(0f, 0f, angle);
        
        if (inSpaceshipMode)
        {
            return;
        }
        
        if (IsHorizontal)
        {
            // combine the horizontal rotation with tilt if it's horizontal paddle
            transform.localRotation = Quaternion.Euler(0, 0, 90 + angle);
        }
        else
        {
            transform.localRotation = baseTiltRotation;
        }
    }
    
    public bool IsTiltActive()
    {
        return networkTiltActive.Value;
    }
    
    public float GetRemainingTiltTime()
    {
        return tiltTimer;
    }

    #endregion
    
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
            // Reset tilt when entering spaceship mode
            if (IsOwner && tiltActive)
            {
                currentTiltAngle = 0f;
                networkTiltAngle.Value = 0f;
            }
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
