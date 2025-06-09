using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BoonButton : MonoBehaviour, IPointerClickHandler
{
   [SerializeField] private string boonId;
   [SerializeField] private Image iconImage;
   [SerializeField] private Text nameText;
   [SerializeField] private Text descriptionText;
    
   private Button button;
   private BoonEffect boonEffect;
    
   private void Awake()
   {
      button = GetComponent<Button>();
      if (button == null)
         button = gameObject.AddComponent<Button>();
   }
    
   public void Initialize(BoonEffect effect)
   {
      boonEffect = effect;
      boonId = effect.name;
        
      if (iconImage != null) iconImage.sprite = effect.icon;
      if (nameText != null) nameText.text = effect.effecvtName;
      if (descriptionText != null) descriptionText.text = effect.description;
   }
    
   public void OnPointerClick(PointerEventData eventData)
   {
      if (!button.interactable) return;
      BoonSelectionManager.Instance.TrySelectBoon(boonId);
   }
    
   public void Disable()
   {
      button.interactable = false;
      // Visual feedback for disabled state
      var colors = button.colors;
      colors.disabledColor = Color.gray;
      button.colors = colors;
   }
    
   public string GetBoonId() => boonId;
   public BoonEffect GetBoonEffect() => boonEffect;
}
