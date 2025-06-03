using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BoonButton : MonoBehaviour, IPointerClickHandler
{
   [SerializeField] private string boonId;
   private Button button;

   private void Awake()
   {
      
   }

   public void OnPointerClick(PointerEventData eventData)
   {
      if (!button.interactable) return;
      BoonSelectionManager.Instance.TrySelectBoon(boonId);
   }

   public void Disable()
   {
      button.interactable = false;
   }
   public string GetBoonId() => boonId;
}
