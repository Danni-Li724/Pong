using UnityEngine;
[System.Serializable]
public class PlayerInfo : MonoBehaviour
{
    public int playerId;
    public ulong clientId;
    public bool isConnected;
    public Transform spawnPos;
    
    public PlayerInfo(int id, ulong client, Transform spawn)
    {
        playerId = id;
        clientId = client;
        isConnected = true;
        spawnPos = spawn;
    }
}
