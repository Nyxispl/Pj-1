using UnityEngine;

public class AnimatorController : MonoBehaviour
{
    private Animator animator;
    private Rigidbody2D rb;

    void Start()
    {
        animator = GetComponent<Animator>();  // Get the Animator component
        rb = GetComponent<Rigidbody2D>();     // Get the Rigidbody2D component
    }

    void Update()
    {
        // Set the animator parameters based on the player's movement and velocity
        bool IsJumping = rb.linearVelocity.y > 0.1;   // Jumping if going up
        bool IsFalling = rb.linearVelocity.y < -0.1;   // Falling if going down
        bool IsRunning = Mathf.Abs(rb.linearVelocity.x) > 0.1f && !IsJumping && !IsFalling; // Running if moving horizontally and not jumping/falling

        animator.SetBool("IsJumping", IsJumping);
        animator.SetBool("IsFalling", IsFalling);
        animator.SetBool("IsRunning", IsRunning);
    }
}