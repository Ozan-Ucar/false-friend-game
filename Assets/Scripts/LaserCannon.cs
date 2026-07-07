using UnityEngine;

public class LaserCannon : MonoBehaviour
{
    [Header("Referenzen")]
    public LaserBeam laserBeam;

    [Header("Bewegung")]
    [Tooltip("Wie schnell die Kanone der Maus horizontal folgt")]
    public float moveSpeed = 15f;
    public float minX = -10f;
    public float maxX = 10f;

    [Header("Zonen-Steuerung")]
    [Tooltip("Wie nah muss die Maus an der Decke (Y-Achse) sein, damit der Laser reagiert?")]
    public float activationRangeY = 5f;

    [Header("Highlight (Shader)")]
    public SpriteRenderer spriteRenderer;
    public Color activeHighlightColor = Color.yellow;
    public float activeOutlineWidth = 1f;
    [Tooltip("Wie schnell das Highlight pulsiert")]
    public float activePulseSpeed = 2f;
    [Tooltip("1 = Starkes Pulsieren, 0 = Kein Pulsieren")]
    [Range(0f, 1f)] public float activePulseAmount = 0.13f;
    [Tooltip("Stärke des inneren Leuchtens (0 = gar nicht, 1 = stark)")]
    [Range(0f, 1f)] public float activeInnerGlowOpacity = 0f;
    [Tooltip("Wie kurz das Innere aufblitzt (hoher Wert = kürzeres Blitzen)")]
    [Range(1f, 30f)] public float activeInnerGlowSharpness = 8f;

    [Space(10)]
    public Color inactiveHighlightColor = Color.yellow;
    public float inactiveOutlineWidth = 1f;
    public float inactivePulseSpeed = 3f;
    [Range(0f, 1f)] public float inactivePulseAmount = 1f;
    [Range(0f, 1f)] public float inactiveInnerGlowOpacity = 0.07f;
    [Range(1f, 30f)] public float inactiveInnerGlowSharpness = 10.7f;

    [Space(10)]
    [Header("Cooldown Highlight")]
    public Color cooldownHighlightColor = Color.red;
    public float cooldownOutlineWidth = 1f;
    public float cooldownPulseSpeed = 5f;
    [Range(0f, 1f)] public float cooldownPulseAmount = 1f;
    [Range(0f, 1f)] public float cooldownInnerGlowOpacity = 0.8f;
    [Range(1f, 30f)] public float cooldownInnerGlowSharpness = 3f;

    [Header("Laser Setup")]
    public float laserCooldown = 5f;
    public float warningDuration = 1f;
    public float laserDuration = 0.5f;

    [Header("Spawn Animation")]
    [Tooltip("Von wo soll die Kanone ins Bild fahren? (z.B. Y = 5 für von oben)")]
    public Vector2 spawnOffset = new Vector2(0f, 5f);
    public float spawnDuration = 1.5f;

    private float nextFireTime = 0f;
    private float firingEndTime = 0f;
    private bool isActive = false;
    private bool isSpawning = false;
    private bool hasOriginalPos = false;
    private Vector3 originalPos;
    private MaterialPropertyBlock propBlock;

    public void SetActive(bool active)
    {
        if (active && !hasOriginalPos)
        {
            originalPos = transform.position;
            hasOriginalPos = true;
        }

        bool wasActive = isActive;
        isActive = active;
        gameObject.SetActive(active);
        
        if (active && !wasActive)
        {
            // Erster Schuss kurz nach Aktivierung
            nextFireTime = Time.time + spawnDuration + 1.5f; // Wait until animation + 1.5s
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

            // Zonen-Check: Ist die Maus nah genug an der Y-Achse (Decke) der Kanone?
            if (Mathf.Abs(mouseWorldPos.y - transform.position.y) <= activationRangeY)
            {
                isHovered = true;
                HandleMovement(mouseWorldPos.x);
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
        
        bool isOnCooldown = Time.time >= firingEndTime && Time.time < nextFireTime;

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
            float rawFade = remaining / laserCooldown; // 1.0 direkt nach Schuss, 0.0 wenn wieder bereit
            
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

    private void HandleMovement(float targetX)
    {
        targetX = Mathf.Clamp(targetX, minX, maxX);

        float currentMoveSpeed = moveSpeed;
        if (Time.time < firingEndTime)
        {
            // Bewegt sich sehr langsam während der Laser feuert
            currentMoveSpeed = moveSpeed * 0.15f; 
        }

        Vector3 newPos = transform.position;
        newPos.x = Mathf.Lerp(transform.position.x, targetX, currentMoveSpeed * Time.deltaTime);
        transform.position = newPos;
    }

    private void HandleShooting()
    {
        if (UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame && Time.time >= nextFireTime && laserBeam != null)
        {
            laserBeam.FireLaser(warningDuration, laserDuration);
            firingEndTime = Time.time + warningDuration + laserDuration;
            // Nächster Schuss erst, wenn der aktuelle komplett fertig ist + Cooldown
            nextFireTime = firingEndTime + laserCooldown;
        }
    }
}
