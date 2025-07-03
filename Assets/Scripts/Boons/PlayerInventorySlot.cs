using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class PlayerInventorySlot : NetworkBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Text nameText;
    [SerializeField] private Button useButton;
    [SerializeField] private Text playerIdText;
    
    private BoonEffect currentBoon;
    private ulong ownerClientId;
    private bool hasBoon = false;
    
    private void Awake()
    {
        if (useButton != null)
        {
            useButton.onClick.AddListener(OnUseButtonClicked);
            useButton.gameObject.SetActive(false);
        }
    }
    
    public void SetBoon(BoonEffect boon, ulong clientId)
    {
        currentBoon = boon;
        ownerClientId = clientId;
        hasBoon = true;
        
        if (iconImage != null) iconImage.sprite = boon.icon;
        if (nameText != null) nameText.text = boon.effectName;
        
        // show use button only for the owner
        if (useButton != null && NetworkManager.Singleton.LocalClientId == clientId)
        {
            useButton.gameObject.SetActive(true);
        }
        
        // update player ID text
        var playerInfo = NetworkGameManager.Instance.GetPlayerInfo(clientId);
        if (playerIdText != null && playerInfo != null)
        {
            playerIdText.text = $"Player {playerInfo.playerId}";
        }
    }
    
    public void ClearBoon()
    {
        currentBoon = null;
        hasBoon = false;
        
        if (iconImage != null) iconImage.sprite = null;
        if (nameText != null) nameText.text = "";
        if (useButton != null) useButton.gameObject.SetActive(false);
        if (playerIdText != null) playerIdText.text = "";
    }
    
    private void OnUseButtonClicked()
    {
        if (hasBoon && currentBoon != null)
        {
            BoonManager.Instance.UseBoon(ownerClientId, currentBoon.name);
        }
    }
}
