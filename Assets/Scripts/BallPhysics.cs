using UnityEngine;
using Unity.Netcode;
using System;

[RequireComponent(typeof(Rigidbody2D))]
public class BallPhysics : NetworkBehaviour
{
    public float speed = 5f;
    private Rigidbody2D rb;
    private int lastPlayerId;

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

        if (collision.gameObject.TryGetComponent<PaddleController>(out var paddle))
        {
            lastPlayerId = paddle.GetPlayerId();
            OnPlayerHit?.Invoke(lastPlayerId);
            return;
        }

        if (collision.gameObject.TryGetComponent<GoalController>(out var goal))
        {
            int goalOwner = goal.GetGoalForPlayerId();
        
            if (lastPlayerId != -1 && lastPlayerId != goalOwner)
            {
                OnPlayerScored?.Invoke(lastPlayerId);
            }
            else
            {
                Debug.Log($"Own goal or no last hitter detected.");
            }

            return;
        }
    }
}
