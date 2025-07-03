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
    [SerializeField] private Transform[] playerInventorySlots = new Transform[4]; // Assign in inspector
    
    public static BoonManager Instance { get; private set; }
    
    // Server-side data
    private Dictionary<ulong, List<string>> playerInventories = new Dictionary<ulong, List<string>>();
    private Dictionary<string, GameObject> activeBoonButtons = new Dictionary<string, GameObject>();
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
    private void SpawnBoonButtonsClientRpc(BoonType[] selectedBoons) // string[] can be networked apparently
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
                activeBoonButtons[boonType.ToString()] = buttonObj;
            }
        }
    }
    
    // called by BoonButton when clicked
    public void TrySelectBoon(string boonId)
    {
        if (!IsClient) return;
        
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        RequestBoonServerRpc(clientId, boonId);
    }
    
    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
    private void RequestBoonServerRpc(ulong clientId, string boonId)
    {
        if (!boonSelectionActive) return;
        
        // check if boon is still available
        if (!activeBoonButtons.ContainsKey(boonId)) return;
        
        // check if player already has a boon
        if (playerInventories.ContainsKey(clientId) && playerInventories[clientId].Count > 0) return;
        
        // add boon to player inventory
        if (!playerInventories.ContainsKey(clientId))
            playerInventories[clientId] = new List<string>();
        
        playerInventories[clientId].Add(boonId);
        
        // remove boon from selection list and update all clients
        RemoveBoonButtonClientRpc(boonId);
        UpdatePlayerInventoryClientRpc(clientId, boonId);
        
        // check if all players have selected boon
        CheckAllPlayersSelected();
    }
    
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void RemoveBoonButtonClientRpc(string boonId)
    {
        if (activeBoonButtons.ContainsKey(boonId))
        {
            Destroy(activeBoonButtons[boonId]);
            activeBoonButtons.Remove(boonId);
        }
    }
    
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void UpdatePlayerInventoryClientRpc(ulong clientId, string boonId)
    {
        // find which player slot this client corresponds to
        var playerInfo = NetworkGameManager.Instance.GetPlayerInfo(clientId);
        if (playerInfo != null)
        {
            int slotIndex = playerInfo.playerId - 1;
            if (slotIndex < playerInventorySlots.Length)
            {
                var inventorySlot = playerInventorySlots[slotIndex].GetComponent<PlayerInventorySlot>();
                if (inventorySlot != null)
                {
                    var boonEffect = availableBoons.Find(b => b.name == boonId);
                    inventorySlot.SetBoon(boonEffect, clientId);
                }
            }
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
    
    public void UseBoon(ulong clientId, string boonId)
    {
        if (!IsClient) return;
        
        UseBoonServerRpc(clientId, boonId);
    }
    
    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
    private void UseBoonServerRpc(ulong clientId, string boonId)
    {
        if (!playerInventories.ContainsKey(clientId) || !playerInventories[clientId].Contains(boonId))
            return;
        playerInventories[clientId].Remove(boonId);
        ApplyBoonEffectClientRpc(boonId);
        ClearPlayerInventoryClientRpc(clientId);
    }
    
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void ApplyBoonEffectClientRpc(string boonId)
    {
        var boonEffect = availableBoons.Find(b => b.name == boonId);
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
    
    public List<string> GetPlayerInventory(ulong clientId)
    {
        return playerInventories.ContainsKey(clientId) ? playerInventories[clientId] : new List<string>();
    }
}
