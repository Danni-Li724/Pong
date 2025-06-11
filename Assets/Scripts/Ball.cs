using Unity.Netcode;
using UnityEngine;
using System;

public class Ball : NetworkBehaviour
{
    public float speed = 5f;
    private Rigidbody2D rb;

    public static event Action<int> OnPlayerHit;
    public static event Action<int> OnPlayerScored;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (IsServer && rb != null)
        {
            Launch();
        }
    }

    void Launch()
    {
        Vector2 direction = new Vector2(UnityEngine.Random.Range(0, 2) == 0 ? -1 : 1, UnityEngine.Random.Range(-1f, 1f)).normalized;
        rb.linearVelocity = direction * speed;
    }

    public void ResetBall()
    {
        if (rb != null) rb.linearVelocity = Vector2.zero;
        transform.position = Vector2.zero;
        Launch();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsServer) return;

        switch (collision.gameObject.tag)
        {
            case "GoalLeft": OnPlayerScored?.Invoke(2); break;
            case "GoalRight": OnPlayerScored?.Invoke(1); break;
            case "GoalTop": OnPlayerScored?.Invoke(4); break;
            case "GoalBottom": OnPlayerScored?.Invoke(3); break;
            case "Player1": OnPlayerHit?.Invoke(1); break;
            case "Player2": OnPlayerHit?.Invoke(2); break;
            case "Player3": OnPlayerHit?.Invoke(3); break;
            case "Player4": OnPlayerHit?.Invoke(4); break;
        }
    }

    private void HandlePaddleHit(int playerId)
    {
        if (IsServer)
        {
            OnPlayerHit?.Invoke(playerId);
            TriggerBackgroundChangeClientRpc(playerId);
        }
    }
    
    [ClientRpc]
    private void TriggerBackgroundChangeClientRpc(int playerId)
    {
        PsychedelicBackground.TriggerGlobalColorChange(playerId);
    }
    
    //Old code which had too much responsibilities
    /*public float speed = 5f;
    private Rigidbody2D rb;
    public PsychedelicBackground psychedelicBackground;
    public ScoreManager scoreManager;
    // Game events
    public event System.Action OnPlayer1Hit;
    public event System.Action OnPlayer2Hit;
    public event System.Action OnPlayer1Scored;
    public event System.Action OnPlayer2Scored;
    void Start()
       {
           rb = GetComponent<Rigidbody2D>();
           if (IsServer && rb != null)
           {
               Launch();
           }
           if (psychedelicBackground == null)
           {
               psychedelicBackground = FindObjectOfType<PsychedelicBackground>();
           }
           if (scoreManager == null)
           {
               scoreManager = FindObjectOfType<ScoreManager>();
           }
       }
   
       void Launch()
       {
           Vector2 direction = new Vector2(Random.Range(0, 2) == 0 ? -1 : 1, Random.Range(-1f, 1f)).normalized;
           rb.linearVelocity = direction * speed;
       }
   
       public void ResetBall()
       {
           rb = GetComponent<Rigidbody2D>();
           if(rb != null) rb.linearVelocity = Vector2.zero;
           transform.position = Vector2.zero;
           Launch();
       }
   
       private void OnCollisionEnter2D(Collision2D collision)
       {
           //if (!IsServer) return;
           if (collision.gameObject.CompareTag("GoalLeft"))
           {
               //OnPlayer2Scored?.Invoke();
               ChangeScoreClientRpc(2);
           }
           else if (collision.gameObject.CompareTag("GoalRight"))
           {
               //OnPlayer1Scored?.Invoke();
               ChangeScoreClientRpc(1);
           }
           
           if (collision.gameObject.CompareTag("GoalTop"))
           {
               //OnPlayer2Scored?.Invoke();
               ChangeScoreClientRpc(4);
           }
           else if (collision.gameObject.CompareTag("GoalBottom"))
           {
               //OnPlayer1Scored?.Invoke();
               ChangeScoreClientRpc(3);
           }

           if (collision.gameObject.CompareTag("Player1"))
           {
               //OnPlayer1Hit?.Invoke();
               ChangeBackgroundColorClientRpc(1);
           }
           if (collision.gameObject.CompareTag("Player2"))
           {
               //OnPlayer2Hit?.Invoke();
               ChangeBackgroundColorClientRpc(2);
           }
           
           if (collision.gameObject.CompareTag("Player3"))
           {
               //OnPlayer1Hit?.Invoke();
               ChangeBackgroundColorClientRpc(3);
           }
           if (collision.gameObject.CompareTag("Player4"))
           {
               //OnPlayer2Hit?.Invoke();
               ChangeBackgroundColorClientRpc(4);
           }
       }
       [ClientRpc]
       void ChangeBackgroundColorClientRpc(int playerHit)
       {
           if (psychedelicBackground == null) return;
           // if (playerHit == 1)
           //     psychedelicBackground.SwitchToPlayer1Colors();
           // else if (playerHit == 2)
           //     psychedelicBackground.SwitchToPlayer2Colors();
           switch (playerHit)
           {
               case 1: 
                   psychedelicBackground.SwitchToPlayer1Colors();
                   break;
               case 2: 
                   psychedelicBackground.SwitchToPlayer2Colors();
                   break;
               case 3: 
                   psychedelicBackground.SwitchToPlayer3Colors();
                   break;
               case 4: 
                   psychedelicBackground.SwitchToPlayer4Colors();
                   break;
               default:
                   return;
           }
       }
       [ClientRpc(Delivery = RpcDelivery.Reliable)]
       void ChangeScoreClientRpc(int playerHit)
       {
           if (scoreManager == null) return;
           // if (playerHit == 1)
           //     scoreManager.Player1Scored();
           // else if (playerHit == 2)
           //     scoreManager.Player2Scored();
           switch (playerHit)
           {
               case 1: 
                   scoreManager.Player1Scored();
                   break;
               case 2: 
                   scoreManager.Player2Scored();
                   break;
               case 3: 
                   scoreManager.Player3Scored();
                   break;
               case 4: 
                   scoreManager.Player4Scored();
                   break;
               default:
                   return;
           }
       }

       [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
       void ExampleClientRpc()
       {
           
       }*/
}
