using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Universal-Skript für Fallen-Tutorials in beliebigen Leveln.
/// - Friert den Tastatur-Spieler zu Beginn ein.
/// - Zeigt einen (hüpfenden) Pfeil nacheinander über den Fallen an.
/// - Der Maus-Spieler muss jede Falle in der vorgegebenen Reihenfolge anklicken/testen.
/// - Nach dem Testen aller Fallen wird der Tastatur-Spieler entfroren und das Spiel geht weiter.
/// </summary>
public class LevelTrapTutorial : MonoBehaviour
{
    [Header("Fallen Liste")]
    [Tooltip("Ziehe hier alle Fallen rein, die der Maus-Spieler nacheinander testen muss.")]
    public List<GameObject> trapsToTest = new List<GameObject>();

    [Header("Pfeil-Indikator")]
    [Tooltip("Optional: Ein eigenes Pfeil-Prefab.")]
    public GameObject arrowPrefab;
    [Tooltip("Optional: Eigenes Pfeil-Sprite (z.B. 'Arrow.png'). Wird sonst im Projekt automatisch gesucht!")]
    public Sprite arrowSprite;
    [Tooltip("Abstand des Pfeils über der jeweiligen Falle.")]
    public Vector3 arrowOffset = new Vector3(0f, 1.5f, 0f);
    [Tooltip("Soll der Pfeil sanft auf und ab hüpfen?")]
    public bool animateArrowBounce = true;
    [Tooltip("Geschwindigkeit des Hüpfens.")]
    public float bounceSpeed = 4f;
    [Tooltip("Höhe des Hüpfens.")]
    public float bounceAmount = 0.25f;

    [Header("Kamera & Player")]
    [Tooltip("Soll der Tastatur-Spieler während des Tutorials eingefroren werden?")]
    public bool freezePlayerDuringTutorial = true;
    [Tooltip("Soll die Kamera sanft auf die jeweils aktive Falle zoomen/fokussieren?")]
    public bool zoomCameraToTraps = false;
    [Tooltip("Kamera-Größe beim Heranzoomen (Standard ist ca. 3.5).")]
    public float cameraZoomSize = 3.5f;

    [Header("UI & Texte (Optional)")]
    [Tooltip("Ein TextMeshPro-Feld für Anweisungen (z.B. 'Teste alle Fallen! (1/3)').")]
    public TextMeshProUGUI promptText;
    [Tooltip("Text-Vorlauf vor der Zahlenanzeige.")]
    public string promptPrefix = "Klicke die markierte Falle!";

    [Header("Bereit-Button (Nach allen Fallen)")]
    [Tooltip("Optional: Ein eigener UI-Button. Wenn LEER, wird automatisch ein schicker 'Bereit?'-Button unten mittig eingeblendet.")]
    public Button readyButton;
    [Tooltip("Text auf dem Bereit-Button.")]
    public string readyButtonText = "Bereit";
    [Tooltip("Optional: Eigene Schriftart (z.B. 'ARCADECLASSIC SDF'). Wird sonst automatisch geladen!")]
    public TMP_FontAsset buttonFont;

    [Header("Ablauf Einstellungen")]
    [Tooltip("Verzögerung in Sekunden, bevor der Pfeil nach dem Klick zur nächsten Falle springt.")]
    public float delayBetweenTraps = 0.3f;
    [Tooltip("Soll das Tutorial automatisch beim Start des Levels beginnen?")]
    public bool startOnLevelStart = true;
    [Tooltip("Soll das Tutorial nur EINMAL pro Spielsitzung stattfinden (nach dem Tod im selben Level nicht erneut)?")]
    public bool playOncePerSession = true;

    // Statische Liste, um sich gemachte Tutorials in dieser Spielsitzung zu merken
    private static HashSet<string> completedTutorials = new HashSet<string>();

    private int currentIndex = 0;
    private GameObject currentArrowInstance;
    private bool isTutorialActive = false;
    private PlayerMovement playerMovement;
    private PlayerInput playerInput;
    private Rigidbody2D playerRb;
    private Animator playerAnim;
    private string tutorialID;

