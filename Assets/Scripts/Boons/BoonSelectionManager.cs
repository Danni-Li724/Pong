using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class BoonSelectionManager : NetworkBehaviour
{
    [SerializeField] private List<BoonEffect> availableBoons;
    private Dictionary<string, ulong> selectedBoons = new Dictionary<string, ulong>();
    
    public static BoonSelectionManager Instance { get; private set; }
    
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
        if (IsClient && !IsServer)
        {
            // Initialize UI for clients
            InitializeBoonSelectionUI();
        }
    }
    
    private void InitializeBoonSelectionUI()
    {
        if (UIBoonSelection.Instance != null)
        {
            UIBoonSelection.Instance.InitializeBoonButtons(availableBoons);
        }
    }
    
    public void TrySelectBoon(string boonId)
    {
        if (!IsClient) return;
        
        // Check if boon is already selected
        if (selectedBoons.ContainsKey(boonId)) return;
        
        // Check if player already has 2 boons
        var playerInventory = PlayerBoonInventory.Instance.GetBoonsForPlayer(NetworkManager.Singleton.LocalClientId);
        if (playerInventory.Count >= 2) return;
        
        RequestSelectBoonServerRPC(NetworkManager.Singleton.LocalClientId, boonId);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void RequestSelectBoonServerRPC(ulong playerID, string boonID)
    {
        // Validate selection on server
        if (selectedBoons.ContainsKey(boonID)) return;
        
        var playerBoons = PlayerBoonInventory.Instance.GetBoonsForPlayer(playerID);
        if (playerBoons.Count >= 2) return;
        
        // Add to selected boons
        selectedBoons.Add(boonID, playerID);
        
        // Add to player inventory
        PlayerBoonInventory.Instance.AddBoonToPlayer(playerID, boonID);
        
        // Disable boon for all clients
        DisableBoonOnAllClientsClientRPC(boonID);
        
        // Update player's inventory UI
        UpdatePlayerInventoryClientRPC(playerID, boonID);
    }
    
    [ClientRpc]
    private void DisableBoonOnAllClientsClientRPC(string boonId)
    {
        if (UIBoonSelection.Instance != null)
        {
            UIBoonSelection.Instance.MarkBoonAsPicked(boonId);
        }
    }
    
    [ClientRpc]
    private void UpdatePlayerInventoryClientRPC(ulong playerID, string boonID)
    {
        if (NetworkManager.Singleton.LocalClientId == playerID)
        {
            if (UIPlayerInventory.Instance != null)
            {
                UIPlayerInventory.Instance.AddBoonToInventory(boonID);
            }
        }
    }
    
    public bool IsBoonAlreadyPicked(string boonId)
    {
        return selectedBoons.ContainsKey(boonId);
    }
    
    public BoonEffect GetBoonEffect(string boonId)
    {
        return availableBoons.Find(b => b.name == boonId);
    }
}
