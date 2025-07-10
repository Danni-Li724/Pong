using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    public float damageAmount;
    public float maxDistance; 
    private Vector2 spawnPosition;
    private float despawnTime = 5f;

    private Rigidbody2D rb;

    private NetworkObject shooterNetObject; // Reference to shooter

    public void SetShooter(NetworkObject shooter)
    {
        shooterNetObject = shooter;
    }

    public override void OnNetworkSpawn()
    {
        spawnPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();

        if (IsServer)
        {
            Invoke(nameof(DespawnBullet), despawnTime);
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        if (Vector2.Distance(spawnPosition, transform.position) > maxDistance)
        {
            DespawnBullet();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        // Prevent hitting the shooter
        NetworkObject otherNetObj = other.GetComponent<NetworkObject>();
        if (otherNetObj != null && otherNetObj == shooterNetObject)
        {
            return;
        }

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
