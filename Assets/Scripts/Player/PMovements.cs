using Mono.Cecil.Cil;
using UnityEngine;
using UnityEngine.InputSystem;

public class PMovements : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float maxSpeed = 5f;          // 🟢 Vitesse maximale du joueur / Player max speed
    [SerializeField] private float acceleration = 10f;     // 🟢 Accélération au sol / Acceleration on ground
    [SerializeField] private float deceleration = 15f;     // 🟢 Décélération au sol ou dans les airs / Deceleration on ground or air
    [SerializeField] private float jumpForce = 10f;        // 🟢 Force du saut / Jump force
    [SerializeField] private float airControl = 0.2f;      // 🟢 Contrôle en l'air / Movement control in air
    [SerializeField] private float dashForce = 20f;        // 🟢 Force du dash / Dash force
    [SerializeField] private float dashBoostTimer = 0.5f;  // 🟢 Temps de bonus après dash / Time for boost after dash
    [SerializeField] private float dashTime = 0.2f;        // 🟢 Durée du dash / Dash duration
    [SerializeField] private float coyoteTime = 0.2f;  // 🟢 Temps de coyote / Coyote time (in seconds)


    [Header("References")]
    [SerializeField] private Rigidbody2D rb;               // 🟢 Rigidbody du joueur / Player Rigidbody
    private float coyoteTimeCounter = 0f;              // 🟢 Compteur du coyote / Coyote time counter

    // 🔵 États de mouvement / Movement states
    private bool grounded;
    private bool onWall;
    private bool jumpQueued;
    private bool dashQueued;
    private bool isDashing;
    private bool canDash;
    private bool DashBoost;
    private Vector2 wallJumpSide;

    private Vector2 preDashVelocity;
    private BoxCollider2D box;
    Bounds bounds;

    private Vector2 dashDirection;
    private Vector2 moveInput;
    private InputSystem_Actions input;

    private void Awake()
    {
        input = new InputSystem_Actions();

        // 🔵 Capture du mouvement / Movement input
        input.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        // 🔵 Capture du saut / Jump input
        input.Player.Jump.performed += ctx => {
            jumpQueued = true;
            Invoke(nameof(ResetJumpQueued), 0.5f);
        };

        // 🔵 Capture du dash / Dash input
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
        Vector2 bottomPos = new Vector2(bounds.center.x, bounds.min.y);
        Vector2 RsidePos = new Vector2(bounds.max.x, bounds.center.y);
        Vector2 LsidePos = new Vector2(bounds.min.x, bounds.center.y);

        float rate = (grounded && Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        if (Mathf.Abs(rb.linearVelocity.x) > Mathf.Abs(targetSpeed)) rate = deceleration;

        // 🔄 Gestion du mouvement horizontal selon l’état (sol, mur, air) / Movement logic based on state
        if (grounded)
        {
            float force = speedDiff * rate;
            rb.AddForce(Vector2.right * force, ForceMode2D.Force);
            canDash = true;
        }
        else if (onWall)
        {
            // Slight control on wall (slower than air)
            float desiredVelX = moveInput.x * (maxSpeed * 0.5f); // Half speed on walls
            float speedDiffX = desiredVelX - rb.linearVelocity.x;
            float wallRate = airControl * 0.5f; // Even weaker than airControl
            float wallForce = speedDiffX * wallRate;

            rb.AddForce(Vector2.right * wallForce, ForceMode2D.Force);

            // Reduce fall speed
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.8f);
        }
        else
        {
            float desiredVelX = moveInputX * maxSpeed;
            float speedDiffX = desiredVelX - rb.linearVelocity.x;

            float airRate = Mathf.Abs(moveInputX) > 0.01f ? airControl : deceleration;
            float airForce = speedDiffX * airRate;
            rb.AddForce(Vector2.right * airForce, ForceMode2D.Force);

            float clampedX = Mathf.Clamp(rb.linearVelocity.x, -maxSpeed, maxSpeed);
            rb.linearVelocity = new Vector2(clampedX, rb.linearVelocity.y);
        }

        // 🔒 Clamp vitesse encore une fois (sécurité) / Clamp speed again for safety
        float clampedSpeed = Mathf.Clamp(rb.linearVelocity.x, -maxSpeed, maxSpeed);
        rb.linearVelocity = new Vector2(clampedSpeed, rb.linearVelocity.y);

        // 🔄 Flip sprite en fonction du mouvement / Flip sprite based on movement
        Flip(moveInputX);

        if (!grounded)
        {
            coyoteTimeCounter -= Time.fixedDeltaTime;

        }

        // 🟢 Jump
        if (jumpQueued && (grounded || coyoteTimeCounter > 0f))
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpQueued = false;
            DashBoost = true;
        }

        if(jumpQueued && onWall)
        {
            wallJumpSide = moveInput.normalized;
            if (moveInput == Vector2.zero)
            {
                wallJumpSide = new Vector2(Mathf.Sign(transform.localScale.x), 0);
                wallJumpSide = wallJumpSide.normalized;
            }
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            rb.AddForce(Vector2.right * -wallJumpSide.x * 5f, ForceMode2D.Impulse);
            jumpQueued = false;
            DashBoost = true;
        }

        // 🟠 Dash
        if (dashQueued && !isDashing && canDash)
        {
            Dash();
            dashQueued = false;
        }

        // ⏱️ Timer du DashBoost / Dash boost timer
        if (dashBoostTimer > 0)
        {
            dashBoostTimer -= Time.fixedDeltaTime;
            if (dashBoostTimer <= 0)
            {
                DashBoost = false;
            }
        }
    }

    // 🔁 Détection de collisions (sol/mur) en continu / Ongoing collision detection
    private void OnCollisionStay2D(Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            Vector2 normal = contact.normal;

            if (normal.y > 0.5f)
            {
                grounded = true;
                coyoteTimeCounter = 0f;
            }
            else if (normal.x > 0.5f || normal.x < -0.5f)
            {
                onWall = true;
            }
        }
    }

    // ❌ Quitte le sol ou un mur / Leaving ground or wall
    private void OnCollisionExit2D(Collision2D collision)
    {
        grounded = false;
        onWall = false;
        coyoteTimeCounter = coyoteTime; // Start
                                        // coyote time when leaving ground
    }

    // ⚡ Dash logic
    private void Dash()
    {
        if (isDashing) return;

        isDashing = true;
        canDash = false;
        preDashVelocity = rb.linearVelocity;

        dashDirection = moveInput.normalized;
        if (dashDirection == Vector2.zero)
        {
            dashDirection = new Vector2(Mathf.Sign(transform.localScale.x), 0);
        }

        dashDirection = dashDirection.normalized;
        rb.AddForce(dashDirection * dashForce, ForceMode2D.Impulse);

        Invoke(nameof(EndDash), dashTime);
    }

    // ⏹️ Fin du dash / End of dash
    private void EndDash()
    {
        isDashing = false;
        rb.linearVelocity = preDashVelocity;
        DashBoost = false;
    }

    // 🔁 Retourne le sprite selon la direction / Flip sprite to match direction
    private void Flip(float moveX)
    {
        if (moveX != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Sign(moveX) * Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }
    // 🔁 Réinitialise le saut en attente / Reset jump queued
    private void ResetJumpQueued()
    {
        jumpQueued = false;
    }
}
