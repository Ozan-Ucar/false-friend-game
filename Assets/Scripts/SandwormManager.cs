using UnityEngine;
using UnityEngine.InputSystem;

public class SandwormManager : MonoBehaviour
{
    [Header("Placement Settings")]
    [Tooltip("Die Gräber-Prefabs in der Reihenfolge, wie sie platziert werden sollen (z.B. Normal, Mid, High)")]
    public GameObject[] wormPrefabs;
    
    [Tooltip("Mindestabstand zu anderen Würmern, damit sie nicht ineinander spawnen")]
    public float minDistanceBetweenWorms = 1.0f;
    
    [Tooltip("Die Durchsichtigkeit des Geists (Vorschau), wenn er platziert werden darf (0 = unsichtbar, 1 = voll sichtbar)")]
    [Range(0f, 1f)]
    public float ghostValidAlpha = 0.5f;

    [Tooltip("Die Durchsichtigkeit des Geists, wenn er ROT ist und NICHT platziert werden darf")]
    [Range(0f, 1f)]
    public float ghostInvalidAlpha = 0.5f;

    [Tooltip("Breite des Wurms (Für den Treppen/Klippen-Check - wie weit links und rechts wird gemessen?)")]
    public float wormWidth = 2.0f;

    [Tooltip("Maximaler erlaubter Höhenunterschied links und rechts (damit er nicht auf Treppen/Schrägen baut)")]
    public float maxSlopeTolerance = 0.1f;

    [Header("Cinematic")]
    [Tooltip("Sprite für den Vogelkäfig, der auf den Spieler fällt")]
    public Sprite birdCageSprite;
    
    [Tooltip("Größe des Käfigs (z.B. X: 2, Y: 2, wenn er zu klein ist)")]
    public Vector2 birdCageScale = Vector2.one;

    [Tooltip("Verschiebt den Landepunkt nach oben oder unten (z.B. -0.5)")]
    public float birdCageDropYOffset = 0f;

    [Tooltip("Wie lange dauert es, bis der Käfig den Boden erreicht? (in Sekunden)")]
    public float birdCageDropDuration = 0.8f;
    
    private GameObject activeBirdCage;

    private int placedCount = 0;
    
    // Gibt an, ob die Bauphase beendet ist
    public bool AllWormsPlaced => wormPrefabs == null || placedCount >= wormPrefabs.Length;

    private PlayerMovement playerMovement;
    
    // Ghost (Vorschau)
    private GameObject ghostWorm;
    private SpriteRenderer[] ghostRenderers;

    void Start()
    {
        // Statt sofort loszulegen, starten wir die Intro-Sequenz mit dem Käfig!
        StartCoroutine(IntroSequence());
    }

