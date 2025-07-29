using UnityEngine;
using Unity.Netcode;
using System;
using System.Linq;

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
        rb.linearDamping = 0f; // to fix deceleration issue
        rb.angularDamping = 0f;
        rb.gravityScale = 0f;
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
    
    private void FixedUpdate()
    {
        if (!IsServer) return;

        float expectedSpeed = speed;
        float actualSpeed = rb.linearVelocity.magnitude;
        // reapply force if ball drops below a certain velocity - fix to deceleration issue
        if (actualSpeed < expectedSpeed * 0.9f)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * expectedSpeed;
        }
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
            int goalOwnerId = goal.GetGoalForPlayerId();

            // get goal owner client ID from player ID
            // var goalOwnerInfo = NetworkGameManager.Instance.GetAllPlayers()
            //     .FirstOrDefault(p => p.playerId == goalOwnerId);
            var goalOwnerInfo = GameManager.Instance.GetAllPlayers()
                .FirstOrDefault(p => p.playerId == goalOwnerId);
            bool goalOwnerExists = goalOwnerInfo != null;
            // FIXED: only score if: 1. last player to touch ball is valid
            //2. goal owner exists and is connected
            //3. last player don't score on their own goal
            if (lastPlayerId != -1 && goalOwnerExists && lastPlayerId != goalOwnerId)
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
    
    public void ModifySpeed(float multiplier, float duration)
    {
        StartCoroutine(ModifySpeedCoroutine(multiplier, duration));
    }
    
    private System.Collections.IEnumerator ModifySpeedCoroutine(float multiplier, float duration)
    {
        float originalSpeed = speed; 
        speed *= multiplier;
        yield return new WaitForSeconds(duration);
        speed = originalSpeed;
    }
}
