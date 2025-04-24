using UnityEngine;

public class PMovements : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 15f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 10f;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;

    private bool grounded;
    private float moveInput;
    private bool jumpQueued;

    private void Update()
    {
        // Raw input
        if (Input.GetKey(KeyCode.D))
            moveInput = 1;
        else if (Input.GetKey(KeyCode.A))
            moveInput = -1;
        else
            moveInput = 0;

        if (Input.GetKeyDown(KeyCode.Space) && grounded)
            jumpQueued = true;
    }

    private void FixedUpdate()
    {
        // Get current horizontal velocity
        float targetSpeed = moveInput * maxSpeed;
        float speedDiff = targetSpeed - rb.linearVelocity.x;

        // Choose accel or decel
        float rate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;

        // Apply acceleration as force
        float force = speedDiff * rate;
        rb.AddForce(Vector2.right * force, ForceMode2D.Force);

        // Handle Jump
        if (jumpQueued)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpQueued = false;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            grounded = true;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            grounded = false;
    }
}
