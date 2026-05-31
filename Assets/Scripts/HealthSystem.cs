using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class HealthSystem : MonoBehaviour
{
    public enum BlinkMode { Transparent, ColorFlash }

    [Header("UI Referenzen (Sprites)")]
    public Image[] heartImages; // Deine 3 Herz-Bilder
    
    public Sprite fullHeart;    // 100%
    public Sprite threeQuarter; // 75%
    public Sprite halfHeart;    // 50%
    public Sprite quarterHeart; // 25%
    public Sprite emptyHeart;   // 0%

    [Header("Invincibility (I-Frames)")]
    public float invincibilityDuration = 1.5f;
    [Tooltip("Wie schnell das Blinken pulsiert (höher = schneller)")]
    public float blinkSpeed = 30f;
    [Tooltip("Soll das Spiel bei einem Treffer kurz einfrieren? (Sehr cooler Juice-Effekt)")]
    public bool useHitStop = true;

    [Header("Blink Style")]
    [Tooltip("Transparent = Player wird durchsichtig. ColorFlash = Player leuchtet in einer Farbe auf.")]
    public BlinkMode blinkMode = BlinkMode.ColorFlash;
    [Tooltip("In welcher Farbe der Player leuchten soll (wenn ColorFlash ausgewählt ist)")]
    public Color flashColor = Color.white;

    [Header("Debug / Testing")]
    [Tooltip("Wenn aktiv, verlierst du kein Leben. Alle Effekte (Hit-Stop, I-Frames, Kamera-Wackeln) passieren aber ganz normal!")]
    public bool godMode = false;

    [Header("Zustand")]
    private int currentHealth;
    private int maxHealth;
    private const int healthPerHeart = 4; // 4 Stufen pro Herz
    
    private bool isInvincible = false;
    private bool isDead = false;

    void Start()
    {
        maxHealth = heartImages.Length * healthPerHeart; // z.B. 3 * 4 = 12
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    void Update()
    {
        // Test: O Taste zieht jetzt nur 1 Punkt ab (eine Stufe)
        if (Keyboard.current != null && Keyboard.current.oKey.wasPressedThisFrame)
        {
            TakeDamage(1);
        }
    }

    public void TakeDamage(int amount)
    {
        // Wenn der Spieler durch I-Frames geschützt ist, blocken wir den Schaden!
        if (isInvincible) return;

        // Wenn God Mode aus ist, ziehen wir ganz normal Leben ab
        if (!godMode)
        {
            currentHealth -= amount;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            UpdateHealthUI();
        }

        // Effekte (Kamera-Wackeln) passieren IMMER, auch im God Mode
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.ShakeHit();
        }

        // Wenn wir noch leben (oder God Mode an ist), starten wir die I-Frames
        if (currentHealth > 0 || godMode)
        {
            StartCoroutine(InvincibilitySequence());
        }
        else
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // 1. Spielerbewegung und Physik einfrieren/ausschalten
        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm != null)
        {
            pm.enabled = false;
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f; // Verhindert, dass der Spieler weiter fällt
        }

        // 2. Todes-Animation abspielen
        Animator anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.Play("DeathPlayerAnim");
        }

        // 3. Coole Kamera-Zoom Animation auf den Player starten
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.DoDeathZoom(this.transform);
        }
        
        // Hier könnte man später noch einen UI Screen einblenden ("Game Over")
        // oder die Scene nach 2 Sekunden neu laden.
    }

    private System.Collections.IEnumerator InvincibilitySequence()
    {
        isInvincible = true;

        // 1. Coolness: Hit Stop (Time Freeze)
        if (useHitStop)
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(0.06f);
            Time.timeScale = 1f;
        }

        // Wir sammeln alle Sprites dynamisch (falls du während des Spiels Items aufhebst etc.)
        SpriteRenderer[] currentSprites = GetComponentsInChildren<SpriteRenderer>();
        Material[] originalMats = new Material[currentSprites.Length];
        for (int i = 0; i < currentSprites.Length; i++)
        {
            // sharedMaterial verhindert, dass Unity versehentlich Kopien der Materialien anlegt
            originalMats[i] = currentSprites[i].sharedMaterial;
        }

        Material tempFlashMat = null;

        // Wenn ColorFlash aktiv ist, laden wir unseren brandneuen Shader
        if (blinkMode == BlinkMode.ColorFlash)
        {
            Shader flashShader = Shader.Find("Custom/SpriteHitFlash");
            if (flashShader != null)
            {
                tempFlashMat = new Material(flashShader);
                tempFlashMat.SetColor("_FlashColor", flashColor);
                
                foreach (SpriteRenderer sr in currentSprites)
                {
                    if (sr != null) sr.sharedMaterial = tempFlashMat;
                }
            }
            else
            {
                Debug.LogWarning("HealthSystem: Custom/SpriteHitFlash Shader wurde nicht gefunden! Fällt auf Transparent zurück.");
            }
        }

        // 2. Visuelles Blinken (I-Frames)
        float elapsed = 0f;
        while (elapsed < invincibilityDuration)
        {
            elapsed += Time.deltaTime;
            
            // Weiche Cosinus-Welle zwischen 0 und 1
            float wave = (Mathf.Cos(elapsed * blinkSpeed) + 1f) / 2f;

            if (blinkMode == BlinkMode.ColorFlash && tempFlashMat != null)
            {
                // Pulsieren der Leuchtfarbe
                tempFlashMat.SetFloat("_FlashAmount", wave);
            }
            else
            {
                // Pulsieren der Transparenz (Alpha zwischen 0.2 und 1.0)
                float alpha = Mathf.Lerp(0.2f, 1f, wave);
                foreach (SpriteRenderer sr in currentSprites)
                {
                    if (sr != null)
                    {
                        Color c = sr.color;
                        c.a = alpha;
                        sr.color = c;
                    }
                }
            }

            yield return null;
        }

        // 3. Am Ende wieder alles zurücksetzen
        for (int i = 0; i < currentSprites.Length; i++)
        {
            if (currentSprites[i] != null)
            {
                if (blinkMode == BlinkMode.ColorFlash && tempFlashMat != null)
                {
                    // Altes Material wiederherstellen
                    currentSprites[i].sharedMaterial = originalMats[i];
                }
                else
                {
                    // Alpha wieder auf 1
                    Color c = currentSprites[i].color;
                    c.a = 1f;
                    currentSprites[i].color = c;
                }
            }
        }

        if (tempFlashMat != null)
        {
            Destroy(tempFlashMat); // Material sauber aus dem Speicher entfernen
        }

        isInvincible = false;
    }

    void UpdateHealthUI()
    {
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] == null) continue;

            // Verhindert das "Zusammenquetschen"
            heartImages[i].preserveAspect = true;

            int heartHealth = currentHealth - (i * healthPerHeart);
            heartHealth = Mathf.Clamp(heartHealth, 0, healthPerHeart);

            switch (heartHealth)
            {
                case 4: heartImages[i].sprite = fullHeart; break;
                case 3: heartImages[i].sprite = threeQuarter; break;
                case 2: heartImages[i].sprite = halfHeart; break;
                case 1: heartImages[i].sprite = quarterHeart; break;
                case 0: heartImages[i].sprite = emptyHeart; break;
            }
        }
    }
}
