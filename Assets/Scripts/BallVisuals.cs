using UnityEngine;
using System.Collections;
public class BallVisuals : MonoBehaviour
{
    public Gradient player1Gradient;
    public Gradient player2Gradient;
    public Gradient player3Gradient;
    public Gradient player4Gradient;
    public float gradientSpeed = 1f;
    public bool animateGradient = true;

    private SpriteRenderer spriteRenderer;
    private Gradient currentGradient;
    private Coroutine gradientCoroutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        currentGradient = player1Gradient;
        if (animateGradient)
        {
            StartGradientAnimation();
        }
    }

    public void HandleGradientChange(int playerId)
    {
        Debug.Log($"Ball gradient changing to player {playerId}");

        switch (playerId)
        {
            case 1: currentGradient = player1Gradient; break;
            case 2: currentGradient = player2Gradient; break;
            case 3: currentGradient = player3Gradient; break;
            case 4: currentGradient = player4Gradient; break;
        }

        if (animateGradient)
        {
            StartGradientAnimation();
        }
    }

    private void StartGradientAnimation()
    {
        if (gradientCoroutine != null)
        {
            StopCoroutine(gradientCoroutine);
        }
        gradientCoroutine = StartCoroutine(AnimateGradient());
    }

    private IEnumerator AnimateGradient()
    {
        float time = 0f;

        while (true)
        {
            if (currentGradient != null && spriteRenderer != null)
            {
                Color color = currentGradient.Evaluate(time);
                spriteRenderer.color = color;
            }

            time += Time.deltaTime * gradientSpeed;
            if (time > 1f)
            {
                time = 0f;
            }
            yield return null;
        }
    }
}