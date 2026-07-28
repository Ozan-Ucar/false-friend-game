using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Tutorial-Skript für den Blitzleiter (LightningRodTrap / Floating Orb).
/// 1. Zeigt ganz oben eine Anweisung: "Ziehe am Ball, um ihn zu schießen!"
/// 2. Zeigt einen bouncing Pfeil über dem Orb.
/// 3. Friert den Tastatur-Spieler ein.
/// 4. Sobald der Maus-Spieler den Ball 1x gezogen & geschossen hat, erscheint der orange Bereit-Button.
/// 5. Klick auf Bereit startet das Level ohne Tutorial sauber neu.
/// </summary>
public class LightningTrapTutorial : MonoBehaviour
{
    [Header("Ziel-Falle")]
    [Tooltip("Die Blitz-Falle im Level. Wenn LEER, wird automatisch die erste LightningRodTrap in der Szene gesucht.")]
    public LightningRodTrap targetTrap;

    [Header("Anweisung (Oben)")]
    [Tooltip("Der Anweisungstext ganz oben auf dem Bildschirm.")]
    public string topPromptText = "Ziehe am Ball, um ihn zu schießen!";

    [Header("Pfeil-Indikator")]
    [Tooltip("Optional: Ein eigenes Pfeil-Prefab.")]
    public GameObject arrowPrefab;
    [Tooltip("Optional: Eigenes Pfeil-Sprite (z.B. 'Arrow.png'). Wird sonst automatisch im Projekt gesucht.")]
    public Sprite arrowSprite;
    [Tooltip("Abstand des Pfeils über dem Orb.")]
    public Vector3 arrowOffset = new Vector3(0f, 1.8f, 0f);

    [Header("Bereit-Button")]
    [Tooltip("Text auf dem Bereit-Button.")]
    public string readyButtonText = "Bereit";
    [Tooltip("Optional: Eigene Schriftart (z.B. 'ARCADECLASSIC SDF').")]
    public TMP_FontAsset buttonFont;

    [Header("Einstellungen")]
    public bool playOncePerSession = true;
    public string tutorialID = "LightningTrapTutorial";

    private static HashSet<string> completedTutorials = new HashSet<string>();

    private PlayerMovement player;
    private Animator playerAnim;
    private GameObject createdTopCanvasObj;

    private void Awake()
    {
        if (playOncePerSession && completedTutorials.Contains(tutorialID))
        {
            Destroy(gameObject);
            return;
        }
    }

