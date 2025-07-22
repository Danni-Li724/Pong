using UnityEngine;
[System.Serializable]
public class PlayerInfo
{
    public int playerId;
    public string playerName;
    public ulong clientId;
    public bool isConnected;
    public Transform spawnPos;
    public bool isReady;
    
    // strings for network sync
    public string paddleSpriteName;
    public string rocketSpriteName;
    // local refs
    public Sprite paddleSprite;
    public Sprite rocketSprite;
    
    public PlayerInfo(int playerId, ulong client, Transform spawn)
    {
        this.playerId = playerId;
        this.playerName = playerName;
        this.clientId = clientId;
        this.spawnPos = spawn;
        this.isReady = false;
        this.isConnected = true;
         // storing names
        this.paddleSpriteName = $"Player{playerId}Paddle";
        this.rocketSpriteName = $"Player{playerId}Rocket";
        
        LoadSprites();
        
        if (paddleSprite == null || rocketSprite == null)
        {
            Debug.LogWarning($"Missing sprite for Player {playerId}");
        }
    }

    public void LoadSprites()
    {
        paddleSprite = Resources.Load<Sprite>($"Sprites/{paddleSpriteName}");
        rocketSprite = Resources.Load<Sprite>($"Sprites/{rocketSpriteName}");
        if (paddleSprite == null)
        {
            Debug.LogWarning($"Missing paddle sprite: Sprites/{paddleSpriteName}");
        }
        if (rocketSprite == null)
        {
            Debug.LogWarning($"Missing rocket sprite: Sprites/{rocketSpriteName}");
        }
    }
}
