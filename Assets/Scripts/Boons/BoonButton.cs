using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BoonButton : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Text nameText;
    [SerializeField] private Text descriptionText;
    
    private BoonEffect boonEffect;
    private BoonManager boonManager;
    
    public void Initialize(BoonEffect effect, BoonManager manager)
    {
        boonEffect = effect;
        boonManager = manager;
        
        if (iconImage != null) iconImage.sprite = effect.icon;
        if (nameText != null) nameText.text = effect.effectName;
        if (descriptionText != null) descriptionText.text = effect.description;
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (boonManager != null && boonEffect != null)
        {
            boonManager.TrySelectBoon(boonEffect.name);
        }
    }
}
