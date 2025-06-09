using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.Netcode;

public class InventorySlot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Text nameText;
    [SerializeField] private Button slotButton;
    
    private BoonEffect currentBoon;
    private bool isEmpty = true;
    
    private void Awake()
    {
        if (slotButton == null)
            slotButton = GetComponent<Button>();
        if (slotButton == null)
            slotButton = gameObject.AddComponent<Button>();
            
        UpdateSlotVisuals();
    }
    public void SetBoon(BoonEffect boon)
    {
        currentBoon = boon;
        isEmpty = false;
        UpdateSlotVisuals();
    }
    public void ClearSlot()
    {
        currentBoon = null;
        isEmpty = true;
        UpdateSlotVisuals();
    }
    private void UpdateSlotVisuals()
    {
        if (isEmpty)
        {
            if (iconImage != null) iconImage.sprite = null;
            if (nameText != null) nameText.text = "";
            slotButton.interactable = false;
        }
        else
        {
            if (iconImage != null) iconImage.sprite = currentBoon.icon;
            if (nameText != null) nameText.text = currentBoon.effecvtName;
            slotButton.interactable = true;
        }
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isEmpty || currentBoon == null) return;
        
        // Use the boon
        UseBoon();
    }
    private void UseBoon()
    {
        if (PlayerBoonInventory.Instance != null)
        {
            ulong clientId = NetworkManager.Singleton.LocalClientId;
            PlayerBoonInventory.Instance.UseBoonServerRPC(clientId, currentBoon.name);
        }
    }
    public bool IsEmpty() => isEmpty;
    public string GetBoonId() => currentBoon?.name ?? "";
}