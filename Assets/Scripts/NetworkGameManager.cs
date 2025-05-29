using Unity.Netcode;
using UnityEngine;

public class NetworkGameManager : NetworkBehaviour
{
    public Transform leftSpawn, rightSpawn;
    public GameObject player1PaddlePrefab;
    public GameObject player2PaddlePrefab;
    public GameObject ballPrefab;
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            GameObject paddle1 = Instantiate(player1PaddlePrefab, leftSpawn.position, Quaternion.identity);
            //paddle1.GetComponent<NetworkObject>().Spawn();
            paddle1.GetComponent<NetworkObject>().SpawnAsPlayerObject(NetworkManager.Singleton.LocalClientId);
        }
    }
    
    void OnClientConnected(ulong clientId)
        {
            // spawn paddles
            //Vector3 spawnPos = NetworkManager.ConnectedClients.Count == 1 ? leftSpawn.position : rightSpawn.position;
            if (clientId == NetworkManager.Singleton.LocalClientId) return;
            GameObject paddle2 = Instantiate(player2PaddlePrefab, rightSpawn.position, Quaternion.identity);
            paddle2.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
            
            // spawn ball
            if (NetworkManager.ConnectedClients.Count == 2)
            {
                GameObject ball = Instantiate(ballPrefab, Vector3.zero, Quaternion.identity);
                ball.GetComponent<NetworkObject>().Spawn();
            }
        }
}
