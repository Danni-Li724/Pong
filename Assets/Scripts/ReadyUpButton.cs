using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;

public class ReadyUpButton : MonoBehaviour
{
    public Button readyButton;

    void Start()
    {
        readyButton.gameObject.SetActive(false);
        StartCoroutine(WaitForLocalPaddle());
    }

    private IEnumerator WaitForLocalPaddle()
    {
        PaddleController paddle = null;

        while (paddle == null)
        {
            var allPaddles = FindObjectsOfType<PaddleController>();
            foreach (var p in allPaddles)
            {
                if (p.IsOwner)
                {
                    paddle = p;
                    break;
                }
            }
            yield return null;
        }

        //readyButton.gameObject.SetActive(true);
        readyButton.onClick.AddListener(paddle.ReadyUp);
    }

    public void EnableButton()
    {
        readyButton.gameObject.SetActive(true);
    }

    public void DisableButton()
    {
        readyButton.gameObject.SetActive(false);
    }
}
