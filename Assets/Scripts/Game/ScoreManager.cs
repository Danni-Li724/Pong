using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : NetworkBehaviour
{
    // Because the scores are constantly updating at runtime and significant to everyone, using Network Var will automatically sync them to all clients
    public NetworkVariable<int> player1Score = new();
    public NetworkVariable<int> player2Score = new();
    public NetworkVariable<int> player3Score = new();
    public NetworkVariable<int> player4Score = new();

    public BallPhysics ballPhysics;
    private bool gameEnded = false;
    public NetworkVariable<int> maxScoreToWin = new NetworkVariable<int>(20, 
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [SerializeField] private Text maxScoreText;
    private void OnEnable()
    {
        BallPhysics.OnPlayerScored += HandleScore;
    }

    private void OnDisable()
    {
        BallPhysics.OnPlayerScored -= HandleScore;
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
        // Pushing latest score values to all clients
        VisualEventsManager.Instance?.UpdateScoreUIClientRpc(
            player1Score.Value, player2Score.Value, player3Score.Value, player4Score.Value
        );

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

        // Check each player's score and give Ids
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
        // Tell NetworkGameManager to handle the game end
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.HandleGameEnd(winnerPlayerId);
        }
        
        Debug.Log($"Game ended! Player {winnerPlayerId} wins!");
    }
    // Resets scores and visuals when the host restarts a game – this is Server only
    public void ResetScores()
    {
        if (!IsServer) return;
        
        player1Score.Value = 0;
        player2Score.Value = 0;
        player3Score.Value = 0;
        player4Score.Value = 0;
        
        gameEnded = false;
        VisualEventsManager.Instance?.UpdateScoreUIClientRpc(0, 0, 0, 0);
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
    
    // Fix score deduction method for new boon
    
    public bool TryDeductPointsByPlayerId(int playerId, int amount)
    {
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

        VisualEventsManager.Instance?.UpdateScoreUIClientRpc(
            player1Score.Value, player2Score.Value, player3Score.Value, player4Score.Value
        );

        return true;
    }
    // Called when bullet hits a paddle in spaceship mode on the server side
    public void HandleBulletHit(int playerId)
    {
        if (!IsServer) return;

        switch (playerId)
        {
            // -1 from score but prevent it dropping below 0.
            case 1: player1Score.Value = Mathf.Max(0, player1Score.Value - 1); break;
            case 2: player2Score.Value = Mathf.Max(0, player2Score.Value - 1); break;
            case 3: player3Score.Value = Mathf.Max(0, player3Score.Value - 1); break;
            case 4: player4Score.Value = Mathf.Max(0, player4Score.Value - 1); break;
            default: return;
        }

        VisualEventsManager.Instance?.UpdateScoreUIClientRpc(
            player1Score.Value, player2Score.Value, player3Score.Value, player4Score.Value
        );
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
}
