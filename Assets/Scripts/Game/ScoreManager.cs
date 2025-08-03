using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : NetworkBehaviour
{
    // NetworkVariables with change callbacks
    public NetworkVariable<int> player1Score = new();
    public NetworkVariable<int> player2Score = new();
    public NetworkVariable<int> player3Score = new();
    public NetworkVariable<int> player4Score = new();

    public BallPhysics ballPhysics;
    private bool gameEnded = false;
    public NetworkVariable<int> maxScoreToWin = new NetworkVariable<int>(20, 
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [SerializeField] private Text maxScoreText;

    public override void OnNetworkSpawn()
    {
        // Subscribe to NetworkVariable change events on all clients
        player1Score.OnValueChanged += OnScoreChanged;
        player2Score.OnValueChanged += OnScoreChanged;
        player3Score.OnValueChanged += OnScoreChanged;
        player4Score.OnValueChanged += OnScoreChanged;
        
        // Initial UI update
        UpdateScoreUI();
    }

    public override void OnNetworkDespawn()
    {
        // Unsubscribe from change events
        player1Score.OnValueChanged -= OnScoreChanged;
        player2Score.OnValueChanged -= OnScoreChanged;
        player3Score.OnValueChanged -= OnScoreChanged;
        player4Score.OnValueChanged -= OnScoreChanged;
    }

    private void OnEnable()
    {
        BallPhysics.OnPlayerScored += HandleScore;
    }

    private void OnDisable()
    {
        BallPhysics.OnPlayerScored -= HandleScore;
    }

    // This gets called automatically when any score NetworkVariable changes
    private void OnScoreChanged(int previousValue, int newValue)
    {
        UpdateScoreUI();
    }

    // Update the UI locally (called on all clients when NetworkVariables change)
    private void UpdateScoreUI()
    {
        if (VisualEventsManager.Instance != null)
        {
            var manager = VisualEventsManager.Instance;
            if (manager.player1ScoreText != null) manager.player1ScoreText.text = player1Score.Value.ToString();
            if (manager.player2ScoreText != null) manager.player2ScoreText.text = player2Score.Value.ToString();
            if (manager.player3ScoreText != null) manager.player3ScoreText.text = player3Score.Value.ToString();
            if (manager.player4ScoreText != null) manager.player4ScoreText.text = player4Score.Value.ToString();
        }
    }

    // Server-side only: called when a player scores
    private void HandleScore(int playerId)
    {
        if (!IsServer) return;

        switch (playerId)
        {
            case 1: player1Score.Value++; break;
            case 2: player2Score.Value++; break;
            case 3: player3Score.Value++; break;
            case 4: player4Score.Value++; break;
        }
        
        // Remove the manual RPC call - NetworkVariable will handle sync automatically
        // VisualEventsManager.Instance?.UpdateScoreUIClientRpc(...); // REMOVE THIS LINE

        CheckForWinner();

        if (!gameEnded)
        {
            ballPhysics?.ResetBall();
        }
    }
    
    private void CheckForWinner()
    {
        if (!IsServer || gameEnded) return;

        int winnerPlayerId = 0;
        int maxScore = 0;

        if (player1Score.Value >= maxScoreToWin.Value && player1Score.Value > maxScore)
        {
            winnerPlayerId = 1;
            maxScore = player1Score.Value;
        }
        if (player2Score.Value >= maxScoreToWin.Value && player2Score.Value > maxScore)
        {
            winnerPlayerId = 2;
            maxScore = player2Score.Value;
        }
        if (player3Score.Value >= maxScoreToWin.Value && player3Score.Value > maxScore)
        {
            winnerPlayerId = 3;
            maxScore = player3Score.Value;
        }
        if (player4Score.Value >= maxScoreToWin.Value && player4Score.Value > maxScore)
        {
            winnerPlayerId = 4;
            maxScore = player4Score.Value;
        }

        if (winnerPlayerId > 0)
        {
            EndGame(winnerPlayerId);
        }
    }
    
    private void EndGame(int winnerPlayerId)
    {
        if (!IsServer) return;

        gameEnded = true;
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.HandleGameEnd(winnerPlayerId);
        }
        
        Debug.Log($"Game ended! Player {winnerPlayerId} wins!");
    }

    public void ResetScores()
    {
        if (!IsServer) return;
        
        player1Score.Value = 0;
        player2Score.Value = 0;
        player3Score.Value = 0;
        player4Score.Value = 0;
        
        gameEnded = false;
        // Remove manual RPC - NetworkVariable callbacks will handle UI update
    }

    public int GetScore(ulong clientId)
    {
        return clientId switch
        {
            0 => player1Score.Value,
            1 => player2Score.Value,
            2 => player3Score.Value,
            3 => player4Score.Value,
            _ => 0,
        };
    }
    
    public bool TryDeductPointsByPlayerId(int playerId, int amount)
    {
        if (!IsServer) return false;

        ref NetworkVariable<int> targetScore = ref player1Score;

        switch (playerId)
        {
            case 1: targetScore = ref player1Score; break;
            case 2: targetScore = ref player2Score; break;
            case 3: targetScore = ref player3Score; break;
            case 4: targetScore = ref player4Score; break;
            default: 
                Debug.LogError($"Invalid player ID: {playerId}");
                return false;
        }

        if (targetScore.Value < amount)
        {
            Debug.Log($"Player {playerId} doesn't have enough points ({targetScore.Value} < {amount})");
            return false;
        }

        targetScore.Value -= amount;
        Debug.Log($"Deducted {amount} points from player {playerId}. New score: {targetScore.Value}");

        // Remove manual RPC - NetworkVariable will handle sync
        return true;
    }

    public void HandleBulletHit(int playerId)
    {
        if (!IsServer) return;

        switch (playerId)
        {
            case 1: player1Score.Value = Mathf.Max(0, player1Score.Value - 1); break;
            case 2: player2Score.Value = Mathf.Max(0, player2Score.Value - 1); break;
            case 3: player3Score.Value = Mathf.Max(0, player3Score.Value - 1); break;
            case 4: player4Score.Value = Mathf.Max(0, player4Score.Value - 1); break;
            default: return;
        }

        // Remove manual RPC - NetworkVariable will handle sync
    }
    
    public void SetMaxScore(int score)
    {
        if (!IsServer) return;
        maxScoreToWin.Value = score;
        UpdateMaxScoreTextClientRpc(score);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void UpdateMaxScoreTextClientRpc(int score)
    {
        if (maxScoreText != null)
        {
            maxScoreText.text = $"Score {score} to win!";
        }
    }

    public void SwapScores(int playerA, int playerB)
    {
        Debug.Log($"[ScoreManager] SwapScores called with A={playerA}, B={playerB}");

        if (!IsServer || playerA == playerB)
        {
            Debug.Log("[ScoreManager] Not swapping: not server or same player.");
            return;
        }
        int scoreAValue = GetPlayerScore(playerA);
        int scoreBValue = GetPlayerScore(playerB);
        
        if (scoreAValue == -1 || scoreBValue == -1)
        {
            Debug.LogError($"Invalid player IDs: {playerA}, {playerB}");
            return;
        }

        Debug.Log($"[ScoreManager] BEFORE SWAP: Player{playerA}: {scoreAValue}, Player{playerB}: {scoreBValue}");
        
        // swap!
        SetPlayerScore(playerA, scoreBValue);
        SetPlayerScore(playerB, scoreAValue);

        Debug.Log($"[ScoreManager] AFTER SWAP: Player{playerA}: {GetPlayerScore(playerA)}, Player{playerB}: {GetPlayerScore(playerB)}");

        // Remove RPC call - NetworkVariable callbacks will handle UI update automatically
    }

    // helper methods
    private int GetPlayerScore(int playerId)
    {
        return playerId switch
        {
            1 => player1Score.Value,
            2 => player2Score.Value,
            3 => player3Score.Value,
            4 => player4Score.Value,
            _ => -1
        };
    }

    private void SetPlayerScore(int playerId, int value)
    {
        switch (playerId)
        {
            case 1: player1Score.Value = value; break;
            case 2: player2Score.Value = value; break;
            case 3: player3Score.Value = value; break;
            case 4: player4Score.Value = value; break;
        }
    }
}
