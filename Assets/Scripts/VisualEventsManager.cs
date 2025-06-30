using UnityEngine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.Serialization;
using System.Collections.Generic;

public class VisualEventsManager : NetworkBehaviour
{
    [Header("Score UI References")]
    public Text player1ScoreText;
    public Text player2ScoreText;
    public Text player3ScoreText;
    public Text player4ScoreText;
    
    public VisualEffects visualEffects;

    [Header("Ball Visuals Reference")]
    public BallVisuals ballVisuals;

    [Header("Paddle Sprites Reference")] 
    public Dictionary<int, string> paddleSpriteNames =
        new Dictionary<int, string>()
        {
            { 1, "Player1Paddle" },
            { 2, "Player2Paddle" },
            { 3, "Player3Paddle" },
            { 4, "Player4Paddle" }
        };
    public Dictionary<int, string> rocketSpriteNames = new Dictionary<int, string>()
    {
        { 1, "Player1Rocket" },
        { 2, "Player2Rocket" },
        { 3, "Player3Rocket" },
        { 4, "Player4Rocket" }
    };

    public static VisualEventsManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        BallPhysics.OnPlayerHit += HandlePlayerHit;
        BallPhysics.OnPlayerScored += HandlePlayerScored;
    }

    private void OnDisable()
    {
        BallPhysics.OnPlayerHit -= HandlePlayerHit;
        BallPhysics.OnPlayerScored -= HandlePlayerScored;
    }

    public void RegisterBallVisuals(BallVisuals visuals)
    {
        ballVisuals = visuals;
        Debug.Log("Ball visuals registered.");
    }
    
    public Sprite GetPaddleSprite(int playerId)
    {
        if (paddleSpriteNames.TryGetValue(playerId, out string spriteName))
        {
            return Resources.Load<Sprite>($"Sprites/{spriteName}");
        }
        return null;
    }
    
    public Sprite GetRocketSprite(int playerId)
    {
        if (rocketSpriteNames.TryGetValue(playerId, out string spriteName))
        {
            return Resources.Load<Sprite>($"Sprites/{spriteName}");
        }
        return null;
    }

    public void AssignPaddleSpritesToAllPlayers()
    {
        if(!IsServer) return;
        var gameManager = FindFirstObjectByType<NetworkGameManager>();
        if (gameManager == null) return;
        foreach (var playerInfo in gameManager.GetAllPlayers())
        {
            Sprite paddleSprite = GetPaddleSprite(playerInfo.playerId);
            Sprite rocketSprite = GetRocketSprite(playerInfo.playerId);
            playerInfo.paddleSprite = paddleSprite;
            playerInfo.rocketSprite = rocketSprite;
            
            // sending sprite names to all clients to sync
            AssignPlayerSpritesClientRpc(playerInfo.clientId, playerInfo.playerId,
                paddleSpriteNames[playerInfo.playerId],
                rocketSpriteNames[playerInfo.playerId]);
        }
    }

    [Rpc(SendTo.Everyone, Delivery = RpcDelivery.Reliable)]
    private void AssignPlayerSpritesClientRpc(ulong targetClientId, int playerId, string paddleSpriteName,
        string rocketSpriteName)
    {
        //if (IsServer) return;
        PaddleController[] paddles = FindObjectsByType<PaddleController>(FindObjectsSortMode.None);
        foreach (var paddle in paddles)
        {
            if (paddle.OwnerClientId == targetClientId)
            {
                var renderer = paddle.GetComponent<SpriteRenderer>();
                Sprite newPaddleSprite = Resources.Load<Sprite>($"Sprites/{paddleSpriteName}");
                renderer.sprite = newPaddleSprite;
                // Store rocket sprite for spaceship mode
                var playerInfo = FindFirstObjectByType<NetworkGameManager>().GetPlayerInfo(paddle.OwnerClientId);
                if (playerInfo != null)
                {
                    playerInfo.paddleSprite = newPaddleSprite;
                    playerInfo.rocketSprite = Resources.Load<Sprite>($"Sprites/{rocketSpriteName}");
                }

                break;
            }
        }
    }

    public void RegisterPaddle(PaddleController paddle)
    {
        var spriteRenderer = paddle.GetComponent<SpriteRenderer>();
        int paddleId = paddle.gameObject.GetInstanceID();
        spriteRenderer.sprite = GetPaddleSprite(paddleId);
    }
    private void HandlePlayerScored(int playerId)
    {
       //
    }

    private void HandlePlayerHit(int playerId)
    {
        if (!IsServer) return;

        Debug.Log($"VisualEventsManager: Player {playerId} hit detected");
        TriggerBackgroundChangeClientRpc(playerId);
        TriggerBallColorChangeClientRpc(playerId);
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void TriggerBackgroundChangeClientRpc(int playerId)
    {
        visualEffects?.HandleColorChange(playerId);
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void TriggerBallColorChangeClientRpc(int playerId)
    {
        ballVisuals?.HandleColorChange(playerId);
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    public void UpdateScoreUIClientRpc(int p1, int p2, int p3, int p4)
    {
        if (player1ScoreText != null) player1ScoreText.text = p1.ToString();
        if (player2ScoreText != null) player2ScoreText.text = p2.ToString();
        if (player3ScoreText != null) player3ScoreText.text = p3.ToString();
        if (player4ScoreText != null) player4ScoreText.text = p4.ToString();
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)] // future method
    public void TriggerInventoryUIClientRpc(int effectId, int playerId)
    {
        switch (effectId)
        {
           case 1:  break; 
        }
    }
}
