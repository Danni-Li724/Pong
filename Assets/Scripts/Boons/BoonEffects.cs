using UnityEngine;

[CreateAssetMenu(menuName = "Boons/BoonEffect")]
public class BoonEffects : ScriptableObject
{
    public string effecvtName;
    public string description;
    public Sprite icon;
    public float duration;
    //public BoonType type;
    public GameObject effectPrefab;
}
