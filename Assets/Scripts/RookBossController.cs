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
    public float wallCheckRadius = 0.25f;
    public float pivotOffsetY = 0f;
    public Material dangerTileMaterial;
    public Light eyeGlowLight;

    private bool isActing = false;
    private bool isStaggered = false;
    private Vector3 lastStepDir = Vector3.zero;
    private float stuckTimer = 0f;
    private List<GameObject> activeWarningTiles = new List<GameObject>();
    private Collider[] ownColliders;

    void Awake()
    {
        // Cache all colliders on the Rook so raycasts/spherecasts never hit itself
        ownColliders = GetComponentsInChildren<Collider>();
    }

    void Start()
    {
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

        // Snap cleanly onto the floor on spawn
        Vector3 startPos = transform.position;
        if (TryGetFloorHeight(startPos, out float floorY))
        {
            startPos.y = floorY;
            transform.position = startPos;
        }

        if (eyeGlowLight != null) eyeGlowLight.intensity = 0.5f;
    }

    void Update()
    {
        if (player == null) return;

        // Smoothly track player with glowing eyes
        if (!isActing && !isStaggered)
        {
            Vector3 lookDirection = (player.position - transform.position);
            lookDirection.y = 0;
            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 6f);
            }
        }

        if (isActing || isStaggered) return;
        StartCoroutine(BossDecisionRoutine());
    }

    bool IsOwnCollider(Collider col)
    {
        if (col == null || ownColliders == null) return false;
        for (int i = 0; i < ownColliders.Length; i++)
        {
            if (ownColliders[i] == col) return true;
        }
        return false;
    }

    bool TryGetFloorHeight(Vector3 checkPos, out float resultY)
    {
        RaycastHit[] hits = Physics.RaycastAll(checkPos + Vector3.up * 3f, Vector3.down, 8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float closestDist = float.MaxValue;
        bool found = false;
        resultY = transform.position.y;

        for (int i = 0; i < hits.Length; i++)
        {
            if (IsOwnCollider(hits[i].collider)) continue;
            if (hits[i].distance < closestDist)
            {
                closestDist = hits[i].distance;
                resultY = hits[i].point.y + pivotOffsetY;
                found = true;
            }
        }
        return found;
    }

    bool CheckObstacle(Vector3 origin, Vector3 dir, float distance)
    {
        RaycastHit[] hits = Physics.SphereCastAll(origin, wallCheckRadius, dir, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            if (IsOwnCollider(hits[i].collider)) continue;
            if (player != null && (hits[i].transform == player || hits[i].transform.IsChildOf(player))) continue;
            return true;
        }
        return false;
    }

    IEnumerator BossDecisionRoutine()
    {
        isActing = true;

        // 1. Check if aligned for a lethal guillotine charge
        Vector3 diff = player.position - transform.position;
        if (Mathf.Abs(diff.x) < 1.0f || Mathf.Abs(diff.z) < 1.0f)
        {
            Vector3 chargeDir = Mathf.Abs(diff.x) < 1.0f 
                ? new Vector3(0, 0, Mathf.Sign(diff.z)) 
                : new Vector3(Mathf.Sign(diff.x), 0, 0);

            float distToPlayer = Vector3.Distance(transform.position, player.position);
            bool wallInWay = CheckObstacle(transform.position + Vector3.up * 1.2f, chargeDir, Mathf.Max(0.1f, distToPlayer - 0.5f));

            if (!wallInWay)
            {
                stuckTimer = 0f;
                yield return StartCoroutine(ExecuteLoomingCharge(chargeDir));
                yield return new WaitForSeconds(Random.Range(0.45f, 0.95f));
                isActing = false;
                yield break;
            }
        }

        // 2. Smart Corridor Navigation
        Vector3 chosenDir = GetBestOpenDirection();
        
        if (chosenDir == Vector3.zero)
        {
            stuckTimer += 1f;
            if (stuckTimer >= 3f)
            {
                stuckTimer = 0f;
                yield return StartCoroutine(ExecuteShadowAmbush());
                isActing = false;
                yield break;
            }
        }
        else
        {
            stuckTimer = 0f;
            lastStepDir = chosenDir;
            yield return StartCoroutine(HeavyStepForward(chosenDir));
        }

        yield return new WaitForSeconds(Random.Range(0.45f, 0.95f));
        isActing = false;
    }

    Vector3 GetBestOpenDirection()
    {
        Vector3 dirToPlayer = player.position - transform.position;
        Vector3 primary = Mathf.Abs(dirToPlayer.x) > Mathf.Abs(dirToPlayer.z)
            ? new Vector3(Mathf.Sign(dirToPlayer.x), 0, 0)
            : new Vector3(0, 0, Mathf.Sign(dirToPlayer.z));

        Vector3 secondary = primary.x != 0
            ? new Vector3(0, 0, Mathf.Sign(dirToPlayer.z == 0 ? 1 : dirToPlayer.z))
            : new Vector3(Mathf.Sign(dirToPlayer.x == 0 ? 1 : dirToPlayer.x), 0, 0);

        Vector3[] candidates = new Vector3[] { primary, secondary, -secondary, -primary };

        foreach (Vector3 dir in candidates)
        {
            if (dir != -lastStepDir && !IsTileBlocked(dir))
                return dir;
        }

        foreach (Vector3 dir in candidates)
        {
            if (!IsTileBlocked(dir))
                return dir;
        }

        return Vector3.zero;
    }

    bool IsTileBlocked(Vector3 dir)
    {
        Vector3 nextTileCenter = transform.position + (dir * tileInterval);
        // Must have a valid floor at the next tile AND no wall blocking the path
        if (!TryGetFloorHeight(nextTileCenter, out _)) return true;
        return CheckObstacle(transform.position + Vector3.up * 1.2f, dir, tileInterval);
    }

    IEnumerator HeavyStepForward(Vector3 dir)
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + (dir * tileInterval);
        float startY = startPos.y;
        float targetY = TryGetFloorHeight(targetPos, out float floorY) ? floorY : startY;

        float duration = 0.22f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float smoothT = t * t * (3f - 2f * t);
            float hop = Mathf.Sin(t * Mathf.PI) * 0.25f;

            Vector3 pos = Vector3.Lerp(startPos, targetPos, smoothT);
            pos.y = Mathf.Lerp(startY, targetY, smoothT) + hop;
            transform.position = pos;

            elapsed += Time.deltaTime;
            yield return null;
        }

        targetPos.y = targetY;
        transform.position = targetPos;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist < 16f)
        {
            float proximity = 1f - (dist / 16f);
            CameraShake.TriggerShake(0.12f, 0.08f * proximity);
        }
    }

    IEnumerator ExecuteShadowAmbush()
    {
        // Find a safe, valid floor spot behind or in front of the player so it never teleports out of bounds
        Vector3[] testOffsets = new Vector3[]
        {
            -player.forward * (tileInterval * 3f),
            player.forward * (tileInterval * 4f),
            -player.forward * (tileInterval * 2f),
            player.forward * (tileInterval * 2f)
        };

        Vector3 validAmbushPos = Vector3.zero;
        bool foundSafeSpot = false;

        for (int i = 0; i < testOffsets.Length; i++)
        {
            Vector3 rawPos = player.position + testOffsets[i];
            float snappedX = Mathf.Round(rawPos.x / tileInterval) * tileInterval;
            float snappedZ = Mathf.Round(rawPos.z / tileInterval) * tileInterval;
            Vector3 candidate = new Vector3(snappedX, player.position.y, snappedZ);

            // Only allow teleport if there is actual floor beneath it and clear air around it
            if (TryGetFloorHeight(candidate, out float safeY) && !Physics.CheckSphere(new Vector3(snappedX, safeY + 1.2f, snappedZ), wallCheckRadius))
            {
                validAmbushPos = new Vector3(snappedX, safeY, snappedZ);
                foundSafeSpot = true;
                break;
            }
        }

        // If no safe spot exists, stay put instead of vanishing into the void
        if (!foundSafeSpot) yield break;

        if (eyeGlowLight != null) eyeGlowLight.intensity = 0f;
        CameraShake.TriggerShake(0.5f, 0.05f);
        yield return new WaitForSeconds(0.8f);

        transform.position = validAmbushPos;

        if (eyeGlowLight != null) eyeGlowLight.intensity = 8f;
        CameraShake.TriggerShake(0.4f, 0.35f);

        Vector3 ambushDir = (player.position - transform.position).normalized;
        ambushDir = Mathf.Abs(ambushDir.x) > Mathf.Abs(ambushDir.z) 
            ? new Vector3(Mathf.Sign(ambushDir.x), 0, 0) 
            : new Vector3(0, 0, Mathf.Sign(ambushDir.z));
        
        yield return StartCoroutine(ExecuteLoomingCharge(ambushDir));
    }

    IEnumerator ExecuteLoomingCharge(Vector3 dir)
    {
        transform.rotation = Quaternion.LookRotation(dir);
        SpawnDangerTiles(dir);

        if (eyeGlowLight != null) eyeGlowLight.intensity = 6f;

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

        float travelTime = 0f;
        bool hitWall = false;
        bool caughtPlayer = false;

        while (travelTime < 1.5f)
        {
            if (CheckObstacle(transform.position + Vector3.up * 1.2f, dir, 1.2f))
            {
                hitWall = true;
                break;
            }

            if (Vector3.Distance(transform.position, player.position) < 1.5f)
            {
                caughtPlayer = true;
                break;
            }

            Vector3 nextPos = transform.position + (dir * chargeSpeed * Time.deltaTime);
            if (TryGetFloorHeight(nextPos, out float floorY))
            {
                nextPos.y = floorY;
            }
            transform.position = nextPos;

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
            GetComponent<RookTendrils>()?.TriggerStaggerThrash(2.5f); // <-- Whip tendrils out!

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
            float tileY = (TryGetFloorHeight(checkPos, out float floorY) ? floorY : transform.position.y) - pivotOffsetY + 0.05f;
            GameObject warning = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(warning.GetComponent<Collider>());
            warning.transform.position = new Vector3(checkPos.x, tileY, checkPos.z);
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