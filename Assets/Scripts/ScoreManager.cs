using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : NetworkBehaviour
{
   public NetworkVariable<int> player1Score = new();
    public NetworkVariable<int> player2Score = new();
    public NetworkVariable<int> player3Score = new();
    public NetworkVariable<int> player4Score = new();

    public Ball ball;

    private void OnEnable()
    {
        Ball.OnPlayerScored += HandleScore;
    }

    private void OnDisable()
    {
        Ball.OnPlayerScored -= HandleScore;
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

        // use VisualEventsManager to update UI across all clients
        if (VisualEventsManager.Instance != null)
        {
            VisualEventsManager.Instance.UpdateScoreUIClientRpc(
                player1Score.Value, player2Score.Value, player3Score.Value, player4Score.Value);
        }
        ball.ResetBall();
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
        if (VisualEventsManager.Instance != null)
        {
            VisualEventsManager.Instance.UpdateScoreUIClientRpc(
                player1Score.Value, player2Score.Value, player3Score.Value, player4Score.Value);
        }
        
        return true;
    }
    
    /*public NetworkVariable<int> player1Score = new NetworkVariable<int>();
    public NetworkVariable<int> player2Score = new NetworkVariable<int>();
    public NetworkVariable<int> player3Score = new NetworkVariable<int>();
    public NetworkVariable<int> player4Score = new NetworkVariable<int>();
    public Text player1ScoreText;
    public Text player2ScoreText;
    public Text player3ScoreText;
    public Text player4ScoreText;
    public Ball ball;
    
    public static ScoreManager instance;
    
    // private void OnEnable()
    // {
    //     if (ball != null)
    //     {
    //         ball.OnPlayer1Scored += Player1Scored;
    //         ball.OnPlayer2Scored += Player2Scored;
    //     }
    // }
    // private void OnDisable()
    // {
    //     if (ball != null)
    //     {
    //         ball.OnPlayer1Scored -= Player1Scored;
    //         ball.OnPlayer2Scored -= Player2Scored;
    //     }
    // }
    private void Awake()
    {
        instance = this;
    }
    private void Start()
    {
        UpdateUI();
    }
    public void Player1Scored()
    {
        //if (!IsServer) return;
        player1Score.Value++;
        UpdateUI();
        ball.ResetBall();
    }
    public void Player2Scored()
    {
        //if (!IsServer) return;
        player2Score.Value++;
        UpdateUI();
        ball.ResetBall();
    }
    
    public void Player3Scored()
    {
        //if (!IsServer) return;
        player3Score.Value++;
        UpdateUI();
        ball.ResetBall();
    }
    
    public void Player4Scored()
    {
        //if (!IsServer) return;
        player4Score.Value++;
        UpdateUI();
        ball.ResetBall();
    }
    private void UpdateUI()
    {
        player1ScoreText.text = player1Score.Value.ToString();
        player2ScoreText.text = player2Score.Value.ToString();
        player3ScoreText.text = player3Score.Value.ToString();
        player4ScoreText.text = player4Score.Value.ToString();
    }

    public int GetScore(ulong clientId)
    {
        if(clientId == 0) return player1Score.Value;
        if (clientId == 1) return player2Score.Value;
        if(clientId == 2) return player3Score.Value;
        if (clientId == 3) return player4Score.Value;
        return 0;
    }

    public bool TrySpendPoints(ulong clientId, int amount)
    {
        if (GetScore(clientId) < amount) return false;
        if(clientId == 0) player1Score.Value -= amount;
        else if(clientId == 1) player2Score.Value -= amount;
        else if(clientId == 2) player3Score.Value -= amount;
        else if(clientId == 3) player4Score.Value -= amount;
        UpdateUI();
        return true;
    }*/
}
