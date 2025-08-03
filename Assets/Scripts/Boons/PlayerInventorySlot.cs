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
    
    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPlayerNamesUpdated += UpdateDisplayName;
        UpdateDisplayName();
    }
    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPlayerNamesUpdated -= UpdateDisplayName;
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
            UpdateDisplayName();
        }
        
        // update player ID text
        // var playerInfo = NetworkGameManager.Instance.GetPlayerInfo(clientId);
        // now show the synced display name rather than generic
        //var playerInfo = GameManager.Instance != null ? GameManager.Instance.GetPlayerInfo(clientId) : null;
        if (playerIdText != null)
        {
            var playerInfo = GameManager.Instance.GetPlayerInfo(clientId);
            if (playerInfo != null)
                playerIdText.text = playerInfo.displayName;
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
    
    public void UpdateDisplayName()
    {
        if (playerIdText == null) return;

        string displayName = null;

        if (GameManager.Instance != null && clientId != 0)
        {
            var playerInfo = GameManager.Instance.GetPlayerInfo(clientId);
            if (playerInfo != null && !string.IsNullOrEmpty(playerInfo.displayName))
            {
                displayName = playerInfo.displayName;
                Debug.Log($"[PlayerInventorySlot] Got name from PlayerInfo: {displayName} for client {clientId}");
            }
        }
        if (!string.IsNullOrEmpty(displayName))
        {
            playerIdText.text = displayName;
        }
        else
        {
            playerIdText.text = "Player"; 
            Debug.Log($"[PlayerInventorySlot] No valid display name for client {clientId} (blank)");
        }
    }

    public bool TryAssign(ulong targetClientId, int newPlayerId)
    {
        bool wasAssigned = false;
        
        // assign if this slot is empty or matches the target
        if (clientId == 0 || clientId == targetClientId)
        {
            clientId = targetClientId;
            playerId = newPlayerId;
            wasAssigned = true;
            Debug.Log($"[PlayerInventorySlot] Assigned slot: clientId={clientId}, playerId={playerId}");
        }
        
        // always try to update the display name after assignment
        if (wasAssigned)
        {
            // Use a coroutine to update after a frame to ensure GameManager has the name
            StartCoroutine(DelayedNameUpdate());
        }
        
        return wasAssigned;
    }
    
    private System.Collections.IEnumerator DelayedNameUpdate()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame(); // EXTRA frame to be sure x_x
        UpdateDisplayName();
    }
}
