using UnityEngine;
[System.Serializable]
public class PlayerInfo
{
    public int playerId;
    public ulong clientId;
    public bool isConnected;
    public Transform spawnPos;
    public string displayName;
    public bool isReady;
    
    // strings for network sync
    public string paddleSpriteName;
    public string rocketSpriteName;
    // local refs
    public Sprite paddleSprite;
    public Sprite rocketSprite;
    
    public PlayerInfo(int playerId, ulong client, Transform spawn, string playerName)
    {
        this.playerId = playerId;
        this.clientId = clientId;
        this.spawnPos = spawn;
        this.isReady = false;
        this.isConnected = true;
        this.displayName = displayName;
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
// Updated PlayerInfo class to work with both systems
// public class PlayerInfo
// {
//     public string playerId;
//     public string playerName;
//     public bool isReady;
//     public bool isConnected;
//     public ulong clientId; // For Netcode compatibility
//     public Sprite paddleSprite;
//     
//     // Constructor for session-based player info
//     public PlayerInfo(string id, string name)
//     {
//         playerId = id;
//         playerName = name;
//         isReady = false;
//         isConnected = true;
//     }
//     
//     // Constructor for Netcode compatibility (from your existing code)
//     public PlayerInfo(int gamePlayerId, ulong networkClientId, Transform spawnTransform)
//     {
//         playerId = gamePlayerId.ToString();
//         clientId = networkClientId;
//         isConnected = true;
//         isReady = false;
//     }
//     
//     // Default constructor
//     public PlayerInfo()
//     {
//         isReady = false;
//         isConnected = true;
//     }
// }