    private System.Collections.IEnumerator IntroSequence()
    {
        // 0. WARTEN: Wir lassen den Spieler erst gemütlich in die Szene reinlaufen
        PortalTransition portalTrans = FindFirstObjectByType<PortalTransition>();
        if (portalTrans != null)
        {
            // Warte einen Frame, um sicherzugehen, dass PortalTransition Start() ausgeführt hat
            yield return null; 
            
            while (portalTrans.IsInTransition())
            {
                yield return null; // Warten, bis der Spieler am Ziel steht
            }
        }
        else
        {
            // Falls es keine Tür gibt, warten wir zumindest, bis der schwarze Ladebildschirm weg ist
            yield return new WaitForSeconds(1.2f);
        }

        // 1. Finde den Spieler und friere ihn ein
        playerMovement = FindFirstObjectByType<PlayerMovement>();
        Vector3 playerPos = Vector3.zero;

        if (playerMovement != null)
        {
            playerMovement.enabled = false;
            playerPos = playerMovement.transform.position;
        }

        // 2. Vogelkäfig animieren
        if (birdCageSprite != null)
        {
            activeBirdCage = new GameObject("BirdCage");
            SpriteRenderer sr = activeBirdCage.AddComponent<SpriteRenderer>();
            sr.sprite = birdCageSprite;
            sr.sortingOrder = 50; // Damit er vor dem Spieler gezeichnet wird
            
            // Setze die Größe
            activeBirdCage.transform.localScale = new Vector3(birdCageScale.x, birdCageScale.y, 1f);

            // Der perfekte Landepunkt auf dem Spieler (inklusive Höhen-Korrektur)
            Vector3 targetPos = playerPos + new Vector3(0, birdCageDropYOffset, 0);

            // Käfig startet 15 Units über dem Landepunkt
            Vector3 startPos = targetPos + new Vector3(0, 15f, 0);
            activeBirdCage.transform.position = startPos;

            // Wir sagen dem SoundManager SCHON JETZT Bescheid, dass der Käfig fällt und wie lange es dauert.
            // Er wartet dann im Hintergrund genau diese Zeit (plus dein manuelles Offset), um den Sound abzuspielen!
            if (SceneSoundManager.Instance != null)
            {
                SceneSoundManager.Instance.PlayCageDrop(birdCageDropDuration);
            }

            // Lass ihn runterfallen
            float t = 0f;
            while (t < birdCageDropDuration)
            {
                t += Time.deltaTime;
                float progress = t / birdCageDropDuration;
                
                // Ein kleines "Easing" (schneller werdend), damit es Wucht hat
                float easeIn = progress * progress * progress; 
                activeBirdCage.transform.position = Vector3.Lerp(startPos, targetPos, easeIn);
                yield return null;
            }
            activeBirdCage.transform.position = targetPos; // Exakt am Ziel ankommen

            // Kurzer Moment der Stille, um den Aufprall wirken zu lassen
            yield return new WaitForSeconds(0.4f);
        }

        // 3. Erstelle den ersten "Geist" zum Bauen
        CreateGhost();
    }

