using UnityEngine;

public class Ball : MonoBehaviour
{
    public float speed = 5f;
    private Rigidbody2D rb;
    public PsychedelicBackground psychedelicBackground;
    public ScoreManager scoreManager;
   
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
               scoreManager.Player2Scored();
           }
           else if (collision.gameObject.CompareTag("GoalRight"))
           {
               scoreManager.Player1Scored();
           }

           if (collision.gameObject.CompareTag("Player1"))
           {
               psychedelicBackground.SwitchToPlayer1Colors();
           }
           if (collision.gameObject.CompareTag("Player2"))
           {
               psychedelicBackground.SwitchToPlayer2Colors();
               
           }
       }
}
