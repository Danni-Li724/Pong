using Unity.Netcode;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerBoonInventory : NetworkBehaviour
{
    public static PlayerBoonInventory Instance { get; private set; }
    private Dictionary<ulong, List<string>> playerBoons = new Dictionary<ulong, List<string>>();
    
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
    
    public void AddBoonToPlayer(ulong clientId, string boonId)
    {
        if (!IsServer) return;
        
        if (!playerBoons.ContainsKey(clientId))
            playerBoons[clientId] = new List<string>();
            
        if (playerBoons[clientId].Count < 2)
        {
            playerBoons[clientId].Add(boonId);
        }
    }
    
    public void RemoveBoonFromPlayer(ulong clientId, string boonId)
    {
        if (!IsServer) return;
        
        if (playerBoons.ContainsKey(clientId))
        {
            playerBoons[clientId].Remove(boonId);
        }
    }
    
    public List<string> GetBoonsForPlayer(ulong clientId)
    {
        return playerBoons.TryGetValue(clientId, out var list) ? new List<string>(list) : new List<string>();
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void UseBoonServerRPC(ulong playerID, string boonID, ulong targetPlayerID = 0)
    {
        var playerBoonsList = GetBoonsForPlayer(playerID);
        if (!playerBoonsList.Contains(boonID)) return;
        
        // Remove boon from inventory after use
        RemoveBoonFromPlayer(playerID, boonID);
        
        // Apply the boon effect
        ApplyBoonEffectClientRPC(playerID, boonID, targetPlayerID);
        
        // Update inventory UI
        UpdateInventoryUIClientRPC(playerID, boonID);
    }
    
    [ClientRpc]
    private void ApplyBoonEffectClientRPC(ulong playerID, string boonID, ulong targetPlayerID)
    {
        var boonEffect = BoonSelectionManager.Instance.GetBoonEffect(boonID);
        if (boonEffect != null)
        {
            // Find the target player object
            GameObject targetPlayer = FindPlayerObject(targetPlayerID == 0 ? playerID : targetPlayerID);
            if (targetPlayer != null)
            {
                BoonEffectTimerManager.Instance.StartTimedEffect(targetPlayer, boonEffect);
            }
        }
    }
    
    [ClientRpc]
    private void UpdateInventoryUIClientRPC(ulong playerID, string boonID)
    {
        if (NetworkManager.Singleton.LocalClientId == playerID)
        {
            if (UIPlayerInventory.Instance != null)
            {
                UIPlayerInventory.Instance.RemoveBoonFromInventory(boonID);
            }
        }
    }
    
    private GameObject FindPlayerObject(ulong playerID)
    {
        // This method should find the player GameObject based on playerID
        // Implementation depends on how you manage player objects
        var networkObjects = FindObjectsOfType<NetworkObject>();
        foreach (var netObj in networkObjects)
        {
            if (netObj.OwnerClientId == playerID && netObj.CompareTag("Player"))
            {
                return netObj.gameObject;
            }
        }
        return null;
    }
}
