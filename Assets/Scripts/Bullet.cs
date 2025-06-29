using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    public float damageAmount = 1;

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

            Destroy(gameObject);
        }
    }
}
