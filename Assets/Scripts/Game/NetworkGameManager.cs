using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// The most involved and probably chaotic script in this project, which handled too many things...
/// If I had more time to optimize this project, I would have: 1. Split this script into smaller manager classes
/// 2. Created the levelInfo as you have suggested. 3. Have a separate event driven UI manager (like my VisualEventsManager)
/// for all UI side of things.
///
/// This Manager:
/// - Handles multiplayer game setup, player connection, sprite syncing and game states (such as the spaceship mode),
/// also spawning paddles and balls; assigns, tracks and sends out playerIds.
/// - Handles game end/restart logic. Calls subsequent managers to perform functions.
/// Basically a central hub for all networked flow.
/// </summary>
public class NetworkGameManager : NetworkBehaviour
{
    [Header("Game Settings")]
    // Synced so that all clients know how many total players are allowed.
    public NetworkVariable<int> maxPlayers = new NetworkVariable<int>(2, 
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // Keeps all clients in sync about how many players are currently connected.
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
            playerCount = 1; // register host
            connectedPlayersCount.Value = 1;
            ShowPlayerSelectionForHost();
            SetupGameEndUI(); // initializing end game button
        }
        connectedPlayersCount.OnValueChanged += OnConnectedPlayersCountChanged;
    }

    // Called on server when a new client connects
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
            // The order of execution here/below is significant, ensuring that players are spawned, registered and sprite synced in that order
            SpawnPlayerPaddle(clientId, playerCount);
            AssignGoals();
            SyncPlayerSprite(clientId); // IMMEDIATELY syncing sprites for new players who joined
            SyncAllPlayersToClient(clientId); // Also sync all existing players to the new client
            
            // NOW show button
            if (playerCount == maxPlayers.Value)
            {
                allPlayersJoined = true;
                EnableReadyUpButtonClientRpc();
            }
        }
    // Server handles player disconnects
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
            
            // hide ready button if not all players are connected, preventing players form readying up too early
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
        // Assign spawn location and instantiate paddle
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
        // * I abandoned the spawnPos rotation spawning during later debugging
        //GameObject paddle = Instantiate(playerPaddlePrefab, spawnTransform.position, spawnTransform.rotation);
        GameObject paddle = Instantiate(playerPaddlePrefab, spawnTransform.position, Quaternion.identity);
        NetworkObject networkObject = paddle.GetComponent<NetworkObject>();
        networkObject.SpawnAsPlayerObject(clientId);
        PaddleController paddleController = paddle.GetComponent<PaddleController>();
        if (paddleController != null)
        {
            paddleController.SetPlayerId(playerId);
            // loading custom paddle sprites onto PaddleVisuals
            var visuals = paddle.GetComponentInChildren<PaddleVisuals>();
            if (visuals != null && playerInfo.paddleSprite != null)
            {
                visuals.SetPaddleSpriteAsDefault(playerInfo.paddleSprite);
            }
        }
    }
    // Only the host can select total player count because this decision can only be made by one person to avoid conflict
    private void ShowPlayerSelectionForHost()
    {
        if (IsHost && playerCountSelectionPanel != null)
        {
            playerCountSelectionPanel.SetActive(true);
            UpdatePlayerCountDisplay();
        }
    }
    // Useful UI to tell players how many have connected using a networked variable
    private void OnConnectedPlayersCountChanged(int previousValue, int newValue)
    {
        UpdatePlayerCountDisplay();
    }
    // shown in a fraction:)
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

    // From host to server: setting player count. This is important and needs to be reliable (same for most function calls in this class)
    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)] 
    public void SetMaxPlayersServerRpc(int playerCount)
    {
        if (!IsServer) return;
        if (playerCount >= 2 && playerCount <= 4)
        {
            maxPlayers.Value = playerCount;
            Debug.Log("Player count in this game set to " + playerCount);
        }
    }
    // Show ready up button for all clients when all joined
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
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
        Transform[] activeSpawns = GetAllPlayers()
            .Select(p => GetSpawnTransform(p.playerId))
            .Where(t => t != null)
            .ToArray(); // fix: only using spawn points for players who are actually in the game
        for (int i = 0; i < allGoals.Length; i++)
        {
            GoalController closestGoal = null;
            float closestDistance = float.MaxValue;
            int matchedPlayerId = -1;
            // setting goal to player by checking distance
            for (int playerId = 1; playerId <= activeSpawns.Length; playerId++)
            {
                float dist = Vector3.Distance(allGoals[i].transform.position, activeSpawns[playerId - 1].position);
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
    
    // Called by server when all players are ready: sets of buttons for player excluding themselves will be spawned
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
    
    // When all players have readied up, tell BoonManager to start the Boon selection process/System
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
    
    // shows start button after boons
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void ShowStartGameButtonClientRpc()
    {
        if (IsHost && startGamePanel != null)
        {
            startGamePanel.SetActive(true);
        }
    }
    
    // Called by pressing the start game button (Host only)
    public void OnStartGameButtonPressed()
    {
        if (!IsHost) return;
        StartGameServerRpc();
    }
    
    // Host tells the server to start the game
    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)] 
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
                var paddleVisuals = paddle.GetComponentInChildren<PaddleVisuals>();
                if (paddleVisuals != null)
                {
                    Sprite paddleSprite = Resources.Load<Sprite>($"Sprites/{paddleSpriteName}");
                    if (paddleSprite != null)
                    {
                        paddleVisuals.SetPaddleSpriteAsDefault(paddleSprite);
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

    AudioManager.instance.ActivateSpaceshipModeMusic(); // controls both music and visual now
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

        EndSpaceshipModeClientRpc();
    
        // Respawn ball
        if (spawnedBall != null && !spawnedBall.GetComponent<NetworkObject>().IsSpawned)
        {
            spawnedBall.GetComponent<NetworkObject>().Spawn();
            spawnedBall.transform.position = Vector3.zero;
        }
        AudioManager.instance.EndSpaceshipModeMusic();
    
        // Sync all player sprites to ensure correct personalized sprites are restored
        foreach (var kvp in allPlayers)
        {
            if (kvp.Value.isConnected)
            {
                SyncPlayerSprite(kvp.Key);
            }
        }
    
        Debug.Log("Spaceship Mode Ended");
    }
    
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void EndSpaceshipModeClientRpc()
    {
        PaddleController[] allPaddles = FindObjectsByType<PaddleController>(FindObjectsSortMode.None);
        foreach (var paddle in allPaddles)
        {
            // Exit spaceship mode
            paddle.SetSpaceshipMode(false, null, null, 0f, 0f);

            // Reset position and rotation
            var info = GetPlayerInfo(paddle.OwnerClientId);
            if (info != null && info.spawnPos != null)
            {
                paddle.transform.position = info.spawnPos.position;
                paddle.transform.rotation = info.spawnPos.rotation;
            }
        }
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

// Reset player count tracking
    playerCount = 0;
    connectedPlayersCount.Value = 0;
    allPlayers.Clear();

// Re-register host as a player so that maxPlayerCount can be reached
    ulong hostClientId = NetworkManager.Singleton.LocalClientId;
    if (!allPlayers.ContainsKey(hostClientId))
    {
        SpawnPlayerPaddle(hostClientId, 1);
        playerCount = 1;
        connectedPlayersCount.Value = 1;
    }
    else
    {
        Debug.Log("Host paddle already exists.");
    }
    playerCount = 1;
    connectedPlayersCount.Value = 1;
    
    // Clear all players except host
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
    VisualEventsManager.Instance.ToggleVisualsClientRpc("circles");

    
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

