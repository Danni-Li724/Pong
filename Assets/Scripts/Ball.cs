using UnityEngine;

public class Ball : MonoBehaviour
{
    public float speed = 5f;
    private Rigidbody2D rb;
    public PsychedelicBackground psychedelicBackground;

    // Game events
    public event System.Action OnPlayer1Hit;
    public event System.Action OnPlayer2Hit;
    public event System.Action OnPlayer1Scored;
    public event System.Action OnPlayer2Scored;
   
    void Start()
       {
         
           rb = GetComponent<Rigidbody2D>();
           Launch();
       }
   
       void Launch()
       {
           Vector2 direction = new Vector2(Random.Range(0, 2) == 0 ? -1 : 1, Random.Range(-1f, 1f)).normalized;
           rb.linearVelocity = direction * speed;
       }
   
       public void ResetBall()
       {
           rb.linearVelocity = Vector2.zero;
           transform.position = Vector2.zero;
           Launch();
       }
   
       private void OnCollisionEnter2D(Collision2D collision)
       {
           if (collision.gameObject.CompareTag("GoalLeft"))
           {
               OnPlayer2Scored?.Invoke();
           }
           else if (collision.gameObject.CompareTag("GoalRight"))
           {
               OnPlayer1Scored?.Invoke();
           }

           if (collision.gameObject.CompareTag("Player1"))
           {
               OnPlayer1Hit?.Invoke();
           }
           if (collision.gameObject.CompareTag("Player2"))
           {
               OnPlayer2Hit?.Invoke();
           }
       }
}
