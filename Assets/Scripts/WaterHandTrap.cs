using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaterHandTrap : MonoBehaviour
{
    [Header("Einstellungen")]
    [Tooltip("Zeit, in der das Ausrufezeichen blinkt, bevor die Hand von unten kommt.")]
    public float warningDelay = 1.5f;
    [Tooltip("Wie lange bleibt die Hand oben, nachdem sie hochgeschossen ist?")]
    public float attackDuration = 1.0f;
    [Tooltip("Schaden, den der Player beim Greifen bekommt.")]
    public int damage = 1;

    [Header("Referenzen")]
    [Tooltip("Das Ausrufezeichen-Sprite (sollte ein Child-Objekt der Falle sein).")]
    public SpriteRenderer warningSprite; 
    
    [Tooltip("Das Objekt, das das PNG der Hand und den Grab-Collider enthält.")]
    public Transform handVisual;
    [Tooltip("Der Trigger-Collider (BoxCollider2D), der bestimmt, wo die Hand greift. Er sollte auf dem handVisual liegen.")]
    public Collider2D grabCollider; 

    [Header("Positionen (Werte auf der Y-Achse)")]
    [Tooltip("Der Y-Wert, an dem die Hand startet und wohin sie sich zurückzieht (z.B. -15 für tief unten)")]
    public float startY = -15.0f;
    
    [Tooltip("Der Y-Wert, bis zu dem die Hand nach oben schießt (z.B. 2 für über Wasser)")]
    public float endY = 2.0f;
    
    [Tooltip("Zeit (in Sekunden): Wie lange braucht die Hand, um hochzuschießen? (Mach das klein, z.B. 0.15 für schnell)")]
    public float riseDuration = 0.15f;
    
    [Tooltip("Die Animationskurve für den Angriff! Klick drauf und wähle eine Kurve aus (z.B. eine, die am Anfang steil hochgeht).")]
    public AnimationCurve riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Tooltip("Schreck-Pause (in Sekunden): Wartet NACHDEM das Warnschild weg ist noch ganz kurz im Wasser, bevor sie losschießt.")]
    public float delayBeforeRise = 0.1f;
    
    [Tooltip("Zeit (in Sekunden): Wie lange braucht die Hand, um sich wieder komplett nach unten zu ziehen?")]
    public float pullDownDuration = 1.0f;
    
    [Header("Hitbox-Verzögerung")]
    [Tooltip("Verzögert die Aktivierung der Hitbox (in Sekunden), NACHDEM die Hand anfängt hochzuschießen. Verhindert, dass man stirbt, obwohl die Hand noch unten ist.")]
    public float colliderEnableDelay = 0.05f;
    
    [Header("Zieh-Effekt (Juice)")]
    [Tooltip("Soll der Player beim Treffer physisch mit der Hand nach unten gezogen werden?")]
    public bool pullPlayerDown = true;
    
    private bool isGrabbing = false;
    private GameObject caughtPlayer = null;
    private HealthSystem caughtPlayerHealth = null;
    
    private Vector3 startLocalPos;
    private Vector3 endLocalPos;
    private Vector3 deepLocalPos;

    void Start()
    {
        if (grabCollider != null) grabCollider.enabled = false;
        
        if (handVisual != null)
        {
            // Behalte das X und Z der Hand bei, aber benutze die festen Y-Werte!
            float x = handVisual.localPosition.x;
            float z = handVisual.localPosition.z;
            
            startLocalPos = new Vector3(x, startY, z);
            endLocalPos = new Vector3(x, endY, z);
            
            // Sie zieht sich nach der Attacke exakt dorthin zurück, wo sie gestartet ist
            deepLocalPos = startLocalPos; 
            
            // Setze die Hand sofort an den Startpunkt und deaktiviere sie
            handVisual.localPosition = startLocalPos;
            handVisual.gameObject.SetActive(false);
        }
        
        // Falle startet automatisch, sobald sie vom Spawner gesetzt wurde
        StartCoroutine(TrapSequence());
    }

    private IEnumerator TrapSequence()
    {
        // ============================================
        // 1. PHASE: Warnung (Ausrufezeichen blinkt)
        // ============================================
        float elapsed = 0f;
        bool spriteVisible = true;
        float blinkRate = 0.2f;
        float nextBlink = blinkRate;

        while (elapsed < warningDelay)
        {
            elapsed += Time.deltaTime;
            
            // Blinken wird gegen Ende immer schneller, um Panik zu machen!
            if (elapsed > warningDelay * 0.7f) blinkRate = 0.08f;

            if (elapsed >= nextBlink)
            {
                spriteVisible = !spriteVisible;
                if (warningSprite != null) warningSprite.enabled = spriteVisible;
                nextBlink = elapsed + blinkRate;
            }

            yield return null;
        }

        if (warningSprite != null) warningSprite.enabled = false;

        // ============================================
        // 1.5 PHASE: Die Schreck-Pause!
        // ============================================
        if (delayBeforeRise > 0f)
        {
            yield return new WaitForSeconds(delayBeforeRise);
        }

        // ============================================
        // 2. PHASE: Hand schießt nach oben!
        // ============================================
        if (handVisual != null)
        {
            // Hand jetzt erst sichtbar machen!
            handVisual.gameObject.SetActive(true);
            
            // Aktiviert den Collider erst nach dem eingestellten Delay
            StartCoroutine(EnableColliderAfterDelay());

            // Die Hand bewegt sich anhand der coolen Animations-Kurve nach oben!
            float t = 0f;
            while (t < 1f)
            {
                if (riseDuration <= 0f) t = 1f; 
                else t += Time.deltaTime / riseDuration;
                
                // Wir lesen den Wert aus deiner Kurve aus.
                float curveValue = riseCurve.Evaluate(t);
                
                // LerpUnclamped erlaubt es der Kurve, ÜBER das Ziel hinauszuschießen (für Bounce-Effekte)!
                handVisual.localPosition = Vector3.LerpUnclamped(startLocalPos, endLocalPos, curveValue);
                yield return null;
            }
            // Anstatt hart auf die Endposition zu snappen, lesen wir aus, wo DEINE Kurve bei t=1 endet!
            // So ruckelt nichts, auch wenn deine Kurve nicht exakt auf der 1.0-Linie aufhört.
            handVisual.localPosition = Vector3.LerpUnclamped(startLocalPos, endLocalPos, riseCurve.Evaluate(1f));
        }

        // ============================================
        // 3. PHASE: Halten (Attacke / Greif-Zeitfenster)
        // ============================================
        yield return new WaitForSeconds(attackDuration);

        // Nach dieser Zeit grabben wir niemanden NEUES mehr
        isGrabbing = false;

        // ============================================
        // 4. PHASE: Hand zieht sich wieder runter
        // ============================================
        if (handVisual != null)
        {
            float t = 0f;
            // GANZ WICHTIG: Wir müssen von der Position starten, wo die Hand JETZT GERADE in der Luft hängt.
            // Falls deine Kurve nämlich über das Ziel hinausgeschossen ist, wäre sie sonst hart zurück auf den "theoretischen" Endpunkt gesnapped!
            Vector3 currentPosBeforePull = handVisual.localPosition;
            
            while (t < 1f)
            {
                if (pullDownDuration <= 0f) t = 1f;
                else t += Time.deltaTime / pullDownDuration;
                
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                
                Vector3 lastPos = handVisual.localPosition;
                // Wir ziehen sie jetzt GANZ TIEF nach unten, beginnend von exakt da, wo sie in der Luft hing
                Vector3 newPos = Vector3.Lerp(currentPosBeforePull, deepLocalPos, smoothT);
                
                // Wie viel hat sich die Hand in genau diesem Frame auf der Y-Achse bewegt?
                float deltaY = newPos.y - lastPos.y; 
                
                handVisual.localPosition = newPos;
                
                // Wenn wir den Player gefangen haben, bewegen wir ihn EXAKT um denselben Betrag mit!
                if (caughtPlayer != null)
                {
                    caughtPlayer.transform.position += new Vector3(0, deltaY, 0);
                }
                
                yield return null;
            }
            handVisual.localPosition = deepLocalPos;
        }

        // Hitbox aus
        if (grabCollider != null) grabCollider.enabled = false;

        // Falls wir den Player noch haben, lassen wir ihn jetzt ganz tief im Wasser fallen
        ReleasePlayer();

        // JETZT ERST töten wir den Spieler! 
        // Vorher hätte er seine Todesanimation in der Luft abgespielt und sich nicht mehr bewegen lassen.
        if (caughtPlayerHealth != null)
        {
            caughtPlayerHealth.godMode = false; // Unverwundbarkeit vom Greifen entfernen
            caughtPlayerHealth.useDeathZoom = false; // Kein Zoom (Kamera bleibt wo sie ist)
            caughtPlayerHealth.deathRestartDelay = 0.5f; // Schnell neuladen
            caughtPlayerHealth.TakeDamage(999);
        }

        // Lass die Falle noch kurz existieren, bevor sie gelöscht wird, damit der Spieler nicht 
        // plötzlich ruckelt, wenn die Physik wieder angeht
        yield return new WaitForSeconds(0.5f);
        
        // Falle räumt sich selbst auf
        Destroy(gameObject);
    }

    private IEnumerator EnableColliderAfterDelay()
    {
        if (colliderEnableDelay > 0f)
        {
            yield return new WaitForSeconds(colliderEnableDelay);
        }
        
        isGrabbing = true;
        if (grabCollider != null) grabCollider.enabled = true;
    }

    void Update()
    {
        // Sobald die Hand oben ist und wir noch keinen Spieler gegriffen haben
        if (isGrabbing && caughtPlayer == null && grabCollider != null)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.NoFilter();
            List<Collider2D> results = new List<Collider2D>();
            grabCollider.Overlap(filter, results);

            foreach (Collider2D col in results)
            {
                if (col.CompareTag("Player"))
                {
                    HealthSystem health = col.GetComponent<HealthSystem>();
                    
                    if (health != null)
                    {
                        caughtPlayerHealth = health;
                        
                        // Solange die Hand ihn hat, machen wir ihn unverwundbar für ALLES andere.
                        // So stirbt er nicht vorzeitig und spielt keine Todesanimation in der Luft ab!
                        health.godMode = true; 

                        // Heftiger Kamera-Shake passend zum Runterziehen!
                        if (CameraShake.Instance != null)
                        {
                            CameraShake.Instance.ShakeCustom(1f, 0.5f);
                        }
                        
                        // Wenn der coole Zieh-Effekt an ist, fixieren wir ihn für Phase 4
                        if (pullPlayerDown)
                        {
                            caughtPlayer = col.gameObject;
                            
                            // Kontrolle klauen!
                            PlayerMovement pm = caughtPlayer.GetComponent<PlayerMovement>();
                            Rigidbody2D rb = caughtPlayer.GetComponent<Rigidbody2D>();

                            if (pm != null) pm.enabled = false; 
                            if (rb != null)
                            {
                                rb.linearVelocity = Vector2.zero;
                                rb.isKinematic = true; // Hand hat die Kontrolle über die Gravitation
                            }
                        }
                    }
                    break;
                }
            }
        }
    }

    private void ReleasePlayer()
    {
        if (caughtPlayer != null)
        {
            PlayerMovement pm = caughtPlayer.GetComponent<PlayerMovement>();
            Rigidbody2D rb = caughtPlayer.GetComponent<Rigidbody2D>();

            // Kontrolle zurückgeben!
            if (pm != null) pm.enabled = true;
            if (rb != null) rb.isKinematic = false;
            
            caughtPlayer = null;
        }
    }
}
