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
    
    [Header("Game Visuals")] // visuals to toggle
    [SerializeField] private GameObject space;
    [SerializeField] private GameObject circles;
    [SerializeField] private GameObject city;
    [SerializeField] private GameObject horror;
    [SerializeField] private GameObject alien;
    // controls background visuals. This VisualEffects class is fully local and registers events from this manager for color switching
    public VisualEffects visualEffects; 

    [Header("Ball Visuals Reference")]
    // This class handles ball color changes depending on the last hit paddle
    public BallVisuals ballVisuals;

    // Global instance ref used by other scripts like PaddleController and ScoreManager for events communication
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

    // Called by PaddleController on spawn
    public void RegisterPaddle(PaddleController paddle)
    {
        var spriteRenderer = paddle.GetComponent<SpriteRenderer>();
        int paddleId = paddle.gameObject.GetInstanceID();
        spriteRenderer.sprite = GetPaddleSprite(paddleId);
    }
    private void HandlePlayerScored(int playerId)
    {
       // empty for now, was going to add sound or vfx effects if I had time
    }

    // Called on the server when ball hits a player paddle
    private void HandlePlayerHit(int playerId)
    {
        if (!IsServer) return;

        Debug.Log($"VisualEventsManager: Player {playerId} hit detected");
        TriggerBackgroundChangeClientRpc(playerId);
        TriggerBallColorChangeClientRpc(playerId);
    }

    // Client Rpc - updates background visuals on all clients. Doesn't need to be reliable as it's not significant to gameplay
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Unreliable)]
    private void TriggerBackgroundChangeClientRpc(int playerId)
    {
        visualEffects?.HandleColorChange(playerId);
    }
    // Client Rpc - tells all clients to update ball color
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void TriggerBallColorChangeClientRpc(int playerId)
    {
        ballVisuals?.HandleColorChange(playerId);
    }
    // Client Rpc - updates visible score text for all clients
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    public void UpdateScoreUIClientRpc(int p1, int p2, int p3, int p4)
    {
        if (player1ScoreText != null) player1ScoreText.text = p1.ToString();
        if (player2ScoreText != null) player2ScoreText.text = p2.ToString();
        if (player3ScoreText != null) player3ScoreText.text = p3.ToString();
        if (player4ScoreText != null) player4ScoreText.text = p4.ToString();
    }
    
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    public void ToggleVisualsClientRpc(string visualName)
    {
        visualName = visualName.ToLowerInvariant(); //returns lowercase version of visualName no matter what

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
