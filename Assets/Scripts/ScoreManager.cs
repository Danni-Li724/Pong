using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public int player1Score = 0;
    public int player2Score = 0;

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

    private void Player1Scored()
    {
        player1Score++;
        UpdateUI();
        ball.ResetBall();
    }

    public void Player2Scored()
    {
        player2Score++;
        UpdateUI();
        ball.ResetBall();
    }

    private void UpdateUI()
    {
        player1ScoreText.text = player1Score.ToString();
        player2ScoreText.text = player2Score.ToString();
    }
}
