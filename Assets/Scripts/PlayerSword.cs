using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem; // <-- Make sure to include this namespace at the top!

public class PlayerSword : MonoBehaviour
{
    [Header("Sword Settings")]
    public Transform swordPivot;      
    public float attackRange = 4.5f;
    public int attackDamage = 1;
    public float attackCooldown = 0.55f;
    public LayerMask hitLayers = ~0;

    [Header("Swing Feel")]
    public Vector3 restRotation = new Vector3(10f, -15f, 0f);
    public Vector3 swingRotation = new Vector3(65f, -45f, 25f);
    public float swingDuration = 0.16f;

    private bool isSwinging = false;
    private float nextAttackTime = 0f;

    void Start()
    {
        if (swordPivot == null)
        {
            swordPivot = transform.Find("SwordPivot");
        }

        if (swordPivot != null)
        {
            swordPivot.localEulerAngles = restRotation;
        }
    }

    void Update()
    {
        // Updated to use the new Input System mouse check
        bool leftClickPressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        if (leftClickPressed && Time.time >= nextAttackTime && !isSwinging)
        {
            nextAttackTime = Time.time + attackCooldown;
            StartCoroutine(SwingRoutine());
        }
    }

    IEnumerator SwingRoutine()
    {
        isSwinging = true;

        float elapsed = 0f;
        Quaternion startRot = Quaternion.Euler(restRotation);
        Quaternion endRot = Quaternion.Euler(swingRotation);

        while (elapsed < swingDuration)
        {
            if (swordPivot != null)
            {
                swordPivot.localRotation = Quaternion.Slerp(startRot, endRot, elapsed / swingDuration);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        PerformHitCheck();

        elapsed = 0f;
        float returnDuration = swingDuration * 1.4f;
        while (elapsed < returnDuration)
        {
            if (swordPivot != null)
            {
                swordPivot.localRotation = Quaternion.Slerp(endRot, startRot, elapsed / returnDuration);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (swordPivot != null) swordPivot.localRotation = startRot;
        isSwinging = false;
    }

    void PerformHitCheck()
    {
        if (Physics.SphereCast(transform.position, 0.75f, transform.forward, out RaycastHit hit, attackRange, hitLayers, QueryTriggerInteraction.Ignore))
        {
            Debug.Log("Sword raycast hit: " + hit.collider.name);
            RookBossController rook = hit.collider.GetComponentInParent<RookBossController>();
            if (rook != null)
            {
                rook.TakeDamage(attackDamage);
                CameraShake.TriggerShake(0.15f, 0.12f);
            }
        }
    }
}