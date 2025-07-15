using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication; // handles player identity and login
using Unity.Services.Core; // Initializes Unity Services (must be called before using any other services like Authentication or Lobby).
                            // Bootstraps Unity's entire services ecosystem (core SDK initialization).
using Unity.Services.Multiplayer; // Provides multiplayer session management, such as allocating and joining game servers.
using Unity.Services.Lobbies; // Manages lobby systems where players can group up before entering a game.
public class SessionManager : MonoBehaviour
{
    private ISession activeSession;  // stores all information relating to the current session

    ISession ActiveSession
    {
        get => (ISession)activeSession;
        set
        {
            activeSession = value;
            Debug.Log(activeSession);
        }
    }
    
    const string playerNamePropertyKey = "playerName";

    async void Start() // Start() marked with async so that it can use 'await' for asynchronous tasks
    // 'asynchronous' means it can pause execution while waiting for tasks to finish (without freezing main thread)
    {
        try
        {
            // await tells the program to:
            // 1. start an async operation,
            // 2. wait for it to finish,
            // 3. then continue to next line of code
            await UnityServices.InitializeAsync(); // initializes unity gaming services SDK
            // Signs the player in anonymously (no username/password required). 
            // Returns a unique PlayerID usable in multiplayer, lobbies, etc.
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            // Logs the player's ID to the console
            Debug.Log($"Sign in anonymously succeeded. PlayerID: {AuthenticationService.Instance.PlayerId}");
        }
        catch (Exception e)
        // using try/catch: async operations may fail. This prevents crashing your game and also logging out errors.
        {
            // If something goes wrong (e.g., no internet), log the full error stack trace
            Debug.LogException(e);
        }
    }

    async Task<Dictionary<string, PlayerProperty>> GetPlayerProperties()
    {
        // Custom game-specific propertioes such as name, role, if is horizontal paddle etc;
        var playerName = await AuthenticationService.Instance.GetPlayerNameAsync();
        var playerNameProperty = new PlayerProperty(playerName, VisibilityPropertyOptions.Member);
        return new Dictionary<string, PlayerProperty> {{playerNamePropertyKey, playerNameProperty}}; // returns a new kvp of this property
    }

    async void StartSessionAsHost()
    {
        var playerProperties = await GetPlayerProperties(); // store player properties as a variable, then, below -
        var options = new SessionOptions
        {
            MaxPlayers = 4,
            IsLocked = false,
            IsPrivate = false,
            PlayerProperties = playerProperties, // - assigns as our PlayerProperties
        }.WithRelayNetwork(); // or WithDistributedAuthorityNetwork() to use Distributed Authority instead of relay
        // Relay centralizes the simulation and decision-making to one person (aka. the Host)
        // Distributed Auth on the other hand shifts the simulation workloads between all the participants
        // This optimises performance and also means that when the host leaves the game, the game doesn't necessarily have to end
        
        // call into the Unity Multiplayer Services API, pass in the options we have created
        // storing our active session into the property we created earlier.
        ActiveSession = await MultiplayerService.Instance.CreateSessionAsync(options); 
        // There's a lot of options and among them are Session ID and a Join Code
        Debug.Log($"Session created: {ActiveSession.Id}. Join Code: {ActiveSession.Code}");
    }

    // maybe use UniTaskVoid so that the functions are awaitable, testable and doesn't block main thread
    async void JoinSessionById(string sessionId) // pass in the ID to join session by ID
    {
        ActiveSession = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId); // Set the ID into our active session
        Debug.Log($"Session joined: {ActiveSession.Id}");
    }

    async void JoinSessionByCode(string sessionCode)
    {
        ActiveSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(sessionCode);
    }

    async void KickPlayer(string playerId)
    {
        if (!ActiveSession.IsHost) return; // only host can kick players
        await ActiveSession.AsHost().RemovePlayerAsync(playerId); // using the stored active session to remove player as host
    }

    // show player all sessions they can join
    async Task<IList<ISessionInfo>> QuerySessions()
    {
        var sessionQueryOptions = new QuerySessionsOptions();
        var results = await MultiplayerService.Instance.QuerySessionsAsync(sessionQueryOptions); // call into the query sessions async method
        // gets back query session results - has a property called Sessions which is an IList of type ISession Info
        return results.Sessions;
    }

    async void LeaveSession()
    {
        if (ActiveSession != null)
        {
            try
            {
                await ActiveSession.LeaveAsync();
            }
            catch
            {
                // ignore as we are exiting the game
            }
            finally
            {
                ActiveSession = null; // no matter what, set active session to null
            }
        }
    }
}

