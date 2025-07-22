using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication; // handles player identity and login
using Unity.Services.Core; // Initializes Unity Services (must be called before using any other services like Authentication or Lobby).
// Bootstraps Unity's entire services ecosystem (core SDK initialization).
using Unity.Services.Multiplayer; // Provides multiplayer session management, such as allocating and joining game servers.
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine.Rendering; // Manages lobby systems where players can group up before entering a game.
using Unity.Netcode;
using Unity.Netcode.Transports.UTP; // for network communication


public class PongSessionManager : MonoBehaviour
{
    [Header("Session Properties")]
    public static PongSessionManager Instance { get; private set; }
    private ISession activeSession;
    private Lobby currentLobby;
    public string SessionCode => activeSession?.Code;
    public bool IsSessionActive => activeSession != null;
    // session settings
    [SerializeField] private int defaultMaxPlayers = 4;
    [SerializeField] private bool autoStartNetcode = true; // To automatically start Netcode when session starts

    [Header("Player Properties")]
    private Dictionary<string, PlayerInfo> allPlayers = new();
    private Dictionary<string, bool> playerReadyStatus = new();
    /// <summary>
    /// Dictionary Keys
    /// </summary>
    private const string playerNameKey = "playerName";
    private const string readyKey = "ready";
    private const string playerDataKey = "playerData";
    public int MaxPlayers => activeSession?.MaxPlayers ?? 0;  // if activeSesh not null, access MaxPlayer and return null instead of error if it's null
    public bool IsHost => activeSession?.IsHost ?? false; // same here
    public string LocalPlayerId => AuthenticationService.Instance?.PlayerId;
    
    /// <summary>
    /// Session Events that manages the session and uses event registration to write data
    /// </summary>
    public event Action<PlayerInfo> OnPlayerJoined;
    public event Action<PlayerInfo> OnPlayerLeft;
    public event Action OnAllPlayersReady;
    public event Action<string> OnSessionCreated;
    public event Action<string> OnSessionJoined;
    public event Action OnSessionLeft;
    public event Action<string> OnError;
    
    [Header("Lobby & Heartbeat")]
    private const float LOBBY_HEARTBEAT_INTERVAL = 15f;
    private float lobbyHeartbeatTimer;
    
    #region Initialization
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeServices(); // initialize here rather than start
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async void InitializeServices()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"Signed in as {AuthenticationService.Instance.PlayerId}");
            SetupEvents();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            OnError?.Invoke($"Failed to initialize: {e.Message}");
        }
    }

    private void SetupEvents()
    {
        
    }
#endregion

#region Session

public async Task<bool> StartSessionAsHost(int maxPlayers = -1, string sessionName = null)
{
    try
    {
        // first, create lobby
        var lobbyCreated = await CreateLobby(maxPlayers, sessionName ?? "Pong!"); // create lobby with a name
        if (!lobbyCreated) return false; // exit if didn't create successfully

        var playerProperties = await GetLocalPlayerProperties();
        // create the SessionOptions data
        var options = new SessionOptions
        {
            MaxPlayers = maxPlayers, // set max player
            IsLocked = false, // not locked so new players can join
            IsPrivate = false, // public so that it can be found
            PlayerProperties = playerProperties // include local player's data
        }.WithRelayNetwork(); // use Unity Relay for networking
            
        // Create the actual multiplayer session
        activeSession = await MultiplayerService.Instance.CreateSessionAsync(options);
            
        // register local player (host) to player tracking
        RegisterLocalPlayer();
            
        Debug.Log($"Created session with Id {activeSession.Id}, max players: {maxPlayers}, Code: {activeSession.Code}");
        OnSessionCreated?.Invoke(activeSession.Code); // tell listeners that session was created
            
        // Begin network communication as session starts
        if (autoStartNetcode)
        {
            await StartNetcodeHost(); // Initialize as network host
        }
            
        return true; 
    }
    catch (Exception e)
    {
        Debug.LogException(e);
        return false;
    }
}
private async Task StartNetcodeHost()
{
    try
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[SessionManager] NetworkManager not found");
            return;
        }
        // Start as host
        NetworkManager.Singleton.StartHost();
        Debug.Log("[SessionManager] Started Netcode as Host");
    }
    catch (Exception e)
    {
        Debug.LogError($"[SessionManager] Failed to start Netcode host: {e.Message}");
        OnError?.Invoke($"Failed to start host: {e.Message}");
    }
}

