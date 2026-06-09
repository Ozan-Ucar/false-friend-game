using UnityEngine;
using UnityEngine.InputSystem;

public class RunePickup : MonoBehaviour
{
    [Header("Visuals")]
    [Tooltip("Wie stark die Rune pulsiert")]
    public float pulseSpeed = 3f;
    public float pulseMin = 0.8f;
    public float pulseMax = 1.2f;
    [Tooltip("Farbe des Leuchtens")]
    public Color glowColor = new Color(1f, 0.85f, 0f, 1f); // Gold

    private SpriteRenderer sr;
    private Vector3 originalScale;
    private bool isCollected = false;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;

        if (sr != null) sr.color = glowColor;
    }

    void Update()
    {
        if (isCollected) return;

        // Pulsierender Leucht-Effekt
        float pulse = Mathf.Lerp(pulseMin, pulseMax, (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
        transform.localScale = originalScale * pulse;

        // Sanftes Farbpulsieren
        if (sr != null)
        {
            float alpha = Mathf.Lerp(0.7f, 1f, (Mathf.Sin(Time.time * pulseSpeed * 1.5f) + 1f) * 0.5f);
            sr.color = new Color(glowColor.r, glowColor.g, glowColor.b, alpha);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCollected) return;

        if (collision.CompareTag("Player"))
        {
            isCollected = true;

            // Boss besiegt!
            if (BossArenaManager.Instance != null)
            {
                BossArenaManager.Instance.OnRuneCollected();
            }

            // Rune verschwindet mit einem kurzen Effekt
            if (sr != null) sr.color = Color.white;
            transform.localScale = originalScale * 2f; // Kurzer Flash
            Destroy(gameObject, 0.3f);
        }
    }
}
