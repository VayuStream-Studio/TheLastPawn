using UnityEngine;
using System.Collections;

public class RookSwordCombat : MonoBehaviour
{
    [Header("References")]
    public Transform playerTarget;
    public Transform swordTransform; 

    [Header("Combat Settings & Ranges")]
    public float meleeRange = 3.5f;
    public float midRange = 7.0f;
    public float attackCooldown = 3.5f;
    public float rotationSpeed = 6.0f; // How fast it turns to face you
    private float attackTimer = 0f;
    private int lastAttackIndex = -1;

    // Sword local transforms for resting vs combat ready
    private Vector3 restPos = new Vector3(0.15f, 1.1f, -0.3f);
    private Vector3 restRot = new Vector3(45f, 0f, -45f);
    
    private Vector3 readyPos = new Vector3(0f, 0.2f, 0.4f);
    private Vector3 readyRot = new Vector3(0f, 0f, 0f);

    private bool isAttacking = false;
    private bool isDashing = false;

    void Start()
    {
        if (playerTarget == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTarget = p.transform;
        }

        if (swordTransform != null)
        {
            swordTransform.localPosition = restPos;
            swordTransform.localEulerAngles = restRot;
        }
    }

    void Update()
    {
        if (playerTarget == null) return;

        // Always face the player smoothly, unless actively executing a fast dash lunge
        if (!isDashing)
        {
            Vector3 dirToPlayer = playerTarget.position - transform.position;
            dirToPlayer.y = 0f; // Keep it level on the ground plane
            if (dirToPlayer != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(dirToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
            }
        }

        if (isAttacking) return;

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            DecideAttackBehavior();
            attackTimer = attackCooldown;
        }
    }

    void DecideAttackBehavior()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        int chosenAttack = -1;

        if (distanceToPlayer <= meleeRange)
        {
            chosenAttack = 0; 
        }
        else if (distanceToPlayer <= midRange)
        {
            chosenAttack = (Random.value > 0.5f) ? 1 : 2; 
        }
        else
        {
            chosenAttack = 3; 
        }

        if (chosenAttack == lastAttackIndex && distanceToPlayer <= midRange)
        {
            chosenAttack = (lastAttackIndex == 1) ? 2 : 1;
        }

        lastAttackIndex = chosenAttack;

