using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class BallVisuals : MonoBehaviour
{
    public Color player1Color = Color.red;
    public Color player2Color = Color.blue;
    public Color player3Color = Color.green;
    public Color player4Color = Color.yellow;
    public GameObject trailBallPrefab;
    public int trailCount = 5;
    public float trailSpacing = 0.05f;

    private SpriteRenderer spriteRenderer;
    private Color currentColor;
    private List<GameObject> trailBalls = new List<GameObject>();
    // using queue for trailing effects
    private Queue<Vector3> previousPositions = new Queue<Vector3>();

     private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentColor = player1Color;
    }

    private void Start()
    {
        CreateTrailBalls();
    }

    private void Update()
    {
        UpdateTrailPositions();
    }

    public void HandleColorChange(int playerId)
    {
        Debug.Log($"Ball color changing to player {playerId}");

        switch (playerId)
        {
            case 1: currentColor = player1Color; break;
            case 2: currentColor = player2Color; break;
            case 3: currentColor = player3Color; break;
            case 4: currentColor = player4Color; break;
        }

        spriteRenderer.color = currentColor;

        // Also updating color of trail balls
        for (int i = 0; i < trailBalls.Count; i++)
        {
            Color fadedColor = currentColor;
            float alphaFactor = 1f - ((i + 1) / (float)(trailCount + 1)); // decreasing alpha
            fadedColor.a = alphaFactor;
            trailBalls[i].GetComponent<SpriteRenderer>().color = fadedColor;
        }
    }

    private void CreateTrailBalls()
    {
        for (int i = 0; i < trailCount; i++)
        {
            GameObject trail = Instantiate(trailBallPrefab, transform.position, Quaternion.identity);
            trail.transform.localScale = transform.localScale;
            trailBalls.Add(trail);
        }
    }

    private void UpdateTrailPositions()
    {
        // Store current position with time spacing
        previousPositions.Enqueue(transform.position);
        if (previousPositions.Count > trailCount * Mathf.RoundToInt(1f / trailSpacing))
        {
            previousPositions.Dequeue();
        }

        Vector3[] positions = previousPositions.ToArray();
        int stepSize = Mathf.FloorToInt(positions.Length / (float)trailCount);

        for (int i = 0; i < trailBalls.Count; i++)
        {
            int index = Mathf.Clamp((i + 1) * stepSize, 0, positions.Length - 1);
            trailBalls[i].transform.position = positions[index];
        }
    }
}