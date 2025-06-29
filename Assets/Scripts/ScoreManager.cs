using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : NetworkBehaviour
{
    public NetworkVariable<int> player1Score = new();
    public NetworkVariable<int> player2Score = new();
    public NetworkVariable<int> player3Score = new();
    public NetworkVariable<int> player4Score = new();

    public BallPhysics ballPhysics;

    private void OnEnable()
    {
        BallPhysics.OnPlayerScored += HandleScore;
    }

    private void OnDisable()
    {
        BallPhysics.OnPlayerScored -= HandleScore;
    }

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

        VisualEventsManager.Instance?.UpdateScoreUIClientRpc(
            player1Score.Value, player2Score.Value, player3Score.Value, player4Score.Value
        );

        ballPhysics?.ResetBall();
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

    public bool TrySpendPoints(ulong clientId, int amount)
    {
        if (GetScore(clientId) < amount) return false;

        switch (clientId)
        {
            case 0: player1Score.Value -= amount; break;
            case 1: player2Score.Value -= amount; break;
            case 2: player3Score.Value -= amount; break;
            case 3: player4Score.Value -= amount; break;
        }

        VisualEventsManager.Instance?.UpdateScoreUIClientRpc(
            player1Score.Value, player2Score.Value, player3Score.Value, player4Score.Value
        );

        return true;
    }
    
    // for spaceship mode
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
}
