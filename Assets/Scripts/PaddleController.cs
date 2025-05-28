using UnityEngine;
using UnityEngine.InputSystem;

public class PaddleController : MonoBehaviour
{
    public int playerId = 1;
    public float moveSpeed;
    
    private Rigidbody2D rb;
    private PlayerInput playerInput;
    private Vector2 moveInput;
    
    public float topLimit = 4f;
    public float bottomLimit = -4f;

   
    private void Awake()
    {
        playerInput = new PlayerInput();
        playerInput.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        playerInput.Player.Move.canceled += ctx => moveInput = Vector2.zero;
    }
   
    private void OnEnable()
    {
        playerInput.Enable();
    }
   
    private void OnDisable()
    {
        playerInput.Disable();
    }
   
    void Update()
    {
        Vector2 movement = new Vector2(0, moveInput.y); 
        transform.Translate(movement * moveSpeed * Time.deltaTime);
           
        Vector3 pos = transform.position;
        pos.y = Mathf.Clamp(pos.y, bottomLimit, topLimit);
        transform.position = pos;
    }
}
