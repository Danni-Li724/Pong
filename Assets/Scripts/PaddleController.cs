using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
/// <summary>
/// Player identity and set up; also handles player specific UI spawning.
/// Now handles spaceship mode switching and bullet firing (maybe seperate script?)
/// </summary>
public class PaddleController : NetworkBehaviour
{
    [Header("Pong Settings")]
    private NetworkVariable<int> playerIdVar = new NetworkVariable<int>();
    public bool IsHorizontal => (PlayerId == 3 || PlayerId == 4);
    
    [Header("Spaceship Mode Settings")]
    private bool inSpaceshipMode = false;
    private Sprite defaultSprite;
    private GameObject bulletPrefab;
    private float bulletSpeed;
    private float fireCooldown;
    private float lastFireTime;
    public bool isInSpaceshipMode() => inSpaceshipMode;

    public int PlayerId { get; private set; }
    public GameObject playerTargetUIPrefab;
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        playerIdVar.OnValueChanged += (_, newId) =>
        {
            PlayerId = newId;
            Debug.Log($"[PaddleController] PlayerId received: {PlayerId}");
            GetComponent<PaddleInputHandler>().InitializeInput(IsHorizontal);
        };

        if (playerIdVar.Value != 0)
        {
            PlayerId = playerIdVar.Value;
            Debug.Log($"[PaddleController] PlayerId was already set: {PlayerId}");
            GetComponent<PaddleInputHandler>().InitializeInput(IsHorizontal);
        }
    }

    public void SetPlayerId(int id)
    {
        playerIdVar.Value = id;
    }

    public int GetPlayerId() => playerIdVar.Value;

    public void ReadyUp()
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

    public void SpawnPlayerSelectionUI()
    {
        if (!IsOwner) return;

        NetworkGameManager manager = FindObjectOfType<NetworkGameManager>();
        List<PlayerInfo> otherPlayers = manager.GetOtherPlayers(NetworkManager.Singleton.LocalClientId);

        GameObject ui = Instantiate(playerTargetUIPrefab);
        PlayerSelectionUI uiScript = ui.GetComponent<PlayerSelectionUI>();
        uiScript.InitializeUI(otherPlayers, NetworkManager.Singleton.LocalClientId, IsHorizontal);
    }
    
    #region Spaceship Mode
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

        var renderer = GetComponent<SpriteRenderer>();
        if (active)
        {
            defaultSprite = renderer.sprite;
            renderer.sprite = rocketSprite;
            GetComponent<PaddleInputHandler>().EnableSpaceshipControls();
        }
        else
        {
            renderer.sprite = defaultSprite;
            GetComponent<PaddleInputHandler>().DisableSpaceshipControls();
        }
    }

    public void TryFire(Vector2 targetWorldPos)
    {
        if (!inSpaceshipMode || !IsOwner) return;
        if (Time.time - lastFireTime < fireCooldown) return;
        lastFireTime = Time.time;

        Vector2 direction = (targetWorldPos - (Vector2)transform.position).normalized;
        FireServerRpc(direction);
    }

    [Rpc(SendTo.Server)]
    private void FireServerRpc(Vector2 direction)
    {
        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        bullet.GetComponent<Rigidbody2D>().linearVelocity = direction * bulletSpeed;

        NetworkObject no = bullet.GetComponent<NetworkObject>();
        no.Spawn();
    }
    #endregion
}
