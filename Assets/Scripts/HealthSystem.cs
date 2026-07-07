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
    [Tooltip("Zeit (in Sekunden), die nach dem Tod gewartet wird, bevor die Szene neu lädt.")]
    public float deathRestartDelay = 3.0f;
    [Header("Effekte")]
    [Tooltip("Soll beim Tod die Zoom-Animation gespielt werden?")]
    public bool useDeathZoom = true;
    [Tooltip("Wie schnell das Blinken pulsiert (höher = schneller)")]
    public float blinkSpeed = 30f;
    [Tooltip("Soll das Spiel bei einem Treffer kurz einfrieren? (Sehr cooler Juice-Effekt)")]
    public bool useHitStop = true;

    [Header("Blink Style")]
    [Tooltip("In welcher Farbe der Player kurz aufleuchtet, wenn er getroffen wird")]
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

    public void InstantKill(bool skipZoom = false, float customDelay = -1f)
    {
        if (godMode || isDead) return;

        currentHealth = 0;
        UpdateHealthUI();
        
        if (CameraShake.Instance != null && !skipZoom)
        {
            CameraShake.Instance.ShakeHit();
        }

        Die(skipZoom, customDelay);
    }

    private void Die(bool skipZoom = false, float customDelay = -1f)
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
        if (useDeathZoom && !skipZoom && CameraShake.Instance != null)
        {
            CameraShake.Instance.DoDeathZoom(this.transform);
        }
        
        // 4. Neustart nach der eingestellten (oder modifizierten) Zeit vorbereiten
        float finalDelay = customDelay >= 0f ? customDelay : deathRestartDelay;
        StartCoroutine(DeathRestartRoutine(finalDelay));
    }

    private System.Collections.IEnumerator DeathRestartRoutine(float delay)
    {
        // Wir warten die eingestellte Zeit. WICHTIG: Realtime nutzen, da der Kamera-Zoom die Zeit verlangsamt!
        yield return new WaitForSecondsRealtime(delay);

        // TransitionShowcase unsichtbar als reinen "Effekt-Manager" spawnen
        GameObject transitionObj = new GameObject("DeathTransition");
        TransitionShowcase ts = transitionObj.AddComponent<TransitionShowcase>();
        ts.isShowcaseMode = false; // Keine Texte/Labels anzeigen!
        
        // Kurz warten, damit Unity das Skript initialisiert (Start() baut das Canvas auf)
        yield return null; 
        
        // Zufälligen Effekt starten und Szene neuladen
        ts.PlayRandomTransitionAndReloadScene();
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

        // Wir sammeln alle Sprites dynamisch
        SpriteRenderer[] currentSprites = GetComponentsInChildren<SpriteRenderer>();
        Material[] originalMats = new Material[currentSprites.Length];
        for (int i = 0; i < currentSprites.Length; i++)
        {
            originalMats[i] = currentSprites[i].sharedMaterial;
        }

        // 2. Initialer Hit-Flash (kurz hart weiß/farbig aufleuchten)
        Shader flashShader = Shader.Find("Custom/SpriteHitFlash");
        Material tempFlashMat = null;
        if (flashShader != null)
        {
            tempFlashMat = new Material(flashShader);
            tempFlashMat.SetColor("_FlashColor", flashColor);
            tempFlashMat.SetFloat("_FlashAmount", 1f); // 100% Leuchten
            
            foreach (SpriteRenderer sr in currentSprites)
            {
                if (sr != null) sr.sharedMaterial = tempFlashMat;
            }
        }

        // Warte für die Dauer des harten Aufleuchtens (0.1 Sekunden)
        yield return new WaitForSeconds(0.1f);

        // Materialien sofort wiederherstellen
        for (int i = 0; i < currentSprites.Length; i++)
        {
            if (currentSprites[i] != null)
            {
                currentSprites[i].sharedMaterial = originalMats[i];
            }
        }
        if (tempFlashMat != null) Destroy(tempFlashMat);

        // 3. I-Frames Blinken (klassisch durchsichtig flackern) für den Rest der Zeit
        float elapsed = 0.1f; // Wir starten bei 0.1, weil der Flash schon vorbei ist
        while (elapsed < invincibilityDuration)
        {
            elapsed += Time.deltaTime;
            
            // Weiche Cosinus-Welle zwischen 0 und 1
            float wave = (Mathf.Cos(elapsed * blinkSpeed) + 1f) / 2f;
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

            yield return null;
        }

        // 4. Am Ende Alpha wieder auf 100% setzen
        foreach (SpriteRenderer sr in currentSprites)
        {
            if (sr != null)
            {
                Color c = sr.color;
                c.a = 1f;
                sr.color = c;
            }
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
