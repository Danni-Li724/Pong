using Unity.Netcode;
using UnityEngine;
/// <summary>
/// Server side paddle movement logic
/// </summary>
public class PaddleMovement : NetworkBehaviour
{
    private Vector2 moveInput;

    public float moveSpeed;
    public float topLimit, bottomLimit, leftLimit, rightLimit;

    private PaddleController controller;

    private void Awake()
    {
        controller = GetComponent<PaddleController>();
    }

    public void SetMoveInput(Vector2 input)
    {
        moveInput = input;
    }

    private void Update()
    {
        if (!IsServer) return;
        Vector3 movementDirection = controller.IsHorizontal
            ? transform.right * -moveInput.x
            : transform.up * moveInput.y;

        transform.Translate(movementDirection * moveSpeed * Time.deltaTime, Space.World);

        Vector3 pos = transform.position;
        if (controller.IsHorizontal)
            pos.x = Mathf.Clamp(pos.x, leftLimit, rightLimit);
        else
            pos.y = Mathf.Clamp(pos.y, bottomLimit, topLimit);

        transform.position = pos;
    }
}
