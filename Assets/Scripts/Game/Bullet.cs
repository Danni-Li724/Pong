using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    public float damageAmount = 1f;
    public float maxDistance = 30f; 

    private Vector2 spawnPosition;
    private float despawnTime = 5f;

    private Rigidbody2D rb;

    public override void OnNetworkSpawn()
    {
        spawnPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();
        if (IsServer)
        {
            // Start despawn countdown
            Invoke(nameof(DespawnBullet), despawnTime);
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        // disappear if max distance exceeded
        if (Vector2.Distance(spawnPosition, transform.position) > maxDistance)
        {
            DespawnBullet();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        PaddleController pc = other.GetComponent<PaddleController>();
        if (pc != null)
        {
            int id = pc.PlayerId;
            ScoreManager scoreManager = FindObjectOfType<ScoreManager>();
            if (scoreManager != null)
            {
                scoreManager.HandleBulletHit(id);
            }

            DespawnBullet();
        }
    }
    private void DespawnBullet()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }
}
