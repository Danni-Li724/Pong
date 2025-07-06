using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BoonManager : NetworkBehaviour
{
   [Header("Boon Setup")]
    [SerializeField] private List<BoonEffect> availableBoons;
    [SerializeField] private Transform boonButtonContainer;
    [SerializeField] private GameObject boonButtonPrefab;
    
    [Header("Player Inventories")]
    [SerializeField] private Transform[] playerInventorySlots = new Transform[4];

    public static BoonManager Instance { get; private set; }
    
    // Server-side data - fixed to using boonTypes instead of strings because netcode has issues sending string[] in rpc :(...
    private Dictionary<ulong, List<BoonType>> playerInventories = new Dictionary<ulong, List<BoonType>>();
    // Client-side data - synced from server
    private Dictionary<ulong, List<BoonType>> clientPlayerInventories = new Dictionary<ulong, List<BoonType>>();
    private Dictionary<BoonType, GameObject> activeBoonButtons = new Dictionary<BoonType, GameObject>();
    
    // Server-side tracking of available boons
    private List<BoonType> availableBoonTypes = new List<BoonType>();
    private bool boonSelectionActive = false;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // initializing client inventory tracking
        if (IsClient)
        {
            clientPlayerInventories.Clear();
        }
    }
    
    // called by NetworkGameManager when all players are ready
    public void StartBoonSelection()
    {
        if (!IsServer) return;

        boonSelectionActive = true;
        playerInventories.Clear();
        
        // Set up available boons on server
        availableBoonTypes = availableBoons
            .OrderBy(x => Random.value) // randomnizes the boon list
            .Take(4) // select the first 4, for now i only have 4
            .Select(b => b.type) // project each boonEffect to its boonType enum
            .ToList(); // keep as list for easier manipulation
        
        // Sending a clear inventory state to all clients
        SyncAllInventoriesClientRpc(new ulong[0], new BoonType[0], new int[0]);
        
        // send these boons to clients so they display the right buttons
        SpawnBoonButtonsClientRpc(availableBoonTypes.ToArray());
    }
    
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void SpawnBoonButtonsClientRpc(BoonType[] selectedBoons)
    {
        foreach (Transform child in boonButtonContainer)
            Destroy(child.gameObject);
        activeBoonButtons.Clear();

        foreach (BoonType boonType in selectedBoons)
        {
            var boonEffect = availableBoons.Find(b => b.type == boonType);
            if (boonEffect != null)
            {
                GameObject buttonObj = Instantiate(boonButtonPrefab, boonButtonContainer);
                var button = buttonObj.GetComponent<BoonButton>();
                button.Initialize(boonEffect, this);
                // Store using BoonType directly
                activeBoonButtons[boonType] = buttonObj;
            }
        }
    }
    
    // called by BoonButton when clicked
    public void TrySelectBoon(BoonType boonType)
    {
        if (!IsClient) return;
        
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        RequestBoonServerRpc(clientId, boonType);
    }
    
    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
    private void RequestBoonServerRpc(ulong clientId, BoonType boonType)
    {
        Debug.Log($"Server received boon request from client {clientId} for boon {boonType}");
        
        if (!boonSelectionActive) 
        {
            Debug.Log("Boon selection not active");
            return;
        }
        
        // check if boon is still available in server list
        if (!availableBoonTypes.Contains(boonType)) 
        {
            Debug.Log($"Boon {boonType} not available");
            return;
        }
        
        if (playerInventories.ContainsKey(clientId) && playerInventories[clientId].Count > 0) 
        {
            Debug.Log($"Player {clientId} already has a boon");
            return;
        }
        
        // add boon to player inventory
        if (!playerInventories.ContainsKey(clientId))
            playerInventories[clientId] = new List<BoonType>();
        
        playerInventories[clientId].Add(boonType);
        Debug.Log($"Added boon {boonType} to player {clientId} inventory");
        
        // remove boon from available list
        availableBoonTypes.Remove(boonType);
        
        // remove boon from selection and sync all inventories
        RemoveBoonButtonClientRpc(boonType);
        
        // Add a small delay before syncing to ensure button removal is processed
        StartCoroutine(DelayedSync());
        
        CheckAllPlayersSelected();
    }
    
    private System.Collections.IEnumerator DelayedSync()
    {
        yield return new WaitForSeconds(0.1f); // small delay to ensure proper order
        SyncAllInventoriesToClients();
    }
    
    private void SyncAllInventoriesToClients() // syncing all player inventories to all clients
    {
        if (!IsServer) return;
        
        List<ulong> clientIds = new List<ulong>();
        List<BoonType> boonTypes = new List<BoonType>();
        List<int> playerIds = new List<int>();
        
        foreach (var kvp in playerInventories)
        {
            ulong clientId = kvp.Key;
            var playerInfo = NetworkGameManager.Instance.GetPlayerInfo(clientId);
            
            if (playerInfo != null)
            {
                foreach (var boonType in kvp.Value)
                {
                    clientIds.Add(clientId);
                    boonTypes.Add(boonType);
                    playerIds.Add(playerInfo.playerId);
                }
            }
        }
        
        SyncAllInventoriesClientRpc(clientIds.ToArray(), boonTypes.ToArray(), playerIds.ToArray());
    }
    
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void SyncAllInventoriesClientRpc(ulong[] clientIds, BoonType[] boonTypes, int[] playerIds)
    {
        Debug.Log($"Syncing inventories - received {clientIds.Length} boons");
        
        // Clear all inventory slots first
        foreach (var slot in playerInventorySlots)
        {
            if (slot != null)
            {
                var inventorySlot = slot.GetComponent<PlayerInventorySlot>();
                if (inventorySlot != null)
                {
                    inventorySlot.ClearBoon();
                }
            }
        }
        
        // clear client inventory
        clientPlayerInventories.Clear();
        
        // rebuild inventories from synced data x_x
        for (int i = 0; i < clientIds.Length; i++)
        {
            ulong clientId = clientIds[i];
            BoonType boonType = boonTypes[i];
            int playerId = playerIds[i];
            
            // Track client inventory
            if (!clientPlayerInventories.ContainsKey(clientId))
                clientPlayerInventories[clientId] = new List<BoonType>();
            clientPlayerInventories[clientId].Add(boonType);
            
            // Update visual slot
            int slotIndex = playerId - 1;
            if (slotIndex < playerInventorySlots.Length && slotIndex >= 0)
            {
                var inventorySlot = playerInventorySlots[slotIndex].GetComponent<PlayerInventorySlot>();
                if (inventorySlot != null)
                {
                    var boonEffect = availableBoons.Find(b => b.type == boonType);
                    if (boonEffect != null)
                    {
                        inventorySlot.SetBoon(boonEffect, clientId);
                        Debug.Log($"Synced boon {boonEffect.effectName} for player {playerId} (client {clientId})");
                    }
                }
            }
        }
    }
    
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void RemoveBoonButtonClientRpc(BoonType boonType)
    {
        Debug.Log($"Removing boon button for {boonType}");
        if (activeBoonButtons.ContainsKey(boonType))
        {
            Destroy(activeBoonButtons[boonType]);
            activeBoonButtons.Remove(boonType);
        }
    }

    private void CheckAllPlayersSelected()
    {
        if (!IsServer) return;
        
        var allPlayers = NetworkGameManager.Instance.GetAllPlayers();
        Debug.Log($"All players count: {allPlayers.Count}");
        if (allPlayers.Count == 0)
        {
            // had to add this because the All() in Linq apparently returns true by default if I have an empty collection...debugged for hours
            return;
        }
        bool allSelected = allPlayers.All(p => playerInventories.ContainsKey(p.clientId) && playerInventories[p.clientId].Count > 0);
        
        Debug.Log($"Checking all players selected: {allPlayers.Count} players, {playerInventories.Count} have boons");
        
        if (allSelected)
        {
            Debug.Log("All players have selected boons!");
            boonSelectionActive = false;
            ClearRemainingButtonsClientRpc();
            var gameManager = NetworkGameManager.Instance;
            if (gameManager != null)
            {
                gameManager.OnAllBoonsSelected();
            }
        }
    }
    
    
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void ClearRemainingButtonsClientRpc()
    {
        foreach (var buttonObj in activeBoonButtons.Values)
        {
            Destroy(buttonObj);
        }
        activeBoonButtons.Clear();
    }
    
    public void UseBoon(ulong clientId, BoonType boonType)
    {
        if (!IsClient) return;
        
        // Only allow the owner to use their boon
        if (NetworkManager.Singleton.LocalClientId != clientId) return;
        
        RequestUseBoonServerRpc(clientId, boonType);
    }
    
    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
    private void RequestUseBoonServerRpc(ulong clientId, BoonType boonType)
    {
        Debug.Log($"Server received use boon request from client {clientId} for boon {boonType}");
        
        // check if player has this boon
        if (!playerInventories.ContainsKey(clientId) || !playerInventories[clientId].Contains(boonType))
        {
            Debug.Log($"Player {clientId} doesn't have boon {boonType}");
            return;
        }
        
        // apply boon effect to all clients
        ApplyBoonEffectClientRpc(boonType);
        
        // remove boon from player inventory
        playerInventories[clientId].Remove(boonType);
        
        // Sync updated inventories to all clients
        // not removing MusicChange boon after use
        if (boonType != BoonType.MusicChange)
        {
            playerInventories[clientId].Remove(boonType);
            SyncAllInventoriesToClients();
        }
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void ApplyBoonEffectClientRpc(BoonType boonType)
    {
        var boonEffect = availableBoons.Find(b => b.type == boonType);
        if (boonEffect != null)
        {
            ApplyBoonEffect(boonEffect);
        }
    }
    
    private void ApplyBoonEffect(BoonEffect boonEffect)
    {
        switch (boonEffect.type)
        {
            case BoonType.SpaceshipMode:
                EnableSpaceshipMode(boonEffect.duration);
                break;
            case BoonType.DoubleBall:
                SpawnDoubleBall(boonEffect.duration);
                break;
            case BoonType.BallSpeedBoost:
                ModifyBallSpeed(1.5f, boonEffect.duration);
                break;
            case BoonType.MusicChange:
                CycleMusicTrack();
                break;
        }
    }
    
    private void CycleMusicTrack()
    {
        var audioManager = AudioManager.instance;
        if (audioManager != null)
        {
            audioManager.CycleMusicServerRpc();
        }
    }
    
    private void EnableSpaceshipMode(float duration)
    {
        // var paddles = FindObjectsOfType<PaddleController>();
        // foreach (var paddle in paddles)
        // {
        //     paddle.EnableSpaceshipMode(duration);
        // }
        NetworkGameManager.Instance.StartSpaceshipMode();
    }
    
    private void SpawnDoubleBall(float duration)
    {
        var gameManager = FindObjectOfType<NetworkGameManager>();
        if (gameManager != null)
        {
            gameManager.SpawnAdditionalBall(duration);
        }
    }
    
    private void ModifyBallSpeed(float multiplier, float duration)
    {
        var balls = FindObjectsOfType<BallPhysics>();
        foreach (var ball in balls)
        {
            ball.ModifySpeed(multiplier, duration);
        }
    }
    public List<BoonType> GetPlayerBoons(ulong clientId) // debug/helper function
    {
        if (IsServer)
        {
            return playerInventories.ContainsKey(clientId) ? playerInventories[clientId] : new List<BoonType>();
        }
        else
        {
            return clientPlayerInventories.ContainsKey(clientId) ? clientPlayerInventories[clientId] : new List<BoonType>();
        }
    }
}
