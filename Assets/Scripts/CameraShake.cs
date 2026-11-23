using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    private static CameraShake instance;
    private Vector3 originalPosition;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(this);
    }

    public static void TriggerShake(float duration, float magnitude)
    {
        if (instance != null)
        {
            instance.StartCoroutine(instance.ShakeRoutine(duration, magnitude));
        }
    }

    IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        originalPosition = transform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = new Vector3(originalPosition.x + x, originalPosition.y + y, originalPosition.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPosition;
    }
}