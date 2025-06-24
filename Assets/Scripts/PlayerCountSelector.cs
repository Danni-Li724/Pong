using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class PlayerCountSelector : NetworkBehaviour
{
    public GameObject playerCountSelectionPanel;

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton.IsListening);
        if (IsHost)
        {
            playerCountSelectionPanel.SetActive(true);
        }
        else
        {
            playerCountSelectionPanel.SetActive(false);
        }
    }
    public void SelectPlayerCount(int count)
    {
        if (!IsHost) return;
        var gameManager = FindObjectOfType<NetworkGameManager>();
        if (gameManager != null)
        {
            gameManager.SetMaxPlayersServerRpc(count);
            HidePlayerPanelClientRpc();
        }
    }
    
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    public void HidePlayerPanelClientRpc()
    {
        playerCountSelectionPanel.SetActive(false); 
    }
}
