using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class MaxScoreInputUI : MonoBehaviour
{
    [SerializeField] private InputField inputField;
    [SerializeField] private Button confirmButton;
    
    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirmClicked);
    }

    private void OnConfirmClicked()
    {
        if (!NetworkManager.Singleton.IsHost) return;
        if (int.TryParse(inputField.text, out int score) && score > 0)
        {
            ScoreManager scoreManager = FindObjectOfType<ScoreManager>();
            if (scoreManager != null)
            {
                scoreManager.SetMaxScore(score);
            }
            gameObject.SetActive(false);
            NetworkGameManager.Instance.StartBoonSelection(); 
        }
    }
}
