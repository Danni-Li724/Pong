using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.UI;


public class GameManager : NetworkBehaviour
{
    [Header("Game Settings")]
    // all player/session join/leave is now handled in PongSessionManager, but this still tracks active players in the game scene
    public NetworkVariable<int> maxPlayers = new NetworkVariable<int>(2, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private NetworkVariable<int> connectedPlayersCount = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool allPlayersJoined = false; // for in-game start logic only
    private bool gameStarted = false;
    private int playerCount = 0; // only counts players in-game, not lobby
    private Dictionary<ulong, PlayerInfo> allPlayers = new Dictionary<ulong, PlayerInfo>();
    private int paddlesSpawned = 0; // tracking clients spawned for name syncing purposes
    
    // Added in Project 2
    [Header("Player Name Trackers")] 
    private string playerName = "Player";
    private Dictionary<ulong, string> clientIdToLobbyId = new();        // NEW
    private Dictionary<string, string> lobbyIdToDisplayName = new();    // NEW
    // Store names with both clientId and a simple counter for fallback
    //private Dictionary<ulong, PlayerInfo> allPlayers = new Dictionary<ulong, PlayerInfo>();
    
   // This dict keeps track of names for all clientIds (from the lobby)
    private Dictionary<ulong, string> playerNames = new Dictionary<ulong, string>();

    [Header("Script Refs")] 
    [SerializeField] private ScoreManager scoreManager;
    public static GameManager Instance { get; private set; }

    [Header("Level Objects")] public Transform leftSpawn, rightSpawn, topSpawn, bottomSpawn;
    public GameObject playerPaddlePrefab;
    public GameObject ballPrefab;
    private GameObject spawnedBall;

    [Header("Game UIs")] 
    [SerializeField] private GameObject maxScorePanel;
    [SerializeField] private GameObject startGamePanel;
    [SerializeField] private Text playerCountText;
    [SerializeField] private GameObject gameEndPanel;
    [SerializeField] private Text winnerText;
    [SerializeField] private Button restartButton;
    [SerializeField] private GameObject lobbyPanel;
    
    public event Action OnPlayerNamesUpdated; // this syncs custom names on player-side (subscribed by PlayerInventory)

    #region INITIALIZING
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // out of habit...
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public override void OnNetworkSpawn()
    {
        // session/lobby is handled outside, but this still hooks up for in-game connection events
        // if (IsServer)
        // {
        //     NetworkManager.OnClientConnectedCallback += OnClientConnected;
        //     NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
        //     paddlesSpawned = 0;
        //
        //     // Host stores their own name immediately
        //     string hostPlayerName = GetPlayerNameFromLobby();
        //     playerNames[NetworkManager.Singleton.LocalClientId] = hostPlayerName;
        //
        //     SpawnPlayerPaddle(NetworkManager.Singleton.LocalClientId, 1, hostPlayerName);
        //     playerCount = 1;
        //     connectedPlayersCount.Value = 1;
        //     SetupGameEndUI();
        //     // // Start the delayed distribution coroutine
        //     // StartCoroutine(DelayedNameDistribution());
        // }
        // else
        // {
        //     // clients send their name to the server when they spawn
        //     string clientPlayerName = GetPlayerNameFromLobby();
        //     Debug.Log($"[GameManager] Client sending name: {clientPlayerName}");
        //     SendPlayerNameToServerRpc(clientPlayerName);
        // }
        // connectedPlayersCount.OnValueChanged += OnConnectedPlayersCountChanged;
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
            paddlesSpawned = 0;
        
            // Host gets their name and stores it immediately
            string hostPlayerName = GetPlayerNameFromLobby();
            playerNames[NetworkManager.Singleton.LocalClientId] = hostPlayerName;
            Debug.Log($"[GameManager] HOST name stored: {hostPlayerName} for clientId: {NetworkManager.Singleton.LocalClientId}");
        
            SpawnPlayerPaddle(NetworkManager.Singleton.LocalClientId, 1, hostPlayerName);
            playerCount = 1;
            connectedPlayersCount.Value = 1;
            SetupGameEndUI();
        }
    
        // ALL clients (including host) should try to send their name after a delay
        StartCoroutine(SendNameAfterDelay());
    
        connectedPlayersCount.OnValueChanged += OnConnectedPlayersCountChanged;
    }
    
