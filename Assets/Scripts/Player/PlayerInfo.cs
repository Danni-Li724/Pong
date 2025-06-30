using UnityEngine;
[System.Serializable]
public class PlayerInfo : MonoBehaviour
{
    public int playerId;
    public ulong clientId;
    public bool isConnected;
    public Transform spawnPos;
    public bool isReady;
    
    public Sprite paddleSprite;
    public Sprite rocketSprite;
    
    public PlayerInfo(int playerId, ulong client, Transform spawn)
    {
        this.playerId = playerId;
        this.clientId = clientId;
        this.spawnPos = spawn;
        this.isReady = false;
        this.isConnected = true;
        
        paddleSprite = Resources.Load<Sprite>($"Sprites/Player{playerId}Paddle");
        rocketSprite = Resources.Load<Sprite>($"Sprites/Player{playerId}Rocket");
        
        if (paddleSprite == null || rocketSprite == null)
        {
            Debug.LogWarning($"Missing sprite for Player {playerId}");
        }
    }
}
