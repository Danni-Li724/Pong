using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIPlayerInventory : MonoBehaviour // local to client
{
    [SerializeField] private Transform inventoryContainer;
    [SerializeField] private GameObject inventorySlotPrefab;
    [SerializeField] private int maxInventorySlots = 2;
    
    public static UIPlayerInventory Instance { get; private set; }
    private List<InventorySlot> inventorySlots = new List<InventorySlot>();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeInventorySlots();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeInventorySlots()
    {
        for (int i = 0; i < maxInventorySlots; i++)
        {
            GameObject slotObj = Instantiate(inventorySlotPrefab, inventoryContainer);
            InventorySlot slot = slotObj.GetComponent<InventorySlot>();
            if (slot == null)
                slot = slotObj.AddComponent<InventorySlot>();
            
            inventorySlots.Add(slot);
        }
    }
    
    public void AddBoonToInventory(string boonId)
    {
        var boonEffect = BoonSelectionManager.Instance.GetBoonEffect(boonId);
        if (boonEffect == null) return;
        
        foreach (var slot in inventorySlots)
        {
            if (slot.IsEmpty())
            {
                slot.SetBoon(boonEffect);
                break;
            }
        }
    }
    
    public void RemoveBoonFromInventory(string boonId)
    {
        foreach (var slot in inventorySlots)
        {
            if (slot.GetBoonId() == boonId)
            {
                slot.ClearSlot();
                break;
            }
        }
    }
}