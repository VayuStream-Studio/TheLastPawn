using UnityEngine;

public class RookTendrils : MonoBehaviour
{
    [Header("X-Arch Venom Tendrils")]
    public int tendrilCount = 4;
    public int segmentsPerTendril = 16;
    public float baseWidth = 0.85f;
    public float tipWidth = 0.15f;
    public float archSpread = 2.2f;    // How wide they flare outward in an X shape
    public Vector3 backMountOffset = new Vector3(0f, 1.4f, -0.3f); // Upper back shoulders
    public Color symbioteColor = new Color(0.03f, 0.03f, 0.04f, 1f);

    [Header("Idle Ambush & Stagger Thrash")]
    public Transform playerTarget;
    public float idleLashInterval = 3.0f;
    public float lashReach = 5.5f;
    public float hitRadius = 1.1f;
    public int lashDamage = 1;
    public float staggerLashInterval = 0.45f;

    private LineRenderer[] tendrils;
    private float[] randomOffsets;
    private float[] lashProgress;
    private bool[] hasDealtDamageThisLash;
    private Vector3[] targetPositions;

    private float staggerTimer = 0f;
    private float nextLashTime = 0f;

    void Start()
    {
        if (playerTarget == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTarget = p.transform;
        }

        tendrils = new LineRenderer[tendrilCount];
        randomOffsets = new float[tendrilCount];
        lashProgress = new float[tendrilCount];
        hasDealtDamageThisLash = new bool[tendrilCount];
        targetPositions = new Vector3[tendrilCount];

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.color = symbioteColor;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.65f);

        for (int i = 0; i < tendrilCount; i++)
        {
            GameObject tObj = new GameObject($"XTendril_{i}");
            tObj.transform.SetParent(transform);
            tObj.transform.localPosition = backMountOffset;

            LineRenderer lr = tObj.AddComponent<LineRenderer>();
            lr.material = mat;
            lr.positionCount = segmentsPerTendril;
            lr.useWorldSpace = false;
            lr.numCapVertices = 5;
            lr.numCornerVertices = 5;

            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0.0f, baseWidth);
            curve.AddKey(0.4f, baseWidth * 0.75f);
            curve.AddKey(1.0f, tipWidth);
            lr.widthCurve = curve;

            tendrils[i] = lr;
            randomOffsets[i] = Random.Range(0f, 50f);
        }

        nextLashTime = Time.time + 1.5f;
    }

    public void TriggerStaggerThrash(float duration = 2.5f)
    {
        staggerTimer = duration;
        nextLashTime = Time.time + 0.1f;
    }

    void Update()
    {
        bool isStaggered = staggerTimer > 0f;
        if (isStaggered) staggerTimer -= Time.deltaTime;

        if (Time.time >= nextLashTime && playerTarget != null)
        {
            nextLashTime = Time.time + (isStaggered ? staggerLashInterval : idleLashInterval);
            TriggerRandomLash(isStaggered);
        }

        float thrashSpeed = isStaggered ? 16f : 2.5f;
        float thrashAmp = isStaggered ? 0.45f : 0.08f;

        for (int i = 0; i < tendrilCount; i++)
        {
            if (lashProgress[i] > 0f)
            {
                float strikeSpeed = isStaggered ? 5.5f : 3.8f;
                lashProgress[i] += Time.deltaTime * strikeSpeed;
                if (lashProgress[i] >= 1f)
                {
                    lashProgress[i] = 0f;
                    hasDealtDamageThisLash[i] = false;
                }
            }

            // X-Shape Spread: 0=Top-Left, 1=Top-Right, 2=Bottom-Left, Bottom-Right
            float xDir = (i == 0 || i == 2) ? -1f : 1f;
            float yDir = (i < 2) ? 1f : -0.4f; // Top pair arch upward, bottom pair sweep downward/outward

            float strikeWeight = Mathf.Sin(lashProgress[i] * Mathf.PI);

            for (int s = 0; s < segmentsPerTendril; s++)
            {
                float t = (float)s / (segmentsPerTendril - 1);

                // X-Arch shape: sweeping diagonally outward and curling back like the reference image
                float spreadCurve = Mathf.Sin(t * Mathf.PI * 0.75f);
                float x = xDir * spreadCurve * archSpread;
                float y = (yDir * t * 1.8f) + (Mathf.Sin(t * Mathf.PI) * 1.2f); // Arches up/down then dips
                float z = -Mathf.Lerp(0.1f, 1.8f, t); // Sweeps backward behind the Rook

                float jitter = Mathf.Sin(Time.time * thrashSpeed + randomOffsets[i] - t * 5f) * thrashAmp * t;
                Vector3 idleArchPos = new Vector3(x + jitter, y + jitter, z);

                // Strike lunge toward the player
                Vector3 strikePos = Vector3.Lerp(Vector3.zero, targetPositions[i], t) + (Vector3.up * Mathf.Sin(t * Mathf.PI) * 1.0f);

                Vector3 finalPos = Vector3.Lerp(idleArchPos, strikePos, strikeWeight);
                tendrils[i].SetPosition(s, finalPos);

                if (s == segmentsPerTendril - 1 && strikeWeight > 0.7f && !hasDealtDamageThisLash[i] && playerTarget != null)
                {
                    Vector3 worldTip = tendrils[i].transform.TransformPoint(finalPos);
                    if (Vector3.Distance(worldTip, playerTarget.position) <= hitRadius)
                    {
                        hasDealtDamageThisLash[i] = true;
                        playerTarget.SendMessage("TakeDamage", lashDamage, SendMessageOptions.DontRequireReceiver);
                        CameraShake.TriggerShake(0.22f, 0.16f);
                    }
                }
            }
        }
    }

    void TriggerRandomLash(bool isStaggered)
    {
        int idx = Random.Range(0, tendrilCount);
        if (lashProgress[idx] > 0f) return;

        if (Vector3.Distance(transform.position, playerTarget.position) > lashReach + 2f) return;

        lashProgress[idx] = 0.01f;
        hasDealtDamageThisLash[idx] = false;

        float spread = isStaggered ? 0.9f : 0.4f;
        Vector3 worldTarget = playerTarget.position + Vector3.up * 1.0f + Random.insideUnitSphere * spread;
        Vector3 localTarget = tendrils[idx].transform.InverseTransformPoint(worldTarget);
        targetPositions[idx] = Vector3.ClampMagnitude(localTarget, lashReach);
    }
}