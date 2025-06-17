using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class RocketSpawner : NetworkBehaviour
{
   public GameObject[] rockets;
   public Transform firePoint;
   public float rocketSpeed = 5f;
   void Update()
   {
      if (!IsOwner) return;

      if (Keyboard.current.spaceKey.wasPressedThisFrame)
      {
         ShootRocketServerRPC(0); // shoot first prefab
      }
      else if (Keyboard.current.qKey.wasPressedThisFrame)
      {
         ShootRocketServerRPC(1); // shoot second prefab for variation
      }
   }
   [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
   private void ShootRocketServerRPC(int rocketIndex, RpcParams rocketParams = default)
   {
      if (rocketIndex < 0 || rocketIndex >= rockets.Length) return;
      GameObject rocket = Instantiate(rockets[rocketIndex], firePoint.position, Quaternion.identity);
      NetworkObject networkObject = rocket.GetComponent<NetworkObject>();
      networkObject.SpawnWithOwnership(OwnerClientId);
      
      Rigidbody2D rb = rocket.GetComponent<Rigidbody2D>();
      rb.linearVelocity = transform.right * rocketSpeed;
      
      Debug.Log($"Spawned rocket [{networkObject.NetworkObjectId}] owned by Client [{OwnerClientId}]");
   }
}
