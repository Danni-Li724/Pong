using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
public class BallVisuals : NetworkBehaviour
{
    
    public Color player1Color;
    public Color player2Color;
    public Color player3Color;
    public Color player4Color;
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
    
    // public Color player1Color;
    // public Color player2Color;
    // public Color player3Color;
    // public Color player4Color;
    // private SpriteRenderer spriteRenderer;
    // private TrailRenderer trailRenderer;
    //
    // /// <summary>
    // /// New variables for interpolation
    // /// </summary>
    // [Header("Variables for Network Optimization")]
    // public BallPhysics ballPhysics;
    // public float interpolationBackTime = 0.05f; // interpolate 50ms behind
    //
    // // buffer of server ball states for interpolation
    // private struct BallState
    // {
    //     public float timestamp;
    //     public Vector2 position;
    //     public Vector2 velocity;
    // }
    // private Queue<BallState> stateBuffer = new Queue<BallState>();
    //
    // // private void Awake()
    // // {
    // //     spriteRenderer = GetComponent<SpriteRenderer>();
    // //     trailRenderer = GetComponent<TrailRenderer>();
    // // }
    // // private void Start()
    // // {
    // //     // register self to events manager
    // //     VisualEventsManager.Instance?.RegisterBallVisuals(this);
    // // }
    // //
    //
    // /// <summary>
    // /// New approach with optimization & interpolation
    // /// well, just my experimentation, probs not the standard & most ideal way...
    // /// </summary>
    // private void Awake()
    // {
    //     spriteRenderer = GetComponent<SpriteRenderer>();
    //     trailRenderer = GetComponent<TrailRenderer>();
    // }
    //
    // private void Start()
    // {
    //     if (ballPhysics == null)
    //         ballPhysics = GetComponent<BallPhysics>();
    //     // register self to events manager
    //     VisualEventsManager.Instance?.RegisterBallVisuals(this);
    //
    //     // puts the current ball position and velocity into the buffer
    //     AddStateToBuffer(Time.time, ballPhysics.GetSyncedPosition(), ballPhysics.GetSyncedVelocity());
    // }
    //
    // private void Update()
    // {
    //     if (BallPhysics.Instance == null) return;
    //
    //     if (IsServer)
    //     {
    //         // Server: Ball physics is authoritative, so visuals will just match transform.
    //         transform.position = ballPhysics.GetSyncedPosition();
    //         return;
    //     }
    //
    //     // add new snapshot to buffer if it has changed
    //     Vector2 netPos = ballPhysics.GetSyncedPosition();
    //     Vector2 netVel = ballPhysics.GetSyncedVelocity();
    //     float netTime = Time.time; // Local clock
    //
    //     // only enqueue if changed
    //     if (stateBuffer.Count == 0 || (stateBuffer.Count > 0 && (stateBuffer.Peek().position - netPos).sqrMagnitude > 0.00001f))
    //     {
    //         AddStateToBuffer(netTime, netPos, netVel);
    //     }
    //     // remove states that are too old to be relevant
    //     while (stateBuffer.Count > 2 && netTime - stateBuffer.Peek().timestamp > interpolationBackTime * 3f)
    //     {
    //         stateBuffer.Dequeue();
    //     }
    //
    //     // now interpolate position based on buffer (50ms in the past)
    //     float interpTime = netTime - interpolationBackTime;
    //
    //     BallState? newer = null;
    //     BallState? older = null;
    //     foreach (var state in stateBuffer)
    //     {
    //         if (state.timestamp <= interpTime)
    //             older = state;
    //         else
    //         {
    //             newer = state;
    //             break;
    //         }
    //     }
    //
    //     Vector2 interpPos;
    //     if (older.HasValue && newer.HasValue)
    //     {
    //         float t = Mathf.InverseLerp(older.Value.timestamp, newer.Value.timestamp, interpTime);
    //         interpPos = Vector2.Lerp(older.Value.position, newer.Value.position, t);
    //     }
    //     else if (older.HasValue)
    //     {
    //         // if only have older, just extrapolate
    //         interpPos = older.Value.position + older.Value.velocity * (interpTime - older.Value.timestamp);
    //     }
    //     else
    //     {
    //         // fallback to just use current server snapshot
    //         interpPos = netPos;
    //     }
    //
    //     transform.position = interpPos;
    // }
    //
    // private void AddStateToBuffer(float time, Vector2 pos, Vector2 vel)
    // {
    //     stateBuffer.Enqueue(new BallState
    //     {
    //         timestamp = time,
    //         position = pos,
    //         velocity = vel
    //     });
    // }
    //
    // /// <summary>
    // /// Unchanged color methods
    // /// </summary>
    // /// <param name="playerId"></param>
    // public void HandleColorChange(int playerId)
    // {
    //     Debug.Log($"Ball color changing to player {playerId}");
    //
    //     switch (playerId)
    //     {
    //         case 1: SetColor(player1Color); break;
    //         case 2: SetColor(player2Color); break;
    //         case 3: SetColor(player3Color); break;
    //         case 4: SetColor(player4Color); break;
    //         default: SetColor(Color.white); break;
    //     }
    // }
    //
    // private void SetColor(Color color)
    // {
    //     spriteRenderer.color = color;
    //     Gradient gradient = new Gradient();
    //     gradient.SetKeys(
    //         new GradientColorKey[] {
    //             new GradientColorKey(color, 0f),
    //             new GradientColorKey(color, 1f)
    //         },
    //         new GradientAlphaKey[] {
    //             new GradientAlphaKey(1f, 0f),
    //             new GradientAlphaKey(0f, 1f)
    //         }
    //     );
    //     trailRenderer.colorGradient = gradient;
    // }
}