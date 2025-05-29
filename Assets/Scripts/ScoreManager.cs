using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : NetworkBehaviour
{
    public NetworkVariable<int> player1Score = new();
    public NetworkVariable<int> player2Score = new();

    public Text player1ScoreText;
    public Text player2ScoreText;

    public Ball ball;
    
    private void OnEnable()
    {
        if (ball != null)
        {
            ball.OnPlayer1Scored += Player1Scored;
            ball.OnPlayer2Scored += Player2Scored;
        }
    }

    private void OnDisable()
    {
        if (ball != null)
        {
            ball.OnPlayer1Scored -= Player1Scored;
            ball.OnPlayer2Scored -= Player2Scored;
        }
    }

    private void Start()
    {
        UpdateUI();
    }

    private void Player1Scored()
    {
        if (!IsServer) return;
        player1Score.Value++;
        UpdateUI();
        ball.ResetBall();
    }

    public void Player2Scored()
    {
        if (!IsServer) return;
        player2Score.Value++;
        UpdateUI();
        ball.ResetBall();
    }

    private void UpdateUI()
    {
        player1ScoreText.text = player1Score.ToString();
        player2ScoreText.text = player2Score.ToString();
    }
}