public async void LeaveSession()
{
    if (activeSession == null) return;
        
    try
    {
        // Stop Netcode
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
            
        // Leave session
        await activeSession.LeaveAsync();
            
        // Leave lobby
        if (currentLobby != null)
        {
            await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, LocalPlayerId);
            currentLobby = null;
        }
            
        Debug.Log("[SessionManager] Left session successfully");
        OnSessionLeft?.Invoke();
    }
    catch (Exception e)
    {
        Debug.LogWarning($"[SessionManager] Error leaving session: {e.Message}");
    }
    finally
    {
        // Clean up
        activeSession = null;
        currentLobby = null;
        allPlayers.Clear();
        playerReadyStatus.Clear();
    }
}
#endregion

#region Lobby 

public async Task<bool> CreateLobby(int maxPlayers, string lobbyName)
{
    try
    {
        var createLobbyOptions =
            new CreateLobbyOptions // param class in the Lobby namespace, where I can create my custom params
            {
                IsPrivate = false, // public and shows up in query results
                Player = await GetLocalLobbyPlayer(),
                Data = new Dictionary<string, DataObject>
                {
                    { "GameMode", new DataObject(DataObject.VisibilityOptions.Public, "Pong") } // game data
                }
            };
        currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions); // create the lobby with all the information
        lobbyHeartbeatTimer = 0f;
        Debug.Log($"Created lobby: {lobbyName}");
        return true;
    }
    catch (Exception e)
    {
        Debug.LogException(e);
        return false;
    }
}

private async Task<Player> GetLocalLobbyPlayer() // Player: information about the player creating the lobby (also part of lobby namespace)
{
    string playerName = await GetPlayerName(); 
    return new Player
    {
        Data = new Dictionary<string, PlayerDataObject>
        {
            { playerNameKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName) },
            { readyKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, "false") }
        }
    };
}
private async Task<bool> JoinLobbyByCode(string sessionCode)
{
    try
    {
        var queryResponse = await LobbyService.Instance.QueryLobbiesAsync(); // stores queries lobbies so players can find the one for their session
        foreach (var lobby in queryResponse.Results)
        {
            if (lobby.Data.ContainsKey("SessionCode") && lobby.Data["SessionCode"].Value == sessionCode) // if code matches
            {
                var player = GetLocalLobbyPlayer();
                // hmmmmm....
                //currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, new JoinLobbyByIdOptions { Player = PlayerInfo.playerId});
                return true;
            }
        }
        Debug.LogWarning("Could not find lobby for session code");
        return false;
    }
    catch (Exception e)
    {
        Debug.LogException(e);
        return false;
    }
}
private async Task SendLobbyHeartbeat()
{
    try
    {
        await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
    }
    catch (Exception e)
    {
        Debug.LogWarning($"[SessionManager] Lobby heartbeat failed: {e.Message}");
    }
}

private void Update()
{
    // handles heartbeat to keep lobby alive
    if (currentLobby != null && IsHost) // only host send heartbeats
    {
        lobbyHeartbeatTimer += Time.deltaTime; 
        if (lobbyHeartbeatTimer >= LOBBY_HEARTBEAT_INTERVAL)
        {
            lobbyHeartbeatTimer = 0f;
            SendLobbyHeartbeat();
        }
    }
}
#endregion

#region Player Management
private void RegisterLocalPlayer()
{
    if (activeSession?.CurrentPlayer != null)
    {
        var localPlayer = activeSession.CurrentPlayer;
        //var playerInfo = CreatePlayerInfoFromSessionPlayer(localPlayer);
        //RegisterPlayer(localPlayer.Id, playerInfo);
    }
}
    
public void RegisterPlayer(string playerId, PlayerInfo playerInfo)
{
    if (!allPlayers.ContainsKey(playerId))
    {
        allPlayers[playerId] = playerInfo;
        playerReadyStatus[playerId] = false;
        OnPlayerJoined?.Invoke(playerInfo);
        Debug.Log($"[SessionManager] Player {playerInfo.playerId} joined"); // maybe need to refactor to PlayerInfo.player name
    }
}
#endregion

#region Helper Methods

