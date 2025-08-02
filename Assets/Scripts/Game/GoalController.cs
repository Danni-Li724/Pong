using Unity.Netcode;
using UnityEngine;

public class GoalController : NetworkBehaviour
{
    public NetworkVariable<int> playerId = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private int cachedId = 999999;
    void Update()
    {
        if (playerId.Value != cachedId)
        {
            cachedId = playerId.Value;
            Debug.Log($"{gameObject.name} on {(IsServer ? "server" : "client")} has playerId {playerId.Value}");
        }
    }
    public void SetGoalForPlayerId(int id)
    {
        if (IsServer)
        {
            playerId.Value = id;
            Debug.Log($"[SERVER] SetGoalForPlayerId: Goal {gameObject.name} set to playerId {id}");
        }
    }
    public int GetGoalForPlayerId()
    {
        Debug.Log($"[CLIENT] GetGoalForPlayerId: Goal {gameObject.name} returning playerId {playerId.Value}");
        return playerId.Value;
    }
}
