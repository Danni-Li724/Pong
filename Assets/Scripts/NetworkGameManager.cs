using Unity.Netcode;
using UnityEngine;

public class NetworkGameManager : NetworkBehaviour
{
    public Transform leftSpawn, rightSpawn;
    public GameObject paddlePrefab;
    public GameObject ballPrefab;
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
        }
    }
    void OnClientConnected(ulong clientId)
        {
            // spawn paddles
            Vector3 spawnPos = NetworkManager.ConnectedClients.Count == 1 ? leftSpawn.position : rightSpawn.position;
            GameObject paddle = Instantiate(paddlePrefab, spawnPos, Quaternion.identity);
            paddle.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
            
            // spawn ball
            GameObject ball = Instantiate(ballPrefab, Vector3.zero, Quaternion.identity);
            ball.GetComponent<NetworkObject>().Spawn();
        }
}
