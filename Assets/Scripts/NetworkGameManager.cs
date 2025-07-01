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
    private GameObject spawnedBall; // current ball in scene
    [SerializeField] private GameObject circles;
    [SerializeField] private GameObject stars;
    
    public NetworkVariable<int> maxPlayers = new NetworkVariable<int>(2, 
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private bool allPlayersJoined = false;

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
            //ShowPlayerSelectionForHost();
        }
    }

   
    void OnClientConnected(ulong clientId)
        {
            if (!IsServer) return;

            if (clientId == NetworkManager.Singleton.LocalClientId) return; // host doesn't reconnect

            if (playerCount >= maxPlayers.Value)
            {
                Debug.LogWarning("Player count exceeds max player count");
                return;
            }
            playerCount++;
            SpawnPlayerPaddle(clientId, playerCount);
            AssignGoals();
            SyncPlayerSprite(clientId); // IMMEDIATELY syncing sprites for new players
            SyncAllPlayersToClient(clientId); // Also sync all existing players to the new client
            // NOW show button
            if (playerCount == maxPlayers.Value)
            {
                EnableReadyUpButtonClientRpc();
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
    
    #region STARTING GAME
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
    
    [Rpc(SendTo.ClientsAndHost)]
    private void EnableReadyUpButtonClientRpc()
    {
        ReadyUpButton[] readyButtons = FindObjectsOfType<ReadyUpButton>();
        foreach (var button in readyButtons)
        {
            button.EnableButton();
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
        spawnedBall = Instantiate(ballPrefab, Vector3.zero, Quaternion.identity);
        spawnedBall.GetComponent<NetworkObject>().Spawn();
        // now syncing all sprites before starting
        foreach (var kvp in allPlayers)
        {
            if (kvp.Value.isConnected)
            {
                SyncPlayerSprite(kvp.Key);
            }
        }
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
    
    #endregion
    
#region SPRITE SYCING
private void SyncPlayerSprite(ulong clientId) // sync sprite for a specific player to all clients
{
    if (!allPlayers.ContainsKey(clientId)) return;
        
    PlayerInfo playerInfo = allPlayers[clientId];
    SyncPlayerSpriteClientRpc(clientId, playerInfo.paddleSpriteName, playerInfo.rocketSpriteName);
}
private void SyncAllPlayersToClient(ulong targetClientId) // syncing all existing players to new joiners
{
    foreach (var kvp in allPlayers)
    {
        if (kvp.Value.isConnected) // kvp = Key-ValuePair for my Dictionary<ulong, PlayerInfo> ('allPlayers') :)
        {
            SyncPlayerSpriteToSpecificClientRpc(
                kvp.Key, 
                kvp.Value.paddleSpriteName, 
                kvp.Value.rocketSpriteName,
                RpcTarget.Single(targetClientId, RpcTargetUse.Temp)
            );
        }
    }
}
[Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
private void SyncPlayerSpriteClientRpc(ulong targetClientId, string paddleSpriteName, string rocketSpriteName)
{
    ApplyPlayerSprite(targetClientId, paddleSpriteName, rocketSpriteName);
}

[Rpc(SendTo.SpecifiedInParams, Delivery = RpcDelivery.Reliable)]
private void SyncPlayerSpriteToSpecificClientRpc(ulong targetClientId, string paddleSpriteName, string rocketSpriteName, RpcParams rpcParams = default)
{
    ApplyPlayerSprite(targetClientId, paddleSpriteName, rocketSpriteName);
}
private void ApplyPlayerSprite(ulong targetClientId, string paddleSpriteName, string rocketSpriteName)
{
    PaddleController[] paddles = FindObjectsByType<PaddleController>(FindObjectsSortMode.None);
    foreach (var paddle in paddles)
    {
        if (paddle.OwnerClientId == targetClientId)
        {
            var renderer = paddle.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                Sprite paddleSprite = Resources.Load<Sprite>($"Sprites/{paddleSpriteName}");
                if (paddleSprite != null)
                {
                    renderer.sprite = paddleSprite;
                }
                else
                {
                    Debug.LogWarning($"can't find paddle sprite {paddleSpriteName}");
                }
            }

            if (allPlayers.ContainsKey(targetClientId)) // update PlayerInfo
            {
                PlayerInfo playerInfo = allPlayers[targetClientId];
                playerInfo.paddleSprite = Resources.Load<Sprite>($"Sprites/{paddleSpriteName}");
                playerInfo.rocketSprite = Resources.Load<Sprite>($"Sprites/{rocketSpriteName}");
            }

            break;
        }
    }
}

private void BroadcastSpaceshipSpriteToAllClients(string spriteName)
{
    foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
    {
        if(clientId == OwnerClientId) continue;
        var rpcParams = new RpcParams
        {
            // Send = new RpcSendParams { Target = new[] { clientId }}

        };
    }
}

#endregion

#region  SPACESHIP MODE

public void ActivateSpaceshipModeFromEditor()
{
    if (!IsServer) return;

    Debug.Log("Editor launched Spaceship mode.");
    StartSpaceshipMode();
}
    private void StartSpaceshipMode()
    {
        Debug.Log("Mode launched");
        if (spawnedBall != null && spawnedBall.TryGetComponent(out NetworkObject netObj) && netObj.IsSpawned)
        {
            netObj.Despawn();
        }
        ToggleSpaceshipVisualsClientRpc(true);
        foreach (var player in GetAllPlayers())
        {
            if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(player.clientId)) continue;
            foreach (var kvp in allPlayers)
            {
                SyncPlayerSprite(kvp.Key);
            }
           SyncRocketSpriteToAllClients(50f, 0.5f);
        }
        StartCoroutine(StopSpaceshipModeAfterSeconds(30f));
    }

    private void SyncRocketSpriteToAllClients(float bulletSpeed, float fireCooldown)
    {
        foreach (var player in GetAllPlayers())
        {
           ActivateSpaceshipModeClientRpc(
               player.clientId,
               player.rocketSpriteName,
               bulletSpeed,
               fireCooldown,
               RpcTarget.Everyone
               );
        }
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    private void ActivateSpaceshipModeClientRpc(ulong targetClientId, string rocketSpriteName, float bulletSpeed, float fireCooldown, RpcParams rpcParams = default)
    {
        PaddleController[] paddles = FindObjectsOfType<PaddleController>();
        foreach (var paddle in paddles)
        {
            if (paddle.OwnerClientId != targetClientId) continue;

            Sprite rocketSprite = Resources.Load<Sprite>($"Sprites/{rocketSpriteName}");
            GameObject bulletPrefab = Resources.Load<GameObject>("Prefabs/Bullet");
            //PlayerInfo playerInfo = GetPlayerInfo(paddle.OwnerClientId);
            if (rocketSprite == null)
            {
                Debug.LogWarning($"Could not load rocket sprite: {rocketSpriteName}");
                return;
            }
            if (bulletPrefab == null)
            {
                Debug.LogError("Could not load bullet prefab from Resources/Prefabs/Bullet");
                return;
            }

            paddle.SetSpaceshipMode(true, rocketSprite, bulletPrefab, bulletSpeed, fireCooldown);
            break;
        }
    }

    private IEnumerator StopSpaceshipModeAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        PaddleController[] allPaddles = FindObjectsByType<PaddleController>(FindObjectsSortMode.None);
        foreach (var paddle in allPaddles)
        {
            paddle.SetSpaceshipMode(false, null, null, 0f, 0f);

            var info = GetPlayerInfo(paddle.OwnerClientId);
            if (info != null && info.spawnPos != null)
            {
                paddle.transform.position = info.spawnPos.position;
                paddle.transform.rotation = info.spawnPos.rotation;

                var renderer = paddle.GetComponent<SpriteRenderer>();
                if (renderer != null && info.paddleSprite != null)
                {
                    renderer.sprite = info.paddleSprite;
                }
            }
        }
        // Respawn ball
        if (spawnedBall != null && !spawnedBall.GetComponent<NetworkObject>().IsSpawned)
        {
            spawnedBall.GetComponent<NetworkObject>().Spawn();
            spawnedBall.transform.position = Vector3.zero;
        }
        ToggleSpaceshipVisualsClientRpc(false);
        Debug.Log("Spaceship Mode Ended");
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    private void ToggleSpaceshipVisualsClientRpc(bool enableSpaceshipMode)
    {
        if (circles != null) circles.SetActive(!enableSpaceshipMode);
        if (stars != null) stars.SetActive(enableSpaceshipMode);
    }
#endregion

}

