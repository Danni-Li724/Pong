using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class BoonSelectionManager : NetworkBehaviour
{
    public List<BoonEffect> availableBoons;
    private Dictionary<string, ulong> selectedBoons = new();
    public static BoonSelectionManager Instance { get; private set; }

    private void Awake()
    {
        if(Instance == null) Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {

        }
       
    }

    public void TrySelectBoon(string boonId)
    {
        if(!IsClient) return;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestSelectBoonServerRPC(ulong playerID, string booniD)
    {
        
    }

    void OnBoonListUpdated(NetworkListEvent<int> change)
    {
        // handle locally?
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitBoonSelectionServerRpc(ulong clientId, string boonId)
    {
        if (selectedBoons.ContainsKey(boonId)) return;
        selectedBoons.Add(boonId, clientId);
        PlayerBoonInventory.Instance.AddBoonToPlayer(clientId, boonId);
    }

    [ClientRpc]
    private void DisableBoonOnAllClients_ClientRPC(string boonId) // disable selected boons
    {
        
    }

    public bool IsBoonAlreadyPicked(string boonId)
    {
        return selectedBoons.ContainsKey(boonId);
    }
}
