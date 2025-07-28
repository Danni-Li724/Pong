using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This is my new game manager! I removed responsibilities now handled by PongSessionManager (lobby, relay, player join logic)
// and focuses purely on Netcode-based gameplay logic, basically all of the rest: paddle/ball spawning, sprite sync, spaceship mode, etc.
/// </summary>
public class GameManager : NetworkBehaviour
{
    [Header("Game Settings")]
    private bool gameStarted = false;
    private Dictionary<ulong, PlayerInfo> allPlayers = new();

    [Header("References")]
    [SerializeField] private ScoreManager scoreManager;
    public static GameManager Instance { get; private set; }

    [Header("Level Objects")]
    public Transform leftSpawn, rightSpawn, topSpawn, bottomSpawn;
    public GameObject playerPaddlePrefab;
    public GameObject ballPrefab;
    private GameObject spawnedBall;

    [Header("UI Panels")]
    [SerializeField] private GameObject startGamePanel;
    [SerializeField] private GameObject gameEndPanel;
    [SerializeField] private Text winnerText;
    [SerializeField] private Button restartButton;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            SetupGameEndUI();
        }
    }

    #region GAME START

    public void StartGame()
    {
        if (!IsServer || gameStarted) return;

        gameStarted = true;

        // Spawn the ball
        spawnedBall = Instantiate(ballPrefab, Vector3.zero, Quaternion.identity);
        spawnedBall.GetComponent<NetworkObject>().Spawn();

        Debug.Log("[GameManager] Game started");
        TriggerPlayerSelectionUIClientRpc();
        HideStartGamePanelClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void HideStartGamePanelClientRpc()
    {
        if (startGamePanel != null)
            startGamePanel.SetActive(false);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerPlayerSelectionUIClientRpc()
    {
        var paddles = FindObjectsByType<PaddleController>(FindObjectsSortMode.None);
        foreach (var paddle in paddles)
        {
            if (paddle.IsOwner)
                paddle.SpawnPlayerSelectionUI();
        }
    }

    #endregion

    #region GOAL ASSIGNING

    public void AssignGoals()
    {
        var goals = FindObjectsOfType<GoalController>();
        var paddles = FindObjectsOfType<PaddleController>();

        foreach (var goal in goals)
        {
            PaddleController closestPaddle = null;
            float shortestDist = float.MaxValue;

            foreach (var paddle in paddles)
            {
                float dist = Vector3.Distance(goal.transform.position, paddle.transform.position);
                if (dist < shortestDist)
                {
                    shortestDist = dist;
                    closestPaddle = paddle;
                }
            }

            if (closestPaddle != null)
            {
                goal.SetGoalForPlayerId(closestPaddle.PlayerId);
            }
        }
    }

    #endregion

    #region SPACESHIP MODE

    public void StartSpaceshipMode()
    {
        if (!IsServer) return;

        if (spawnedBall != null)
        {
            var netObj = spawnedBall.GetComponent<NetworkObject>();
            if (netObj.IsSpawned) netObj.Despawn();
        }

        StartCoroutine(DelayedSpaceshipActivation());
    }

    private IEnumerator DelayedSpaceshipActivation()
    {
        yield return new WaitForSeconds(0.2f);
        ActivateSpaceshipModeClientRpc(50f, 0.5f);
        StartCoroutine(StopSpaceshipModeAfterSeconds(30f));
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ActivateSpaceshipModeClientRpc(float bulletSpeed, float cooldown)
    {
        var paddles = FindObjectsByType<PaddleController>(FindObjectsSortMode.None);
        foreach (var paddle in paddles)
        {
            //paddle.EnterSpaceshipMode(bulletSpeed, cooldown);
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
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void EndSpaceshipModeClientRpc()
    {
        var paddles = FindObjectsByType<PaddleController>(FindObjectsSortMode.None);
        foreach (var paddle in paddles)
        {
            //paddle.SetSpaceshipMode(false);
        }
    }

    #endregion

    #region GAME END

    public void HandleGameEnd(int winnerPlayerId)
    {
        if (!IsServer) return;

        var ball = FindFirstObjectByType<BallPhysics>();
        if (ball != null)
        {
            var netObj = ball.GetComponent<NetworkObject>();
            netObj.Despawn();
        }

        ShowGameEndUIClientRpc(winnerPlayerId);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ShowGameEndUIClientRpc(int winnerPlayerId)
    {
        gameEndPanel?.SetActive(true);
        if (winnerText != null)
            winnerText.text = $"Player {winnerPlayerId} has won!";
        if (restartButton != null)
            restartButton.gameObject.SetActive(IsHost);
    }

    public void OnRestartButtonPressed()
    {
        if (!IsHost) return;

        RestartGame();
    }

    private void RestartGame()
    {
        Debug.Log("Restarting game...");

        HideGameEndUIClientRpc();

        scoreManager?.ResetScores();

        // just respawn the ball and reset gameStarted flag
        if (spawnedBall != null && spawnedBall.TryGetComponent(out NetworkObject netObj) && netObj.IsSpawned)
        {
            netObj.Despawn();
        }

        gameStarted = false;

        // show start panel again for host
        if (IsHost && startGamePanel != null)
        {
            startGamePanel.SetActive(true);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void HideGameEndUIClientRpc()
    {
        if (gameEndPanel != null)
            gameEndPanel.SetActive(false);
    }

    private void SetupGameEndUI()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartButtonPressed);
    }

    #endregion
}
