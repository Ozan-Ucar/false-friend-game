using UnityEngine;

public class MouseCannon : MonoBehaviour
{
    public enum CannonSide { Right, Left }

    [Header("Einstellungen")]
    public CannonSide side = CannonSide.Right;
    public GameObject cannonBallPrefab;
    public Transform firePoint;
    public float fireRate = 2.5f;
    public AudioClip shootSound;
    
    [Header("Bewegung")]
    [Tooltip("Wie schnell die Kanone der Maus vertikal folgt")]
    public float moveSpeed = 15f;
    [Tooltip("Untere Grenze (Y)")]
    public float minY = -5f;
    [Tooltip("Obere Grenze (Y)")]
    public float maxY = 5f;

    [Header("Zonen-Steuerung")]
    [Tooltip("Wie nah muss die Maus auf der X-Achse sein, damit die Kanone reagiert?")]
    public float activationRangeX = 8f;
    [Tooltip("Wenn die Maus HÖHER als dieser Y-Wert ist, ignoriert die Kanone sie (z.B. Laser-Zone).")]
    public float ignoreAboveY = 3.5f;

    [Header("Highlight (Shader)")]
    public SpriteRenderer spriteRenderer;
    public Color activeHighlightColor = Color.yellow;
    public float activeOutlineWidth = 2f;
    [Tooltip("Wie schnell das Highlight pulsiert")]
    public float activePulseSpeed = 2f;
    [Tooltip("1 = Starkes Pulsieren, 0 = Kein Pulsieren")]
    [Range(0f, 1f)] public float activePulseAmount = 1f;
    [Tooltip("Stärke des inneren Leuchtens (0 = gar nicht, 1 = stark)")]
    [Range(0f, 1f)] public float activeInnerGlowOpacity = 0.35f;
    [Tooltip("Wie kurz das Innere aufblitzt (hoher Wert = kürzeres Blitzen)")]
    [Range(1f, 30f)] public float activeInnerGlowSharpness = 8f;

    [Space(10)]
    public Color inactiveHighlightColor = Color.white;
    public float inactiveOutlineWidth = 0f;
    public float inactivePulseSpeed = 0f;
    [Range(0f, 1f)] public float inactivePulseAmount = 0f;
    [Range(0f, 1f)] public float inactiveInnerGlowOpacity = 0f;
    [Range(1f, 30f)] public float inactiveInnerGlowSharpness = 8f;

    [Space(10)]
    [Header("Cooldown Highlight")]
    public Color cooldownHighlightColor = Color.red;
    public float cooldownOutlineWidth = 1f;
    public float cooldownPulseSpeed = 5f;
    [Range(0f, 1f)] public float cooldownPulseAmount = 1f;
    [Range(0f, 1f)] public float cooldownInnerGlowOpacity = 0.8f;
    [Range(1f, 30f)] public float cooldownInnerGlowSharpness = 3f;

    [Header("Spawn Animation")]
    [Tooltip("Von wo soll die Kanone ins Bild fahren? (z.B. X = 5 für rechts, X = -5 für links)")]
    public Vector2 spawnOffset = new Vector2(5f, 0f);
    public float spawnDuration = 1.5f;

    private float nextFireTime = 0f;
    private bool isActive = false;
    private bool isSpawning = false;
    private bool hasOriginalPos = false;
    private Vector3 originalPos;
    private MaterialPropertyBlock propBlock;

    // Wird vom BossCannonFightManager aufgerufen
    public void SetActive(bool active)
    {
        if (active && !hasOriginalPos)
        {
            originalPos = transform.position;
            hasOriginalPos = true;
        }

        bool wasActive = isActive;
        isActive = active;
        // Die Kanone wird sichtbar gemacht, wenn sie aktiv ist
        gameObject.SetActive(active);

        if (active && !wasActive)
        {
            StartCoroutine(SpawnRoutine());
        }
    }

    private System.Collections.IEnumerator SpawnRoutine()
    {
        isSpawning = true;
        
        Vector3 startPos = originalPos + new Vector3(spawnOffset.x, spawnOffset.y, 0f);
        transform.position = startPos;

        float elapsed = 0f;
        while (elapsed < spawnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / spawnDuration;
            // Smoother slide-in (EaseOutCubic)
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            
            transform.position = Vector3.Lerp(startPos, originalPos, ease);
            yield return null;
        }

        transform.position = originalPos;
        isSpawning = false;
    }

    private void OnValidate()
    {
        // Damit man die Farben im Editor anpassen und direkt sehen kann
        UpdateHighlight(false);
    }

