using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets; // Needed to talk to the FirstPersonController
using System.Collections;

public class PlayerDodge : MonoBehaviour
{
    private CharacterController controller;
    private FirstPersonController fpsController;
    private Camera playerCamera;

    [Header("Camera Transform (Drag PlayerCameraRoot here)")]
    public Transform cameraRoot;

    [Header("Dodge Settings")]
    public float dodgeDistance = 2.5f;     
    public float dodgeDuration = 0.14f;    
    public float dodgeCooldown = 0.6f;     
    
    [HideInInspector] public bool isInvulnerable = false; 
    private bool isDodging = false;
    private float lastDodgeTime = -999f;

    [Header("Camera Juice (FOV & Tilt)")]
    public float fovKickAmount = 8f;      
    public float maxTiltAngle = 7f;        
    private float defaultFOV;
    private float currentTiltAngle = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        fpsController = GetComponent<FirstPersonController>();
        playerCamera = Camera.main;

        if (playerCamera != null)
            defaultFOV = playerCamera.fieldOfView;

        if (cameraRoot == null && playerCamera != null)
            cameraRoot = playerCamera.transform.parent;
    }

    void Update()
    {
        if (!isDodging && Time.time >= lastDodgeTime + dodgeCooldown)
        {
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                Vector3 dodgeDir = GetDodgeDirection();
                if (dodgeDir != Vector3.zero)
                {
                    float tiltSign = Vector3.Dot(playerCamera.transform.right, dodgeDir);
                    StartCoroutine(ExecuteDodge(dodgeDir, tiltSign));
                }
            }
        }
    }

    // LateUpdate runs AFTER FirstPersonController finishes moving the camera, preventing jitter
    void LateUpdate()
    {
        if (cameraRoot != null && Mathf.Abs(currentTiltAngle) > 0.01f)
        {
            Vector3 currentRot = cameraRoot.localEulerAngles;
            cameraRoot.localEulerAngles = new Vector3(currentRot.x, currentRot.y, currentTiltAngle);
        }
    }

    Vector3 GetDodgeDirection()
    {
        Vector3 dir = Vector3.zero;
        Transform camTransform = playerCamera.transform;

        Vector3 forward = camTransform.forward;
        Vector3 right = camTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Keyboard kb = Keyboard.current;
        if (kb == null) return Vector3.zero;

        if (kb.wKey.isPressed) dir += forward;
        if (kb.sKey.isPressed) dir -= forward;
        if (kb.dKey.isPressed) dir += right;
        if (kb.aKey.isPressed) dir -= right;

        if (dir == Vector3.zero) dir = -forward; 

        return dir.normalized;
    }

    IEnumerator ExecuteDodge(Vector3 direction, float tiltDir)
    {
        isDodging = true;
        isInvulnerable = true; 
        lastDodgeTime = Time.time;

        // 1. Pause normal walking movement so it doesn't fight the dodge
        if (fpsController != null)
            fpsController.enabled = false;

        float elapsed = 0f;
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + (direction * dodgeDistance);
        targetPosition.y = startPosition.y;

        while (elapsed < dodgeDuration)
        {
            float t = elapsed / dodgeDuration;
            float curveT = Mathf.Sin(t * Mathf.PI); 

            // Smooth FOV kick
            if (playerCamera != null)
                playerCamera.fieldOfView = Mathf.Lerp(defaultFOV, defaultFOV + fovKickAmount, curveT);

            // Set tilt angle (applied cleanly in LateUpdate)
            currentTiltAngle = -tiltDir * maxTiltAngle * curveT;

            // Smooth frame-by-frame movement
            float moveT = Mathf.Sin(t * Mathf.PI * 0.5f); 
            Vector3 nextPos = Vector3.Lerp(startPosition, targetPosition, moveT);
            controller.Move(nextPos - transform.position);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Final placement
        controller.Move(targetPosition - transform.position);

        // Reset camera values cleanly
        if (playerCamera != null)
            playerCamera.fieldOfView = defaultFOV;

        currentTiltAngle = 0f;
        if (cameraRoot != null)
        {
            Vector3 resetRot = cameraRoot.localEulerAngles;
            cameraRoot.localEulerAngles = new Vector3(resetRot.x, resetRot.y, 0f);
        }

        // 2. Re-enable normal walking movement immediately
        if (fpsController != null)
            fpsController.enabled = true;

        isInvulnerable = false; 
        isDodging = false;
    }
}