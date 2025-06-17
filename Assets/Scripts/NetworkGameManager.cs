using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Linq;

public class NetworkGameManager : NetworkBehaviour
{
    public Transform leftSpawn, rightSpawn, topSpawn, bottomSpawn;
    public GameObject playerPaddlePrefab;
    public GameObject ballPrefab;

    private int playerCount = 0;
    private Dictionary<ulong, PlayerInfo> allPlayers = new Dictionary<ulong, PlayerInfo>();
    
    // Get list of other players for a specific client
    public List<PlayerInfo> GetOtherPlayers(ulong clientId)
    {
        return allPlayers.Values.Where(p => p.clientId != clientId && p.isConnected).ToList();
    }
    
    // Get player info by client ID
    public PlayerInfo GetPlayerInfo(ulong clientId)
    {
        return allPlayers.ContainsKey(clientId) ? allPlayers[clientId] : null;
    }
    
    // Get all connected players
    public List<PlayerInfo> GetAllPlayers()
    {
        return allPlayers.Values.Where(p => p.isConnected).ToList();
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            // handling disconnection
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
            // spawn paddle 1 for host
            SpawnPlayerPaddle(NetworkManager.Singleton.LocalClientId, 1);
            playerCount = 1;
        }
    }
    
    void OnClientConnected(ulong clientId)
        {
            if (!IsServer) return;

            if (clientId == NetworkManager.Singleton.LocalClientId) return; // host doesn't reconnect

            if (playerCount >= 4)
            {
                Debug.LogWarning("Player count exceeds max player count");
                return;
            }
            playerCount++;
            SpawnPlayerPaddle(clientId, playerCount);
            if (playerCount == 2) // temp
            {
                GameObject ball = Instantiate(ballPrefab, Vector3.zero, Quaternion.identity);
                ball.GetComponent<NetworkObject>().Spawn();
            }
        }
    
    void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;
        
        if (allPlayers.ContainsKey(clientId))
        {
            allPlayers[clientId].isConnected = false;
            Debug.Log($"Player {allPlayers[clientId].playerId} disconnected");
        }
    }

    void SpawnPlayerPaddle(ulong clientId, int playerId)
    {
        Transform spawnTransform = GetSpawnTransform(playerId);
        if (spawnTransform == null)
        {
            Debug.LogError($"No spawnPos found for player {playerId}");
            return;
        }

        // Create player info
        PlayerInfo playerInfo = new PlayerInfo(playerId, clientId, spawnTransform);
        allPlayers[clientId] = playerInfo;

        // Spawn the paddle
        GameObject paddle = Instantiate(playerPaddlePrefab, spawnTransform.position, spawnTransform.rotation);
        paddle.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
        PaddleController paddleController = paddle.GetComponent<PaddleController>();
        if (paddleController != null)
        {
            paddleController.SetPlayerId(playerId);
        }
    }

    Transform GetSpawnTransform(int playerId)
        {
            switch (playerId)
            {
                case 1: return leftSpawn;
                case 2: return rightSpawn;
                case 3: return topSpawn;
                case 4: return bottomSpawn;
                default: return null;
            }
        }
    
    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }
}
