using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.Netcode;

public class PlayerTargetingUI : NetworkBehaviour
{
    public Transform buttonContainer;
        public GameObject targetButtonPrefab;
        
        private NetworkGameManager gameManager;
        private List<Button> targetButtons = new List<Button>();
        
        void Start()
        {
            gameManager = FindObjectOfType<NetworkGameManager>();
        }
        
        public void ShowTargetingOptions()
        {
            if (!IsOwner) return;
        
            ClearTargetButtons();
        
            // Get the list of other players for this client
            List<PlayerInfo> otherPlayers = gameManager.GetOtherPlayers(NetworkManager.Singleton.LocalClientId);
        
            foreach (PlayerInfo player in otherPlayers)
            {
                CreateTargetButton(player);
            }
        }
    
        void CreateTargetButton(PlayerInfo targetPlayer)
        {
            GameObject buttonObj = Instantiate(targetButtonPrefab, buttonContainer);
            Button button = buttonObj.GetComponent<Button>();
            Text buttonText = buttonObj.GetComponentInChildren<Text>();
        
            buttonText.text = $"Target Player {targetPlayer.playerId}";
            
            button.onClick.AddListener(() => OnTargetSelected(targetPlayer));
        
            targetButtons.Add(button);
        }

        void OnTargetSelected(PlayerInfo targetPlayer)
        {
            Debug.Log($"Selected target: Player {targetPlayer.playerId}");
        }
        
        // if need to disable
        void ClearTargetButtons()
        {
            foreach (Button button in targetButtons)
            {
                if (button != null)
                    Destroy(button.gameObject);
            }
            targetButtons.Clear();
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
        void ApplyEffectToPlayerServerRpc(ulong targetClientId, int effectType)
        {
            ApplyEffectToPlayerClientRpc(targetClientId, effectType);
        }

        [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
        void ApplyEffectToPlayerClientRpc(ulong targetClientId, int effectType)
        {
            if (NetworkManager.Singleton.LocalClientId == targetClientId)
            {
                Debug.Log($"Received effect {effectType}");
            }
        }

}
