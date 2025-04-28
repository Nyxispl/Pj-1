using UnityEngine;

public class Ca : MonoBehaviour
{
    public Transform player;        // Assign your Player Transform here
    public Vector2 screenSize = new Vector2(16, 9); // Size of one screen (example for 16:9 units)
    public float smoothTime = 0.2f;  // How smooth the camera moves (optional)

    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (player != null)
        {
            // Find which "screen" the player is currently in
            int screenX = Mathf.FloorToInt(player.position.x / screenSize.x);
            int screenY = Mathf.FloorToInt(player.position.y / screenSize.y);

            // Target position for the camera (centered on the current screen)
            Vector3 targetPosition = new Vector3(
                screenX * screenSize.x + screenSize.x / 2,
                screenY * screenSize.y + screenSize.y / 2,
                transform.position.z // Keep original Z
            );

            // Smoothly move the camera
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }
    }
}