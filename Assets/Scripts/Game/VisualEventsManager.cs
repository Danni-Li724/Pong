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
    
    [Header("Game Visuals")]
    [SerializeField] private GameObject space;
    [SerializeField] private GameObject circles;
    [SerializeField] private GameObject city;
    [SerializeField] private GameObject horror;
    [SerializeField] private GameObject alien;
    
    public VisualEffects visualEffects; 
    [Header("Ball Visuals Reference")]
    public BallVisuals ballVisuals;

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
        return Resources.Load<Sprite>($"Sprites/Player{playerId}Paddle");
    }
    
    public Sprite GetRocketSprite(int playerId)
    {
        return Resources.Load<Sprite>($"Sprites/Player{playerId}Rocket");
    }

    public void RegisterPaddle(PaddleController paddle)
    {
        var spriteRenderer = paddle.GetComponentInChildren<SpriteRenderer>();
        int paddleId = paddle.gameObject.GetInstanceID();
        spriteRenderer.sprite = GetPaddleSprite(paddleId);
    }
    
    private void HandlePlayerScored(int playerId)
    {
       // empty for now, was going to add sound or vfx effects if I had time
    }

    private void HandlePlayerHit(int playerId)
    {
        if (!IsServer) return;

        Debug.Log($"VisualEventsManager: Player {playerId} hit detected");
        TriggerBackgroundChangeClientRpc(playerId);
        TriggerBallColorChangeClientRpc(playerId);
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Unreliable)]
    private void TriggerBackgroundChangeClientRpc(int playerId)
    {
        visualEffects?.HandleColorChange(playerId);
    }
    
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void TriggerBallColorChangeClientRpc(int playerId)
    {
        ballVisuals?.HandleColorChange(playerId);
    }
    
    // REMOVED: UpdateScoreUIClientRpc() - no longer needed because now ScoreManager handles UI updates directly
    
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    public void ToggleVisualsClientRpc(string visualName)
    {
        visualName = visualName.ToLowerInvariant();

        bool showSpace = visualName == "space";
        bool showCircles = visualName == "circles";
        bool showCity = visualName == "city";
        bool showHorror = visualName == "horror";
        bool showAlien = visualName == "alien";

        if (space != null) space.SetActive(showSpace);
        if (circles != null) circles.SetActive(showCircles);
        if (city != null) city.SetActive(showCity);
        if (horror != null) horror.SetActive(showHorror);
        if (alien != null) alien.SetActive(showAlien);
    }
}