    private Dictionary<ClickableHighlight, bool> originalHighlightStates = new Dictionary<ClickableHighlight, bool>();

    private void Awake()
    {
        tutorialID = SceneManager.GetActiveScene().name + "_" + gameObject.name;

        // Prüfen, ob dieses Tutorial in dieser Spielsitzung schon absolviert wurde
        if (playOncePerSession && completedTutorials.Contains(tutorialID))
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (startOnLevelStart && !isTutorialActive)
        {
            StartTutorial();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!startOnLevelStart && !isTutorialActive && other.CompareTag("Player"))
        {
            StartTutorial();
        }
    }

    public void StartTutorial()
    {
        if (isTutorialActive || trapsToTest == null || trapsToTest.Count == 0) return;

        // Leere Einträge bereinigen
        trapsToTest.RemoveAll(item => item == null);
        if (trapsToTest.Count == 0) return;

        StartCoroutine(TutorialRoutine());
    }

    private IEnumerator TutorialRoutine()
    {
        isTutorialActive = true;
        currentIndex = 0;

        // 0. Warten, falls die PortalTransition (Eingangs-Einlaufanimation) gerade läuft!
        PortalTransition portalTrans = FindAnyObjectByType<PortalTransition>();
        if (portalTrans != null && portalTrans.IsInTransition())
        {
            yield return new WaitWhile(() => portalTrans.IsInTransition());
        }

        // 1. Player finden und einfrieren
        FindAndFreezePlayer(true);

        // 2. Pfeil-Objekt erstellen oder vorbereiten & Ursprungszustände speichern
        CreateOrPrepareArrow();
        StoreOriginalTrapStates();

        // 3. Durch alle Fallen nacheinander gehen
        while (currentIndex < trapsToTest.Count)
        {
            GameObject activeTrap = trapsToTest[currentIndex];

            if (activeTrap == null)
            {
                currentIndex++;
                continue;
            }

            // NUR die aktive Falle leuchtend & anklickbar machen, alle anderen deaktivieren!
            UpdateActiveTrapFocus(activeTrap);

            // Pfeil über die aktive Falle setzen
            UpdateArrowPosition(activeTrap.transform.position);

            // UI aktualisieren
            UpdateUI();

            // Kamera-Zoom (falls in den Einstellungen aktiviert)
            if (zoomCameraToTraps && CameraShake.Instance != null)
            {
                CameraShake.Instance.DoTutorialZoom(activeTrap.transform, cameraZoomSize, 0.8f);
            }

            // Warten, bis der Mausspieler genau DIESE Falle anklickt oder auslöst
            bool trapTested = false;
            while (!trapTested)
            {
                if (activeTrap == null) break;

                if (WasTrapClicked(activeTrap))
                {
                    trapTested = true;
                }
                yield return null;
            }

            // Warten, bis die Auslöse-Animation der Falle vollständig abgeschlossen ist!
            yield return StartCoroutine(WaitForTrapToFinish(activeTrap));

            // Kurze Pause vor dem nächsten Schritt
            if (delayBetweenTraps > 0f)
            {
                yield return new WaitForSeconds(delayBetweenTraps);
            }

            currentIndex++;
        }

        // 4. In Sitzung als erledigt markieren
        if (playOncePerSession)
        {
            completedTutorials.Add(tutorialID);
        }

        // 5. Pfeil & Prompt ausblenden & Fallen bis zum Level-Reset inaktiv halten
        if (currentArrowInstance != null) Destroy(currentArrowInstance);
        if (promptText != null) promptText.gameObject.SetActive(false);
        UpdateActiveTrapFocus(null);

        // 6. Kamera zurücksetzen (falls Heranzoomen aktiviert war)
        if (zoomCameraToTraps && CameraShake.Instance != null)
        {
            CameraShake.Instance.ResetTutorialZoom(0.5f);
        }

        // 7. Ready-Button anzeigen und auf Klick warten -> Reset Level ohne Tutorial!
        yield return StartCoroutine(ShowReadyButtonAndWait());
    }

