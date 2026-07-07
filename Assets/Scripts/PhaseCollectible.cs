using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class PhaseCollectible : MonoBehaviour
{
    [Header("Einstellungen")]
    public float floatSpeed = 2f;
    public float floatHeight = 0.5f;
    public float rotationSpeed = 50f;
    public AudioClip collectSound;
    
    private Vector3 startPos;
    private bool isCollected = false;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        if (isCollected) return;

        // Leichtes Schweben auf und ab
        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
        
        // Langsame Drehung (um die Y Achse für 2.5D Effekt)
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCollected) return;

        if (collision.CompareTag("Player"))
        {
            isCollected = true;
            
            // Dem Manager bescheid geben
            if (BossCannonFightManager.Instance != null)
            {
                BossCannonFightManager.Instance.OnCollectiblePickedUp();
            }

            // Feedback Sound
            if (collectSound != null && SceneSoundManager.Instance != null)
            {
                SceneSoundManager.Instance.PlaySFX(collectSound);
            }

            // Sammel-Animation starten
            StartCoroutine(CollectAnimation());
        }
    }

    private IEnumerator CollectAnimation()
    {
        float elapsed = 0f;
        float duration = 0.5f;
        Vector3 startScale = transform.localScale;
        Vector3 startP = transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Schrumpfen und nach oben fliegen
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            transform.position = Vector3.Lerp(startP, startP + Vector3.up * 2f, t);
            
            yield return null;
        }

        Destroy(gameObject);
    }
}
