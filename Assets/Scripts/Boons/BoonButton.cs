using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BoonButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Text nameText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Button button;
    
    private BoonEffect boonEffect;
    private BoonManager boonManager;
    
    public void Initialize(BoonEffect effect, BoonManager manager)
    {
        boonEffect = effect;
        boonManager = manager;
        
        if (iconImage != null) iconImage.sprite = effect.icon;
        if (nameText != null) nameText.text = effect.effectName;
        if (descriptionText != null) descriptionText.text = effect.description;
        
        // making sure button is interactable doe debugging sake
        if (button != null)
        {
            button.interactable = true;
            button.onClick.AddListener(OnButtonClick);
        }
        
        Debug.Log($"created boon button for {effect.effectName} with type {effect.type}");
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        OnButtonClick();
    }
    
    private void OnButtonClick()
    {
        if (boonManager != null && boonEffect != null)
        {
            Debug.Log($"Boon button clicked: {boonEffect.effectName} (Type: {boonEffect.type})");
            // pass BoonType instead of string
            boonManager.TrySelectBoon(boonEffect.type);
        }
    }
    
    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }
}
