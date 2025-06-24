using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
/// <summary>
/// Player identity and set up; also handles player specific UI spawning
/// </summary>
public class PaddleController : NetworkBehaviour
{
    private NetworkVariable<int> playerIdVar = new NetworkVariable<int>();
    public bool IsHorizontal => (PlayerId == 3 || PlayerId == 4);

    public int PlayerId { get; private set; }
    public GameObject playerTargetUIPrefab;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        playerIdVar.OnValueChanged += (_, newId) =>
        {
            PlayerId = newId;
            Debug.Log($"[PaddleController] PlayerId received: {PlayerId}");
            GetComponent<PaddleInputHandler>().InitializeInput(IsHorizontal);
        };

        if (playerIdVar.Value != 0)
        {
            PlayerId = playerIdVar.Value;
            Debug.Log($"[PaddleController] PlayerId was already set: {PlayerId}");
            GetComponent<PaddleInputHandler>().InitializeInput(IsHorizontal);
        }
    }

    public void SetPlayerId(int id)
    {
        playerIdVar.Value = id;
    }

    public int GetPlayerId() => playerIdVar.Value;

    public void ReadyUp()
    {
        if (IsOwner)
            NotifyReadyServerRpc();
    }

    [Rpc(SendTo.Server)]
    void NotifyReadyServerRpc()
    {
        NetworkGameManager manager = FindObjectOfType<NetworkGameManager>();
        manager.MarkPlayerReady(OwnerClientId);
    }

    public void SpawnPlayerSelectionUI()
    {
        if (!IsOwner) return;

        NetworkGameManager manager = FindObjectOfType<NetworkGameManager>();
        List<PlayerInfo> otherPlayers = manager.GetOtherPlayers(NetworkManager.Singleton.LocalClientId);

        GameObject ui = Instantiate(playerTargetUIPrefab);
        PlayerSelectionUI uiScript = ui.GetComponent<PlayerSelectionUI>();
        uiScript.InitializeUI(otherPlayers, NetworkManager.Singleton.LocalClientId, IsHorizontal);
    }
}
