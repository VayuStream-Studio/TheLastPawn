using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class RookBossController : MonoBehaviour
{
    [Header("Boss Stats")]
    public int maxHealth = 3;
    private int currentHealth;
    public Transform player;
    public Slider healthBarUI;

    [Header("Combat & Horror Settings")]
    public float chargeSpeed = 24f;
    public float tileInterval = 2f;
    public Material dangerTileMaterial;
    public Light eyeGlowLight; // Optional: Assign red Point Light on the Rook's face

    private bool isActing = false;
    private bool isStaggered = false;
    private float baseY;
    private List<GameObject> activeWarningTiles = new List<GameObject>();

    void Start()
    {
        baseY = transform.position.y;
        currentHealth = maxHealth;
        if (healthBarUI != null)
        {
            healthBarUI.maxValue = maxHealth;
            healthBarUI.value = currentHealth;
        }

        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }

        if (eyeGlowLight != null) eyeGlowLight.intensity = 0.5f;
    }

    void Update()
    {
        if (player == null) return;

        // Constantly track player rotation smoothly when not in the middle of a fast charge
        if (!isActing && !isStaggered)
        {
            Vector3 lookDirection = (player.position - transform.position);
            lookDirection.y = 0; // Keep rotation locked flat on the floor plane
            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 6f);
            }
        }

        if (isActing || isStaggered) return;
        StartCoroutine(BossDecisionRoutine());
    }

    IEnumerator BossDecisionRoutine()
    {
        isActing = true;

        // 1. Heavy, mechanical lurch toward player
        Vector3 dirToPlayer = (player.position - transform.position);
        if (Mathf.Abs(dirToPlayer.x) > Mathf.Abs(dirToPlayer.z))
        {
            yield return StartCoroutine(HeavyStepForward(new Vector3(Mathf.Sign(dirToPlayer.x), 0, 0)));
        }
        else
        {
            yield return StartCoroutine(HeavyStepForward(new Vector3(0, 0, Mathf.Sign(dirToPlayer.z))));
        }

        // 2. Check if aligned on a rank or file for a lethal charge
        Vector3 diff = player.position - transform.position;
        if (Mathf.Abs(diff.x) < 1.0f || Mathf.Abs(diff.z) < 1.0f)
        {
            Vector3 chargeDir = Mathf.Abs(diff.x) < 1.0f 
                ? new Vector3(0, 0, Mathf.Sign(diff.z)) 
                : new Vector3(Mathf.Sign(diff.x), 0, 0);
            yield return StartCoroutine(ExecuteLoomingCharge(chargeDir));
        }

        yield return new WaitForSeconds(Random.Range(0.45f, 0.95f));
        isActing = false;
    }

    IEnumerator HeavyStepForward(Vector3 dir)
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + (dir * tileInterval);
        float duration = 0.22f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float smoothT = t * t * (3f - 2f * t);
            float hop = Mathf.Sin(t * Mathf.PI) * 0.25f;

            Vector3 pos = Vector3.Lerp(startPos, targetPos, smoothT);
            pos.y = baseY + hop;
            transform.position = pos;

            elapsed += Time.deltaTime;
            yield return null;
        }

        targetPos.y = baseY;
        transform.position = targetPos;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist < 16f)
        {
            float proximity = 1f - (dist / 16f);
            CameraShake.TriggerShake(0.12f, 0.08f * proximity);
        }
    }

    IEnumerator ExecuteLoomingCharge(Vector3 dir)
    {
        // Snap instantly to face the exact charge direction
        transform.rotation = Quaternion.LookRotation(dir);
        SpawnDangerTiles(dir);

        if (eyeGlowLight != null) eyeGlowLight.intensity = 6f;

        // 1. Rise ominously and tremble violently (0.65s)
        Vector3 groundPos = transform.position;
        float riseDuration = 0.65f;
        float elapsed = 0f;

        while (elapsed < riseDuration)
        {
            float progress = elapsed / riseDuration;
            float lift = Mathf.Lerp(0f, 1.3f, progress);
            Vector3 tremor = Random.insideUnitSphere * (0.08f * progress);
            tremor.y = 0f;

            transform.position = groundPos + Vector3.up * lift + tremor;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 2. Guillotine slam down onto the board (0.12s)
        float slamDuration = 0.12f;
        elapsed = 0f;
        Vector3 peakPos = transform.position;

        while (elapsed < slamDuration)
        {
            transform.position = Vector3.Lerp(peakPos, groundPos, elapsed / slamDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = groundPos;
        CameraShake.TriggerShake(0.22f, 0.18f);
        ClearDangerTiles();

        // 3. Relentless high-speed charge
        float travelTime = 0f;
        bool hitWall = false;
        bool caughtPlayer = false;

        while (travelTime < 1.5f)
        {
            if (Physics.SphereCast(transform.position + Vector3.up, 0.6f, dir, out RaycastHit hit, 1.2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                hitWall = true;
                break;
            }

            if (Vector3.Distance(transform.position, player.position) < 1.5f)
            {
                caughtPlayer = true;
                break;
            }

            transform.position += dir * chargeSpeed * Time.deltaTime;
            travelTime += Time.deltaTime;
            yield return null;
        }

        if (eyeGlowLight != null) eyeGlowLight.intensity = 0.5f;

        if (caughtPlayer)
        {
            PlayerHealth hp = player.GetComponent<PlayerHealth>();
            if (hp != null) hp.TakeDamage(1);
        }

        if (hitWall)
        {
            isStaggered = true;
            CameraShake.TriggerShake(0.4f, 0.28f);

            Quaternion normalRot = transform.rotation;
            transform.rotation = normalRot * Quaternion.Euler(12f, 0f, 0f);

            float staggerTimer = 0f;
            Vector3 wallPos = transform.position;
            while (staggerTimer < 2.5f)
            {
                transform.position = wallPos + Random.insideUnitSphere * 0.04f;
                staggerTimer += Time.deltaTime;
                yield return null;
            }

            transform.position = wallPos;
            transform.rotation = normalRot;
            isStaggered = false;
        }
    }

    public void TakeDamage(int damage)
    {
        if (!isStaggered) return;

        currentHealth -= damage;
        if (healthBarUI != null) healthBarUI.value = currentHealth;

        if (currentHealth <= 0)
        {
            Destroy(gameObject);
        }
    }

    void SpawnDangerTiles(Vector3 dir)
    {
        ClearDangerTiles();
        for (int i = 1; i <= 8; i++)
        {
            Vector3 checkPos = transform.position + (dir * (i * tileInterval));
            GameObject warning = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(warning.GetComponent<Collider>());
            warning.transform.position = new Vector3(checkPos.x, 0.12f, checkPos.z);
            warning.transform.localScale = new Vector3(tileInterval * 0.95f, 0.02f, tileInterval * 0.95f);
            if (dangerTileMaterial != null) warning.GetComponent<Renderer>().material = dangerTileMaterial;
            activeWarningTiles.Add(warning);
        }
    }

    void ClearDangerTiles()
    {
        foreach (var tile in activeWarningTiles) if (tile != null) Destroy(tile);
        activeWarningTiles.Clear();
    }
}