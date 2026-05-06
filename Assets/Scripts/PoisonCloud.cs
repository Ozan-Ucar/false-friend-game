using UnityEngine;
using System.Collections;

public class PoisonCloud : MonoBehaviour
{
    [Header("Sequenz Zeiten")]
    public float lifetime = 5f;
    public float fadeOutTime = 1.0f;
    public bool destroyAfterLifetime = true;

    [Header("Referenzen")]
    public SpriteRenderer spriteRenderer;
    public ParticleSystem particles;
    private CircleCollider2D cloudCollider;

    private float damageTimer = 0f;

    private void Start()
    {
        cloudCollider = GetComponent<CircleCollider2D>();
        if (cloudCollider != null) cloudCollider.radius = 0f; 

        StartCoroutine(Sequence_CloudLifeCycle());
    }

    private IEnumerator Sequence_CloudLifeCycle()
    {
        // 0. AUFBAU
        float growthTime = 0.5f;
        float targetRadius = 1.5f; 
        float elapsedGrowth = 0f;
        while (elapsedGrowth < growthTime)
        {
            elapsedGrowth += Time.deltaTime;
            if (cloudCollider != null) cloudCollider.radius = Mathf.Lerp(0, targetRadius, elapsedGrowth / growthTime);
            yield return null;
        }

        // 1. WARTEN
        yield return new WaitForSeconds(lifetime);

        // 2. LÖSCHEN (Falls aktiviert)
        if (destroyAfterLifetime)
        {
            if (particles != null)
            {
                var emission = particles.emission;
                emission.enabled = false; 
            }

            if (spriteRenderer != null)
            {
                float elapsed = 0;
                Color startColor = spriteRenderer.color;
                while (elapsed < fadeOutTime)
                {
                    elapsed += Time.deltaTime;
                    float alpha = Mathf.Lerp(startColor.a, 0, elapsed / fadeOutTime);
                    spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(fadeOutTime);
            }

            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            HealthSystem health = other.GetComponent<HealthSystem>();
            if (health != null) health.TakeDamage(1);
            damageTimer = 0f;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMovement mov = other.GetComponent<PlayerMovement>();
            if (mov != null) mov.SetInPoison(true);

            damageTimer += Time.deltaTime;
            if (damageTimer >= 0.5f)
            {
                damageTimer = 0f;
                HealthSystem health = other.GetComponent<HealthSystem>();
                if (health != null) health.TakeDamage(1);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            damageTimer = 0f;
            PlayerMovement mov = other.GetComponent<PlayerMovement>();
            if (mov != null) mov.SetInPoison(false);
        }
    }
}
