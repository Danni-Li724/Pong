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
using UnityEngine.Rendering; // Manages lobby systems where players can group up before entering a game.

public class PongSessionManager : MonoBehaviour
{
    public static PongSessionManager Instance { get; private set; }
    private ISession activeSession;

    private Dictionary<string, PlayerInfo> allPlayers = new();
    // if activeSesh not null, access MaxPlayer and return null instead of error if it's null
    // similar declaration for other variables below
    public int MaxPlayers => activeSession?.MaxPlayers ?? 0;
    public bool IsHost => activeSession?.IsHost ?? false;
    public string LocalPlayerId => AuthenticationService.Instance?.PlayerId;
    // using event registration to write data
    public event Action<PlayerInfo> OnPlayerJoined;
    public event Action<PlayerInfo> OnPlayerLeft;
    public event Action OnAllPlayersReady;
    
    [Header("Dictrionary Keys")]
    private const string playerNameKey = "playerName";
    private const string readyKey = "ready";
    
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
    private async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"Signed in as {AuthenticationService.Instance.PlayerId}");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public async Task StartSessionAsHost(int maxPlayer)
    {
        var playerProperties = await GetLocalPlayerProperties();
        var options = new SessionOptions
        {
            MaxPlayers = MaxPlayers,
            IsLocked = false,
            IsPrivate = false,
            PlayerProperties = playerProperties
        }.WithRelayNetwork();
        activeSession = await MultiplayerService.Instance.CreateSessionAsync(options);
        Debug.Log($"Created session with Id {activeSession.Id}, max players: {maxPlayer}, Code: {activeSession.Code}");
    }
    
    public async Task JoinSessionByCode(string sessionCode)
    {
        activeSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(sessionCode);
        Debug.Log($"Joined session: {activeSession.Id}");
    }

    public async Task<List<ISessionInfo>> GetAvailableSessions()
    {
        var results = await MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions());
        return new List<ISessionInfo>(results.Sessions);
    }

    public async void LeaveSession()
    {
        if (activeSession == null) return;

        try
        {
            await activeSession.LeaveAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning("Error leaving session: " + e.Message);
        }
        finally
        {
            activeSession = null;
            allPlayers.Clear();
        }
    }

    public async void KickPlayer(string playerId)
    {
        if (!IsHost || activeSession == null) return;

        await activeSession.AsHost().RemovePlayerAsync(playerId);
    }
    
    ISession ActiveSession
    {
        get => (ISession)activeSession;
        set
        {
            activeSession = value;
            Debug.Log(activeSession);
        }
    }

    public async void SetPlayerReady(bool isReady) // HAS ISSUES
    {
        if (activeSession == null) return;
        var updates = new Dictionary<string, PlayerProperty>
        {
            { readyKey, new PlayerProperty(isReady.ToString(), VisibilityPropertyOptions.Member)}
        };
        //await activeSession.UpdatePlayersAsync(updates);
        CheckAllPlayersReady();
    }
    
    private async Task<Dictionary<string, PlayerProperty>> GetLocalPlayerProperties()
    {
        string playerName = await AuthenticationService.Instance.GetPlayerNameAsync();
        return new Dictionary<string, PlayerProperty>
        {
            {playerNameKey, new PlayerProperty(playerName, VisibilityPropertyOptions.Public)},
            {readyKey, new PlayerProperty("false", VisibilityPropertyOptions.Public)}
        };
    }

    private void CheckAllPlayersReady()
    {
        if (activeSession == null) return;
        int readyCount = 0;
        foreach (var player in activeSession.Players)
        {
            if (player.Properties.TryGetValue(readyKey, out var value))
            {
                if(value.Value == "true") readyCount++;
            }

            if (readyCount == activeSession.Players.Count)
            {
                Debug.Log("All Players are ready");
                OnAllPlayersReady?.Invoke();
            }
        }
    }

    public void RegisterPlayer(string playerId, PlayerInfo playerInfo)
    {
        if (!allPlayers.ContainsKey(playerId))
        {
            allPlayers[playerId] = playerInfo;
            OnPlayerJoined?.Invoke(playerInfo);
        }
    }

    public void UnregisterPlayer(string playerId)
    {
        if (allPlayers.ContainsKey(playerId))
        {
            OnPlayerLeft?.Invoke(allPlayers[playerId]);
            allPlayers.Remove(playerId);
        }
    }
}
