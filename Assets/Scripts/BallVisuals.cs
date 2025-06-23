using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class BallVisuals : MonoBehaviour
{
    public Color player1Color = Color.red;
    public Color player2Color = Color.blue;
    public Color player3Color = Color.green;
    public Color player4Color = Color.yellow;
    private SpriteRenderer spriteRenderer;
    private TrailRenderer trailRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        trailRenderer = GetComponent<TrailRenderer>();
    }
    private void Start()
    {
        // register self to events manager
        VisualEventsManager.Instance?.RegisterBallVisuals(this);
    }
    
    public void HandleColorChange(int playerId)
    {
        Debug.Log($"Ball color changing to player {playerId}");

        switch (playerId)
        {
            case 1: SetColor(player1Color); break;
            case 2: SetColor(player2Color); break;
            case 3: SetColor(player3Color); break;
            case 4: SetColor(player4Color); break;
            default: SetColor(Color.white); break;
        }
    }

    private void SetColor(Color color)
    {
        spriteRenderer.color = color;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        trailRenderer.colorGradient = gradient;
    }
}