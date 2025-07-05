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
    private Dictionary<BoonType, GameObject> activeBoonButtons = new Dictionary<BoonType, GameObject>();
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
    
    // called by NetworkGameManager when all players are ready
    public void StartBoonSelection()
    {
        if (!IsServer) return;

        boonSelectionActive = true;

        var selectedBoons = availableBoons
            .OrderBy(x => Random.value) // randomnizes the boon list
            .Take(4) // select the first 4, for now i only have 4
            .Select(b => b.type) // project each boonEffect to its boonType enum
            .ToArray(); // converts to an array
        // send these boons to clients so they display the right buttons
        SpawnBoonButtonsClientRpc(selectedBoons);
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
        
        // check if boon is still available
        if (!activeBoonButtons.ContainsKey(boonType)) 
        {
            Debug.Log($"Boon {boonType} not available");
            return;
        }
        
        // check if player already has a boon
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
        
        // remove boon from selection list and update all clients
        RemoveBoonButtonClientRpc(boonType);
        var playerInfo = NetworkGameManager.Instance.GetPlayerInfo(clientId);
        if (playerInfo != null)
        {
            UpdatePlayerInventoryClientRpc(clientId, boonType, playerInfo.playerId); // sending player id
        }
        else
        {
            Debug.LogError($"playerInfo is null for {clientId}");
        }
        
        // check if all players have selected boon
        CheckAllPlayersSelected();
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

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void UpdatePlayerInventoryClientRpc(ulong clientId, BoonType boonType, int playerId)
    {
        Debug.Log($"Updating inventory for client {clientId} with boon {boonType}");

        // find which player slot this client corresponds to
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
                    Debug.Log($"Set boon {boonEffect.effectName} for player {playerId}");
                }
                else
                {
                    Debug.LogError($"could not find BoonEffect for BoonType {boonType}");
                }
            }
            else
            {
                Debug.LogError($"PlayerInventorySlot not found on slot {slotIndex}");
            }
        }
        else
        {
            Debug.LogError($"Slot index {slotIndex} out of range");
        }
    }

    private void CheckAllPlayersSelected()
    {
        var allPlayers = NetworkGameManager.Instance.GetAllPlayers();
        bool allSelected = allPlayers.All(p => playerInventories.ContainsKey(p.clientId) && playerInventories[p.clientId].Count > 0);
        
        if (allSelected)
        {
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
        
        UseBoonServerRpc(clientId, boonType);
    }
    
    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
    private void UseBoonServerRpc(ulong clientId, BoonType boonType)
    {
        if (NetworkManager.Singleton.LocalClientId != clientId)
        {
            Debug.LogWarning($"Client {NetworkManager.Singleton.LocalClientId} tried to use boon owned by {clientId}. Ignored.");
            return;
        }

        if (!playerInventories.ContainsKey(clientId) || !playerInventories[clientId].Contains(boonType))
        {
            Debug.LogWarning($"Client {clientId} tried to use an invalid or unauthorized boon.");
            return;
        }
        
        playerInventories[clientId].Remove(boonType);
        ApplyBoonEffectClientRpc(boonType);
        ClearPlayerInventoryClientRpc(clientId);
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
    
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void ClearPlayerInventoryClientRpc(ulong clientId)
    {
        var playerInfo = NetworkGameManager.Instance.GetPlayerInfo(clientId);
        if (playerInfo != null)
        {
            int slotIndex = playerInfo.playerId - 1;
            if (slotIndex < playerInventorySlots.Length)
            {
                var inventorySlot = playerInventorySlots[slotIndex].GetComponent<PlayerInventorySlot>();
                if (inventorySlot != null)
                {
                    inventorySlot.ClearBoon();
                }
            }
        }
    }
    
    private void ApplyBoonEffect(BoonEffect boonEffect)
    {
        switch (boonEffect.type)
        {
            case BoonType.SpaceshipMode:
                // enable spaceship mode for all paddles
                EnableSpaceshipMode(boonEffect.duration);
                break;
            case BoonType.DoubleBall:
                // Spawn additional ball
                SpawnDoubleBall(boonEffect.duration);
                break;
            case BoonType.BallSpeedBoost:
                // Increase ball speed
                ModifyBallSpeed(1.5f, boonEffect.duration);
                break;
            case BoonType.BallSpeedSlow:
                // Decrease ball speed
                ModifyBallSpeed(0.5f, boonEffect.duration);
                break;
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
}
