using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Handles all UI for multiplayer session setup using Unity Lobby + Relay.
/// Interfaces with PongSessionManager for sessiona and lobby acreation & sign-ins,
/// also linked to GameManager for setting player count and ready status.
/// </summary>
public class SessionUIManager : MonoBehaviour
{
    public static SessionUIManager Instance { get; private set; }
    private SessionBridge sessionBridge;

    [Header("Game Panels")]
    public GameObject createLobbyPanel;
    public GameObject lobbyPanel;
    public GameObject loadingPanel;
    public GameObject errorPanel;

    [Header("Create Lobby UI")]
    public InputField lobbyNameInput;
    public Dropdown maxPlayersDropdown;
    public Button createLobbyButton;

    [Header("Join Lobby UI")]
    public InputField joinCodeInput;
    public Button joinByCodeButton;
    public RectTransform sessionListContent;
    public GameObject sessionListItemPrefab;
    public Button refreshLobbyListButton;
    public float refreshCoolDown = 5f;
    private bool canRefresh = true;

    [Header("Lobby UI")]
    public Text lobbyCodeText;
    public Text playerCountText;
    public Transform playerListParent;
    public GameObject playerListItemPrefab;
    public Button readyButton;
    public Button leaveLobbyButton;
    
    public InputField playerNameInput;
    public Button changeNameButton;

    [Header("Error Notice UI")]
    public Text errorMessageText;
    public Button closeErrorButton;

