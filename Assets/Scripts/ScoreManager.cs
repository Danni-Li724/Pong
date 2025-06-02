using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : NetworkBehaviour
{
    public NetworkVariable<int> player1Score = new NetworkVariable<int>();
    public NetworkVariable<int> player2Score = new NetworkVariable<int>();
    public NetworkVariable<int> player3Score = new NetworkVariable<int>();
    public NetworkVariable<int> player4Score = new NetworkVariable<int>();
    public Text player1ScoreText;
    public Text player2ScoreText;
    public Text player3ScoreText;
    public Text player4ScoreText;
    public Ball ball;
    
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
    // 
}
