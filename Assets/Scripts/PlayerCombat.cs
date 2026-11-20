using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

public class PlayerCombat : MonoBehaviour
{
    private Animator animator;
    private ThirdPersonController movementController;

    private int comboStep = 0;
    private float lastAttackTime = 0f;
    private float cooldownEndTime = 0f; 
    
    public float comboResetTime = 3.0f; // Window before combo times out

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        movementController = GetComponent<ThirdPersonController>();
    }

    void Update()
    {
        // Check if we are locked out on a cooldown
        if (Time.time < cooldownEndTime)
        {
            return; 
        }

        // If the 3-second window expires while mid-combo, reset with a short penalty
        if (comboStep > 0 && Time.time - lastAttackTime > comboResetTime)
        {
            ApplyCooldown(1.0f); 
        }

        // Left-click detection
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            PerformComboAttack();
        }
    }

    void PerformComboAttack()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (comboStep > 0 && !stateInfo.IsName("Idle") && stateInfo.normalizedTime < 0.3f)
        {
            return; 
        }

        lastAttackTime = Time.time;
        comboStep++;

        // If they reached past the 3-hit combo, apply full combo cooldown and reset
        if (comboStep > 3)
        {
            ApplyCooldown(1.0f); 
            return;
        }

        if (movementController != null)
        {
            movementController.MoveSpeed = 1.0f;
            movementController.SprintSpeed = 1.0f;
        }

        animator.SetInteger("ComboStep", comboStep);
    }

    void ApplyCooldown(float duration)
    {
        int reachedStep = comboStep;
        
        comboStep = 0;
        animator.SetInteger("ComboStep", 0);

        // Fixed the float values (using 'f' instead of stray letters)
        float finalCd = duration;
        if (reachedStep == 1) finalCd = 0.3f;       // Fast repeat for single swing
        else if (reachedStep == 2) finalCd = 0.6f;  // Short recovery for 2-hit
        else if (reachedStep >= 3) finalCd = 1.0f;  // Recovery for full 3-hit string

        cooldownEndTime = Time.time + finalCd;
        Debug.Log($"Combo finished at step {reachedStep}. Cooldown applied: {finalCd} seconds.");

        if (movementController != null)
        {
            movementController.MoveSpeed = 2.0f;
            movementController.SprintSpeed = 5.333f;
        }
    }

    public void ResetCombo()
    {
        ApplyCooldown(1.0f);
    }
}