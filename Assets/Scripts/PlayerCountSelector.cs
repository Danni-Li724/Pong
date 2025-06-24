using Unity.Netcode;
using UnityEngine;

public class PlayerCountSelector : MonoBehaviour
{
    public GameObject playerCountSelectionPanel;

    private void Start()
    {
        if (!NetworkManager.Singleton.IsHost) // only show for host
        {
            playerCountSelectionPanel.SetActive(false);
        }
    }
    public void SelectPlayerCount(int count)
    {
        if (NetworkManager.Singleton.IsHost)
        {
            FindObjectOfType<NetworkGameManager>().SetMaxPlayersServerRpc(count);
            playerCountSelectionPanel.SetActive(false);
        }
    }
}
