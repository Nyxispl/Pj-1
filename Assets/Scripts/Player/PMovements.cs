using UnityEngine;
using UnityEngine.InputSystem;

public class PMovements : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 15f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float airControl = 0.2f;
    [SerializeField] private float dashForce = 20f;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;

    private bool grounded;
    private bool jumpQueued;
    private bool dashQueued;
    private bool isDashing;
    private bool canDash;
    private float dashTime = 0.2f;
    private Vector2 preDashVelocity;

    private Vector2 moveInput;

    private InputSystem_Actions input;

    private void Awake()
    {
        input = new InputSystem_Actions();

        input.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        input.Player.Jump.performed += ctx => {
            if (grounded || isDashing) jumpQueued = true;
        };

        input.Player.Dash.performed += ctx => {
            if (!isDashing) dashQueued = true;
        };
        canDash = true;
    }

    private void OnEnable() => input.Enable();
    private void OnDisable() => input.Disable();

    private void FixedUpdate()
    {
        // Dash
        if (isDashing)
        {
            if (jumpQueued)
            {
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                jumpQueued = false;
            }
            return;
        }

        float moveInputX = moveInput.x;

        float targetSpeed = moveInputX * maxSpeed;
        float speedDiff = targetSpeed - rb.linearVelocity.x;

        float rate = (grounded && Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;

        if (grounded)
        {
            float force = speedDiff * rate;
            rb.AddForce(Vector2.right * force, ForceMode2D.Force);
            canDash = true;
        }
        else
        {
            float airSpeed = moveInputX * maxSpeed * airControl;
            rb.linearVelocity = new Vector2(airSpeed, rb.linearVelocity.y);
        }

        float clampedSpeed = Mathf.Clamp(rb.linearVelocity.x, -maxSpeed, maxSpeed);
        rb.linearVelocity = new Vector2(clampedSpeed, rb.linearVelocity.y);

        Flip(moveInputX);

        if (jumpQueued)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpQueued = false;
        }

        if (dashQueued && !isDashing && canDash)
        {
            Dash();
            dashQueued = false;
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

    private void Dash()
    {
        if (isDashing) return;

        isDashing = true;
        canDash = false;
        preDashVelocity = rb.linearVelocity;

        Vector2 dashDirection = moveInput.normalized;

        if (dashDirection == Vector2.zero)
        {
            dashDirection = new Vector2(Mathf.Sign(transform.localScale.x), 0);
        }

        dashDirection *= dashForce;

        rb.AddForce(dashDirection, ForceMode2D.Impulse);

        Invoke(nameof(EndDash), dashTime);
    }

    private void EndDash()
    {
        isDashing = false;

        float targetSpeed = moveInput.x * maxSpeed;
        rb.linearVelocity = new Vector2(targetSpeed, preDashVelocity.y);
    }

    private void Flip(float moveX)
    {
        if (moveX != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Sign(moveX) * Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }
}
