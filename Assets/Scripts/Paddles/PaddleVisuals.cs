using Unity.Netcode;
using UnityEngine;

public class PaddleVisuals : NetworkBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Sprite defaultSprite;
    private Vector3 originalScale;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            defaultSprite = spriteRenderer.sprite;
        }
        originalScale = transform.localScale;
    }

    public void SetPaddleSprite(Sprite paddleSprite)
    {
        if (spriteRenderer != null && paddleSprite != null)
        {
            spriteRenderer.sprite = paddleSprite;
        }
    }

    public void SetSpaceshipSprite(Sprite rocketSprite)
    {
        if (spriteRenderer != null)
        {
            if (defaultSprite == null)
                defaultSprite = spriteRenderer.sprite;
            
            spriteRenderer.sprite = rocketSprite;
            
            if (originalScale == Vector3.zero)
                originalScale = transform.localScale;

            transform.localScale = originalScale * 0.5f; // Shrink player so i don't have to redraw sprites
        }
    }

    public void RestoreDefaultSprite()
    {
        if (spriteRenderer != null && defaultSprite != null)
        {
            spriteRenderer.sprite = defaultSprite;
            
            if (originalScale != Vector3.zero)
                transform.localScale = originalScale;
            else
                transform.localScale = Vector3.one; // fallback
        }
    }
}
