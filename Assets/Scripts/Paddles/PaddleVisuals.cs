using Unity.Netcode;
using UnityEngine;

public class PaddleVisuals : NetworkBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Sprite defaultSprite;
    private Vector3 originalScale;
    [SerializeField] private float spriteRotationOffset = -90f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            defaultSprite = spriteRenderer.sprite;
        }
        originalScale = transform.localScale;
    }
    public void SetPaddleSpriteAsDefault(Sprite paddleSprite)
    {
        if (spriteRenderer != null && paddleSprite != null)
        {
            defaultSprite = paddleSprite;
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
    
    public void RotateInDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + spriteRotationOffset;
            transform.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }

}
