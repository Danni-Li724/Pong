using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerCountSelector : NetworkBehaviour
{
    [SerializeField] private Dropdown playerCountDropdown;
    void Start()
    {
        if (playerCountDropdown != null)
        {
            // remove old listeners
            playerCountDropdown.onValueChanged.RemoveAllListeners();
            // new listener for dropdown
            playerCountDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }
    }

    // called when the dropdown is changed
    private void OnDropdownValueChanged(int dropdownIndex)
    {
        // index 0: 2 players, 1: 3 players, 2: 4 players, etc.
        int playerCount = dropdownIndex + 2; // the fist is 2 players
        SelectPlayerCount(playerCount);
    }

    public void SelectPlayerCount(int count)
    {
        var gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            gameManager.OnPlayerCountSelected(count);
        }
    }
}
