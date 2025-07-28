using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Lobbies.Models;

/// <summary>
/// Connects Unity Lobby system with my NetworkGameManager logic,
/// tracks ready states and triggers StartGame() when all players are ready.
/// </summary>
public class SessionBridge : MonoBehaviour
{
    private Dictionary<string, bool> playerReadyStatus = new(); // track who's ready
    private bool gameStarted = false;

    private void Start()
    {
        // listen for lobby changes
        PongSessionManager.Instance.OnLobbyJoined += OnLobbyChanged;
        PongSessionManager.Instance.OnLobbyCreated += OnLobbyChanged;
        PongSessionManager.Instance.OnLobbyLeft += ResetState;
    }

    private void OnDestroy()
    {
        // cleanup
        if (PongSessionManager.Instance != null)
        {
            PongSessionManager.Instance.OnLobbyJoined -= OnLobbyChanged;
            PongSessionManager.Instance.OnLobbyCreated -= OnLobbyChanged;
            PongSessionManager.Instance.OnLobbyLeft -= ResetState;
        }
    }

    private void OnLobbyChanged()
    {
        RefreshPlayerList(); // refresh local state whenever lobby changes
    }

    private void RefreshPlayerList()
    {
        var lobby = PongSessionManager.Instance?.GetCurrentLobby();
        if (lobby == null) return;

        playerReadyStatus.Clear();

        foreach (var player in lobby.Players)
        {
            playerReadyStatus[player.Id] = false; // everyone starts unready
        }
    }

    /// <summary>
    /// Called when local player presses ready/unready.
    /// </summary>
    public void SetLocalPlayerReady(bool isReady)
    {
        var playerId = PongSessionManager.Instance?.LocalPlayerId;
        if (string.IsNullOrEmpty(playerId)) return;

        if (playerReadyStatus.ContainsKey(playerId))
        {
            playerReadyStatus[playerId] = isReady;
            CheckAllReady();
        }
    }

    /// <summary>
    /// Checks if everyone is ready. If so, host will trigger game start.
    /// </summary>
    private void CheckAllReady()
    {
        if (gameStarted || !PongSessionManager.Instance.IsLobbyHost) return;

        foreach (var kvp in playerReadyStatus)
        {
            if (!kvp.Value) return; // wait for everyone
        }

        Debug.Log("[Bridge] All players ready. Starting game...");
        gameStarted = true;
        NetworkGameManager.Instance?.StartGame(); // this kicks off your whole multiplayer scene logic
    }

    private void ResetState()
    {
        playerReadyStatus.Clear();
        gameStarted = false;
    }

    public bool IsPlayerReady(string playerId) =>
        playerReadyStatus.TryGetValue(playerId, out var ready) && ready;

    public IReadOnlyDictionary<string, bool> GetReadyStates() => playerReadyStatus;
}