    void Update()
    {
        bool isHovered = false;

        if (isActive && !isSpawning && UnityEngine.InputSystem.Mouse.current != null)
        {
            // Mausposition in Weltkoordinaten umwandeln
            Vector2 mouseScreenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 10f));

            // Zonen-Check: Ist die Maus auf der X-Achse nah genug UND NICHT im Decken-Bereich?
            if (mouseWorldPos.y <= ignoreAboveY && Mathf.Abs(mouseWorldPos.x - transform.position.x) <= activationRangeX)
            {
                isHovered = true;
                HandleMovement(mouseWorldPos.y);
                HandleShooting();
            }
        }

        UpdateHighlight(isHovered);
    }

    private void UpdateHighlight(bool isHovered)
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null) return;
        
        if (propBlock == null) propBlock = new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(propBlock);
        
        bool isOnCooldown = Time.time < nextFireTime;

        Color targetColor = isHovered ? activeHighlightColor : inactiveHighlightColor;
        float targetWidth = isHovered ? activeOutlineWidth : inactiveOutlineWidth;
        float targetSpeed = isHovered ? activePulseSpeed : inactivePulseSpeed;
        float targetPulseAmt = isHovered ? activePulseAmount : inactivePulseAmount;
        float targetInnerGlow = isHovered ? activeInnerGlowOpacity : inactiveInnerGlowOpacity;
        float targetSharpness = isHovered ? activeInnerGlowSharpness : inactiveInnerGlowSharpness;
        
        if (isSpawning)
        {
            // Während der Spawn-Animation (Waffe fährt rein) komplett unsichtbar machen
            targetWidth = 0f;
            targetInnerGlow = 0f;
        }
        else if (isOnCooldown)
        {
            float remaining = nextFireTime - Time.time;
            float rawFade = remaining / fireRate; // 1.0 direkt nach Schuss, 0.0 wenn wieder bereit
            
            // Optischer Trick: Lineares Ausblenden wirkt oft so, als wäre es zu früh weg. 
            // Durch Pow(..., 0.5f) bleibt es länger kräftig rot und verschwindet erst GANZ am Ende.
            float fadeAmount = Mathf.Pow(rawFade, 0.5f);

            targetColor = cooldownHighlightColor;
            targetColor.a *= fadeAmount; // Lässt die Outline-Farbe langsam verblassen

            targetWidth = cooldownOutlineWidth;
            targetSpeed = 0f; // Kein automatisches Sinus-Pulsieren mehr
            targetPulseAmt = 0f; // Fixiert den Shader auf 100% (damit wir ihn per Skript faden können)
            targetInnerGlow = cooldownInnerGlowOpacity * fadeAmount; // Lässt das Innere faden
            targetSharpness = cooldownInnerGlowSharpness;
        }

        propBlock.SetColor("_HighlightColor", targetColor);
        propBlock.SetFloat("_OutlineWidth", targetWidth);
        propBlock.SetFloat("_PulseSpeed", targetSpeed);
        propBlock.SetFloat("_PulseAmount", targetPulseAmt);
        propBlock.SetFloat("_InnerGlowMaxOpacity", targetInnerGlow);
        propBlock.SetFloat("_InnerGlowSharpness", targetSharpness);
        
        spriteRenderer.SetPropertyBlock(propBlock);
    }

    private void HandleMovement(float targetY)
    {
        // Ziel Y-Position an die Grenzen (minY, maxY) klammern
        targetY = Mathf.Clamp(targetY, minY, maxY);

        // Kanone weich zur neuen Position bewegen
        Vector3 newPos = transform.position;
        newPos.y = Mathf.Lerp(transform.position.y, targetY, moveSpeed * Time.deltaTime);
        transform.position = newPos;
    }

    private void HandleShooting()
    {
        // Wenn Linksklick gedrückt wird UND der Cooldown abgelaufen ist
        if (UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    private void Shoot()
    {
        if (cannonBallPrefab == null) return;

        Transform origin = firePoint != null ? firePoint : transform;

        // Sound abspielen
        if (shootSound != null && SceneSoundManager.Instance != null)
        {
            SceneSoundManager.Instance.PlaySFX(shootSound);
        }

        // Mini Shake für Kanonen-Schuss (Stärke reduziert)
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.ShakeCustom(0.15f, 0.1f);
        }

        // Kugel spawnen
        GameObject ball = Instantiate(cannonBallPrefab, origin.position, origin.rotation);
        CannonBall cb = ball.GetComponent<CannonBall>();
        
        if (cb != null)
        {
            // Je nach Seite nach links oder rechts schießen
            Vector2 direction = (side == CannonSide.Right) ? Vector2.left : Vector2.right;
            cb.Fire(direction);
        }
    }
}