    private Dictionary<string, GameObject> activePlayerItems = new();
    private bool isLocalReady = false;
    
    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); 
            return;
        }
        Instance = this; 
        DontDestroyOnLoad(gameObject); 
    }

    void Start()
    {
        sessionBridge = FindObjectOfType<SessionBridge>();
        SetupUIEvents();
        SubscribeToSessionEvents();
        ShowMainMenu();
        
        if(changeNameButton != null)
            changeNameButton.onClick.AddListener(OnPlayerNameChanged);
    }

    #region UI Setup and Events

    void SetupUIEvents()
    {
        createLobbyButton.onClick.AddListener(CreateLobbyClicked);
        joinByCodeButton.onClick.AddListener(JoinLobbyByCodeClicked);
        leaveLobbyButton.onClick.AddListener(() => PongSessionManager.Instance.LeaveLobbyAsync());
        closeErrorButton.onClick.AddListener(HideError);
        readyButton.onClick.AddListener(ToggleReadyStatus);
        refreshLobbyListButton.onClick.AddListener(RefreshLobbyList);
    }

    void SubscribeToSessionEvents()
    {
        var session = PongSessionManager.Instance;
        session.OnLobbyCreated += HandleLobbyCreated;
        session.OnLobbyJoined += HandleLobbyJoined;
        session.OnLobbyLeft += HandleLobbyLeft;
        session.OnError += DisplayError;
        sessionBridge = FindObjectOfType<SessionBridge>();
        sessionBridge.OnReadyStatesChanged += PopulatePlayerList;
    }

    void OnDestroy()
    {
        if (PongSessionManager.Instance == null) return;
        var session = PongSessionManager.Instance;
        session.OnLobbyCreated -= HandleLobbyCreated;
        session.OnLobbyJoined -= HandleLobbyJoined;
        session.OnLobbyLeft -= HandleLobbyLeft;
        session.OnError -= DisplayError;
        
        if (sessionBridge != null)
            sessionBridge.OnReadyStatesChanged -= PopulatePlayerList;
    }

    #endregion

    #region Navigation

    public void ShowMainMenu()
    {
        createLobbyPanel?.SetActive(true);
        lobbyPanel?.SetActive(false);
        loadingPanel?.SetActive(false);
    }
    
    public void ShowCreateSessionPanel()
    {
        createLobbyPanel?.SetActive(false);
        loadingPanel?.SetActive(false);
        UpdateLobbyUI();
    }

    public void ShowLobbyPanel()
    {
        createLobbyPanel?.SetActive(false);
        lobbyPanel?.SetActive(true);
        loadingPanel?.SetActive(false);
        UpdateLobbyUI();
    }

    void ShowLoading(string message = "Loading...")
    {
        createLobbyPanel?.SetActive(false);
        lobbyPanel?.SetActive(false);
        loadingPanel?.SetActive(true);
    }

    void DisplayError(string message)
    {
        errorPanel.SetActive(true);
        errorMessageText.text = message;
    }

    void HideError()
    {
        errorPanel.SetActive(false);
        errorMessageText.text = "";
    }

    #endregion

    #region Button Callbacks

    async void CreateLobbyClicked()
    {
        // todo: send this to session list...?
        ShowLoading("Creating Lobby...");
        string lobbyName = string.IsNullOrWhiteSpace(lobbyNameInput.text) ? "Pong Lobby" : lobbyNameInput.text;
        int maxPlayers = maxPlayersDropdown.value + 2; // 0-indexed
        string playerName = string.IsNullOrWhiteSpace(playerNameInput.text) ? "Pong Player" : playerNameInput.text;
        await PongSessionManager.Instance.CreateLobbyAsync(lobbyName, maxPlayers, playerName);
    }

    public async void OnPlayerNameChanged()
    {
        if (PongSessionManager.Instance.IsInLobby) return;
        string newName = playerNameInput.text.Trim();
        if(string.IsNullOrEmpty(newName)) return;
        try
        {
            var playerData = new Dictionary<string, PlayerDataObject>
            {
                { "DisplayName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, newName) }
            };
            var options = new UpdatePlayerOptions
            {
                Data = playerData
            };
            var lobby = PongSessionManager.Instance.currentLobby;
            string playerId = PongSessionManager.Instance.LocalPlayerId;
            await LobbyService.Instance.UpdatePlayerAsync(lobby.Id, playerId, options);
            playerNameInput.text = newName;
            UpdateLobbyUI();
        }
        catch(Exception e)
        {
            DisplayError("Failed to update name: " + e.Message);
        }
    }
    async void JoinLobbyByCodeClicked()
    {
        string code = joinCodeInput.text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            DisplayError("Please enter a join code.");
            return;
        }
        ShowLoading("Joining Lobby...");
        
        // grab player's name
        string playerName = string.IsNullOrWhiteSpace(playerNameInput.text) ? "Player" : playerNameInput.text;
        await PongSessionManager.Instance.JoinLobbyByCodeAsync(code, playerName);
    }

    void ToggleReadyStatus()
    {
        isLocalReady = !isLocalReady;
        readyButton.GetComponentInChildren<Text>().text = isLocalReady ? "Unready" : "Ready";
        sessionBridge.SetLocalPlayerReady(isLocalReady);
        // if (isLocalReady)
        // {
        //     TryTriggerStart();
        // }
        //UpdateReadyDisplay();
    }

    public async void RefreshLobbyList()
    {
        if (!canRefresh) return;
        canRefresh = false;
        refreshLobbyListButton.interactable = false;
        ShowLoading("Refreshing Lobby...");
        ClearSessionList();
        try
        {
            var query = new QueryLobbiesOptions()
            {
                // query filters can filter lobbies based on what lobby have open slots, certain lobby names & regions etc
                Filters = new List<QueryFilter>()
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                    // param notes: FieldOptions.AvailableSlots: built in unity field, asking how many empty spots there are
                    // there's also FieldOptions.name, .MaxPlayers, .IsLocked, .Id (lobby id) etc
                    // "0": the value to be compared against. 0 means 
                    // OpOptions: comparison operator, 'GT' = greater than (there's also: EQ, LT etc)
                }
            };
            var result = await LobbyService.Instance.QueryLobbiesAsync(query);
            foreach (var lobby in result.Results)
            {
                GameObject item = Instantiate(sessionListItemPrefab, sessionListContent);
                var texts = item.GetComponentsInChildren<Text>();
                texts[0].text = lobby.Name;
                texts[1].text = $"Players: {lobby.Players.Count}/{lobby.MaxPlayers}";
                var joinButton = item.GetComponentInChildren<Button>();
                joinButton.onClick.AddListener(() => { JoinLobbyById(lobby.Id); });
            }
        }
        catch (Exception ex)
        {
            DisplayError(ex.Message);
        }
        loadingPanel.SetActive(false);
        createLobbyPanel?.SetActive(true);
        await Task.Delay((int)(refreshCoolDown * 1000));
        canRefresh = true;
        refreshLobbyListButton.interactable = true;
    }

    public async void JoinLobbyById(string lobbyId)
    {
        ShowLoading("Joining Lobby...");
        loadingPanel.SetActive(false);
        lobbyPanel?.SetActive(true);
        try
        {
            // get player name from input field
            string playerName = string.IsNullOrWhiteSpace(playerNameInput.text) ? "Player" : playerNameInput.text;
        
            var playerData = new Dictionary<string, PlayerDataObject>
            {
                { "DisplayName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName) }
            };
        
            var joinOptions = new JoinLobbyByIdOptions
            {
                Player = new Player(
                    id: AuthenticationService.Instance.PlayerId,
                    data: playerData  // Add playerData (name)
                )
            };
        
            var lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, joinOptions);

            // now use join code to connect via relay & JoinByCode()
            string joinCode = lobby.Data["JoinCode"].Value;
            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            // start session
            PongSessionManager.Instance.StartClientWithRelay(joinAllocation);
            // set current lobby
            PongSessionManager.Instance.currentLobby = lobby;
        }
        catch (Exception e)
        {
            DisplayError("Failed to join lobby" + e.Message);
        }
    }

    void ClearSessionList()
    {
        foreach (Transform child in sessionListContent)
            Destroy(child.gameObject);
    }
    

    #endregion

    #region Lobby Event Handlers

    void HandleLobbyCreated()
    {
        ShowLobbyPanel();
        PopulatePlayerList();
    }

    void HandleLobbyJoined()
    {
        ShowLobbyPanel();
        PopulatePlayerList();
    }

    void HandleLobbyLeft()
    {
        ShowMainMenu();
        ClearPlayerList();
    }

    #endregion

    #region Lobby UI

    void PopulatePlayerList()
    {
        ClearPlayerList();
        if (!PongSessionManager.Instance.IsInLobby) return;
        string code = PongSessionManager.Instance.GetLobbyCode();
        lobbyCodeText.text = $"Lobby Code: {code}";
        var readyStates = sessionBridge.GetReadyStates();
        var lobby = PongSessionManager.Instance.GetCurrentLobby();
        if (lobby == null) return;
        for (int i = 0; i < lobby.Players.Count; i++)
        {
            var player = lobby.Players[i];
            GameObject item = Instantiate(playerListItemPrefab, playerListParent);

            string displayName = $"Player {i + 1}";
            if (player.Data != null && player.Data.TryGetValue("DisplayName", out var nameData))
                displayName = nameData.Value;
            // bool isReady = readyStates.TryGetValue(player.Id, out var r) && r;
            bool isReady = false;
            if (player.Data != null && player.Data.TryGetValue("Ready", out var readyData))
            {
                bool.TryParse(readyData.Value, out isReady);
            }
            item.GetComponentInChildren<Text>().text = displayName + (isReady ? " (Ready)" : " (Not Ready)");
        }
        playerCountText.text = $"Players: {lobby.Players.Count}";
    }

    void ClearPlayerList()
    {
        foreach (Transform child in playerListParent)
        {
            Destroy(child.gameObject);
        }
    }

    public void UpdateLobbyUI()
    {
        if (!PongSessionManager.Instance.IsInLobby) return;

        lobbyCodeText.text = $"Lobby Code: {PongSessionManager.Instance.GetLobbyCode()}";
        playerCountText.text = $"Players: {PongSessionManager.Instance.GetPlayerCount()}";
        PopulatePlayerList();
        
        // reflecting local player name input if they are in lobby
        if (PongSessionManager.Instance.currentLobby != null)
        {
            var localPlayer = PongSessionManager.Instance.currentLobby.Players.Find(p => p.Id == PongSessionManager.Instance.LocalPlayerId);
            if (localPlayer != null && localPlayer.Data != null && localPlayer.Data.TryGetValue("DisplayName", out var nameData))
            {
                playerNameInput.text = nameData.Value;
            }
        }
    }

    #endregion
}
        