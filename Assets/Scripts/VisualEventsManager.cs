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

    public static VisualEventsManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        Ball.OnPlayerHit += HandlePlayerHit;
        Ball.OnPlayerScored += HandlePlayerScored;
    }

    private void OnDisable()
    {
        Ball.OnPlayerHit -= HandlePlayerHit;
        Ball.OnPlayerScored -= HandlePlayerScored;
    }
    private void HandlePlayerScored(int playerId)
    {
    }
    private void HandlePlayerHit(int playerId)
    {
        if (IsServer)
        {
            TriggerBackgroundChangeClientRpc(playerId);
        }
    }
    [ClientRpc]
    private void TriggerBackgroundChangeClientRpc(int playerId)
    {
        if (psychedelicBackground != null)
        {
            psychedelicBackground.HandleColorChange(playerId);
        }
    }

    [ClientRpc]
    public void UpdateScoreUIClientRpc(int p1, int p2, int p3, int p4)
    {
        if (player1ScoreText != null) player1ScoreText.text = p1.ToString();
        if (player2ScoreText != null) player2ScoreText.text = p2.ToString();
        if (player3ScoreText != null) player3ScoreText.text = p3.ToString();
        if (player4ScoreText != null) player4ScoreText.text = p4.ToString();
    }
    [ClientRpc]
    public void TriggerUIEffectClientRpc(int effectId, int playerId)
    {
        switch (effectId)
        {
           case 1:  break; 
        }
    }

}
