using UnityEngine;

public class Ball : MonoBehaviour
{
    public float speed = 5f;
    private Rigidbody2D rb;
   
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
           if (collision.gameObject.CompareTag("Goal"))
           {
               ResetBall();
           }
       }
}