        switch (chosenAttack)
        {
            case 0:
                StartCoroutine(MeleeSlashAndSheathe());
                break;
            case 1:
                StartCoroutine(AirSlashAttack());
                break;
            case 2:
                StartCoroutine(OverheadAreaSlamWithIndicator());
                break;
            case 3:
                StartCoroutine(SamuraiDashAttack());
                break;
        }
    }

    // --- ATTACK: Overhead Slam ---
    IEnumerator OverheadAreaSlamWithIndicator()
    {
        isAttacking = true;
        yield return StartCoroutine(TransitionSword(readyPos, readyRot, 0.3f));

        GameObject redCircle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        redCircle.name = "SlamWarningCircle";
        redCircle.transform.position = transform.position + new Vector3(0, 0.05f, 0);
        redCircle.transform.localScale = new Vector3(5f, 0.02f, 5f);
        
        Collider circCol = redCircle.GetComponent<Collider>();
        if (circCol != null) Destroy(circCol);

        MeshRenderer circRend = redCircle.GetComponent<MeshRenderer>();
        if (circRend != null)
        {
            Material redMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (redMat.shader.name == "Hidden/InternalErrorShader") redMat = new Material(Shader.Find("Unlit/Color"));
            redMat.color = new Color(1f, 0f, 0f, 0.7f);
            circRend.material = redMat;
        }

        float elapsed = 0f;
        Quaternion startRot = swordTransform.localRotation;
        Quaternion peakRot = Quaternion.Euler(110f, 0f, 0f);

        while (elapsed < 0.6f)
        {
            elapsed += Time.deltaTime;
            swordTransform.localRotation = Quaternion.Slerp(startRot, peakRot, elapsed / 0.6f);
            yield return null;
        }

        Destroy(redCircle);

        elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            swordTransform.localRotation = Quaternion.Slerp(peakRot, Quaternion.Euler(-30f, 0f, 0f), elapsed / 0.15f);
            yield return null;
        }

        CameraShake.TriggerShake(0.35f, 0.25f);
        CheckCircularAoEHit(5.0f);

        yield return new WaitForSeconds(0.8f);
        yield return StartCoroutine(TransitionSword(restPos, restRot, 0.4f));
        isAttacking = false;
    }

    // --- ATTACK: Samurai Dash ---
    IEnumerator SamuraiDashAttack()
    {
        isAttacking = true;
        yield return StartCoroutine(TransitionSword(readyPos, readyRot, 0.2f));

        // Lock rotation toward player right before locking in the dash trajectory
        Vector3 dir = (playerTarget.position - transform.position);
        dir.y = 0;
        transform.rotation = Quaternion.LookRotation(dir);

        isDashing = true; // Pauses regular Update rotation so it dashes straight

        Vector3 startPos = transform.position;
        Vector3 targetPos = playerTarget.position + (playerTarget.forward * 1.5f);

        yield return new WaitForSeconds(0.4f); // Telegraph pause

        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / 0.2f);

            if (Vector3.Distance(transform.position, playerTarget.position) <= 2.0f)
            {
                playerTarget.SendMessage("TakeDamage", 1, SendMessageOptions.DontRequireReceiver);
            }
            yield return null;
        }

        isDashing = false;
        CameraShake.TriggerShake(0.25f, 0.15f);
        yield return new WaitForSeconds(0.6f);
        yield return StartCoroutine(TransitionSword(restPos, restRot, 0.4f));
        isAttacking = false;
    }

    // --- ATTACK: Melee Slash + Sheathe ---
    IEnumerator MeleeSlashAndSheathe()
    {
        isAttacking = true;
        yield return StartCoroutine(TransitionSword(readyPos, readyRot, 0.2f));

        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            float angle = Mathf.Lerp(-45f, 90f, elapsed / 0.3f);
            swordTransform.localRotation = Quaternion.Euler(0f, angle, 0f);

            if (Vector3.Distance(swordTransform.position, playerTarget.position) <= 2.2f)
            {
                playerTarget.SendMessage("TakeDamage", 1, SendMessageOptions.DontRequireReceiver);
            }
            yield return null;
        }

        yield return new WaitForSeconds(0.4f);
        yield return StartCoroutine(TransitionSword(restPos, restRot, 0.35f));
        isAttacking = false;
    }

    // --- ATTACK: Air Slash Projectile ---
    IEnumerator AirSlashAttack()
    {
        isAttacking = true;
        yield return StartCoroutine(TransitionSword(readyPos, readyRot, 0.2f));
        yield return new WaitForSeconds(0.2f);
        
        GameObject airWave = GameObject.CreatePrimitive(PrimitiveType.Cube);
        airWave.name = "AirSlashWave";
        airWave.transform.position = swordTransform.position;
        airWave.transform.rotation = Quaternion.LookRotation(playerTarget.position - airWave.transform.position);
        airWave.transform.localScale = new Vector3(1.5f, 0.4f, 0.2f);

        MeshRenderer mr = airWave.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            Material waveMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (waveMat.shader.name == "Hidden/InternalErrorShader") waveMat = new Material(Shader.Find("Unlit/Color"));
            waveMat.color = new Color(0.1f, 0.1f, 0.12f, 1f);
            mr.material = waveMat;
        }

        Rigidbody rb = airWave.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearVelocity = airWave.transform.forward * 14f;

        Destroy(airWave, 2.5f);

        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(TransitionSword(restPos, restRot, 0.4f));
        isAttacking = false;
    }

    IEnumerator TransitionSword(Vector3 targetPos, Vector3 targetRotEuler, float duration)
    {
        if (swordTransform == null) yield break;

        Vector3 startPos = swordTransform.localPosition;
        Quaternion startRot = swordTransform.localRotation;
        Quaternion targetRot = Quaternion.Euler(targetRotEuler);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            swordTransform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            swordTransform.localRotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }
    }

    void CheckCircularAoEHit(float radius)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                hit.SendMessage("TakeDamage", 2, SendMessageOptions.DontRequireReceiver);
                CameraShake.TriggerShake(0.3f, 0.2f);
            }
        }
    }
}