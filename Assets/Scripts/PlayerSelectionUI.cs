using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;

public class PlayerSelectionUI : NetworkBehaviour
{
    public GameObject buttonPrefab;
    public Transform verticalContainer;
    public Transform horizontalContainer;

    private ulong localClientId;

    public void InitializeUI(List<PlayerInfo> otherPlayers, ulong clientId, bool isHorizontal)
    {
        localClientId = clientId;

        Transform targetContainer = isHorizontal ? horizontalContainer : verticalContainer;
        verticalContainer.gameObject.SetActive(!isHorizontal);
        horizontalContainer.gameObject.SetActive(isHorizontal);
        foreach (var other in otherPlayers)
        {
            GameObject buttonObj = Instantiate(buttonPrefab, targetContainer);
            Text buttonText = buttonObj.GetComponentInChildren<Text>();
            buttonText.text = $"Player {other.playerId}";
            Button btn = buttonObj.GetComponent<Button>();
            ulong targetClientId = other.clientId;
            btn.onClick.AddListener(() => OnTargetPlayerButtonClicked(targetClientId));
        }
    }
    private void OnTargetPlayerButtonClicked(ulong targetClientId)
    {
        Debug.Log($"Player {localClientId} wants to use effect on Player {targetClientId}");
    }
}
