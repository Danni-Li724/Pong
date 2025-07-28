using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles all UI for multiplayer session setup using Unity Lobby + Relay.
/// Interfaces with PongSessionManager for logic.
/// </summary>
public class SessionUIManager : MonoBehaviour
{
    [Header("Game Panels")]
    public GameObject mainMenuPanel;
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

    [Header("Lobby UI")]
    public Text lobbyCodeText;
    public Text playerCountText;
    public Transform playerListParent;
    public GameObject playerListItemPrefab;
    public Button readyButton;
    public Button leaveLobbyButton;

    [Header("Error Notice UI")]
    public Text errorMessageText;
    public Button closeErrorButton;

    private Dictionary<string, GameObject> activePlayerItems = new();
    private bool isLocalReady = false;

    void Start()
    {
        SetupUIEvents();
        SubscribeToSessionEvents();
        ShowMainMenu();
    }

    #region UI Setup and Events

    void SetupUIEvents()
    {
        createLobbyButton.onClick.AddListener(CreateLobbyClicked);
        joinByCodeButton.onClick.AddListener(JoinLobbyByCodeClicked);
        leaveLobbyButton.onClick.AddListener(() => PongSessionManager.Instance.LeaveLobbyAsync());
        closeErrorButton.onClick.AddListener(HideError);
        readyButton.onClick.AddListener(ToggleReadyStatus);
    }

    void SubscribeToSessionEvents()
    {
        var session = PongSessionManager.Instance;
        session.OnLobbyCreated += HandleLobbyCreated;
        session.OnLobbyJoined += HandleLobbyJoined;
        session.OnLobbyLeft += HandleLobbyLeft;
        session.OnError += DisplayError;
    }

    void OnDestroy()
    {
        if (PongSessionManager.Instance == null) return;
        var session = PongSessionManager.Instance;
        session.OnLobbyCreated -= HandleLobbyCreated;
        session.OnLobbyJoined -= HandleLobbyJoined;
        session.OnLobbyLeft -= HandleLobbyLeft;
        session.OnError -= DisplayError;
    }

    #endregion

    #region Navigation

    public void ShowMainMenu()
    {
        mainMenuPanel?.SetActive(true);
        lobbyPanel?.SetActive(false);
        loadingPanel?.SetActive(false);
    }

    void ShowLobbyPanel()
    {
        mainMenuPanel?.SetActive(false);
        lobbyPanel?.SetActive(true);
        loadingPanel?.SetActive(false);
        UpdateLobbyUI();
    }

    void ShowLoading(string message = "Loading...")
    {
        mainMenuPanel?.SetActive(false);
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
        ShowLoading("Creating Lobby...");
        string lobbyName = string.IsNullOrWhiteSpace(lobbyNameInput.text) ? "Pong Lobby" : lobbyNameInput.text;
        int maxPlayers = maxPlayersDropdown.value + 2; // 0-indexed
        await PongSessionManager.Instance.CreateLobbyAsync(lobbyName, maxPlayers);
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
        await PongSessionManager.Instance.JoinLobbyByCodeAsync(code);
    }

    void ToggleReadyStatus()
    {
        isLocalReady = !isLocalReady;
        // Todo: implement sync logic via SessionManager
        readyButton.GetComponentInChildren<Text>().text = isLocalReady ? "Unready" : "Ready";

        if (isLocalReady)
        {
            TryTriggerStart();
        }
    }

    void TryTriggerStart()
    {
        if (!PongSessionManager.Instance.IsLobbyHost) return;

        int playerCount = PongSessionManager.Instance.GetPlayerCount();
        // Todo: check everyone's ready state
        if (playerCount >= 2)
        {
            Debug.Log("Host sees all players ready. Starting game.");
            NetworkGameManager.Instance?.StartGame(); 
        }
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

        int players = PongSessionManager.Instance.GetPlayerCount();

        for (int i = 0; i < players; i++)
        {
            GameObject item = Instantiate(playerListItemPrefab, playerListParent);
            item.GetComponentInChildren<Text>().text = $"Player {i + 1}";
        }

        playerCountText.text = $"Players: {players}";
    }

    void ClearPlayerList()
    {
        foreach (Transform child in playerListParent)
        {
            Destroy(child.gameObject);
        }
    }

    void UpdateLobbyUI()
    {
        if (!PongSessionManager.Instance.IsInLobby) return;

        lobbyCodeText.text = $"Lobby Code: {PongSessionManager.Instance.GetLobbyCode()}";
        playerCountText.text = $"Players: {PongSessionManager.Instance.GetPlayerCount()}";
    }

    #endregion
}
        