using Unity.Netcode;
using UnityEngine;

public class NetworkGameManager : NetworkBehaviour
{
    public Transform leftSpawn, rightSpawn, topSpawn, bottomSpawn;
    public GameObject player1PaddlePrefab;
    public GameObject player2PaddlePrefab;
    public GameObject player3PaddlePrefab;
    public GameObject player4PaddlePrefab;
    public GameObject ballPrefab;

    private int playerCount = 0;
    
    // spaning players
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            // spawn paddle 1 for host
            GameObject paddle1 = Instantiate(player1PaddlePrefab, leftSpawn.position, Quaternion.identity);
            paddle1.GetComponent<NetworkObject>().SpawnAsPlayerObject(NetworkManager.Singleton.LocalClientId);
            playerCount = 1;
        }
    }
    
    void OnClientConnected(ulong clientId)
        {
            if (!IsServer) return;

            if (clientId == NetworkManager.Singleton.LocalClientId) return; // host doesn't reconnect

            GameObject paddleToSpawn = null;
            Vector3 spawnPosition = Vector3.zero;

            switch (playerCount)
            {
                case 1: // second player
                    paddleToSpawn = player2PaddlePrefab;
                    spawnPosition = rightSpawn.position;
                    break;
                case 2: // third player
                    paddleToSpawn = player3PaddlePrefab;
                    spawnPosition = topSpawn.position;
                    break;
                case 3: // fourth player
                    paddleToSpawn = player4PaddlePrefab;
                    spawnPosition = bottomSpawn.position;
                    break;
                default:
                    Debug.LogWarning("More than 4 players attempted to join. Not supported."); // output this to ui text
                    return;
            }

            if (paddleToSpawn != null)
            {
                GameObject paddle = Instantiate(paddleToSpawn, spawnPosition, Quaternion.identity);
                paddle.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
                playerCount++;

                // Only spawn ball once (when 2+ players are ready)
                if (playerCount == 2)
                {
                    GameObject ball = Instantiate(ballPrefab, Vector3.zero, Quaternion.identity);
                    ball.GetComponent<NetworkObject>().Spawn();
                }
            }
        }
}
