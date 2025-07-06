using Unity.Netcode;
using UnityEngine;

public class PlayerLobbyState : NetworkBehaviour
{
    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(false, 
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public void ToggleReady()
    { if(IsOwner)
        isReady.Value = !isReady.Value;
    }
}