    // called when a new client is assigned to the in-game scene
    void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        Debug.Log($"Client connected with ID: {clientId}");
        if (clientId == NetworkManager.Singleton.LocalClientId) return;

        if (playerCount >= maxPlayers.Value)
        {
            Debug.LogWarning("Player count exceeds max player count");
            return;
        }

        playerCount++;
        connectedPlayersCount.Value = playerCount;
    
        // Use a temporary name for now - the real name will come via RPC
        string tempName = $"Player {playerCount}";
        SpawnPlayerPaddle(clientId, playerCount, tempName);
    
        AssignGoals();
        SyncPlayerSprite(clientId);
        SyncAllPlayersToClient(clientId);

        if (playerCount == maxPlayers.Value)
        {
            allPlayersJoined = true;
            EnableReadyUpButtonClientRpc();
            // // Give a bit more time for names to sync, then distribute
            // StartCoroutine(DelayedNameDistribution());
        }
        DistributeAllKnownNames();
    }

    void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;
        Debug.Log($"[GameManager] *** CLIENT CONNECTED *** ID: {clientId}");
        if (clientId == NetworkManager.Singleton.LocalClientId) return;

        if (playerCount >= maxPlayers.Value)
        {
            Debug.LogWarning("Player count exceeds max player count");
            return;
        }

        playerCount++;
        connectedPlayersCount.Value = playerCount;
    
        // Wait for the client to send their name before spawning paddle
        StartCoroutine(WaitForClientNameThenSpawn(clientId, playerCount));

        if (playerCount == maxPlayers.Value)
        {
            allPlayersJoined = true;
            EnableReadyUpButtonClientRpc();
        }
    }
    
    #endregion
    
    // #region CUSTOM PLAYER NAMES
    // [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    // private void SendPlayerNameToServerRpc(string playerName, RpcParams rpcParams = default)
    // {
    //     ulong clientId = rpcParams.Receive.SenderClientId;
    //     Debug.Log($"[GameManager] Received name from client {clientId}: {playerName}");
    //
    //     // store the name immediately
    //     if (!playerNames.ContainsKey(clientId))
    //     {
    //         playerNames[clientId] = playerName;
    //     
    //         // update PlayerInfo
    //         if (allPlayers.ContainsKey(clientId))
    //         {
    //             allPlayers[clientId].displayName = playerName;
    //         }
    //     
    //         // IMMEDIATELY distribute this specific player's name to all clients
    //         var ids = new ulong[] { clientId };
    //         var names = new FixedString128Bytes[] { new FixedString128Bytes(playerName) };
    //         ReceivePlayerNamesClientRpc(ids, names);
    //     
    //         Debug.Log($"[GameManager] Distributed name for client {clientId}: {playerName}");
    //     }
    // }
    //
    // // called by PaddleController to register names
    // public void ReceivePlayerNameFromPaddle(ulong clientId, string playerName)
    // {
    //     if (!IsServer) return;
    //
    //     Debug.Log($"[GameManager] Received name from paddle {clientId}: {playerName}");
    //
    //     if (!playerNames.ContainsKey(clientId))
    //     {
    //         playerNames[clientId] = playerName;
    //     
    //         if (allPlayers.ContainsKey(clientId))
    //         {
    //             allPlayers[clientId].displayName = playerName;
    //         }
    //     
    //         // Immediately distribute to all clients
    //         var ids = new ulong[] { clientId };
    //         var names = new FixedString128Bytes[] { new FixedString128Bytes(playerName) };
    //         ReceivePlayerNamesClientRpc(ids, names);
    //     }
    // }
    // private string GetPlayerNameFromLobby()
    // {
    //     if (PongSessionManager.Instance != null && PongSessionManager.Instance.IsInLobby)
    //     {
    //         var lobby = PongSessionManager.Instance.GetCurrentLobby();
    //         var localPlayer = lobby?.Players?.Find(p => p.Id == PongSessionManager.Instance.LocalPlayerId);
    //         if (localPlayer?.Data != null && localPlayer.Data.TryGetValue("DisplayName", out var nameData))
    //         {
    //             return nameData.Value;
    //         }
    //     }
    //     // fallback
    //     return !string.IsNullOrEmpty(PlayerSessionInfo.PlayerName) ? PlayerSessionInfo.PlayerName : "Player";
    // }
    //
    // public void RegisterLobbyIdentity(ulong clientId, string lobbyPlayerId, string displayName) 
    // {
    //     clientIdToLobbyId[clientId] = lobbyPlayerId; 
    //     lobbyIdToDisplayName[lobbyPlayerId] = displayName;
    //
    //     Debug.Log($"[NameSync] Registered: clientId={clientId}, lobbyId={lobbyPlayerId}, displayName={displayName}"); // NEW
    //
    //     // check if all players have registered
    //     if (clientIdToLobbyId.Count == maxPlayers.Value) 
    //     {
    //         SendAllPlayerNamesToClients(); 
    //     }
    // }
    // private void SendAllPlayerNamesToClients() 
    // {
    //     var clientIds = clientIdToLobbyId.Keys.ToArray(); 
    //     var names = clientIds
    //         .Select(cid => new FixedString128Bytes(lobbyIdToDisplayName[clientIdToLobbyId[cid]]))
    //         .ToArray();
    //
    //     Debug.Log("[NameSync] Sending player names to all clients: " +
    //               string.Join(", ", clientIds.Select((cid, idx) => $"[{cid}]={names[idx]}"))); 
    //
    //     ReceivePlayerNamesClientRpc(clientIds, names);
    // }
    //
    // // distribute player names to clients after delay (make sure everyone is connected)
    // private IEnumerator DelayedNameDistribution()
    // {
    //     yield return new WaitForSeconds(1f); // Give time for all clients to send their names
    //     DistributeAllKnownNames();
    // }
    //
    // // distribute what players have when they have it
    // private void DistributeAllKnownNames()
    // {
    //     if (!IsServer) return;
    //
    //     var clientIds = new List<ulong>();
    //     var names = new List<FixedString128Bytes>();
    //
    //     foreach (var kvp in playerNames)
    //     {
    //         clientIds.Add(kvp.Key);
    //         names.Add(new FixedString128Bytes(kvp.Value));
    //         Debug.Log($"[GameManager] Adding to distribution: {kvp.Key} = {kvp.Value}");
    //     }
    //
    //     if (clientIds.Count > 0)
    //     {
    //         ReceivePlayerNamesClientRpc(clientIds.ToArray(), names.ToArray());
    //     }
    // }
    //
    // /// <summary>
    // /// Apparently Rpcs can't serialize string arrays out of the box, 
    // /// </summary>
    // /// <param name="clientIds"></param>
    // /// <param name="names"></param>
    // [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    // private void ReceivePlayerNamesClientRpc(ulong[] clientIds, FixedString128Bytes[] names)
    // {
    //     for (int i = 0; i < clientIds.Length; i++)
    //     {
    //         string nameStr = names[i].ToString();
    //         playerNames[clientIds[i]] = nameStr;
    //     
    //         if (allPlayers.ContainsKey(clientIds[i]))
    //         {
    //             allPlayers[clientIds[i]].displayName = nameStr;
    //         }
    //     
    //         Debug.Log($"[NameSync] Client received: {clientIds[i]} = {nameStr}");
    //     }
    //
    //     // Force update all UI elements
    //     UpdateAllPlayerUI();
    //     OnPlayerNamesUpdated?.Invoke();
    // }
    //
    // private void UpdateAllPlayerUI()
    // {
    //     // update inventory slots
    //     foreach (var slot in FindObjectsOfType<PlayerInventorySlot>())
    //     {
    //         slot.UpdateDisplayName();
    //     }
    //
    //     // update session UI
    //     if (SessionUIManager.Instance != null)
    //     {
    //         SessionUIManager.Instance.UpdateLobbyUI();
    //     }
    // }
    // #endregion
    
     #region CUSTOM PLAYER NAMES - SIMPLIFIED VERSION

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    private void SendPlayerNameToServerRpc(string playerName, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        Debug.Log($"[GameManager] *** SERVER RECEIVED NAME *** from client {clientId}: '{playerName}'");
    
        // only update if we don't already have a better name
        bool shouldUpdate = true;
        if (playerNames.ContainsKey(clientId))
        {
            string existingName = playerNames[clientId];
            // don't overwrite a good name with "Player"
            if (!string.IsNullOrEmpty(existingName) && existingName != "Player" && playerName == "Player")
            {
                shouldUpdate = false;
                Debug.Log($"[GameManager] Keeping existing name '{existingName}' instead of generic '{playerName}'");
            }
        }
        if (shouldUpdate)
        {
            playerNames[clientId] = playerName;
        
            // update PlayerInfo if it exists
            if (allPlayers.ContainsKey(clientId))
            {
                allPlayers[clientId].displayName = playerName;
                Debug.Log($"[GameManager] Updated PlayerInfo for client {clientId} with name: '{playerName}'");
            }
        
            // immediately send this name to all clients
            SendSinglePlayerNameClientRpc(clientId, playerName);
        }
    }
    private IEnumerator SendNameAfterDelay()
    {
        // Wait for network to be fully established
        yield return new WaitForSeconds(0.5f);
    
        // Try multiple times to get the name
        string playerName = "";
        for (int attempts = 0; attempts < 5; attempts++)
        {
            playerName = GetPlayerNameFromLobby();
            if (!string.IsNullOrEmpty(playerName) && playerName != "Player")
            {
                break; // Got a real name
            }
            yield return new WaitForSeconds(0.2f);
        }
    
        Debug.Log($"[GameManager] Client sending name after delay: '{playerName}' for clientId: {NetworkManager.Singleton.LocalClientId}");
    
        // Send the name to server (even if we're the host, for consistency)
        if (NetworkManager.Singleton.IsConnectedClient)
        {
            SendPlayerNameToServerRpc(playerName);
        }
    }
    
    private IEnumerator WaitForClientNameThenSpawn(ulong clientId, int playerId)
    {
        string playerName = $"Player {playerId}"; // fallback
    
        // Wait up to 3 seconds for the client to send their name
        float waitTime = 0f;
        while (waitTime < 3f)
        {
            if (playerNames.ContainsKey(clientId))
            {
                playerName = playerNames[clientId];
                Debug.Log($"[GameManager] Got name for client {clientId}: '{playerName}'");
                break;
            }
            yield return new WaitForSeconds(0.1f);
            waitTime += 0.1f;
        }
    
        Debug.Log($"[GameManager] Spawning paddle for client {clientId} with name: '{playerName}'");
        SpawnPlayerPaddle(clientId, playerId, playerName);
        AssignGoals();
        SyncPlayerSprite(clientId);
        // Send all known names to the new client
        DistributeAllKnownNames();
    }

    // Send a single player's name to all clients
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void SendSinglePlayerNameClientRpc(ulong clientId, string playerName)
    {
        Debug.Log($"[GameManager] Received name update: Client {clientId} = {playerName}");
        
        playerNames[clientId] = playerName;
        
        if (allPlayers.ContainsKey(clientId))
        {
            allPlayers[clientId].displayName = playerName;
        }
        
        // Force update all UI immediately
        UpdateAllPlayerUI();
        OnPlayerNamesUpdated?.Invoke();
    }

    // Send all known names to all clients
    private void DistributeAllKnownNames()
    {
        if (!IsServer) return;
        
        foreach (var kvp in playerNames)
        {
            SendSinglePlayerNameClientRpc(kvp.Key, kvp.Value);
            Debug.Log($"[GameManager] Redistributing name: {kvp.Key} = {kvp.Value}");
        }
    }

    // Helper method to get player name from various sources
    private string GetPlayerNameFromLobby()
    {
        // first try SessionUIManager
        if (SessionUIManager.Instance != null && !string.IsNullOrEmpty(SessionUIManager.Instance.savedPlayerName))
        {
            Debug.Log($"[GameManager] Got name from SessionUIManager: '{SessionUIManager.Instance.savedPlayerName}'");
            return SessionUIManager.Instance.savedPlayerName;
        }
    
        // then try lobby data
        if (PongSessionManager.Instance != null && PongSessionManager.Instance.IsInLobby)
        {
            var lobby = PongSessionManager.Instance.GetCurrentLobby();
            if (lobby != null)
            {
                var localPlayer = lobby.Players?.Find(p => p.Id == PongSessionManager.Instance.LocalPlayerId);
                if (localPlayer?.Data != null && localPlayer.Data.TryGetValue("DisplayName", out var nameData))
                {
                    Debug.Log($"[GameManager] Got name from lobby: '{nameData.Value}'");
                    return nameData.Value;
                }
            }
        }
    
        // try input field directly as last resort
        if (SessionUIManager.Instance != null && !string.IsNullOrEmpty(SessionUIManager.Instance.playerNameInput.text))
        {
            string inputName = SessionUIManager.Instance.playerNameInput.text.Trim();
            Debug.Log($"[GameManager] Got name from input field: '{inputName}'");
            return inputName;
        }
    
        // Fallback
        Debug.Log("[GameManager] Using fallback name: 'Player'");
        return "Player";
    }

    // Update all player-related UI elements
    private void UpdateAllPlayerUI()
    {
        // Update inventory slots
        var inventorySlots = FindObjectsOfType<PlayerInventorySlot>();
        foreach (var slot in inventorySlots)
        {
            slot.UpdateDisplayName();
        }
        
        // Update session UI
        if (SessionUIManager.Instance != null)
        {
            SessionUIManager.Instance.UpdateLobbyUI();
        }
        
        Debug.Log($"[GameManager] Updated {inventorySlots.Length} inventory slots");
    }

    #endregion
    

    #region START GAME

    // spawns paddle and creates PlayerInfo for a joining player
    void SpawnPlayerPaddle(ulong clientId, int playerId, string displayName)
    {
        Transform spawnTransform = GetSpawnTransform(playerId);
        if (spawnTransform == null)
        {
            Debug.LogError($"No spawnPos found for player {playerId}");
            return;
        }

        PlayerInfo playerInfo = new PlayerInfo(playerId, clientId, spawnTransform, displayName);
        playerInfo.isConnected = true;
        allPlayers[clientId] = playerInfo;
        // making sure player names is stored
        if (!playerNames.ContainsKey(clientId))
        {
            playerNames[clientId] = displayName;
        }
        GameObject paddle = Instantiate(playerPaddlePrefab, spawnTransform.position, Quaternion.identity);
        NetworkObject networkObject = paddle.GetComponent<NetworkObject>();
        networkObject.SpawnAsPlayerObject(clientId);
        PaddleController paddleController = paddle.GetComponent<PaddleController>();
        if (paddleController != null)
        {
            paddleController.SetPlayerId(playerId);
            var visuals = paddle.GetComponentInChildren<PaddleVisuals>();
            if (visuals != null && playerInfo.paddleSprite != null)
            {
                visuals.SetPaddleSpriteAsDefault(playerInfo.paddleSprite);
            }
        }
        UpdateClientPlayerUIClientRpc(clientId, playerId);
        if (IsServer)
        {
            // paddlesSpawned++;
            // if (paddlesSpawned >= maxPlayers.Value) // all paddles spawned
            // {
            //     // NOW send out name mappings
            //     Debug.Log("[GameManager] All paddles spawned. Distributing names.");
            //     StartCoroutine(DelayedNameDistribution());
            // }
            SendSinglePlayerNameClientRpc(clientId, displayName);
        }
        UpdateClientPlayerUIClientRpc(clientId, playerId);
    }
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void UpdateClientPlayerUIClientRpc(ulong clientId, int playerId)
    {
        var allSlots = FindObjectsOfType<PlayerInventorySlot>();
        foreach (var slot in allSlots)
        {
            if (slot.TryAssign(clientId, playerId)) 
            {
                Debug.Log($"[GameManager] Assigned UI slot for client {clientId}, player {playerId}");
                break;
            }
        }
        
        // Force update display names after assignment
        StartCoroutine(UpdateUIAfterDelay());
    }
    
    private IEnumerator UpdateUIAfterDelay()
    {
        yield return new WaitForEndOfFrame();
        UpdateAllPlayerUI();
    }

    // still used for in-game player limit UI (lobby player selection is handled in SerssionManager)
    /*private void ShowPlayerSelectionForHost()
    {
        if (IsHost && playerCountSelectionPanel != null)
        {
            playerCountSelectionPanel.SetActive(true);
            UpdatePlayerCountDisplay();
        }
    }*/

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
        //HidePlayerCountSelectionPanel();
    }

    /*private void HidePlayerCountSelectionPanel()
    {
        if (playerCountSelectionPanel != null)
        {
            playerCountSelectionPanel.SetActive(false);
        }
    }*/

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
        // Transform[] activeSpawns =
        //     GetAllPlayers().Select(p => GetSpawnTransform(p.playerId)).Where(t => t != null).ToArray();
        var activePlayers = GetAllPlayers();
        Transform[] activeSpawns = activePlayers.Select(p => GetSpawnTransform(p.playerId)).Where(t => t != null).ToArray();
        // tracking which player IDs for clients to know which goals to score form
        HashSet<int> assignedPlayerIds = new HashSet<int>();
        for (int i = 0; i < allGoals.Length; i++)
        {
            GoalController closestGoal = null;
            float closestDistance = float.MaxValue;
            int matchedPlayerId = -1;
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
                assignedPlayerIds.Add(matchedPlayerId);
                Debug.Log($"Assigned goal at {closestGoal.transform.position} to player {matchedPlayerId}");
            }
        }
        foreach (var goal in allGoals)
        {
            // reset this goal's playerId if it's not one of the assigned
            if (!assignedPlayerIds.Contains(goal.GetGoalForPlayerId()))
            {
                // set all unused goals to -1
                goal.SetGoalForPlayerId(-1);
                Debug.Log($"Unassigned goal at {goal.transform.position} (playerId = -1)");
            }
        }
        foreach (var goal in allGoals)
        {
            Debug.Log($"GOAL {goal.gameObject.name} assigned to playerId {goal.GetGoalForPlayerId()}");
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

    // called by PaddleController when a player is ready
    public void MarkPlayerReady(ulong clientId)
    {
        if (!IsServer) return;
        if (allPlayers.ContainsKey(clientId))
        {
            allPlayers[clientId].isReady = true;
            if (AllPlayersReady())
            {
                ShowMaxScorePanelClientRpc();
            }
        }

        ;
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void ShowMaxScorePanelClientRpc()
    {
        if (IsHost)
        {
            if (maxScorePanel != null)
            {
                maxScorePanel.SetActive(true);
            }
        }
    }

    // delegates boons and sprite syncing, still an in-game concern
    public void StartBoonSelection()
    {
        HideLobbyClientRpc();
        if (!IsServer) return;
        foreach (var kvp in allPlayers)
        {
            if (kvp.Value.isConnected)
            {
                SyncPlayerSprite(kvp.Key);
            }
        }
        BoonManager.Instance.StartBoonSelection();
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void HideLobbyClientRpc()
    {
        lobbyPanel.SetActive(false);
    }

    public void OnAllBoonsSelected()
    {
        if (!IsServer) return;
        ShowStartGameButtonClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void ShowStartGameButtonClientRpc()
    {
        if (IsHost && startGamePanel != null)
        {
            startGamePanel.SetActive(true);
        }
    }

    public void OnStartGameButtonPressed()
    {
        if (!IsHost) return;
        StartGameServerRpc();
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    private void StartGameServerRpc()
    {
        if (!IsServer) return;
        StartGame();
    }

    public void StartGame()
    {
        if (gameStarted) return;
        gameStarted = true;
        spawnedBall = Instantiate(ballPrefab, Vector3.zero, Quaternion.identity);
        spawnedBall.GetComponent<NetworkObject>().Spawn();
        HideStartGameButtonClientRpc();
        Debug.Log("Game officially started!");
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

    // PlayerInfo and look up helpers!
    public List<PlayerInfo> GetOtherPlayers(ulong clientId)
    {
        return allPlayers.Values.Where(p => p.clientId != clientId && p.isConnected).ToList();
    }

    public PlayerInfo GetPlayerInfo(ulong clientId)
    {
        return allPlayers.ContainsKey(clientId) ? allPlayers[clientId] : null;
    }

    public List<PlayerInfo> GetAllPlayers()
    {
        return allPlayers.Values.Where(p => p.isConnected).ToList();
    }

    #endregion

    #region SPRITE SYNCING

    // called any time a player's sprite or selection changes - propagates to all clients
    private void SyncPlayerSprite(ulong clientId)
    {
        if (!allPlayers.ContainsKey(clientId)) return;
        PlayerInfo playerInfo = allPlayers[clientId];
        SyncPlayerSpriteClientRpc(clientId, playerInfo.paddleSpriteName, playerInfo.rocketSpriteName);
    }

    // called when a new player joins, to update all their visuals
    private void SyncAllPlayersToClient(ulong targetClientId)
    {
        foreach (var kvp in allPlayers)
        {
            if (kvp.Value.isConnected)
            {
                SyncPlayerSpriteToSpecificClientRpc(
                    kvp.Key,
                    kvp.Value.paddleSpriteName,
                    kvp.Value.rocketSpriteName,
                    RpcTarget.Single(targetClientId, RpcTargetUse.Temp)
                );
            }
        }

        SendMaxPlayersToClientRpc(maxPlayers.Value, targetClientId);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SendMaxPlayersToClientRpc(int maxPlayersValue, ulong targetClientId)
    {
        maxPlayers.Value = maxPlayersValue; // update local variable for UI
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void SyncPlayerSpriteClientRpc(ulong targetClientId, string paddleSpriteName, string rocketSpriteName)
    {
        ApplyPlayerSprite(targetClientId, paddleSpriteName, rocketSpriteName);
    }

    [Rpc(SendTo.SpecifiedInParams, Delivery = RpcDelivery.Reliable)]
    private void SyncPlayerSpriteToSpecificClientRpc(ulong targetClientId, string paddleSpriteName,
        string rocketSpriteName, RpcParams rpcParams = default)
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

                if (allPlayers.ContainsKey(targetClientId))
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

    #region SPACESHIP MODE

    public void ActivateSpaceshipModeFromEditor()
    {
        if (!IsServer) return;
        Debug.Log("Editor launched Spaceship mode.");
        StartSpaceshipMode();
    }

    public void StartSpaceshipMode()
    {
        Debug.Log("Spaceship Mode launched");
        if (spawnedBall != null && spawnedBall.TryGetComponent(out NetworkObject netObj) && netObj.IsSpawned)
        {
            netObj.Despawn();
        }

        AudioManager.instance.ActivateSpaceshipModeMusic();
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

        if (spawnedBall == null || !spawnedBall.GetComponent<NetworkObject>().IsSpawned)
        {
            spawnedBall = Instantiate(ballPrefab, Vector3.zero, Quaternion.identity);
            spawnedBall.GetComponent<NetworkObject>().Spawn();
        }
        else
        {
            spawnedBall.transform.position = Vector3.zero;
        }

        AudioManager.instance.EndSpaceshipModeMusic();

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
            // exit spaceship mode
            paddle.SetSpaceshipMode(false, null, null, 0f, 0f);

            // reset position and rotation
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
            if (networkObject.IsSpawned)
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
            if (networkObject.IsSpawned)
                networkObject.Despawn();
        }
        ShowGameEndUIClientRpc(winnerPlayerId);
        Debug.Log($"GameManager handling game end for Player {winnerPlayerId}");
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void ShowGameEndUIClientRpc(int winnerPlayerId)
    {
        if (gameEndPanel != null)
            gameEndPanel.SetActive(true);

        if (winnerText != null)
            winnerText.text = $"Player {winnerPlayerId} has won this game!";

        if (restartButton != null)
            restartButton.gameObject.SetActive(IsHost);
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
        HideGameEndUIClientRpc();

        // reset scores
        if (scoreManager != null)
            scoreManager.ResetScores();

        // disconnect all clients except host
        var connectedClients = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
        foreach (var clientId in connectedClients)
        {
            if (clientId != NetworkManager.Singleton.LocalClientId)
                NetworkManager.Singleton.DisconnectClient(clientId);
        }
        ReturnToHostLobby();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void HideGameEndUIClientRpc()
    {
        if (gameEndPanel != null)
            gameEndPanel.SetActive(false);
    }

    public void ReturnToHostLobby()
    {
        if (!IsHost) return;
        // reset game state and all player data
        gameStarted = false;
        allPlayersJoined = false;
        playerCount = 0;
        connectedPlayersCount.Value = 0;
        allPlayers.Clear();

        // re-register host as a player
        ulong hostClientId = NetworkManager.Singleton.LocalClientId;
        if (!allPlayers.ContainsKey(hostClientId))
        {
            if (!playerNames.ContainsKey(NetworkManager.Singleton.LocalClientId))
                playerNames[NetworkManager.Singleton.LocalClientId] = PlayerSessionInfo.PlayerName;

            SpawnPlayerPaddle(NetworkManager.Singleton.LocalClientId, 1, PlayerSessionInfo.PlayerName);
            playerCount = 1;
            connectedPlayersCount.Value = 1;
        }
        else
        {
            Debug.Log("Host paddle already exists.");
        }
        playerCount = 1;
        connectedPlayersCount.Value = 1;

        // remove all players except host
        var playersToRemove = new List<ulong>();
        foreach (var kvp in allPlayers)
        {
            if (kvp.Key != hostClientId)
                playersToRemove.Add(kvp.Key);
        }
        foreach (var clientId in playersToRemove)
            allPlayers.Remove(clientId);

        // despawn ball if exists
        if (spawnedBall != null)
        {
            var ballNetObj = spawnedBall.GetComponent<NetworkObject>();
            if (ballNetObj != null && ballNetObj.IsSpawned)
                ballNetObj.Despawn();
            spawnedBall = null;
        }

        // re-show host selection UI and reset visual effects
        // ShowPlayerSelectionForHost();
        if (startGamePanel != null)
            startGamePanel.SetActive(false);

        VisualEventsManager.Instance.ToggleVisualsClientRpc("circles");
        Debug.Log("Returned to host lobby");
    }

    private void SetupGameEndUI()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartButtonPressed);
    }
    #endregion
}
