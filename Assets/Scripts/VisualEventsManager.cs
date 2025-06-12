using UnityEngine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System;

public class VisualEventsManager : NetworkBehaviour
{
    [Header("Score UI References")]
    public Text player1ScoreText;
    public Text player2ScoreText;
    public Text player3ScoreText;
    public Text player4ScoreText;

    [Header("Background Reference")]
    public PsychedelicBackground psychedelicBackground;

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

    void Start()
    {
        GameObject ballGO = GameObject.FindWithTag("Ball");
        if (ballGO != null)
        {
            ballVisuals = ballGO.GetComponentInChildren<BallVisuals>();
        }
        else
        {
            Debug.LogWarning("Ball visual not found");
        }
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
        TriggerBallGradientChangeClientRpc(playerId);
    }

    [ClientRpc]
    private void TriggerBackgroundChangeClientRpc(int playerId)
    {
        psychedelicBackground?.HandleColorChange(playerId);
    }

    [ClientRpc]
    private void TriggerBallGradientChangeClientRpc(int playerId)
    {
        ballVisuals?.HandleGradientChange(playerId);
    }

    [ClientRpc]
    public void UpdateScoreUIClientRpc(int p1, int p2, int p3, int p4)
    {
        if (player1ScoreText != null) player1ScoreText.text = p1.ToString();
        if (player2ScoreText != null) player2ScoreText.text = p2.ToString();
        if (player3ScoreText != null) player3ScoreText.text = p3.ToString();
        if (player4ScoreText != null) player4ScoreText.text = p4.ToString();
    }

    [ClientRpc] // future method
    public void TriggerInventoryUIClientRpc(int effectId, int playerId)
    {
        switch (effectId)
        {
           case 1:  break; 
        }
    }
}
