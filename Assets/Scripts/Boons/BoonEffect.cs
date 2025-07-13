using UnityEngine;

[CreateAssetMenu(menuName = "Boons/BoonEffect")]
public class BoonEffect : ScriptableObject
{
    public string effectName;
    public string description;
    public Sprite icon;
    public float duration;
    public BoonType type;
    public bool isReusable;
}
public enum BoonType
{
    SpaceshipMode,
    DoubleBall,
    BallSpeedBoost,
    MusicChange,
    ScoreThief,
    PaddleTilt
}
