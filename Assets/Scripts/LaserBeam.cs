using UnityEngine;
using System.Collections;

public class LaserBeam : MonoBehaviour
{
    [Header("Laser Setup")]
    public LineRenderer coreLine;
    public LineRenderer glowLine;
    public Transform firePoint;
    public LayerMask hitMask; 
    
    [Header("Visuals")]
    public Color laserColor = Color.red;
    [Tooltip("Maximale Breite der Laser-Aura (Glow)")]
    public float fireWidth = 1.2f;
    [Tooltip("Maximale Breite des Laser-Kerns")]
    public float coreWidth = 0.4f;
    
    [Header("Damage & Audio")]
    public int damage = 2;
    public AudioClip fireSound;

    private void Start()
    {
        if (coreLine != null) coreLine.enabled = false;
        if (glowLine != null) glowLine.enabled = false;
    }

    public void FireLaser(float warningDuration, float fireDuration)
    {
        StartCoroutine(LaserSequence(warningDuration, fireDuration));
    }

    private IEnumerator LaserSequence(float warningDur, float fireDur)
    {
        // Farben vorbereiten
        Color glowColor = new Color(laserColor.r, laserColor.g, laserColor.b, 0.6f);
        // Kern fast weiß, aber leicht in der Zielfarbe eingefärbt
        Color coreColor = Color.Lerp(Color.white, laserColor, 0.2f);
        Color warningColor = new Color(laserColor.r, laserColor.g, laserColor.b, 0.4f);

        // 1. Warnphase (Dünner, schnell blinkender Strich)
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.ShakeCustom(warningDur, 0.02f); // Ganz leichtes Wackeln beim Aufladen
        }

        if (glowLine != null)
        {
            glowLine.enabled = true;
            glowLine.startWidth = 0.05f;
            glowLine.endWidth = 0.05f;
            glowLine.startColor = warningColor;
            glowLine.endColor = warningColor;
        }
        if (coreLine != null) coreLine.enabled = false;

        float elapsed = 0;
        while (elapsed < warningDur)
        {
            UpdateLinePosition();
            
            // Blinken (an/aus) alle 0.1 Sekunden
            if (glowLine != null)
            {
                glowLine.enabled = Mathf.PingPong(elapsed * 15f, 1f) > 0.5f;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 2. Schussphase (Beide an, pulsierend)
        if (glowLine != null)
        {
            glowLine.enabled = true;
            glowLine.startColor = glowColor;
            glowLine.endColor = glowColor;
        }
        
        if (coreLine != null)
        {
            coreLine.enabled = true;
            coreLine.startColor = coreColor;
            coreLine.endColor = coreColor;
        }

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.ShakeCustom(fireDur, 0.15f); // Stärkerer Shake beim Haupt-Laserstrahl
        }

        if (fireSound != null && SceneSoundManager.Instance != null)
        {
            SceneSoundManager.Instance.PlaySFX(fireSound);
        }

        elapsed = 0;
        while (elapsed < fireDur)
        {
            UpdateLinePosition();
            DealDamage();
            
            // Wackeln/Pulsieren über Sinus-Welle (sehr schnell)
            float wobble = Mathf.Sin(elapsed * 50f) * 0.2f + 0.8f; // Werte zwischen 0.6 und 1.0
            
            if (glowLine != null)
            {
                glowLine.startWidth = fireWidth * wobble;
                glowLine.endWidth = fireWidth * wobble;
            }
            if (coreLine != null)
            {
                coreLine.startWidth = coreWidth * wobble;
                coreLine.endWidth = coreWidth * wobble;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 3. Ende
        if (glowLine != null) glowLine.enabled = false;
        if (coreLine != null) coreLine.enabled = false;
    }

    private void UpdateLinePosition()
    {
        Transform origin = firePoint != null ? firePoint : transform;
        
        Vector3 startPos = origin.position;
        Vector3 endPos = startPos + Vector3.down * 50f;
        
        RaycastHit2D hit = Physics2D.Raycast(startPos, Vector2.down, 50f, hitMask);
        if (hit.collider != null)
        {
            endPos = hit.point;
        }
        
        if (glowLine != null)
        {
            glowLine.SetPosition(0, startPos);
            glowLine.SetPosition(1, endPos);
        }
        if (coreLine != null)
        {
            coreLine.SetPosition(0, startPos);
            coreLine.SetPosition(1, endPos);
        }
    }

    private void DealDamage()
    {
        Transform origin = firePoint != null ? firePoint : transform;
        
        // Checkt alle Layer, sucht dann nach dem Tag "Player"
        RaycastHit2D[] hits = Physics2D.BoxCastAll(origin.position, new Vector2(fireWidth, 0.1f), 0f, Vector2.down, 50f);
        
        foreach (var hit in hits)
        {
            if (hit.collider != null && hit.collider.CompareTag("Player"))
            {
                HealthSystem health = hit.collider.GetComponent<HealthSystem>();
                if (health != null)
                {
                    health.TakeDamage(damage); 
                }
            }
        }
    }
}
