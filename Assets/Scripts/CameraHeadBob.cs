using UnityEngine;

public class CameraHeadBob : MonoBehaviour
{
    private CharacterController characterController;
    private Vector3 originalLocalPos;

    [Header("Bobbing Settings")]
    public Transform playerCapsule;        // Drag your PlayerCapsule here
    public float walkBobSpeed = 14f;
    public float walkBobAmount = 0.05f;
    public float runBobSpeed = 20f;
    public float runBobAmount = 0.12f;
    public float sprintThreshold = 5.0f;   // Speed required to trigger run bob

    private float timer = 0f;

    void Start()
    {
        originalLocalPos = transform.localPosition;
        
        // Auto-find character controller if not manually linked
        if (playerCapsule != null)
            characterController = playerCapsule.GetComponent<CharacterController>();
    }

    void Update()
    {
        if (playerCapsule == null || characterController == null) return;

        // Check if player is moving on the ground
        bool isMoving = characterController.isGrounded && characterController.velocity.magnitude > 0.1f;
        bool isRunning = characterController.velocity.magnitude > sprintThreshold;

        if (isMoving)
        {
            // Choose speed and intensity based on walking vs running
            float currentSpeed = isRunning ? runBobSpeed : walkBobSpeed;
            float currentAmount = isRunning ? runBobAmount : walkBobAmount;

            timer += Time.deltaTime * currentSpeed;

            // Apply sine wave bobbing to Y (height) and X (side-to-side sway)
            float targetX = originalLocalPos.x + (Mathf.Cos(timer * 0.5f) * currentAmount * 0.5f);
            float targetY = originalLocalPos.y + (Mathf.Sin(timer) * currentAmount);

            transform.localPosition = new Vector3(targetX, targetY, originalLocalPos.z);
        }
        else
        {
            // Smoothly return camera to center when standing still
            timer = 0f;
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalLocalPos, Time.deltaTime * 6f);
        }
    }
}