using UnityEngine;
using UnityEngine.UI;

public class StartGameButton : MonoBehaviour
{
    [SerializeField] private Button startButton;
    
    private void Start()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartGamePressed);
        }
    }
    
    private void OnStartGamePressed()
    {
        var gameManager = NetworkGameManager.Instance;
        if (gameManager != null)
        {
            gameManager.OnStartGameButtonPressed();
        }
    }
}
