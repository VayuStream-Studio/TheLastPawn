using UnityEngine;

public class CameraHeadBob : MonoBehaviour
{
    private CharacterController characterController;
    private Vector3 originalLocalPos;

    [Header("Bobbing Settings")]
    public Transform playerCapsule;        // Drag your PlayerCapsule here
    public float walkBobSpeed = 7.5f;      // Natural walking stride (~1.2 steps/sec)
    public float walkBobAmount = 0.025f;   // Subtle vertical dip
    public float runBobSpeed = 11.5f;      // Sprinting cadence
    public float runBobAmount = 0.055f;    // Heavier sprint weight
    public float sprintThreshold = 4.5f;   // Speed required to trigger run bob

    private float timer = 0f;

    void Start()
    {
        originalLocalPos = transform.localPosition;
        
        if (playerCapsule != null)
            characterController = playerCapsule.GetComponent<CharacterController>();
    }

    void Update()
    {
        if (playerCapsule == null || characterController == null) return;

        // Ignore vertical falling velocity so jumping doesn't trigger rapid bobbing
        Vector3 horizontalVelocity = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z);
        float speed = horizontalVelocity.magnitude;

        bool isMoving = characterController.isGrounded && speed > 0.2f;
        bool isRunning = speed > sprintThreshold;

        if (isMoving)
        {
            float targetBobSpeed = isRunning ? runBobSpeed : walkBobSpeed;
            float targetBobAmount = isRunning ? runBobAmount : walkBobAmount;

            timer += Time.deltaTime * targetBobSpeed;

            // Gentle figure-eight sway: vertical dip on every step, slight side sway every two steps
            float targetX = originalLocalPos.x + (Mathf.Cos(timer * 0.5f) * targetBobAmount * 0.6f);
            float targetY = originalLocalPos.y + (Mathf.Sin(timer) * targetBobAmount);

            // Smoothly interpolate so transitions between walk, sprint, and stop never snap
            Vector3 targetPos = new Vector3(targetX, targetY, originalLocalPos.z);
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * 12f);
        }
        else
        {
            timer = 0f;
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalLocalPos, Time.deltaTime * 8f);
        }
    }
}