    void CreateGhost()
    {
        if (wormPrefabs == null || wormPrefabs.Length == 0 || placedCount >= wormPrefabs.Length) return;

        // Instanziere das Prefab passend zur aktuellen Runde (0 = erstes, 1 = zweites, etc.)
        ghostWorm = Instantiate(wormPrefabs[placedCount]);
        
        // Deaktiviere das Skript, damit man es noch nicht anklicken kann
        SandwormGrave trapScript = ghostWorm.GetComponent<SandwormGrave>();
        if (trapScript != null)
        {
            // Zwinge Unity, die Animation BEREITS IM ERSTEN FRAME korrekt zu laden!
            Animator anim = ghostWorm.GetComponent<Animator>();
            if (anim != null && !string.IsNullOrEmpty(trapScript.previewAnimationName))
            {
                anim.Play(trapScript.previewAnimationName, 0, 0f);
                anim.Update(0f); // Der magische Befehl: Überspringt Unitys Ladeverzögerung!
            }

            trapScript.enabled = false; // Wird erst nach dem Platzieren aktiviert
            trapScript.isPlaced = false;
        }

        // Mache ihn halb-transparent (Geist) und schalte seinen Collider aus (damit der Raycast nicht aus Versehen den Geist trifft!)
        Collider2D col = ghostWorm.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        ghostRenderers = ghostWorm.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in ghostRenderers)
        {
            Color c = sr.color;
            c.a = ghostValidAlpha; 
            sr.color = c;
        }
    }

    void Update()
    {
        if (wormPrefabs == null || placedCount >= wormPrefabs.Length) return; // Bau-Phase ist vorbei!

        if (ghostWorm != null && Mouse.current != null)
        {
            // MANUELLER LOOP FÜR DEN GEIST
            Animator anim = ghostWorm.GetComponent<Animator>();
            SandwormGrave trapScript = ghostWorm.GetComponent<SandwormGrave>();
            if (anim != null && trapScript != null && !string.IsNullOrEmpty(trapScript.previewAnimationName))
            {
                AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
                
                // Wenn die Animation fertig ist, starte sie neu (Loop-Ersatz)
                if (state.normalizedTime >= 1.0f)
                {
                    anim.Play(trapScript.previewAnimationName, 0, 0f);
                    anim.Update(0f);
                }
            }

            // 1. Maus-Position in der Welt finden
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            // Prüfen, ob die Maus BEREITS IM BODEN steckt (um zu verhindern, dass man Würmer im Boden platziert)
            bool isMouseInsideGround = false;
            Collider2D[] colsAtMouse = Physics2D.OverlapPointAll(mousePos);
            foreach (Collider2D c in colsAtMouse)
            {
                if (!c.CompareTag("Player") && !c.isTrigger && c.GetComponent<SandwormGrave>() == null && c.GetComponent<SandwormAttack>() == null)
                {
                    isMouseInsideGround = true;
                    break;
                }
            }

            RaycastHit2D validHit = new RaycastHit2D();
            
            if (!isMouseInsideGround)
            {
                // 2. Wir suchen den Boden. Der Strahl startet EXAKT an der Maus (minimal darüber) 
                // und geht nur 3 Meter nach unten. Man muss also auf den Boden zielen!
                validHit = GetValidGroundHit(mousePos + Vector2.up * 0.1f, 3f);
            }

            if (validHit.collider != null)
            {
                // PERFEKT! Wir setzen den Geist auf den gefundenen Boden + das individuelle Offset des jeweiligen Wurms
                Vector2 finalPos = validHit.point;
                
                SandwormGrave currentTrap = ghostWorm.GetComponent<SandwormGrave>();
                if (currentTrap != null)
                {
                    finalPos.y += currentTrap.ghostYOffset;
                }

                ghostWorm.transform.position = finalPos;

                bool canPlace = true;

                // --- 1. SCHRITT: Flacher Boden-Check (Treppen/Schrägen blockieren) ---
                float halfWidth = wormWidth / 2f;
                // Wir schießen links und rechts vom Mittelpunkt leicht von oben nach unten
                Vector2 leftStart = validHit.point + new Vector2(-halfWidth, 1.0f);
                Vector2 rightStart = validHit.point + new Vector2(halfWidth, 1.0f);

                RaycastHit2D hitLeft = GetValidGroundHit(leftStart, 3f);
                RaycastHit2D hitRight = GetValidGroundHit(rightStart, 3f);

                if (hitLeft.collider != null && hitRight.collider != null)
                {
                    // Wenn der Höhenunterschied zwischen dem linken und rechten Punkt zu groß ist = Treppe/Schräge!
                    if (Mathf.Abs(hitLeft.point.y - hitRight.point.y) > maxSlopeTolerance)
                    {
                        canPlace = false;
                    }
                }
                else
                {
                    // Wenn ein Raycast keinen Boden gefunden hat, schwebt eine Hälfte in der Luft (Klippe!)
                    canPlace = false;
                }

                // --- 2. SCHRITT: Prüfen, ob der Platz frei ist (Abstand zu anderen Würmern) ---
                if (canPlace)
                {
                    SandwormGrave[] allGraves = FindObjectsByType<SandwormGrave>(FindObjectsSortMode.None);
                    foreach (SandwormGrave g in allGraves)
                    {
                        if (g.gameObject != ghostWorm && Vector2.Distance(finalPos, g.transform.position) < minDistanceBetweenWorms)
                        {
                            canPlace = false;
                            break;
                        }
                    }
                }

                if (canPlace)
                {
                    SandwormAttack[] allAttacks = FindObjectsByType<SandwormAttack>(FindObjectsSortMode.None);
                    foreach (SandwormAttack a in allAttacks)
                    {
                        if (a.gameObject != ghostWorm && Vector2.Distance(finalPos, a.transform.position) < minDistanceBetweenWorms)
                        {
                            canPlace = false;
                            break;
                        }
                    }
                }

                if (canPlace)
                {
                    foreach (SpriteRenderer sr in ghostRenderers)
                    {
                        Color c = sr.color;
                        c.r = 1f; c.g = 1f; c.b = 1f; c.a = ghostValidAlpha; // Weiß transparent
                        sr.color = c;
                    }

                    if (UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                    {
                        PlaceWorm();
                    }
                }
                else
                {
                    foreach (SpriteRenderer sr in ghostRenderers)
                    {
                        Color c = sr.color;
                        c.r = 1f; c.g = 0f; c.b = 0f; c.a = ghostInvalidAlpha; // Rot transparent (blockiert)
                        sr.color = c;
                    }
                }
            }
            else
            {
                // Falls du die Maus ganz aus der Map rausbewegst, wo wirklich GAR KEIN Boden ist:
                ghostWorm.transform.position = mousePos;
                foreach (SpriteRenderer sr in ghostRenderers)
                {
                    sr.enabled = true;
                    sr.color = new Color(1f, 0f, 0f, ghostInvalidAlpha); // Rot
                }
            }
        }
    }

    void PlaceWorm()
    {
        // Geist wird wieder zu 100% sichtbar und sein Collider wird angeschaltet!
        Collider2D col = ghostWorm.GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        foreach (SpriteRenderer sr in ghostRenderers)
        {
            Color c = sr.color;
            c.a = 1f;
            sr.color = c;
        }

        // Skript aktivieren, damit Spieler 2 es später anklicken kann
        SandwormGrave trapScript = ghostWorm.GetComponent<SandwormGrave>();
        if (trapScript != null)
        {
            // JETZT wird er zum Grab!
            trapScript.PlayIdle();
            trapScript.enabled = true;
            trapScript.isPlaced = true;
        }

        // --- NEU: Soundeffekt für das Platzieren abspielen ---
        if (SceneSoundManager.Instance != null)
        {
            SceneSoundManager.Instance.PlayPlaceWorm();
        }

        ghostWorm = null;
        placedCount++;

        // Nächsten Geist spawnen oder Action-Phase starten
        if (placedCount < wormPrefabs.Length)
        {
            CreateGhost();
        }
        else
        {
            EndPlacementPhase();
        }
    }

    void EndPlacementPhase()
    {
        Debug.Log("Alle Würmer platziert! Action-Phase beginnt!");
        
        StartCoroutine(OutroSequence());
    }

    private System.Collections.IEnumerator OutroSequence()
    {
        // Käfig ausfaden lassen
        if (activeBirdCage != null)
        {
            // --- NEU: Fade Sound abspielen ---
            if (SceneSoundManager.Instance != null)
            {
                SceneSoundManager.Instance.PlayCageFade();
            }

            SpriteRenderer sr = activeBirdCage.GetComponent<SpriteRenderer>();
            float fadeDuration = 1.0f;
            float t = 0f;
            Color startColor = sr.color;

            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float progress = t / fadeDuration;
                startColor.a = Mathf.Lerp(1f, 0f, progress);
                sr.color = startColor;
                yield return null;
            }

            Destroy(activeBirdCage);
        }

        // Spieler wieder auftauen
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }
    }

    // Hilfsmethode, um den perfekten Boden zu finden (ignoriert Spieler, Trigger und andere Würmer!)
    private RaycastHit2D GetValidGroundHit(Vector2 startPos, float distance)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(startPos, Vector2.down, distance);
        foreach (RaycastHit2D h in hits)
        {
            if (h.collider != null && !h.collider.CompareTag("Player") && !h.collider.isTrigger)
            {
                // Ignoriere platzierte Würmer
                if (h.collider.GetComponent<SandwormGrave>() == null && h.collider.GetComponent<SandwormAttack>() == null)
                {
                    return h;
                }
            }
        }
        return new RaycastHit2D(); // Nichts gefunden
    }
}
