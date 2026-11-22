using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class RookBossController : MonoBehaviour
{
    [Header("Boss Stats")]
    public int maxHealth = 3;
    private int currentHealth;
    public Transform player;
    public Slider healthBarUI;

    [Header("Combat Settings")]
    public float marchSpeed = 4f;
    public float chargeSpeed = 18f;
    public float tileInterval = 2f;
    public Material dangerTileMaterial;

    private bool isActing = false;
    private bool isStaggered = false;
    private List<GameObject> activeWarningTiles = new List<GameObject>();

    void Start()
    {
        currentHealth = maxHealth;
        if (healthBarUI != null)
        {
            healthBarUI.maxValue = maxHealth;
            healthBarUI.value = currentHealth;
        }
    }

    void Update()
    {
        if (isActing || isStaggered || player == null) return;

        // Ominously decide next boss action based on player distance/alignment
        StartCoroutine(BossDecisionRoutine());
    }

    IEnumerator BossDecisionRoutine()
    {
        isActing = true;

        // 1. Ominous slow march closer to player
        Vector3 dirToPlayer = (player.position - transform.position);
        if (Mathf.Abs(dirToPlayer.x) > Mathf.Abs(dirToPlayer.z))
        {
            yield return StartCoroutine(StepForward(new Vector3(Mathf.Sign(dirToPlayer.x), 0, 0)));
        }
        else
        {
            yield return StartCoroutine(StepForward(new Vector3(0, 0, Mathf.Sign(dirToPlayer.z))));
        }

        // 2. Check if aligned for a devastating straight-line charge
        Vector3 diff = player.position - transform.position;
        if (Mathf.Abs(diff.x) < 0.8f || Mathf.Abs(diff.z) < 0.8f)
        {
            Vector3 chargeDir = Mathf.Abs(diff.x) < 0.8f ? new Vector3(0, 0, Mathf.Sign(diff.z)) : new Vector3(Mathf.Sign(diff.x), 0, 0);
            yield return StartCoroutine(ExecuteBossCharge(chargeDir));
        }

        yield return new WaitForSeconds(1.0f);
        isActing = false;
    }

    IEnumerator StepForward(Vector3 dir)
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + (dir * tileInterval);
        float elapsed = 0f;

        while (elapsed < 0.4f)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / 0.4f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPos;
    }

    IEnumerator ExecuteBossCharge(Vector3 dir)
    {
        // Telegraph danger tiles
        SpawnDangerTiles(dir);
        yield return new WaitForSeconds(0.8f); // Windup warning
        ClearDangerTiles();

        float travelTime = 0f;
        bool hitWall = false;
        bool caughtPlayer = false;

        while (travelTime < 1.5f)
        {
            if (Physics.Raycast(transform.position + Vector3.up, dir, 1.2f))
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

        if (caughtPlayer)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            yield break;
        }

        // If it hit a wall, it enters a Stagger State (Boss Weak Point Window)
        if (hitWall)
        {
            isStaggered = true;
            Debug.Log("Boss slammed into the wall! Staggered for 2.5 seconds—attack now!");
            
            // Visual stagger feedback (shake slightly)
            float staggerTimer = 0f;
            Vector3 wallPos = transform.position;
            while (staggerTimer < 2.5f)
            {
                transform.position = wallPos + Random.insideUnitSphere * 0.05f;
                staggerTimer += Time.deltaTime;
                yield return null;
            }
            transform.position = wallPos;
            isStaggered = false;
        }
    }

    public void TakeDamage(int damage)
    {
        if (!isStaggered) return; // Only take damage during stagger window!

        currentHealth -= damage;
        if (healthBarUI != null) healthBarUI.value = currentHealth;

        Debug.Log($"Boss hit! Current Health: {currentHealth}");

        if (currentHealth <= 0)
        {
            Debug.Log("Checkmate. Boss Defeated.");
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