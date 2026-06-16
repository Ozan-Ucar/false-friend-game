using UnityEngine;

public class StarPickup : MonoBehaviour
{
    [Header("Schweben")]
    public float floatAmount = 0.15f;
    public float floatSpeed = 2f;

    [Header("Pulsieren")]
    public float pulseMin = 0.9f;
    public float pulseMax = 1.1f;
    public float pulseSpeed = 3f;

    [Header("Einsammel-Effekt")]
    public float shrinkSpeed = 5f;

    private Vector3 startPos;
    private Vector3 originalScale;
    private bool isCollected = false;
    private float collectTimer = 0f;

    void Start()
    {
        startPos = transform.position;
        originalScale = transform.localScale;
    }

    void Update()
    {
        if (isCollected)
        {
            collectTimer += Time.deltaTime * shrinkSpeed;
            float t = Mathf.Clamp01(collectTimer);

            // Schrumpfen + nach oben fliegen
            transform.localScale = originalScale * (1f - t);
            transform.position += Vector3.up * Time.deltaTime * 2f;

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
            return;
        }

        float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmount;
        transform.position = startPos + new Vector3(0f, yOffset, 0f);

        float pulse = Mathf.Lerp(pulseMin, pulseMax, (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
        transform.localScale = originalScale * pulse;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCollected) return;

        if (collision.CompareTag("Player"))
        {
            isCollected = true;

            if (StarManager.Instance != null)
            {
                StarManager.Instance.CollectStar();
            }
        }
    }
}
