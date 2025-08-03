using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;

// public class PlayerSelectionUI : NetworkBehaviour
// {
//     [Header("UI References")]
//     public GameObject buttonPrefab;
//     public RectTransform Player1SelectionContainer;
//     public RectTransform Player2SelectionContainer;
//     public RectTransform Player3SelectionContainer;
//     public RectTransform Player4SelectionContainer;
//
//     private ulong localClientId;
//
//     public void InitializeUI(List<PlayerInfo> otherPlayers, ulong clientId, int localPlayerId)
//     {
//         localClientId = clientId;
//
//         RectTransform targetContainer = GetContainerForPlayer(localPlayerId);
//         if (targetContainer == null)
//         {
//             Debug.LogError($"[PlayerSelectionUI] No container for player ID {localPlayerId}");
//             return;
//         }
//
//         Debug.Log($"[PlayerSelectionUI] Spawning buttons under container {targetContainer.name}");
//
//         foreach (var other in otherPlayers)
//         {
//             GameObject buttonObj = Instantiate(buttonPrefab, targetContainer); 
//             Debug.Log("spawning selection buttons");
//             buttonObj.transform.localScale = Vector3.one; 
//             Text buttonText = buttonObj.GetComponentInChildren<Text>();
//             if (buttonText != null)
//                 buttonText.text = $"Player {other.playerId}";
//             else
//                 Debug.LogWarning("Missing Text component on button prefab!");
//
//             Button btn = buttonObj.GetComponent<Button>();
//             ulong targetClientId = other.clientId;
//             btn.onClick.AddListener(() => OnTargetPlayerButtonClicked(targetClientId));
//         }
//     }
//
//     private RectTransform GetContainerForPlayer(int playerId)
//     {
//         switch (playerId)
//         {
//             case 1: return Player1SelectionContainer;
//             case 2: return Player2SelectionContainer;
//             case 3: return Player3SelectionContainer;
//             case 4: return Player4SelectionContainer;
//             default: return null;
//         }
//     }
//
//     private void OnTargetPlayerButtonClicked(ulong targetClientId)
//     {
//         Debug.Log($"Player {localClientId} wants to use effect on Player {targetClientId}");
//     }
// }
