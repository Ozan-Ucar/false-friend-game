using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

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

    [Header("Tutorial & Extra Fallen")]
    [Tooltip("Optional: Pfeil-Prefab (wie bei TutorialTrap). Wenn LEER, wird arrowSprite oder prozeduraler Pfeil verwendet.")]
    public GameObject arrowPrefab;
    [Tooltip("Optional: Pfeil-Sprite für das Tutorial.")]
    public Sprite arrowSprite;
    [Tooltip("Abstand / Y-Höhe des Pfeils über den Fallen.")]
    public Vector3 arrowOffset = new Vector3(0f, 1.8f, 0f);
    [Tooltip("Zusätzliche Fallen im Level (z.B. RollingStone), die NACH den Sandwürmern auch getestet werden sollen.")]
    public List<GameObject> extraTrapsToTest = new List<GameObject>();
    
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
        ProceduralTrapSFX.PlayPlaceWormSound();
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
        Debug.Log("Alle Würmer platziert! Test-Phase beginnt!");
        StartCoroutine(SandwormTestingSequence());
    }

    private IEnumerator SandwormTestingSequence()
    {
        // 1. Warten, bis die Gräber sich in SandwormAttacks verwandelt haben (~1.2s)
        yield return new WaitForSeconds(1.2f);

        List<GameObject> allTraps = new List<GameObject>();

        // 1a. Alle platzierte Sandwürmer erfassen
        SandwormAttack[] sandworms = FindObjectsByType<SandwormAttack>(FindObjectsSortMode.None);
        if (sandworms != null)
        {
            foreach (var sw in sandworms)
            {
                if (sw != null && !allTraps.Contains(sw.gameObject))
                {
                    allTraps.Add(sw.gameObject);
                }
            }
        }

        // 1b. Zusätzliche Fallen (z.B. RollingStone) hinzufügen
        if (extraTrapsToTest != null)
        {
            foreach (var extra in extraTrapsToTest)
            {
                if (extra != null && !allTraps.Contains(extra))
                {
                    allTraps.Add(extra);
                }
            }
        }

        if (allTraps.Count == 0)
        {
            StartCoroutine(OutroSequence());
            yield break;
        }

        // 2. Bouncing Pfeil erstellen (Prefab, Custom Sprite oder Arrow.png)
        GameObject arrowObj = null;
        if (arrowPrefab != null)
        {
            arrowObj = Instantiate(arrowPrefab);
        }
        else
        {
            arrowObj = new GameObject("SandwormTutorialArrow");
            SpriteRenderer sr = arrowObj.AddComponent<SpriteRenderer>();

            Sprite spr = arrowSprite;
            if (spr == null)
            {
                Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
                foreach (var s in allSprites)
                {
                    if (s != null && (s.name.Equals("Arrow", System.StringComparison.OrdinalIgnoreCase) || s.name.StartsWith("Arrow_")))
                    {
                        spr = s;
                        break;
                    }
                }
            }

            sr.sprite = (spr != null) ? spr : CreateProceduralArrowSprite();
            sr.sortingOrder = 100;
            arrowObj.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
        }

        // 3. Durch alle Fallen gehen (Sandwürmer + RollingStone etc.)
        for (int i = 0; i < allTraps.Count; i++)
        {
            GameObject activeTrap = allTraps[i];
            if (activeTrap == null) continue;

            // Highlight-Fokus setzen (NUR die aktive Falle leuchtet)
            foreach (var t in allTraps)
            {
                if (t != null)
                {
                    ClickableHighlight hl = t.GetComponent<ClickableHighlight>();
                    if (hl != null)
                    {
                        hl.isTriggered = (t != activeTrap);
                        hl.UpdateHighlight();
                    }
                }
            }

            // Warten bis diese Falle geklickt/ausgelöst wurde + Pfeil hüpfen lassen
            bool tested = false;
            while (!tested)
            {
                if (activeTrap == null) break;

                // Sanfte Hüpf-Animation des Pfeils (wie bei TutorialTrap)
                float bounce = Mathf.Sin(Time.time * 4.5f) * 0.2f;
                if (arrowObj != null)
                {
                    arrowObj.transform.position = activeTrap.transform.position + arrowOffset + new Vector3(0f, bounce, 0f);
                }

                if (WasTrapTriggered(activeTrap))
                {
                    tested = true;
                }
                yield return null;
            }

            // Warten, bis die Falle (Animation/Effekt) fertig ist
            yield return new WaitForSeconds(1.4f);
        }

        // 4. Pfeil aufräumen & Highlights zurücksetzen
        if (arrowObj != null) Destroy(arrowObj);
        foreach (var t in allTraps)
        {
            if (t != null)
            {
                ClickableHighlight hl = t.GetComponent<ClickableHighlight>();
                if (hl != null)
                {
                    hl.isTriggered = false;
                    hl.UpdateHighlight();
                }
            }
        }

        // 5. Orangefarbenen Bereit-Button anzeigen & auf Klick warten
        yield return StartCoroutine(ShowReadyButtonAndWait());

        // 6. Erst NACH dem Bereit-Button verschwindet der Käfig!
        yield return StartCoroutine(OutroSequence());
    }

    private bool WasTrapTriggered(GameObject trap)
    {
        if (trap == null) return true;

        // 1. ClickableHighlight Check
        ClickableHighlight hl = trap.GetComponent<ClickableHighlight>();
        if (hl != null && hl.isTriggered) return true;

        // 2. SandwormAttack Check
        SandwormAttack swa = trap.GetComponent<SandwormAttack>();
        if (swa != null && !swa.IsReady) return true;

        // 3. Mausklick-Check auf Collider der Falle
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Collider2D[] colliders = trap.GetComponentsInChildren<Collider2D>();
            foreach (var col in colliders)
            {
                if (col != null && col.enabled && col.OverlapPoint(mouseWorldPos))
                {
                    return true;
                }
            }
        }

        return false;
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

    private IEnumerator ShowReadyButtonAndWait()
    {
        bool readyClicked = false;
        GameObject createdCanvasObj = null;
        RectTransform btnRect = null;

        if (UnityEngine.EventSystems.EventSystem.current == null && FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        createdCanvasObj = new GameObject("ReadyButtonCanvas");
        Canvas canvas = createdCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        createdCanvasObj.AddComponent<CanvasScaler>();
        createdCanvasObj.AddComponent<GraphicRaycaster>();

        Sprite roundedSprite = CreateRoundedRectangleSprite(64, 64, 14f);

        // 1. Dunkler Schatten-Hintergrund für 3D-Tiefe
        GameObject shadowObj = new GameObject("ReadyButtonShadow");
        shadowObj.transform.SetParent(createdCanvasObj.transform, false);
        Image shadowImg = shadowObj.AddComponent<Image>();
        shadowImg.sprite = roundedSprite;
        shadowImg.type = Image.Type.Sliced;
        shadowImg.color = new Color(0.4f, 0.12f, 0.01f, 0.85f);

        RectTransform shadowRect = shadowObj.GetComponent<RectTransform>();
        shadowRect.anchorMin = new Vector2(0.5f, 0f);
        shadowRect.anchorMax = new Vector2(0.5f, 0f);
        shadowRect.pivot = new Vector2(0.5f, 0f);
        shadowRect.anchoredPosition = new Vector2(0f, 40f);
        shadowRect.sizeDelta = new Vector2(358f, 98f);

        // 2. Haupt-Button in leuchtendem Orange (unten mittig)
        GameObject btnObj = new GameObject("ReadyButton");
        btnObj.transform.SetParent(createdCanvasObj.transform, false);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.sprite = roundedSprite;
        btnImg.type = Image.Type.Sliced;
        btnImg.color = new Color(1f, 0.46f, 0.05f, 1f);

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(1f, 0.46f, 0.05f, 1f);
        cb.highlightedColor = new Color(1f, 0.68f, 0.15f, 1f);
        cb.pressedColor = new Color(0.8f, 0.28f, 0.02f, 1f);
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.08f;
        btn.colors = cb;

        btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0f);
        btnRect.anchorMax = new Vector2(0.5f, 0f);
        btnRect.pivot = new Vector2(0.5f, 0f);
        btnRect.anchoredPosition = new Vector2(0f, 48f);
        btnRect.sizeDelta = new Vector2(350f, 90f);

        // 3. Text ("Bereit")
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();

        TMP_FontAsset arcadeFont = null;
        TMP_FontAsset[] allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (var f in allFonts)
        {
            if (f != null && (f.name.Contains("ARCADECLASSIC") || f.name.Contains("Arcade")))
            {
                arcadeFont = f;
                break;
            }
        }
        if (arcadeFont != null) tmp.font = arcadeFont;

        tmp.text = "Bereit";
        tmp.fontSize = 46;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.outlineColor = new Color(0.35f, 0.08f, 0.01f, 1f);
        tmp.outlineWidth = 0.22f;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        textRect.pivot = new Vector2(0.5f, 0.5f);

        btn.onClick.AddListener(() => readyClicked = true);

        // === EXTREM COOLER SLIDE-IN EFFEKT VON UNTEN NACH OBEN (Mit Overshoot Bounce) ===
        if (btnRect != null)
        {
            Vector2 targetBtnPos = new Vector2(0f, 48f);
            Vector2 startBtnPos = new Vector2(0f, -180f);

            Vector2 targetShadowPos = new Vector2(0f, 40f);
            Vector2 startShadowPos = new Vector2(0f, -188f);

            btnRect.anchoredPosition = startBtnPos;
            if (shadowRect != null) shadowRect.anchoredPosition = startShadowPos;
            btnRect.localScale = new Vector3(0.6f, 0.6f, 1f);

            float slideTime = 0f;
            float slideDuration = 0.45f;

            while (slideTime < slideDuration)
            {
                slideTime += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(slideTime / slideDuration);

                // EaseOutBack Formel für einen extrem coolen Overshoot / Swish-Effekt!
                float c1 = 1.70158f;
                float c3 = c1 + 1f;
                float ease = 1f + c3 * Mathf.Pow(progress - 1f, 3f) + c1 * Mathf.Pow(progress - 1f, 2f);

                btnRect.anchoredPosition = Vector2.LerpUnclamped(startBtnPos, targetBtnPos, ease);
                if (shadowRect != null) shadowRect.anchoredPosition = Vector2.LerpUnclamped(startShadowPos, targetShadowPos, ease);

                float slideScale = Mathf.LerpUnclamped(0.6f, 1f, ease);
                btnRect.localScale = new Vector3(slideScale, slideScale, 1f);

                yield return null;
            }

            btnRect.anchoredPosition = targetBtnPos;
            if (shadowRect != null) shadowRect.anchoredPosition = targetShadowPos;
            btnRect.localScale = Vector3.one;
        }

        // Hover-Animation & Klick-Erkennung
        float currentScale = 1f;
        while (!readyClicked)
        {
            if (Mouse.current != null)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();
                bool isHovering = (btnRect != null && RectTransformUtility.RectangleContainsScreenPoint(btnRect, mousePos, null));

                float targetScale = isHovering ? 1.06f : 1.0f;
                currentScale = Mathf.Lerp(currentScale, targetScale, Time.unscaledDeltaTime * 12f);

                if (btnRect != null)
                {
                    btnRect.localScale = new Vector3(currentScale, currentScale, 1f);
                }

                if (isHovering && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    readyClicked = true;
                }
            }

            if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
            {
                readyClicked = true;
            }

            yield return null;
        }

        // Klick-Effekt
        if (btnRect != null)
        {
            btnRect.localScale = new Vector3(0.92f, 0.92f, 1f);
            yield return new WaitForSecondsRealtime(0.08f);
            btnRect.localScale = new Vector3(1.02f, 1.02f, 1f);
            yield return new WaitForSecondsRealtime(0.06f);
        }

        if (SceneSoundManager.Instance != null && SceneSoundManager.Instance.stoneHitSound != null)
        {
            SceneSoundManager.Instance.PlaySFX(SceneSoundManager.Instance.stoneHitSound);
        }

        yield return new WaitForSecondsRealtime(0.1f);

        if (createdCanvasObj != null) Destroy(createdCanvasObj);
    }

    private Sprite CreateRoundedRectangleSprite(int width, int height, float cornerRadius)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        Color transparent = new Color(0, 0, 0, 0);
        Color white = Color.white;

        float r = cornerRadius;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float cx = x < r ? r : (x > width - r ? width - r : x);
                float cy = y < r ? r : (y > height - r ? height - r : y);

                float dx = x - cx;
                float dy = y - cy;
                float distSq = dx * dx + dy * dy;

                if (distSq > r * r)
                {
                    float dist = Mathf.Sqrt(distSq);
                    float alpha = Mathf.Clamp01(1f - (dist - r));
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, white);
                }
            }
        }

        tex.Apply();
        Vector4 border = new Vector4(r, r, r, r);
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }

    private Sprite CreateProceduralArrowSprite()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color transparent = new Color(0, 0, 0, 0);
        Color border = new Color(0.1f, 0.1f, 0.1f, 1f);
        Color fill = new Color(1f, 0.85f, 0.1f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                tex.SetPixel(x, y, transparent);
            }
        }

        for (int y = 14; y <= 30; y++)
        {
            for (int x = 11; x <= 20; x++)
            {
                bool isEdge = (x == 11 || x == 20 || y == 30);
                tex.SetPixel(x, y, isEdge ? border : fill);
            }
        }

        for (int y = 2; y <= 13; y++)
        {
            int halfWidth = y + 1;
            int centerX = 15;
            for (int x = centerX - halfWidth; x <= centerX + halfWidth + 1; x++)
            {
                if (x >= 0 && x < size)
                {
                    bool isEdge = (x == centerX - halfWidth || x == centerX + halfWidth + 1 || y == 2);
                    tex.SetPixel(x, y, isEdge ? border : fill);
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
    }
}