    private IEnumerator Start()
    {
        // 1. Warten, bis PortalTransition / Ladebildschirm fertig ist
        PortalTransition portalTrans = FindFirstObjectByType<PortalTransition>();
        if (portalTrans != null)
        {
            yield return null;
            while (portalTrans.IsInTransition())
            {
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        // 2. Tastatur-Spieler finden und einfrieren
        player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
        {
            player.enabled = false;
            playerAnim = player.GetComponent<Animator>();
            if (playerAnim != null)
            {
                playerAnim.SetFloat("Speed", 0f);
            }
            Rigidbody2D prb = player.GetComponent<Rigidbody2D>();
            if (prb != null)
            {
                prb.linearVelocity = Vector2.zero;
            }
        }

        // 3. Blitz-Falle finden
        if (targetTrap == null)
        {
            targetTrap = FindFirstObjectByType<LightningRodTrap>();
        }

        if (targetTrap == null)
        {
            // Keine Blitzfalle vorhanden -> Spieler freigeben & beenden
            if (player != null) player.enabled = true;
            yield break;
        }

        // 4. Anweisungs-Banner ganz oben erstellen
        CreateTopPromptUI();

        // 5. Bouncing Pfeil über dem Orb erstellen
        GameObject arrowObj = CreateArrowInstance();

        // 6. Warten, bis der Ball 1x geschossen wurde!
        bool shotDetected = false;
        while (!shotDetected)
        {
            if (targetTrap == null) break;

            // Pfeil sanft bouncing halten
            float bounce = Mathf.Sin(Time.time * 4.5f) * 0.2f;
            if (arrowObj != null)
            {
                arrowObj.transform.position = targetTrap.transform.position + arrowOffset + new Vector3(0f, bounce, 0f);
            }

            // Prüfen ob der Ball geschossen wurde
            if (targetTrap.HasBeenShot)
            {
                shotDetected = true;
            }
            else if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                Rigidbody2D trapRb = targetTrap.GetComponent<Rigidbody2D>();
                if (trapRb != null && trapRb.linearVelocity.sqrMagnitude > 0.5f)
                {
                    shotDetected = true;
                }
            }

            yield return null;
        }

        // 7. Pfeil & Anweisung oben ausblenden
        if (arrowObj != null) Destroy(arrowObj);
        if (createdTopCanvasObj != null) Destroy(createdTopCanvasObj);

        // 8. Kurz warten, bis der Schuss abgesendet wurde
        yield return new WaitForSeconds(0.6f);

        // 9. Orangenen Bereit-Button anzeigen & warten
        yield return StartCoroutine(ShowReadyButtonAndWait());
    }

    private TMP_FontAsset GetBestFontAsset()
    {
        if (buttonFont != null) return buttonFont;

        TMP_FontAsset[] allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (var f in allFonts)
        {
            if (f != null && (f.name.Contains("ARCADECLASSIC") || f.name.Contains("Arcade")))
            {
                return f;
            }
        }

        if (TMP_Settings.defaultFontAsset != null)
        {
            return TMP_Settings.defaultFontAsset;
        }

        return null;
    }

    private void CreateTopPromptUI()
    {
        createdTopCanvasObj = new GameObject("LightningTutorialTopCanvas");
        Canvas canvas = createdTopCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        createdTopCanvasObj.AddComponent<CanvasScaler>();
        createdTopCanvasObj.AddComponent<GraphicRaycaster>();

        // 1. Dunkler Banner-Hintergrund ganz oben
        GameObject bannerObj = new GameObject("TopBanner");
        bannerObj.transform.SetParent(createdTopCanvasObj.transform, false);
        Image bannerImg = bannerObj.AddComponent<Image>();
        bannerImg.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);

        RectTransform bannerRect = bannerObj.GetComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0f, 1f);
        bannerRect.anchorMax = new Vector2(1f, 1f);
        bannerRect.pivot = new Vector2(0.5f, 1f);
        bannerRect.anchoredPosition = new Vector2(0f, -30f); // 30 Pixel vom oberen Bildschirmland
        bannerRect.sizeDelta = new Vector2(0f, 75f);

        // 2. Anweisungstext in leuchtendem Gold-Gelb
        GameObject textObj = new GameObject("TopText");
        textObj.transform.SetParent(bannerObj.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();

        TMP_FontAsset font = GetBestFontAsset();
        if (font != null)
        {
            tmp.font = font;
        }

        tmp.text = string.IsNullOrEmpty(topPromptText) ? "Ziehe am Ball, um ihn zu schießen!" : topPromptText;
        tmp.fontSize = 38;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.88f, 0.15f, 1f);
        tmp.fontStyle = FontStyles.Bold;
        tmp.outlineColor = new Color(0.1f, 0.05f, 0f, 1f);
        tmp.outlineWidth = 0.25f;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        textRect.pivot = new Vector2(0.5f, 0.5f);
    }

    private GameObject CreateArrowInstance()
    {
        GameObject arrowObj = null;
        if (arrowPrefab != null)
        {
            arrowObj = Instantiate(arrowPrefab);
        }
        else
        {
            arrowObj = new GameObject("LightningTutorialArrow");
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

        if (targetTrap != null)
        {
            arrowObj.transform.position = targetTrap.transform.position + arrowOffset;
        }

        return arrowObj;
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

        // 1. Schatten
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

        // 2. Haupt-Button
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

        // 3. Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();

        TMP_FontAsset font = GetBestFontAsset();
        if (font != null) tmp.font = font;

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

        // === SLIDE-IN EFFEKT (Overshoot Bounce) ===
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

        // Hover & Klick-Warteschleife
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

        yield return new WaitForSecondsRealtime(0.12f);

        if (createdCanvasObj != null) Destroy(createdCanvasObj);

        // Session speichern
        if (playOncePerSession && !string.IsNullOrEmpty(tutorialID))
        {
            completedTutorials.Add(tutorialID);
        }

        // Level neu starten ohne Tutorial
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
