using Unity.Netcode;
using UnityEngine;
using System.Collections;
/// <summary>
/// Handles transitioning to spaceship mode
/// </summary>
public class SpaceshipModeManager : NetworkBehaviour
{
    public float modeDuration = 10f;
    public Sprite rocketSprite;
    public GameObject bulletPrefab;
    public float bulletSpeed = 10f;
    public float fireCooldown = 0.5f;

    public void StartSpaceshipMode()
    {
        StartCoroutine(SpaceshipModeCoroutine());
    }

    private IEnumerator SpaceshipModeCoroutine()
    {
        SetSpaceshipModeClientRpc(true);
        yield return new WaitForSeconds(modeDuration);
        SetSpaceshipModeClientRpc(false);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SetSpaceshipModeClientRpc(bool enabled)
    {
        PaddleController[] paddles = FindObjectsOfType<PaddleController>();
        foreach (var paddle in paddles)
        {
            paddle.SetSpaceshipMode(enabled, rocketSprite, bulletPrefab, bulletSpeed, fireCooldown);
        }
    }
}
