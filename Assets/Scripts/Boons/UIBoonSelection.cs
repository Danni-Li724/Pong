using UnityEngine;
using System.Collections.Generic;

public class UIBoonSelection : MonoBehaviour
{
    public static UIBoonSelection Instance { get; private set; }
    private Dictionary<string, BoonButton> boonButtons = new();

    private void Awake()
    {
        Instance = this;
        var buttons = GetComponentsInChildren<BoonButton>(true);
        foreach (var button in buttons)
        {
            boonButtons[button.GetBoonId()] = button;
        }
    }

    public void MarkBoonAsPicked(string boonId)
    {
        if (boonButtons.TryGetValue(boonId, out var button))
        {
            button.Disable();
        }
    }
}
