using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class LightningRodTrap : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Zeit in Sekunden, die das Objekt stillstehen muss, bis der Blitz einschlägt.")]
    public float timeToStrike = 3.0f;
    [Tooltip("Wie viel Schaden der Blitz verursacht.")]
    public int damage = 1;
    [Tooltip("Radius der Explosion/des Flächenschadens um den Blitzleiter herum.")]
    public float damageRadius = 2.0f;
    [Tooltip("Ab welcher Geschwindigkeit der Timer zurückgesetzt wird.")]
    public float movementThreshold = 0.1f;

    [Header("Visuals")]
    [Tooltip("Farbe des Blitzes.")]
    public Color lightningColor = new Color(0.4f, 0.8f, 1f, 1f); // Cyan-Blau
    [Tooltip("Breite des Blitzes.")]
    public float lightningWidth = 0.3f;

    [Header("Orb Settings")]
    [Tooltip("Wenn aktiv, fällt die Falle nicht runter, sondern schwebt wie ein Orb in der Luft.")]
    public bool isFloatingOrb = true;
    [Tooltip("Reibung: Bestimmt, wie schnell der Orb nach dem Werfen in der Luft abbremst (0.95 = bremst schnell, 0.99 = bremst langsam).")]
    public float orbFriction = 0.98f;

    [Header("Slingshot Settings")]
    [Tooltip("Maximale Länge, die man ziehen kann.")]
    public float maxPullDistance = 3f;
    [Tooltip("Multiplikator für die Wurfstärke.")]
    public float throwPowerMultiplier = 15f;
    public Color aimColorWeak = Color.green;
    public Color aimColorStrong = Color.red;

    private Rigidbody2D rb;
    private float currentTimer;
    private LineRenderer lr;
    private Material chargeMaterial;
    private Collider2D myCollider;
    private bool isDragging = false;
    private LineRenderer aimLine;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();
        currentTimer = timeToStrike;

        // Shader zuweisen (sucht auch in Child-Objekten!)
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            Shader chargeShader = Shader.Find("Custom/LightningRodCharge");
            if (chargeShader != null)
            {
                chargeMaterial = new Material(chargeShader);
                chargeMaterial.SetColor("_ChargeColor", lightningColor);
                sr.sharedMaterial = chargeMaterial;
            }
        }

        // LineRenderer für den Blitz dynamisch erstellen, damit wir keine Assets brauchen!
        lr = gameObject.AddComponent<LineRenderer>();
        lr.positionCount = 0; // Unsichtbar am Anfang
        lr.startWidth = lightningWidth;
        lr.endWidth = lightningWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lightningColor;
        lr.endColor = Color.white;
        lr.sortingOrder = 50; // Weit vorne zeichnen

        // LineRenderer für die Ziel-Linie (Slingshot)
        GameObject aimObj = new GameObject("AimLine");
        aimObj.transform.SetParent(this.transform);
        aimLine = aimObj.AddComponent<LineRenderer>();
        aimLine.positionCount = 2;
        aimLine.startWidth = 0.1f;
        aimLine.endWidth = 0.02f; // Wird nach vorne hin spitzer (Pfeil-Gefühl)
        aimLine.material = new Material(Shader.Find("Sprites/Default"));
        aimLine.enabled = false;
        aimLine.sortingOrder = 49;

        if (isFloatingOrb && rb != null)
        {
            rb.gravityScale = 0f; // Schwerelosigkeit aktivieren!
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        if (!isDragging && isFloatingOrb)
        {
            // Orb-Reibung anwenden, damit er nach dem Werfen in der Luft stehen bleibt
            rb.linearVelocity = rb.linearVelocity * orbFriction;
        }
    }

    void Update()
    {
        // 1. Drag & Drop Logik mit der Maus
        if (Mouse.current != null)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (myCollider != null && myCollider.OverlapPoint(mousePos))
                {
                    isDragging = true;
                    // Ball in der Luft einfrieren beim Zielen
                    if (rb != null) rb.linearVelocity = Vector2.zero;
                    if (aimLine != null) aimLine.enabled = true;
                }
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                if (isDragging)
                {
                    isDragging = false;
                    if (aimLine != null) aimLine.enabled = false;
                    
                    if (rb != null)
                    {
                        // Zieh-Vektor berechnen (von Maus ZUM Ball -> Richtung in die er fliegt)
                        Vector2 pullVector = (Vector2)transform.position - mousePos;
                        
                        // Maximale Stärke kappen
                        if (pullVector.magnitude > maxPullDistance)
                        {
                            pullVector = pullVector.normalized * maxPullDistance;
                        }
                        
                        // Kraft anwenden!
                        rb.linearVelocity = pullVector * throwPowerMultiplier;
                    }
                }
            }

            if (isDragging)
            {
                currentTimer = timeToStrike; // Timer beim Ziehen zurücksetzen
                
                Vector2 pullVector = (Vector2)transform.position - mousePos;
                if (pullVector.magnitude > maxPullDistance)
                {
                    pullVector = pullVector.normalized * maxPullDistance;
                }
                
                // Ziellinie zeichnen (vom Ball wegzeigend)
                if (aimLine != null)
                {
                    aimLine.SetPosition(0, transform.position);
                    aimLine.SetPosition(1, (Vector2)transform.position + pullVector);
                    
                    // Farbe je nach Spannung (Grün -> Rot)
                    float strength = pullVector.magnitude / maxPullDistance;
                    Color lerpedColor = Color.Lerp(aimColorWeak, aimColorStrong, strength);
                    aimLine.startColor = lerpedColor;
                    aimLine.endColor = lerpedColor;
                }
            }
        }

        // 2. Timer & Physik-Logik (nur wenn nicht gezogen wird)
        if (!isDragging && rb != null)
        {
            // Lädt immer auf, solange es nicht mit der Maus festgehalten wird!
            currentTimer -= Time.deltaTime;

            if (currentTimer <= 0f)
            {
                Strike();
                currentTimer = timeToStrike; // Wieder von vorne beginnen
            }
        }

        // 3. Shader updaten (Visualisierung des Aufladens)
        if (chargeMaterial != null)
        {
            float progress = 1f - (currentTimer / timeToStrike);
            // Begrenzen, damit es nicht über 1 geht
            progress = Mathf.Clamp01(progress);
            chargeMaterial.SetFloat("_ChargeAmount", progress);
        }
    }

    private void Strike()
    {
        // 1. Visueller Effekt
        StartCoroutine(LightningVisualRoutine());
        
        // 2. Schaden austeilen (Flächenschaden / AoE)
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, damageRadius);
        foreach (Collider2D hit in hitColliders)
        {
            if (hit.CompareTag("Player"))
            {
                HealthSystem health = hit.GetComponent<HealthSystem>();
                if (health != null)
                {
                    // Wenn der Blitz tödlich ist, wollen wir den fetten Shake sehen und nicht
                    // sofort vom langsamen DeathZoom unterbrochen werden!
                    health.useDeathZoom = false;
                    if (health.deathRestartDelay > 1.5f) health.deathRestartDelay = 1.5f;
                    
                    health.TakeDamage(damage);
                }
            }
        }

        // 3. Kamera-Shake ganz am Ende aufrufen!
        // Warum? Weil TakeDamage() intern auch einen kleinen Kamera-Shake oder Death-Zoom aufruft.
        // Wenn wir diesen fetten Shake HIER GANZ AM ENDE aufrufen, hat er immer das letzte Wort!
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.ShakeCustom(0.4f, 0.3f);
        }
    }

    private IEnumerator LightningVisualRoutine()
    {
        // Startpunkt weit oben im Himmel, Endpunkt am Blitzleiter
        Vector3 startPos = transform.position + new Vector3(Random.Range(-1f, 1f), 15f, 0f);
        Vector3 endPos = transform.position + new Vector3(0f, 0.5f, 0f); // Spitze des Blitzleiters (ungefähr)

        // Wir generieren einen zackigen Blitz mit mehreren Punkten
        int segments = 10;
        lr.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / (segments - 1);
            Vector3 pointPos = Vector3.Lerp(startPos, endPos, t);
            
            // Zufälliges Rauschen für die Zacken hinzufügen (außer am Start- und Endpunkt)
            if (i > 0 && i < segments - 1)
            {
                pointPos.x += Random.Range(-0.8f, 0.8f);
            }

            lr.SetPosition(i, pointPos);
        }

        // Blitz kurz anzeigen (Flashen)
        lr.enabled = true;
        
        // 3 Frames Flackern lassen für einen coolen Effekt
        for(int f = 0; f < 3; f++)
        {
            lr.startColor = Color.white;
            yield return new WaitForSeconds(0.05f);
            lr.startColor = lightningColor;
            
            // Position leicht updaten für Animation
            for (int i = 1; i < segments - 1; i++)
            {
                Vector3 p = lr.GetPosition(i);
                p.x += Random.Range(-0.2f, 0.2f);
                lr.SetPosition(i, p);
            }
            yield return new WaitForSeconds(0.05f);
        }

        lr.enabled = false;
        lr.positionCount = 0;
    }

    // Damit man im Editor (Scene Ansicht) den Radius sieht, in dem der Blitz Schaden macht
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawSphere(transform.position, damageRadius);
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}
