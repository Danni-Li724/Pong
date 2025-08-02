using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
/// <summary>
/// Attached to the UI inventory slots to display a player's selected boon.
/// Created and managed by BoonManager via Sync RPCs.
/// </summary>
public class PlayerInventorySlot : NetworkBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Text nameText;
    [SerializeField] private Button useButton;
    [SerializeField] private Text playerIdText;
    
    private BoonEffect currentBoon;
    private ulong ownerClientId;
    private bool hasBoon = false;
    
    public ulong clientId;
    public int playerId;
    
    private void Awake()
    {
        if (useButton != null)
        {
            useButton.onClick.AddListener(OnUseButtonClicked);
            useButton.gameObject.SetActive(false);
        }
    }
    
    // called by BoonManager when syncing inventories (this syncs their visuals and descriptions)
    public void SetBoon(BoonEffect boon, ulong clientId)
    {
        currentBoon = boon;
        ownerClientId = clientId;
        hasBoon = true;
        
        if (iconImage != null) 
        {
            iconImage.sprite = boon.icon;
            iconImage.gameObject.SetActive(true);
        }
        if (nameText != null) nameText.text = boon.effectName;
        
        // show use button only for the owner.
        if (useButton != null)
        {
            bool isOwner = NetworkManager.Singleton.LocalClientId == clientId;
            useButton.interactable = isOwner;

            useButton.onClick.RemoveAllListeners();
            if (isOwner)
            {
                useButton.onClick.AddListener(OnUseButtonClicked); // add listener only for the owner
            }
        }
        
        // update player ID text
        // var playerInfo = NetworkGameManager.Instance.GetPlayerInfo(clientId);
        // now show the synced display name rather than generic
        var playerInfo = GameManager.Instance != null ? GameManager.Instance.GetPlayerInfo(clientId) : null;
        if (playerIdText != null)
        {
            playerIdText.text = playerInfo != null ? playerInfo.displayName : $"Player {playerId}";
        }
        Debug.Log($"Set boon {boon.effectName} for client {clientId} in inventory slot");
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
    // Called locally when a player clicks to use their boon
    private void OnUseButtonClicked()
    {
        if (hasBoon && currentBoon != null)
        {
            // Check if it's PaddleTilt boon and if we're in spaceship mode
            if (currentBoon.type == BoonType.PaddleTilt)
            {
                // Find the paddle controller for this client to check spaceship mode
                var paddles = FindObjectsOfType<PaddleController>();
                foreach (var paddle in paddles)
                {
                    var networkObject = paddle.GetComponent<NetworkObject>();
                    if (networkObject != null && networkObject.OwnerClientId == ownerClientId)
                    {
                        if (paddle.isInSpaceshipMode())
                        {
                            Debug.Log("[PlayerInventorySlot] Cannot use Paddle Tilt during Spaceship Mode");
                            return; // Exit early and don't use the boon
                        }
                        break;
                    }
                }
            }
        
            // Cache currentboon locally
            var boon = currentBoon;
            Debug.Log($"Player {ownerClientId} clicked to use boon {boon.type}");
            BoonManager.Instance.UseBoon(ownerClientId, boon.type);
        }
    }
    public bool TryAssign(ulong targetClientId, int newPlayerId)
    {
        if (clientId == 0 || clientId == targetClientId)
        {
            clientId = targetClientId;
            playerId = newPlayerId;

            // changed: always show the current synced displayName now
            var playerInfo = GameManager.Instance != null ? GameManager.Instance.GetPlayerInfo(clientId) : null;
            if (playerIdText != null)
            {
                playerIdText.text = playerInfo != null ? playerInfo.displayName : $"Player {playerId}";
            }

            return true;
        }
        return false;
    }
}
