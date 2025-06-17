using Unity.Netcode;
using UnityEngine;

public class Rocket : NetworkBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;
        Debug.Log($"Projectile [{NetworkObjectId}] owned by {OwnerClientId} collided with {collision.gameObject.name}");
    }

    [Rpc(SendTo.Everyone)]
    private void DestroyClientRpc(ulong objectId, ulong ownerClientId, RpcParams rpcParams = default)
    {
        Debug.Log($"[CLIENT] Rocket [{objectId}] owned by client {ownerClientId} has been destroyed.");
    }
}