    private void Update()
    {
        if (isTutorialActive)
        {
            // Tastatur-Spieler während des gesamten Tutorials strikt blockieren & einfrieren
            if (freezePlayerDuringTutorial)
            {
                if (playerMovement != null && playerMovement.enabled) playerMovement.enabled = false;
                if (playerInput != null && playerInput.enabled) playerInput.enabled = false;
                if (playerRb != null) playerRb.linearVelocity = Vector2.zero;
            }

            // Auf- und Abbewegen des Pfeils für gute Sichtbarkeit
            if (currentArrowInstance != null && animateArrowBounce && currentIndex < trapsToTest.Count)
            {
                GameObject activeTrap = trapsToTest[currentIndex];
                if (activeTrap != null)
                {
                    Vector3 basePos = activeTrap.transform.position + arrowOffset;
                    float bounceY = Mathf.Sin(Time.time * bounceSpeed) * bounceAmount;
                    currentArrowInstance.transform.position = basePos + new Vector3(0f, bounceY, 0f);
                }
            }
        }
    }

    private bool WasTrapClicked(GameObject trap)
    {
        // Method 1: Prüfen ob das ClickableHighlight getriggert wurde
        ClickableHighlight highlight = trap.GetComponent<ClickableHighlight>();
        if (highlight != null && highlight.isTriggered)
        {
            return true;
        }

        // Method 2: Direkter Mausklick auf den Collider der Falle
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            Collider2D[] colliders = trap.GetComponents<Collider2D>();
            foreach (var col in colliders)
            {
                if (col != null && col.enabled && col.OverlapPoint(mouseWorldPos))
                {
                    return true;
                }
            }

            // Auch Kinder-Collider durchsuchen
            Collider2D[] childColliders = trap.GetComponentsInChildren<Collider2D>();
            foreach (var col in childColliders)
            {
                if (col != null && col.enabled && col.OverlapPoint(mouseWorldPos))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void CreateOrPrepareArrow()
    {
        if (arrowPrefab != null)
        {
            currentArrowInstance = Instantiate(arrowPrefab);
        }
        else
        {
            // Erstelle Pfeil mit Arrow.png oder prozeduralem Sprite
            currentArrowInstance = new GameObject("TutorialArrow");
            SpriteRenderer sr = currentArrowInstance.AddComponent<SpriteRenderer>();

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
            sr.sortingOrder = 100; // Hoch, damit der Pfeil über allem gerendert wird
            currentArrowInstance.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
        }

        if (trapsToTest.Count > 0 && trapsToTest[0] != null)
        {
            currentArrowInstance.transform.position = trapsToTest[0].transform.position + arrowOffset;
        }
    }

    private void UpdateArrowPosition(Vector3 trapPosition)
    {
        if (currentArrowInstance != null)
        {
            currentArrowInstance.transform.position = trapPosition + arrowOffset;
        }
    }

    private void UpdateUI()
    {
        if (promptText != null)
        {
            promptText.gameObject.SetActive(true);
            promptText.text = $"{promptPrefix} ({currentIndex + 1}/{trapsToTest.Count})";
        }
    }

    private IEnumerator ShowReadyButtonAndWait()
    {
        bool readyClicked = false;
        GameObject createdCanvasObj = null;
        RectTransform btnRect = null;

        // Sicherstellen, dass ein EventSystem in der Szene existiert (wichtig für UI-Klicks in Unity!)
        if (UnityEngine.EventSystems.EventSystem.current == null && FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        if (readyButton != null)
        {
            readyButton.gameObject.SetActive(true);
            btnRect = readyButton.GetComponent<RectTransform>();
            readyButton.onClick.RemoveAllListeners();
            readyButton.onClick.AddListener(() => readyClicked = true);
        }
        else
        {
            // Automatisch einen extrem coolen leuchtend-orangen Bereit-Button mit 3D-Look generieren
            createdCanvasObj = new GameObject("ReadyButtonCanvas");
            Canvas canvas = createdCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            createdCanvasObj.AddComponent<CanvasScaler>();
            createdCanvasObj.AddComponent<GraphicRaycaster>();

            Sprite roundedSprite = CreateRoundedRectangleSprite(64, 64, 14f);

            // 1. Dunkler Schatten-Hintergrund für 3D-Tiefe (abgerundet)
            GameObject shadowObj = new GameObject("ReadyButtonShadow");
            shadowObj.transform.SetParent(createdCanvasObj.transform, false);
            Image shadowImg = shadowObj.AddComponent<Image>();
            shadowImg.sprite = roundedSprite;
            shadowImg.type = Image.Type.Sliced;
            shadowImg.color = new Color(0.4f, 0.12f, 0.01f, 0.85f); // Dunkles Kastanienbraun/Schatten

            RectTransform shadowRect = shadowObj.GetComponent<RectTransform>();
            shadowRect.anchorMin = new Vector2(0.5f, 0f);
            shadowRect.anchorMax = new Vector2(0.5f, 0f);
            shadowRect.pivot = new Vector2(0.5f, 0f);
            shadowRect.anchoredPosition = new Vector2(0f, 40f); // Leicht nach unten versetzt für 3D-Tiefe
            shadowRect.sizeDelta = new Vector2(358f, 98f);

            // 2. Haupt-Button in leuchtendem Orange (unten mittig)
            GameObject btnObj = new GameObject("ReadyButton");
            btnObj.transform.SetParent(createdCanvasObj.transform, false);

            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.sprite = roundedSprite;
            btnImg.type = Image.Type.Sliced;
            btnImg.color = new Color(1f, 0.46f, 0.05f, 1f); // Strahlendes Orange!

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(1f, 0.46f, 0.05f, 1f);
            cb.highlightedColor = new Color(1f, 0.68f, 0.15f, 1f); // Goldenes Leuchten beim Hovern
            cb.pressedColor = new Color(0.8f, 0.28f, 0.02f, 1f);   // Dunkleres Orange beim Klick
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.08f;
            btn.colors = cb;

            btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0f);
            btnRect.anchorMax = new Vector2(0.5f, 0f);
            btnRect.pivot = new Vector2(0.5f, 0f);
            btnRect.anchoredPosition = new Vector2(0f, 48f); // Unten mittig platziert!
            btnRect.sizeDelta = new Vector2(350f, 90f);

            // 3. Fett gedruckter Text ("Bereit")
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();

            // ARCADECLASSIC Schriftart laden
            TMP_FontAsset arcadeFont = buttonFont;
            if (arcadeFont == null)
            {
                TMP_FontAsset[] allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                foreach (var f in allFonts)
                {
                    if (f != null && (f.name.Contains("ARCADECLASSIC") || f.name.Contains("Arcade")))
                    {
                        arcadeFont = f;
                        break;
                    }
                }
            }

            if (arcadeFont != null)
            {
                tmp.font = arcadeFont;
            }

            // Text säubern (ohne Satzzeichen wie ? oder !)
            string labelText = string.IsNullOrEmpty(readyButtonText) ? "Bereit" : readyButtonText;
            if (labelText.EndsWith("?") || labelText.EndsWith("!"))
            {
                labelText = labelText.Substring(0, labelText.Length - 1).Trim();
            }

            tmp.text = labelText;
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
        }

        // === EXTREM COOLER SLIDE-IN EFFEKT VON UNTEN NACH OBEN (Mit Overshoot Bounce) ===
        if (btnRect != null)
        {
            RectTransform shadowRect = createdCanvasObj != null ? createdCanvasObj.transform.Find("ReadyButtonShadow") as RectTransform : null;

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

        // Warten bis der Maus-Spieler auf Bereit klickt + smoothe Hover-Skalierung NUR beim Drüberfahren!
        float currentScale = 1f;
        while (!readyClicked)
        {
            if (Mouse.current != null)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();
                bool isHovering = (btnRect != null && RectTransformUtility.RectangleContainsScreenPoint(btnRect, mousePos, null));

                // Smoothe Hover-Vergrößerung NUR beim Drüberfahren!
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

            // Tastatur-Alternative (Leertaste / Enter)
            if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
            {
                readyClicked = true;
            }

            yield return null;
        }

        // Clean-Klick Effekt: Kurzer sanfter Druck-Impuls
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
        if (readyButton != null) readyButton.gameObject.SetActive(false);

        // Level neu starten (das Tutorial ist in completedTutorials gespeichert und wird daher übersprungen!)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }



    private void FindAndFreezePlayer(bool freeze)
    {
        if (!freezePlayerDuringTutorial && freeze) return;

        if (playerMovement == null)
        {
            playerMovement = FindAnyObjectByType<PlayerMovement>();
        }

        if (playerMovement != null)
        {
            playerMovement.enabled = !freeze;

            if (playerInput == null) playerInput = playerMovement.GetComponent<PlayerInput>();
            if (playerInput != null) playerInput.enabled = !freeze;

            if (playerRb == null) playerRb = playerMovement.GetComponent<Rigidbody2D>();
            if (playerAnim == null) playerAnim = playerMovement.GetComponentInChildren<Animator>();

            if (freeze)
            {
                if (playerRb != null) playerRb.linearVelocity = Vector2.zero;
                if (playerAnim != null)
                {
                    playerAnim.SetBool("isWalking", false);
                    playerAnim.SetFloat("Speed", 0f);
                    playerAnim.speed = 1f; // Steh-Animation (Idle) weiterlaufen lassen, aber Gehen ausschalten
                }
            }
            else
            {
                if (playerAnim != null)
                {
                    playerAnim.speed = 1f;
                }
            }
        }
    }

    private IEnumerator WaitForTrapToFinish(GameObject trap)
    {
        if (trap == null) yield break;

        // Warten, bis die Falle (z.B. Pfeilschuss, Feuerstoß, Pilz-Explosion) ihre Animation beendet hat
        float duration = 1.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (trap == null || !trap.activeSelf) break;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void StoreOriginalTrapStates()
    {
        originalHighlightStates.Clear();

        foreach (var trap in trapsToTest)
        {
            if (trap == null) continue;

            ClickableHighlight[] highlights = trap.GetComponentsInChildren<ClickableHighlight>(true);
            foreach (var hl in highlights)
            {
                if (hl != null && !originalHighlightStates.ContainsKey(hl))
                {
                    originalHighlightStates[hl] = hl.isTriggered;
                }
            }
        }
    }

    private void UpdateActiveTrapFocus(GameObject currentActiveTrap)
    {
        foreach (var trap in trapsToTest)
        {
            if (trap == null) continue;

            bool isActiveTrap = (trap == currentActiveTrap);

            // Highlights steuern (NUR die aktive Falle leuchtet gold/gelb)
            ClickableHighlight[] highlights = trap.GetComponentsInChildren<ClickableHighlight>(true);
            foreach (var hl in highlights)
            {
                if (hl != null)
                {
                    hl.isTriggered = !isActiveTrap;
                    hl.UpdateHighlight();
                }
            }
        }
    }

    private void RestoreAllTrapStates()
    {
        foreach (var kvp in originalHighlightStates)
        {
            if (kvp.Key != null)
            {
                kvp.Key.isTriggered = kvp.Value;
                kvp.Key.UpdateHighlight();
            }
        }
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
                    float alpha = Mathf.Clamp01(1f - (dist - r)); // Anti-Aliased Ecken
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
        // Erzeugt ein scharfes Pfeil-Icon (gelb/gold mit dunklem Rand)
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color transparent = new Color(0, 0, 0, 0);
        Color border = new Color(0.1f, 0.1f, 0.1f, 1f);
        Color fill = new Color(1f, 0.85f, 0.1f, 1f); // Gold-Gelb

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                tex.SetPixel(x, y, transparent);
            }
        }

        // Pfeil-Schaft (oben)
        for (int y = 14; y <= 30; y++)
        {
            for (int x = 11; x <= 20; x++)
            {
                bool isEdge = (x == 11 || x == 20 || y == 30);
                tex.SetPixel(x, y, isEdge ? border : fill);
            }
        }

        // Pfeil-Spitze (unten)
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
