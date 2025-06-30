using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using System.Linq;
using System.Runtime.InteropServices;


public class NetworkGameManager : NetworkBehaviour
{
    public Transform leftSpawn, rightSpawn, topSpawn, bottomSpawn;
    public GameObject playerPaddlePrefab;
    public GameObject ballPrefab;
    
    public NetworkVariable<int> maxPlayers = new NetworkVariable<int>(2, 
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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
    [Rpc(SendTo.Server)]
    public void SetMaxPlayersServerRpc(int playerCount)
    {
        if (!IsServer) return;
        if (playerCount >= 2 && playerCount <= 4)
        {
            maxPlayers.Value = playerCount;
            Debug.Log("Player count in this game set to " + playerCount);
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
            AssignGoals();
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
        NetworkObject networkObject = paddle.GetComponent<NetworkObject>();
        networkObject.SpawnAsPlayerObject(clientId);
        PaddleController paddleController = paddle.GetComponent<PaddleController>();
        if (paddleController != null)
        {
            paddleController.SetPlayerId(playerId);
            // loading custom paddle sprites
            var renderer = paddle.GetComponent<SpriteRenderer>();
            if (renderer != null && playerInfo.paddleSprite != null)
            {
                renderer.sprite = playerInfo.paddleSprite;
            }
        }
    }
    private void AssignGoals()
    {
        GoalController[] allGoals = FindObjectsOfType<GoalController>();
        Transform[] spawnPoints = new Transform[]
        {
            leftSpawn,
            rightSpawn,
            topSpawn,
            bottomSpawn
        };
        for (int i = 0; i < allGoals.Length; i++)
        {
            GoalController closestGoal = null;
            float closestDistance = float.MaxValue;
            int matchedPlayerId = -1;
            // setting goal to player by checking distance
            for (int playerId = 1; playerId <= spawnPoints.Length; playerId++)
            {
                float dist = Vector3.Distance(allGoals[i].transform.position, spawnPoints[playerId - 1].position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestGoal = allGoals[i];
                    matchedPlayerId = playerId;
                }
            }

            if (closestGoal != null)
            {
                closestGoal.SetGoalForPlayerId(matchedPlayerId);
                Debug.Log($"Assigned goal at {closestGoal.transform.position} to player {matchedPlayerId}");
            }
        }
    }
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void TriggerPlayerSelectionUIClientRpc()
    {
        Debug.Log("All players are ready, making player selection buttons.");
        PaddleController[] allPaddles = FindObjectsOfType<PaddleController>();

        foreach (var paddle in allPaddles)
        {
            if (paddle.IsOwner)
            {
                paddle.SpawnPlayerSelectionUI();
            }
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
    public void MarkPlayerReady(ulong clientId)
    {
        if(!IsServer) return;
        if(allPlayers.ContainsKey(clientId))
        {
            allPlayers[clientId].isReady = true;
            if (AllPlayersReady())
            {
                StartGame();
            }
        };
    }
    private void StartGame()
    {
        GameObject ball = Instantiate(ballPrefab, Vector3.zero, Quaternion.identity);
        ball.GetComponent<NetworkObject>().Spawn();
        TriggerPlayerSelectionUIClientRpc();
    }
    private bool AllPlayersReady()
    {
        return allPlayers.Values.Where(p => p.isConnected).Count(p => p.isReady) == maxPlayers.Value;
    }
    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    /// TEST
  [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
    public void RequestSpaceshipModeServerRpc()
    {
        Debug.Log("Requesting Spaceship mode.");
        StartSpaceshipModeClientRpc();
    }
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    public void StartSpaceshipModeClientRpc()
    {
        PaddleController[] allPaddles = FindObjectsOfType<PaddleController>();

        foreach (var paddle in allPaddles)
        {
            if (paddle == null) continue;
            ulong ownerId = paddle.OwnerClientId;
            PlayerInfo playerInfo = GetPlayerInfo(ownerId);
            if (playerInfo == null) continue;
            GameObject bulletPrefab = Resources.Load<GameObject>("Prefabs/Bullet");
            paddle.SetSpaceshipMode(
                true,
                playerInfo.rocketSprite,
                bulletPrefab,
                10f,
                0.5f
            );
        }
        StartCoroutine(StopSpaceshipModeAfterSeconds(10f));
    }
    private IEnumerator StopSpaceshipModeAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        // fine unity whatever -_- i do sort
        PaddleController[] allPaddles = FindObjectsByType<PaddleController>(FindObjectsSortMode.None);
        foreach (var paddle in allPaddles)
        {
            paddle.SetSpaceshipMode(false, null, null, 0f, 0f);
            // reset paddle position to pong
            var info = GetPlayerInfo(paddle.OwnerClientId);
            if (info != null && info.spawnPos != null)
            {
                paddle.transform.position = info.spawnPos.position;
                paddle.transform.rotation = info.spawnPos.rotation;
            }
        }
        Debug.Log("Spaceship Mode Ended");
    }
}

