using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;
using static UnityEngine.UI.Image;

public class PMovements : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 15f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float airControl = 0.2f;
    [SerializeField] private float dashForce = 20f;
    [SerializeField] private float dashBoostTimer = 0.5f;
    [SerializeField] private float dashTime = 0.2f;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;

    private bool grounded;
    private bool onWall;
    private bool jumpQueued;
    private bool dashQueued;
    private bool isDashing;
    private bool canDash;
    private bool DashBoost; // New variable to track jumping
    private Vector2 preDashVelocity;
    private BoxCollider2D box;
    Bounds bounds;
    private Vector2 dashDirection;
    private Vector2 colliderPos;
    private Vector2 colliderSize;
    private Vector2 colliderOffset;

    private Vector2 bottomPos;
    private Vector2 RsidePos;
    private Vector2 LsidePos;
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
        box = GetComponent<BoxCollider2D>();
    }

    private void OnEnable() => input.Enable();
    private void OnDisable() => input.Disable();

    private void FixedUpdate()
    {

        float moveInputX = moveInput.x;

        float targetSpeed = moveInputX * maxSpeed;
        float speedDiff = targetSpeed - rb.linearVelocity.x;
        bounds = box.bounds;
        Vector2 colliderPos = box.transform.position;
        Vector2 colliderSize = box.size;
        Vector2 colliderOffset = box.offset;
        Vector2 bottomPos = new Vector2(bounds.center.x, bounds.min.y - 0.05f);
        Vector2 RsidePos = new Vector2(bounds.max.x + 0.05f, bounds.center.y);
        Vector2 LsidePos = new Vector2(bounds.min.x - 0.05f, bounds.center.y);

        float rate = (grounded && Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;

        if (Mathf.Abs(rb.linearVelocity.x) > Mathf.Abs(targetSpeed)) rate = deceleration;

        RaycastHit2D GroundCheck = Physics2D.Raycast(bottomPos, Vector2.down, 1f);
        if (GroundCheck.collider != null && GroundCheck.collider.CompareTag("Ground"))
        {
            grounded = true;
            DashBoost = false; // Player is no longer jumping
        }
        else
        {
            grounded = false;
        }
        RaycastHit2D RwallCheck = Physics2D.Raycast(RsidePos, Vector2.right, 1f);
        RaycastHit2D LwallCheck = Physics2D.Raycast(LsidePos, Vector2.left, 1f);
        if (((RwallCheck.collider != null && RwallCheck.collider.CompareTag("Ground")) || (LwallCheck.collider != null && LwallCheck.collider.CompareTag("Ground"))) && !grounded)
        {
            onWall = true;
        }
        else if (onWall && grounded)
        {
            onWall = false; // Reset onWall when grounded
        }
        Color rayColor = GroundCheck.collider != null ? Color.red : Color.green;
        Debug.DrawRay(bottomPos, Vector2.down * 1f, rayColor);
        Color rayColor1 = RwallCheck.collider != null ? Color.red : Color.green;
        Debug.DrawRay(RsidePos, Vector2.right * 1f, rayColor1);
        Color rayColor2 = LwallCheck.collider != null ? Color.red : Color.green;
        Debug.DrawRay(LsidePos, Vector2.left * 1f, rayColor2);
        if (grounded)
        {
            float force = speedDiff * rate;
            rb.AddForce(Vector2.right * force, ForceMode2D.Force);
            canDash = true;
        }
        else if (onWall)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y*0.5f); // Stop horizontal movement on wall
        }
        else
        {
            float desiredVelX = moveInputX * maxSpeed;
            float speedDiffX  = desiredVelX - rb.linearVelocity.x;

            // if you’re holding a direction, use your weakened airControl.
            // if you let go, use your full deceleration to brake.
            float airRate = Mathf.Abs(moveInputX) > 0.01f 
                ? airControl 
                : deceleration;

            float airForce = speedDiffX * airRate;
            rb.AddForce(Vector2.right * airForce, ForceMode2D.Force);

            // clamp so you never rocket above your maxSpeed
            float clampedX = Mathf.Clamp(rb.linearVelocity.x, -maxSpeed, maxSpeed);
            rb.linearVelocity = new Vector2(clampedX, rb.linearVelocity.y);
                }
                float clampedSpeed = Mathf.Clamp(rb.linearVelocity.x, -maxSpeed, maxSpeed);
                rb.linearVelocity = new Vector2(clampedSpeed, rb.linearVelocity.y);
                Flip(moveInputX);


        // Jump
        if (jumpQueued)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpQueued = false;
            DashBoost = true; // Player is jumping
        }

        if (dashQueued && !isDashing && canDash)
        {
            Dash();
            dashQueued = false;
        }
        if (dashBoostTimer > 0)
        {
            dashBoostTimer -= Time.fixedDeltaTime; // Decrease timer
            if (dashBoostTimer <= 0)
            {
                DashBoost = false; // Reset DashBoost when timer ends
            }
        }

    }

    private void Dash()
    {
        if (isDashing) return;

        Debug.Log("Dashing called");

        isDashing = true;
        canDash = false;
        preDashVelocity = rb.linearVelocity;

        dashDirection = moveInput.normalized;

        if (dashDirection == Vector2.zero)
        {
            dashDirection = new Vector2(Mathf.Sign(transform.localScale.x), 0);
        }

        dashDirection = dashDirection.normalized;

        rb.AddForce(dashDirection*dashForce, ForceMode2D.Impulse);
        Debug.Log($"dash force on y : {dashDirection.y*dashForce}");
        Debug.Log($"dash force on x : {dashDirection.x * dashForce}");

        Invoke(nameof(EndDash), dashTime);
    }

    private void EndDash()
    {
        isDashing = false;

        float targetSpeed = dashDirection.x * maxSpeed;
        if (DashBoost)
        {
            rb.linearVelocity = new Vector2(targetSpeed*1.5f, preDashVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(targetSpeed, preDashVelocity.y);
        }
        DashBoost = false;
        Debug.Log($"dash end on y : {rb.linearVelocity.y}");
        Debug.Log($"dash end on x : {rb.linearVelocity.x}");
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