using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Runtime.InteropServices;

public class NetworkGameManager : NetworkBehaviour
{
    [Header("Game Settings")]
    public NetworkVariable<int> maxPlayers = new NetworkVariable<int>(2, 
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> connectedPlayersCount = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private bool allPlayersJoined = false;
    private bool gameStarted = false;
    private int playerCount = 0;
    private Dictionary<ulong, PlayerInfo> allPlayers = new Dictionary<ulong, PlayerInfo>(); 
    
    [Header("Script Refs")]
    [SerializeField] private ScoreManager scoreManager;
    public static NetworkGameManager Instance { get; private set; }
    
    [Header("Level Objects")]
   public Transform leftSpawn, rightSpawn, topSpawn, bottomSpawn;
    public GameObject playerPaddlePrefab;
    public GameObject ballPrefab;
    private GameObject spawnedBall; // current ball in scene
    
    [Header("Game Visuals")]
    [SerializeField] private GameObject circles;
    [SerializeField] private GameObject stars;
    
    [Header("Game UIs")]
    [SerializeField] private GameObject playerCountSelectionPanel;
    [SerializeField] private GameObject startGamePanel;
    [SerializeField] private Text playerCountText;
    [SerializeField] private GameObject gameEndPanel;
    [SerializeField] private Text gameEndText;
    [SerializeField] private Text winnerText;
    [SerializeField] private Button restartButton;

    #region INITIALIZING

    // Get list of other players for a specific client
    public List<PlayerInfo> GetOtherPlayers(ulong clientId)
    {
        return allPlayers.Values.Where(p => p.clientId != clientId && p.isConnected).ToList();
    }
    // Get player info by client ID
    public PlayerInfo GetPlayerInfo(ulong clientId)
    {
        return allPlayers.ContainsKey(clientId) ? allPlayers[clientId] : null;
    }
    // Get all connected players
    public List<PlayerInfo> GetAllPlayers()
    {
        return allPlayers.Values.Where(p => p.isConnected).ToList();
    }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            // handling disconnection
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
            // spawn paddle 1 for host
            SpawnPlayerPaddle(NetworkManager.Singleton.LocalClientId, 1);
            playerCount = 1;
            connectedPlayersCount.Value = 1;
            ShowPlayerSelectionForHost();
            SetupGameEndUI(); // initializing end game button
        }
        connectedPlayersCount.OnValueChanged += OnConnectedPlayersCountChanged;
    }

   
    void OnClientConnected(ulong clientId)
        {
            if (!IsServer) return;
            Debug.Log($"Client connected with ID: {clientId}");
            Debug.Log($"Host LocalClientId: {NetworkManager.Singleton.LocalClientId}");
            if (clientId == NetworkManager.Singleton.LocalClientId) return; // host doesn't reconnect

            if (playerCount >= maxPlayers.Value)
            {
                Debug.LogWarning("Player count exceeds max player count");
                return;
            }
            playerCount++;
            connectedPlayersCount.Value = playerCount;
            
            SpawnPlayerPaddle(clientId, playerCount);
            AssignGoals();
            SyncPlayerSprite(clientId); // IMMEDIATELY syncing sprites for new players
            SyncAllPlayersToClient(clientId); // Also sync all existing players to the new client
            
            // NOW show button
            if (playerCount == maxPlayers.Value)
            {
                allPlayersJoined = true;
                EnableReadyUpButtonClientRpc();
            }
        }
    void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;
        
        if (allPlayers.ContainsKey(clientId))
        {
            allPlayers[clientId].isConnected = false;
            playerCount--;
            connectedPlayersCount.Value = playerCount;
            allPlayersJoined = false;
            Debug.Log($"Player {allPlayers[clientId].playerId} disconnected");
            
            // hide ready button if not all players are connected
            if (playerCount < maxPlayers.Value)
            {
                DisableReadyUpButtonClientRpc();
            }
        }
    }

    #endregion
    
    #region START GAME
    void SpawnPlayerPaddle(ulong clientId, int playerId)
    {
        Transform spawnTransform = GetSpawnTransform(playerId);
        if (spawnTransform == null)
        {
            Debug.LogError($"No spawnPos found for player {playerId}");
            return;
        }
        // Create player info
        PlayerInfo playerInfo = new PlayerInfo(playerId, clientId, spawnTransform);
        playerInfo.isConnected = true;
        allPlayers[clientId] = playerInfo;
        // Spawn the paddle
        //GameObject paddle = Instantiate(playerPaddlePrefab, spawnTransform.position, spawnTransform.rotation);
        GameObject paddle = Instantiate(playerPaddlePrefab, spawnTransform.position, Quaternion.identity);
        NetworkObject networkObject = paddle.GetComponent<NetworkObject>();
        networkObject.SpawnAsPlayerObject(clientId);
        PaddleController paddleController = paddle.GetComponent<PaddleController>();
        if (paddleController != null)
        {
            paddleController.SetPlayerId(playerId);
            // loading custom paddle sprites
            var renderer = paddle.GetComponent<SpriteRenderer>();
            if (renderer != null && playerInfo.paddleSprite != null)
            {
                renderer.sprite = playerInfo.paddleSprite;
            }
        }
    }
    
    private void ShowPlayerSelectionForHost()
    {
        if (IsHost && playerCountSelectionPanel != null)
        {
            playerCountSelectionPanel.SetActive(true);
            UpdatePlayerCountDisplay();
        }
    }
    
    private void OnConnectedPlayersCountChanged(int previousValue, int newValue)
    {
        UpdatePlayerCountDisplay();
    }
    
    private void UpdatePlayerCountDisplay()
    {
        if (playerCountText != null)
        {
            playerCountText.text = $"Players Connected: {connectedPlayersCount.Value}/{maxPlayers.Value}";
        }
    }
    
    public void OnPlayerCountSelected(int count)
    {
        if (!IsHost) return;
        SetMaxPlayersServerRpc(count);
        HidePlayerCountSelectionPanel();
    }
    
    private void HidePlayerCountSelectionPanel()
    {
        if (playerCountSelectionPanel != null)
        {
            playerCountSelectionPanel.SetActive(false);
        }
    }

    
    [Rpc(SendTo.Server)]
    public void SetMaxPlayersServerRpc(int playerCount)
    {
        if (!IsServer) return;
        if (playerCount >= 2 && playerCount <= 4)
        {
            maxPlayers.Value = playerCount;
            Debug.Log("Player count in this game set to " + playerCount);
        }
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    private void EnableReadyUpButtonClientRpc()
    {
        ReadyUpButton[] readyButtons = FindObjectsOfType<ReadyUpButton>();
        foreach (var button in readyButtons)
        {
            button.EnableButton();
        }
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    private void DisableReadyUpButtonClientRpc()
    {
        ReadyUpButton[] readyButtons = FindObjectsOfType<ReadyUpButton>();
        foreach (var button in readyButtons)
        {
            button.DisableButton();
        }
    }
    
    private void AssignGoals()
    {
        GoalController[] allGoals = FindObjectsOfType<GoalController>();
        Transform[] spawnPoints = new Transform[]
        {
            leftSpawn,
            rightSpawn,
            topSpawn,
            bottomSpawn
        };
        for (int i = 0; i < allGoals.Length; i++)
        {
            GoalController closestGoal = null;
            float closestDistance = float.MaxValue;
            int matchedPlayerId = -1;
            // setting goal to player by checking distance
            for (int playerId = 1; playerId <= spawnPoints.Length; playerId++)
            {
                float dist = Vector3.Distance(allGoals[i].transform.position, spawnPoints[playerId - 1].position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestGoal = allGoals[i];
                    matchedPlayerId = playerId;
                }
            }

            if (closestGoal != null)
            {
                closestGoal.SetGoalForPlayerId(matchedPlayerId);
                Debug.Log($"Assigned goal at {closestGoal.transform.position} to player {matchedPlayerId}");
            }
        }
    }
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void TriggerPlayerSelectionUIClientRpc()
    {
        Debug.Log("All players are ready, making player selection buttons.");
        PaddleController[] allPaddles = FindObjectsOfType<PaddleController>();
        foreach (var paddle in allPaddles)
        {
            if (paddle.IsOwner)
            {
                paddle.SpawnPlayerSelectionUI();
            }
        }
    }
    Transform GetSpawnTransform(int playerId)
        {
            switch (playerId)
            {
                case 1: return leftSpawn;
                case 2: return rightSpawn;
                case 3: return topSpawn;
                case 4: return bottomSpawn;
                default: return null;
            }
        }
    public void MarkPlayerReady(ulong clientId)
    {
        if(!IsServer) return;
        if(allPlayers.ContainsKey(clientId))
        {
            allPlayers[clientId].isReady = true;
            if (AllPlayersReady())
            {
                StartBoonSelection();
            }
        };
    }
    
    private void StartBoonSelection()
    {
        // now syncing all sprites before starting
        foreach (var kvp in allPlayers)
        {
            if (kvp.Value.isConnected)
            {
                SyncPlayerSprite(kvp.Key);
            }
        }
        if (!IsServer) return;
        // start boon selection
        BoonManager.Instance.StartBoonSelection();
    }
    
    // called by BoonManager when all players have selected their boons
    public void OnAllBoonsSelected()
    {
        if (!IsServer) return;
        ShowStartGameButtonClientRpc();
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    private void ShowStartGameButtonClientRpc()
    {
        if (IsHost && startGamePanel != null)
        {
            startGamePanel.SetActive(true);
        }
    }
    
    // Called by the start game button
    public void OnStartGameButtonPressed()
    {
        if (!IsHost) return;
        StartGameServerRpc();
    }
    
    [Rpc(SendTo.Server)]
    private void StartGameServerRpc()
    {
        if (!IsServer) return;
        StartGame();
    }
    private void StartGame()
    {
        if (gameStarted) return;
        gameStarted = true;
        // Spawn the ball
        spawnedBall = Instantiate(ballPrefab, Vector3.zero, Quaternion.identity);
        spawnedBall.GetComponent<NetworkObject>().Spawn();
        HideStartGameButtonClientRpc();
        Debug.Log("Game officially started!");
        // initialize player selection ui
        TriggerPlayerSelectionUIClientRpc();
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    private void HideStartGameButtonClientRpc()
    {
        if (startGamePanel != null)
        {
            startGamePanel.SetActive(false);
        }
    }
    private bool AllPlayersReady()
    {
        return allPlayers.Values.Where(p => p.isConnected).Count(p => p.isReady) == maxPlayers.Value;
    }
    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }
    
    #endregion
    
#region SPRITE SYCING
private void SyncPlayerSprite(ulong clientId) // sync sprite for a specific player to all clients
{
    if (!allPlayers.ContainsKey(clientId)) return;
        
    PlayerInfo playerInfo = allPlayers[clientId];
    SyncPlayerSpriteClientRpc(clientId, playerInfo.paddleSpriteName, playerInfo.rocketSpriteName);
}
private void SyncAllPlayersToClient(ulong targetClientId) // syncing all existing players to new joiners
{
    foreach (var kvp in allPlayers)
    {
        if (kvp.Value.isConnected) // kvp = Key-ValuePair for my Dictionary<ulong, PlayerInfo> ('allPlayers') :)
        {
            SyncPlayerSpriteToSpecificClientRpc(
                kvp.Key, 
                kvp.Value.paddleSpriteName, 
                kvp.Value.rocketSpriteName,
                RpcTarget.Single(targetClientId, RpcTargetUse.Temp)
            );
        }
    }
}
[Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
private void SyncPlayerSpriteClientRpc(ulong targetClientId, string paddleSpriteName, string rocketSpriteName)
{
    ApplyPlayerSprite(targetClientId, paddleSpriteName, rocketSpriteName);
}

[Rpc(SendTo.SpecifiedInParams, Delivery = RpcDelivery.Reliable)]
private void SyncPlayerSpriteToSpecificClientRpc(ulong targetClientId, string paddleSpriteName, string rocketSpriteName, RpcParams rpcParams = default)
{
    ApplyPlayerSprite(targetClientId, paddleSpriteName, rocketSpriteName);
}
private void ApplyPlayerSprite(ulong targetClientId, string paddleSpriteName, string rocketSpriteName)
{
    PaddleController[] paddles = FindObjectsByType<PaddleController>(FindObjectsSortMode.None);
    foreach (var paddle in paddles)
    {
        if (paddle.OwnerClientId == targetClientId)
        {
            var renderer = paddle.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                Sprite paddleSprite = Resources.Load<Sprite>($"Sprites/{paddleSpriteName}");
                if (paddleSprite != null)
                {
                    renderer.sprite = paddleSprite;
                }
                else
                {
                    Debug.LogWarning($"can't find paddle sprite {paddleSpriteName}");
                }
            }

            if (allPlayers.ContainsKey(targetClientId)) // update PlayerInfo
            {
                PlayerInfo playerInfo = allPlayers[targetClientId];
                playerInfo.paddleSprite = Resources.Load<Sprite>($"Sprites/{paddleSpriteName}");
                playerInfo.rocketSprite = Resources.Load<Sprite>($"Sprites/{rocketSpriteName}");
            }

            break;
        }
    }
}

private void BroadcastSpaceshipSpriteToAllClients(string spriteName)
{
    foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
    {
        if(clientId == OwnerClientId) continue;
        var rpcParams = new RpcParams
        {
            // Send = new RpcSendParams { Target = new[] { clientId }}

        };
    }
}

#endregion

#region  SPACESHIP MODE
public void ActivateSpaceshipModeFromEditor()
{
    if (!IsServer) return;

    Debug.Log("Editor launched Spaceship mode.");
    StartSpaceshipMode();
}
    public void StartSpaceshipMode()
{
    Debug.Log("Mode launched");

    if (spawnedBall != null && spawnedBall.TryGetComponent(out NetworkObject netObj) && netObj.IsSpawned)
    {
        netObj.Despawn();
    }

    ToggleSpaceshipVisualsClientRpc(true);
    foreach (var kvp in allPlayers)
    {
        if (kvp.Value.isConnected)
        {
            SyncPlayerSprite(kvp.Key);
        }
    }
    StartCoroutine(DelayedSpaceshipActivation());
}

private IEnumerator DelayedSpaceshipActivation()
{
    yield return new WaitForSeconds(0.2f); 
    ActivateSpaceshipModeClientRpc(50f, 0.5f);
    
    StartCoroutine(StopSpaceshipModeAfterSeconds(30f));
}

[Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
private void ActivateSpaceshipModeClientRpc(float bulletSpeed, float fireCooldown)
{
    PaddleController[] paddles = FindObjectsByType<PaddleController>(FindObjectsSortMode.None);

    foreach (var paddle in paddles)
    {
        // Get the player info for this paddle
        var info = GetPlayerInfo(paddle.OwnerClientId);
        if (info == null)
        {
            Debug.LogWarning($"No player info found for paddle with OwnerClientId: {paddle.OwnerClientId}");
            continue;
        }

        string rocketSpriteName = info.rocketSpriteName;
        if (string.IsNullOrEmpty(rocketSpriteName))
        {
            Debug.LogWarning($"No rocket sprite name set for player {info.playerId}");
        }

        Debug.Log($"Activating spaceship mode for player {info.playerId} with rocket sprite: {rocketSpriteName}");
        paddle.EnterSpaceshipMode(bulletSpeed, fireCooldown, rocketSpriteName);
    }
}
    private IEnumerator StopSpaceshipModeAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        PaddleController[] allPaddles = FindObjectsByType<PaddleController>(FindObjectsSortMode.None);
        foreach (var paddle in allPaddles)
        {
            paddle.SetSpaceshipMode(false, null, null, 0f, 0f);

            var info = GetPlayerInfo(paddle.OwnerClientId);
            if (info != null && info.spawnPos != null)
            {
                paddle.transform.position = info.spawnPos.position;
                paddle.transform.rotation = info.spawnPos.rotation;

                var renderer = paddle.GetComponent<SpriteRenderer>();
                if (renderer != null && info.paddleSprite != null)
                {
                    renderer.sprite = info.paddleSprite;
                }
            }
        }
        // Respawn ball
        if (spawnedBall != null && !spawnedBall.GetComponent<NetworkObject>().IsSpawned)
        {
            spawnedBall.GetComponent<NetworkObject>().Spawn();
            spawnedBall.transform.position = Vector3.zero;
        }
        ToggleSpaceshipVisualsClientRpc(false);
        Debug.Log("Spaceship Mode Ended");
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    private void ToggleSpaceshipVisualsClientRpc(bool enableSpaceshipMode)
    {
        if (circles != null) circles.SetActive(!enableSpaceshipMode);
        if (stars != null) stars.SetActive(enableSpaceshipMode);
    }
#endregion

#region DOUBLE BALL MODE

public void SpawnAdditionalBall(float duration)
{
    if (!IsServer) return;
    GameObject additionalBall = Instantiate(ballPrefab, Vector3.zero, Quaternion.identity);
    var networkObject = additionalBall.GetComponent<NetworkObject>();
    networkObject.Spawn();
    StartCoroutine(DestroyBallAfterDuration(additionalBall, duration));
}

private IEnumerator DestroyBallAfterDuration(GameObject ball, float duration)
{
    yield return new WaitForSeconds(duration);
    if (ball != null)
    {
        var networkObject = ball.GetComponent<NetworkObject>();
        networkObject.Despawn();
    }
}
#endregion

#region END GAME

public void HandleGameEnd(int winnerPlayerId)
{
    if (!IsServer) return;
    BallPhysics ball = FindFirstObjectByType<BallPhysics>();
    if (ball != null)
    {
        var networkObject = ball.GetComponent<NetworkObject>();
        networkObject.Despawn();
    }
    ShowGameEndUIClientRpc(winnerPlayerId);
    Debug.Log($"NetworkGameManager handling game end for Player {winnerPlayerId}");
}

[Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
private void ShowGameEndUIClientRpc(int winnerPlayerId)
{
    if (gameEndPanel != null)
    {
        gameEndPanel.SetActive(true);
    }

    if (winnerText != null)
    {
        winnerText.text = $"Player {winnerPlayerId} has won this game!";
    }

    // Show restart button ONLY for host
    if (restartButton != null)
    {
        restartButton.gameObject.SetActive(IsHost);
    }
}

public void OnRestartButtonPressed()
{
    if (!IsHost) return;
    
    RestartGameServerRpc();
}

[Rpc(SendTo.Server)]
private void RestartGameServerRpc()
{
    if (!IsServer) return;
    
    RestartGame();
}

private void RestartGame()
{
    if (!IsServer) return;
    Debug.Log("Host is restarting the game...");
    // hide game end UI first
    HideGameEndUIClientRpc();
    // Reset scores
    if (scoreManager != null)
    {
        scoreManager.ResetScores();
    }
    
    // disconnect all clients except host
    var connectedClients = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
    foreach (var clientId in connectedClients)
    {
        if (clientId != NetworkManager.Singleton.LocalClientId)
        {
            NetworkManager.Singleton.DisconnectClient(clientId);
        }
    }
    ReturnToHostLobby();
}

[Rpc(SendTo.ClientsAndHost)]
private void HideGameEndUIClientRpc()
{
    if (gameEndPanel != null)
    {
        gameEndPanel.SetActive(false);
    }
}

public void ReturnToHostLobby()
{
    if (!IsHost) return;
    
    // Reset game state
    gameStarted = false;
    allPlayersJoined = false;
    playerCount = 1; // Host stays for game 2
    connectedPlayersCount.Value = 1;
    
    // Clear all players except host
    var hostClientId = NetworkManager.Singleton.LocalClientId;
    var playersToRemove = new List<ulong>();
    
    foreach (var kvp in allPlayers)
    {
        if (kvp.Key != hostClientId)
        {
            playersToRemove.Add(kvp.Key);
        }
    }
    
    foreach (var clientId in playersToRemove)
    {
        allPlayers.Remove(clientId);
    }
    
    // Despawn ball
    if (spawnedBall != null)
    {
        var ballNetObj = spawnedBall.GetComponent<NetworkObject>();
        if (ballNetObj != null && ballNetObj.IsSpawned)
        {
            ballNetObj.Despawn();
        }
        spawnedBall = null;
    }
    
    // Show player count selection for host again
    ShowPlayerSelectionForHost();
    
    // Hide start game panel
    if (startGamePanel != null)
    {
        startGamePanel.SetActive(false);
    }
    
    // Reset visual elements
    if (circles != null) circles.SetActive(true);
    if (stars != null) stars.SetActive(false);
    
    Debug.Log("Returned to host lobby");
}

private void SetupGameEndUI()
{
    if (restartButton != null)
    {
        restartButton.onClick.AddListener(OnRestartButtonPressed);
    }
}
#endregion
}

