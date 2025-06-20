using UnityEngine;
using Unity.Netcode;
using System;

[RequireComponent(typeof(Rigidbody2D))]
public class BallPhysics : NetworkBehaviour
{
    public float speed = 5f;
    private Rigidbody2D rb;

    public static event Action<int> OnPlayerHit;
    public static event Action<int> OnPlayerScored;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("rb2D not found on " + gameObject.name);
        }
    }

    private void Start()
    {
        if (IsServer)
        {
            Launch();
        }
    }

    private void Launch()
    {
        Vector2 direction = new Vector2(UnityEngine.Random.Range(0, 2) == 0 ? -1 : 1, UnityEngine.Random.Range(-1f, 1f)).normalized;
        rb.linearVelocity = direction * speed;
    }

    public void ResetBall()
    {
        if (!IsServer) return;

        rb.linearVelocity = Vector2.zero;
        transform.position = Vector2.zero;
        Launch();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsServer) return;
        
        PaddleController paddle = collision.gameObject.GetComponent<PaddleController>();
        if (paddle != null)
        {
            OnPlayerHit?.Invoke(paddle.GetPlayerId());
            return;
        }

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
}
