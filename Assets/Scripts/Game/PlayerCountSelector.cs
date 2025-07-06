using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerCountSelector : NetworkBehaviour
{
    [SerializeField] private Button twoPlayersButton;
    [SerializeField] private Button threePlayersButton;
    [SerializeField] private Button fourPlayersButton;
    
    private void Start()
    {
        // button listeners to send info to game manager
        if (twoPlayersButton != null)
            twoPlayersButton.onClick.AddListener(() => SelectPlayerCount(2));
        
        if (threePlayersButton != null)
            threePlayersButton.onClick.AddListener(() => SelectPlayerCount(3));
        
        if (fourPlayersButton != null)
            fourPlayersButton.onClick.AddListener(() => SelectPlayerCount(4));
    }
    
    public void SelectPlayerCount(int count)
    {
        var gameManager = NetworkGameManager.Instance;
        if (gameManager != null)
        {
            gameManager.OnPlayerCountSelected(count);
        }
    }
}
