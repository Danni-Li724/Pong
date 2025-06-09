using UnityEngine;
using System.Collections.Generic;

public class UIBoonSelection : MonoBehaviour
{
    [SerializeField] private Transform boonButtonContainer;
    [SerializeField] private GameObject boonButtonPrefab;
    
    public static UIBoonSelection Instance { get; private set; }
    private Dictionary<string, BoonButton> boonButtons = new Dictionary<string, BoonButton>();
    
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
    
    public void InitializeBoonButtons(List<BoonEffect> availableBoons)
    {
        // Clear existing buttons
        foreach (Transform child in boonButtonContainer)
        {
            Destroy(child.gameObject);
        }
        boonButtons.Clear();
        
        // Create buttons for each available boon
        foreach (var boon in availableBoons)
        {
            GameObject buttonObj = Instantiate(boonButtonPrefab, boonButtonContainer);
            BoonButton boonButton = buttonObj.GetComponent<BoonButton>();
            
            if (boonButton != null)
            {
                boonButton.Initialize(boon);
                boonButtons[boon.name] = boonButton;
            }
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