private async Task<Dictionary<string, PlayerProperty>> GetLocalPlayerProperties()
    {
        string playerName = await GetPlayerName();
        return new Dictionary<string, PlayerProperty>
        {
            { playerNameKey, new PlayerProperty(playerName, VisibilityPropertyOptions.Public) },
            { readyKey, new PlayerProperty("false", VisibilityPropertyOptions.Public) }
        };
    }
    
    private async Task<string> GetPlayerName()
    {
        try
        {
            return await AuthenticationService.Instance.GetPlayerNameAsync() ?? $"Player_{LocalPlayerId?.Substring(0, 4)}";
        }
        catch
        {
            return $"Player_{LocalPlayerId?.Substring(0, 4) ?? "Unknown"}";
        }
    }
    
    public async void KickPlayer(string playerId)
    {
        if (!IsHost || activeSession == null) return;
        
        try
        {
            await activeSession.AsHost().RemovePlayerAsync(playerId);
            Debug.Log($"Kicked player {playerId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to kick player: {e.Message}");
            OnError?.Invoke($"Failed to kick player: {e.Message}");
        }
    }
    
    public List<PlayerInfo> GetConnectedPlayers()
    {
        return new List<PlayerInfo>(allPlayers.Values);
    }
    
    public PlayerInfo GetPlayerInfo(string playerId)
    {
        return allPlayers.TryGetValue(playerId, out var info) ? info : null;
    }
    
    public bool IsPlayerReady(string playerId)
    {
        return playerReadyStatus.TryGetValue(playerId, out var ready) ? ready : false;
    }
    private void OnDestroy()
    {
        if (Instance == this)
        {
           LeaveSession();
        }
    }
    
    #endregion
    


    // public async Task StartSessionAsHost(int maxPlayer)
    // {
    //     var playerProperties = await GetLocalPlayerProperties();
    //     var options = new SessionOptions
    //     {
    //         MaxPlayers = MaxPlayers,
    //         IsLocked = false,
    //         IsPrivate = false,
    //         PlayerProperties = playerProperties
    //     }.WithRelayNetwork();
    //     activeSession = await MultiplayerService.Instance.CreateSessionAsync(options);
    //     Debug.Log($"Created session with Id {activeSession.Id}, max players: {maxPlayer}, Code: {activeSession.Code}");
    // }
    //
    // public async Task JoinSessionByCode(string sessionCode)
    // {
    //     activeSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(sessionCode);
    //     Debug.Log($"Joined session: {activeSession.Id}");
    // }
    //
    // public async Task<List<ISessionInfo>> GetAvailableSessions()
    // {
    //     var results = await MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions());
    //     return new List<ISessionInfo>(results.Sessions);
    // }
    //
    // public async void LeaveSession()
    // {
    //     if (activeSession == null) return;
    //
    //     try
    //     {
    //         await activeSession.LeaveAsync();
    //     }
    //     catch (Exception e)
    //     {
    //         Debug.LogWarning("Error leaving session: " + e.Message);
    //     }
    //     finally
    //     {
    //         activeSession = null;
    //         allPlayers.Clear();
    //     }
    // }
    //
    // public async void KickPlayer(string playerId)
    // {
    //     if (!IsHost || activeSession == null) return;
    //
    //     await activeSession.AsHost().RemovePlayerAsync(playerId);
    // }
    //
    // ISession ActiveSession
    // {
    //     get => (ISession)activeSession;
    //     set
    //     {
    //         activeSession = value;
    //         Debug.Log(activeSession);
    //     }
    // }
    //
    // public async void SetPlayerReady(bool isReady) // HAS ISSUES
    // {
    //     if (activeSession == null) return;
    //     var updates = new Dictionary<string, PlayerProperty>
    //     {
    //         { readyKey, new PlayerProperty(isReady.ToString(), VisibilityPropertyOptions.Member)}
    //     };
    //     //await activeSession.UpdatePlayersAsync(updates);
    //     CheckAllPlayersReady();
    // }
    //
    // private async Task<Dictionary<string, PlayerProperty>> GetLocalPlayerProperties()
    // {
    //     string playerName = await AuthenticationService.Instance.GetPlayerNameAsync();
    //     return new Dictionary<string, PlayerProperty>
    //     {
    //         {playerNameKey, new PlayerProperty(playerName, VisibilityPropertyOptions.Public)},
    //         {readyKey, new PlayerProperty("false", VisibilityPropertyOptions.Public)}
    //     };
    // }
    //
    // private void CheckAllPlayersReady()
    // {
    //     if (activeSession == null) return;
    //     int readyCount = 0;
    //     foreach (var player in activeSession.Players)
    //     {
    //         if (player.Properties.TryGetValue(readyKey, out var value))
    //         {
    //             if(value.Value == "true") readyCount++;
    //         }
    //
    //         if (readyCount == activeSession.Players.Count)
    //         {
    //             Debug.Log("All Players are ready");
    //             OnAllPlayersReady?.Invoke();
    //         }
    //     }
    // }
    //
    // public void RegisterPlayer(string playerId, PlayerInfo playerInfo)
    // {
    //     if (!allPlayers.ContainsKey(playerId))
    //     {
    //         allPlayers[playerId] = playerInfo;
    //         OnPlayerJoined?.Invoke(playerInfo);
    //     }
    // }
    //
    // public void UnregisterPlayer(string playerId)
    // {
    //     if (allPlayers.ContainsKey(playerId))
    //     {
    //         OnPlayerLeft?.Invoke(allPlayers[playerId]);
    //         allPlayers.Remove(playerId);
    //     }
    // }
}
