using UnityEngine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.Serialization;

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
