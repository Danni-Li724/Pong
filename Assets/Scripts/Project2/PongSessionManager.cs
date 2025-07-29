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
    
    // todo: add warning msg if ping is bad
    // todo: web build
    // todo: pin & rtt?
    [Header("Session Settings")]
    public static PongSessionManager Instance { get; private set; } // singleton so any script can easily call PongSessionManager.Instance

    private Lobby currentLobby;         // stores the currently joined or hosted lobby
    private Allocation relayAllocation; // stores host's relay server info (used to start netcode host)

    public string SessionCode => currentLobby?.LobbyCode; // safely returns the lobby code (used for players to join)

    // checks if this player is the host by comparing local ID to lobby host ID
    public bool IsLobbyHost => currentLobby?.HostId == AuthenticationService.Instance.PlayerId; 
    
    // returns the current lobby object (for bridge or UI access)
    public Lobby GetCurrentLobby() => currentLobby;

    // returns the player's Unity-assigned ID
     public string LocalPlayerId => AuthenticationService.Instance?.PlayerId;


    public bool IsInLobby => currentLobby != null; // quick check if player's currently inside a lobby

    // these events help UI and other systems respond to lobby state changes
    public event Action OnLobbyCreated;
    public event Action OnLobbyJoined;
    public event Action OnLobbyLeft;
    public event Action<string> OnError;

    private float lobbyHeartbeatTimer;         // timer used to send periodic heartbeats
    private const float LobbyHeartbeatInterval = 15f; // how often host should ping to keep lobby alive
    private float lobbyRefreshInterval = 2f; // used to refresh the lobby on host's side so that they are up to date with lobby
    private float lobbyRefreshTimer = 0f;

    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); 
            return;
        }
        Instance = this; 
        DontDestroyOnLoad(gameObject); 

        await InitializeUnityServices(); // boot up auth & relay sevices
    }

    private async Task InitializeUnityServices()
    {
        try
        {
            await UnityServices.InitializeAsync(); // starts up Unity’s whole backend service framework
            await AuthenticationService.Instance.SignInAnonymouslyAsync(); // logs the player in anonymously

            Debug.Log("Signed in as: " + AuthenticationService.Instance.PlayerId);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to initialize Unity services: " + e);
            OnError?.Invoke("Service init error: " + e.Message);
        }
    }

    public async Task CreateLobbyAsync(string lobbyName, int maxPlayers)
    {
        try
        {
            // create a relay allocation (host reserves a spot for players to connect through Unity Relay)
            relayAllocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1); 
            // -1 as host doesn't count in player connection slots

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(relayAllocation.AllocationId); 
            // this join code will be given to other players so they can connect to this relay allocation

            // now build metadata for the lobby — this is where we embed the join code so players can access it
            var lobbyData = new Dictionary<string, DataObject>
            {
                {
                    "JoinCode", 
                    new DataObject(
                        DataObject.VisibilityOptions.Member, // only players in the lobby can see this key
                        joinCode                             // the actual relay join code for this lobby
                    )
                }
            };

            // set up the host player and pass in the metadata
            var createOptions = new CreateLobbyOptions
            {
                IsPrivate = false,                               // public lobby, will show up in lobby queries
                Player = new Player(id: AuthenticationService.Instance.PlayerId), // identify host player in lobby
                Data = lobbyData                                 // attach the relay join code as part of lobby data
            };

            // actually create the lobby
            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createOptions);

            Debug.Log("Lobby created. Code: " + currentLobby.LobbyCode);

            // start the host via Netcode, using the relay allocation we just made
            StartHostWithRelay(relayAllocation);

            OnLobbyCreated?.Invoke(); // notify anything listening (e.g., SessionUIManager)
        }
        catch (Exception e)
        {
            Debug.LogError("Error creating lobby: " + e);
            OnError?.Invoke("Lobby creation failed: " + e.Message);
        }
    }

    public async Task JoinLobbyByCodeAsync(string code)
    {
        try
        {
            // build options for the join request, pass in this player
            var joinOptions = new JoinLobbyByCodeOptions
            {
                Player = new Player(id: AuthenticationService.Instance.PlayerId)
            };

            // try joining the lobby with the code
            currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code, joinOptions);

            string joinCode = currentLobby.Data["JoinCode"].Value; 
            // grab the relay join code embedded in the lobby data

            // use relay join code to join the relay server
            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            // start the client using the relay data
            StartClientWithRelay(joinAllocation);

            OnLobbyJoined?.Invoke(); // notify UI or anything else listening
        }
        catch (Exception e)
        {
            Debug.LogError("Error joining lobby: " + e);
            OnError?.Invoke("Failed to join lobby: " + e.Message);
        }
    }

    private void StartHostWithRelay(Allocation allocation)
    {
        // grab Unity Transport (which lets Netcode communicate via Relay)
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        var endpoint = allocation.RelayServer.IpV4; // IP of the relay server

        // plug relay allocation data into UnityTransport for the host
        transport.SetHostRelayData(
            endpoint,
            (ushort)allocation.RelayServer.Port,     // port for relay server
            allocation.AllocationIdBytes,            // allocation ID as byte[]
            allocation.Key,                          // encryption key for this session
            allocation.ConnectionData                // connection info for this host
        );

        NetworkManager.Singleton.StartHost(); // now the host start the actual Netcode session
        Debug.Log("Started Netcode Host with Relay");
    }

    private void StartClientWithRelay(JoinAllocation joinAllocation)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        // plug relay join info into UnityTransport for this client
        transport.SetClientRelayData(
            joinAllocation.RelayServer.IpV4,
            (ushort)joinAllocation.RelayServer.Port,
            joinAllocation.AllocationIdBytes,
            joinAllocation.Key,
            joinAllocation.ConnectionData,      // connection info for this client
            joinAllocation.HostConnectionData   // connection info to the host
        );

        NetworkManager.Singleton.StartClient(); // start netcode client
        Debug.Log("Started Netcode Client with Relay");
    }

    public async void LeaveLobbyAsync()
    {
        try
        {
            if (currentLobby != null)
            {
                await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
                // removes this player from the lobby
                Debug.Log("Left lobby");
                currentLobby = null;
                OnLobbyLeft?.Invoke(); // notify UI
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error leaving lobby: " + e);
            OnError?.Invoke("Leave lobby failed: " + e.Message);
        }
    }

    private void Update()
    {
        // host send a heartbeat to keep it alive
        if (currentLobby != null && IsLobbyHost)
        {
            lobbyHeartbeatTimer += Time.deltaTime;
            if (lobbyHeartbeatTimer >= LobbyHeartbeatInterval)
            {
                lobbyHeartbeatTimer = 0f;
                SendHeartbeatPing();
            }
        }

        if (IsLobbyHost && currentLobby != null)
        {
            lobbyRefreshTimer += Time.deltaTime;
            if (lobbyRefreshTimer >= lobbyRefreshInterval)
            {
                lobbyRefreshTimer = 0f;
                RefreshLobbyUpdate();
            }
        }
    }

    // refresh the lobby (UI) for host when clients join
    private async void RefreshLobbyUpdate()
    {
        try
        {
            var lobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
            currentLobby = lobby;
            SessionUIManager.Instance?.UpdateLobbyUI();
        }
        catch (Exception e)
        {
            Debug.LogWarning("refresh lobby failed: " + e.Message);
        }
    }

    private async void SendHeartbeatPing()
    {
        try
        {
            await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
            // tells Unity servers the heart still beating (prevents lobby from timing out)
        }
        catch (Exception e)
        {
            Debug.LogWarning("Lobby heartbeat failed: " + e.Message);
        }
    }

    // gets the current lobby code for UI display
    public string GetLobbyCode() => currentLobby?.LobbyCode;

    // gets the number of players currently in the lobby
    public int GetPlayerCount() => currentLobby?.Players?.Count ?? 0;
}

