using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Spielt eine Cutscene mit epischen Animationen ab (Ken Burns, Letterbox, Fade, Typewriter).
/// Wird automatisch von PortalTransition / InteractableDoor erstellt.
/// Du musst dieses Skript NIRGENDWO manuell draufziehen!
/// </summary>
public class CutscenePlayer : MonoBehaviour
{
    // ========================================================
    // STATISCHE DATEN (werden vor dem Abspielen gesetzt)
    // ========================================================
    public static CutsceneData pendingCutscene = null;
    public static string pendingTargetScene = "";

    // ========================================================
    // INTERNE REFERENZEN
    // ========================================================
    private CutsceneData data;
    private string targetScene;

    // UI Elemente
    private Canvas canvas;
    private Image backgroundImage;
    private Image[] slideImages = new Image[2];
    private RectTransform[] slideRects = new RectTransform[2];
    private Image fadeOverlay;
    private Image vignetteOverlay;
    private Image topBar, bottomBar;
    private TextMeshProUGUI captionText;
    private TextMeshProUGUI skipText;
    private AudioSource musicSource;
    private AudioSource sfxSource;

    // Staub-Partikel
    private List<RectTransform> dustRects = new List<RectTransform>();
    private List<Image> dustImages = new List<Image>();

    // Input State
    private bool isPlaying = false;
    private bool wantsSkip = false;
    private bool skipAll = false;

    // ========================================================
    // ÖFFENTLICHE API
    // ========================================================

    /// <summary>
    /// Startet die Cutscene. Wird von PortalTransition / InteractableDoor aufgerufen,
    /// NACHDEM der Bildschirm bereits schwarz ist (Pixel-Transition).
    /// Setzt pendingCutscene und pendingTargetScene VORHER!
    /// </summary>
    public static void Play()
    {
        if (pendingCutscene == null || pendingCutscene.slides == null || pendingCutscene.slides.Count == 0)
        {
            // Keine Cutscene → direkt die Ziel-Szene laden (normales Verhalten)
            if (!string.IsNullOrEmpty(pendingTargetScene))
            {
                PixelSceneReveal.globalTransitionColor = Color.black;
                SceneManager.LoadScene(pendingTargetScene);
            }
            pendingCutscene = null;
            pendingTargetScene = "";
            return;
        }

        // Neues GameObject erstellen, das den Szenenwechsel überlebt
        GameObject go = new GameObject("CutscenePlayer");
        DontDestroyOnLoad(go);

        CutscenePlayer player = go.AddComponent<CutscenePlayer>();
        player.data = pendingCutscene;
        player.targetScene = pendingTargetScene;

        // Statische Daten leeren
        pendingCutscene = null;
        pendingTargetScene = "";
    }

    // ========================================================
    // UNITY CALLBACKS
    // ========================================================

    void Start()
    {
        if (data == null)
        {
            Destroy(gameObject);
            return;
        }
        CreateUI();
        StartCoroutine(PlayCutscene());
    }

    void Update()
    {
        if (!isPlaying) return;

        // Input abfragen
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            wantsSkip = true;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
                wantsSkip = true;

            // Escape = Gesamte Cutscene überspringen
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
                skipAll = true;
        }
    }

    // ========================================================
    // UI ERSTELLEN (alles wird zur Laufzeit gebaut)
    // ========================================================

    void CreateUI()
    {
        // --- Canvas (über ALLEM, sortingOrder 1000) ---
        GameObject canvasObj = new GameObject("CutsceneCanvas");
        canvasObj.transform.SetParent(transform);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // === Reihenfolge der UI-Elemente (von hinten nach vorne) ===

        // 1. Schwarzer Hintergrund
        backgroundImage = CreateFullscreenImage(canvasObj.transform, "Background", Color.black);

        // 2. Slide-Bilder (Array für Crossfades)
        for (int i = 0; i < 2; i++)
        {
            GameObject slideObj = new GameObject("SlideImage_" + i);
            slideObj.transform.SetParent(canvasObj.transform, false);
            slideImages[i] = slideObj.AddComponent<Image>();
            slideImages[i].color = new Color(1, 1, 1, 0);
            slideImages[i].preserveAspect = false;
            slideImages[i].type = Image.Type.Simple;
            slideRects[i] = slideImages[i].rectTransform;
            slideRects[i].anchorMin = Vector2.zero;
            slideRects[i].anchorMax = Vector2.one;
            slideRects[i].sizeDelta = Vector2.zero;
            slideImages[i].enabled = false;
        }

        // 3. Vignette (dunkle Ecken für Kino-Feeling)
        CreateVignette(canvasObj.transform);

        // 4. Staub-Partikel (optional)
        if (data.showParticles)
            CreateDustParticles(canvasObj.transform);

        // 5. Fade-Overlay (für Übergänge zwischen Slides)
        fadeOverlay = CreateFullscreenImage(canvasObj.transform, "FadeOverlay", new Color(0, 0, 0, 1));
        fadeOverlay.raycastTarget = false;

        // 6. Letterbox-Balken (Kino-Streifen oben/unten)
        topBar = CreateLetterboxBar(canvasObj.transform, "TopBar", true);
        bottomBar = CreateLetterboxBar(canvasObj.transform, "BottomBar", false);

        // 7. Caption-Text (z.B. "Kapitel 1: Der Wald")
        GameObject captionObj = new GameObject("CaptionText");
        captionObj.transform.SetParent(canvasObj.transform, false);
        captionText = captionObj.AddComponent<TextMeshProUGUI>();
        captionText.alignment = TextAlignmentOptions.Center;
        captionText.fontSize = 42;
        captionText.color = new Color(1, 1, 1, 0);
        captionText.enableWordWrapping = true;
        captionText.text = "";
        // Shadow für bessere Lesbarkeit auf Bildern
        Shadow shadow = captionObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.85f);
        shadow.effectDistance = new Vector2(3, -3);
        // Position: unten, über dem Letterbox-Balken
        RectTransform captionRect = captionText.rectTransform;
        captionRect.anchorMin = new Vector2(0.08f, 0.12f);
        captionRect.anchorMax = new Vector2(0.92f, 0.22f);
        captionRect.sizeDelta = Vector2.zero;

        // 8. Skip-Indikator ("Klicke zum Fortfahren")
        GameObject skipObj = new GameObject("SkipIndicator");
        skipObj.transform.SetParent(canvasObj.transform, false);
        skipText = skipObj.AddComponent<TextMeshProUGUI>();
        skipText.text = "\u25B6 Klicke zum Fortfahren";
        skipText.fontSize = 22;
        skipText.color = new Color(1, 1, 1, 0);
        skipText.alignment = TextAlignmentOptions.BottomRight;
        skipText.raycastTarget = false;
        RectTransform skipRect = skipText.rectTransform;
        skipRect.anchorMin = new Vector2(0.55f, 0.03f);
        skipRect.anchorMax = new Vector2(0.97f, 0.09f);
        skipRect.sizeDelta = Vector2.zero;

        // --- Audio Sources ---
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
    }

    /// <summary>
    /// Erstellt ein Fullscreen-Image (Hintergrund, Overlay etc.)
    /// </summary>
    Image CreateFullscreenImage(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = color;
        RectTransform rect = img.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        return img;
    }

    /// <summary>
    /// Erstellt einen schwarzen Letterbox-Balken (oben oder unten)
    /// </summary>
    Image CreateLetterboxBar(Transform parent, string name, bool isTop)
    {
        GameObject barObj = new GameObject(name);
        barObj.transform.SetParent(parent, false);
        Image bar = barObj.AddComponent<Image>();
        bar.color = Color.black;
        bar.raycastTarget = false;
        RectTransform rect = bar.rectTransform;

        if (isTop)
        {
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
        }
        else
        {
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0.5f, 0);
        }
        rect.sizeDelta = new Vector2(0, 0); // Starten mit Höhe 0
        return bar;
    }

    /// <summary>
    /// Erstellt die Vignette (Radial-Gradient, dunkle Ecken)
    /// </summary>
    void CreateVignette(Transform parent)
    {
        GameObject vigObj = new GameObject("Vignette");
        vigObj.transform.SetParent(parent, false);
        vignetteOverlay = vigObj.AddComponent<Image>();
        vignetteOverlay.raycastTarget = false;

        // Vignette-Textur per Code generieren
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x / (float)size - 0.5f) * 2f;
                float ny = (y / (float)size - 0.5f) * 2f;
                float dist = Mathf.Sqrt(nx * nx + ny * ny);
                float alpha = Mathf.Clamp01((dist - 0.4f) / 0.9f);
                alpha = alpha * alpha; // Quadratischer Falloff für weicheren Rand
                tex.SetPixel(x, y, new Color(0, 0, 0, alpha));
            }
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;

        vignetteOverlay.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        vignetteOverlay.color = new Color(1, 1, 1, 0.7f);
        vignetteOverlay.preserveAspect = false;

        RectTransform rect = vignetteOverlay.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
    }

    /// <summary>
    /// Erstellt subtile Staub-Partikel die über den Bildschirm schweben
    /// </summary>
    void CreateDustParticles(Transform parent)
    {
        int count = 25;
        for (int i = 0; i < count; i++)
        {
            GameObject dot = new GameObject("Dust_" + i);
            dot.transform.SetParent(parent, false);
            Image img = dot.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0);
            img.raycastTarget = false;

            RectTransform rect = img.rectTransform;
            float size = Random.Range(1.5f, 4f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = new Vector2(
                Random.Range(-960f, 960f),
                Random.Range(-540f, 540f)
            );

            dustRects.Add(rect);
            dustImages.Add(img);
        }
    }

    // ========================================================
    // CUTSCENE ABSPIELEN (Hauptlogik)
    // ========================================================

    IEnumerator PlayCutscene()
    {
        isPlaying = true;

        // --- Musik starten (sanft einfaden) ---
        if (data.backgroundMusic != null)
        {
            musicSource.clip = data.backgroundMusic;
            musicSource.volume = 0f;
            musicSource.Play();
            StartCoroutine(FadeAudio(musicSource, 0f, data.musicVolume, 2f));
        }

        // --- Staub-Partikel-Animation starten ---
        if (data.showParticles)
        {
            for (int i = 0; i < dustRects.Count; i++)
                StartCoroutine(AnimateDustParticle(dustRects[i], dustImages[i]));
        }

        // Kurze Pause am Anfang (damit es nicht zu abrupt startet)
        yield return new WaitForSeconds(0.3f);

        // --- Letterbox-Balken reinfahren ---
        if (data.showLetterbox)
            yield return StartCoroutine(AnimateLetterbox(true));

        // ============================
        // === SLIDES NACHEINANDER ===
        // ============================
        bool didDipToBlack = true; // Das erste Bild startet immer aus dem Schwarz
        
        for (int i = 0; i < data.slides.Count; i++)
        {
            // Escape gedrückt? → Alles überspringen!
            if (skipAll) break;

            CutsceneData.Slide slide = data.slides[i];
            if (slide.image == null) continue;

            int currentIdx = i % 2;
            int prevIdx = (i + 1) % 2;
            Image currentImage = slideImages[currentIdx];
            RectTransform currentRect = slideRects[currentIdx];
            Image prevImage = slideImages[prevIdx];
            RectTransform prevRect = slideRects[prevIdx];

            // Sicherstellen, dass das neue Bild ÜBER dem alten Bild liegt
            currentRect.SetSiblingIndex(prevRect.GetSiblingIndex() + 1);

            // Bild setzen & Ken Burns Startposition
            currentImage.sprite = slide.image;
            currentImage.enabled = true;
            currentImage.color = new Color(1, 1, 1, 0); // Start transparent für Fade
            SetupSlideTransform(slide, 0f, currentRect);

            // Sound-Effekt abspielen (optional)
            if (slide.soundEffect != null)
                sfxSource.PlayOneShot(slide.soundEffect);

            // --- BILD EINBLENDEN ---
            if (didDipToBlack)
            {
                currentImage.color = Color.white;
                if (i > 0) prevImage.enabled = false;
                yield return StartCoroutine(FadeImage(fadeOverlay, 1f, 0f, data.fadeInDuration));
            }
            else
            {
                // Crossfade mit der Dauer, die im VORHERIGEN Slide eingestellt wurde
                float crossfadeTime = data.slides[i - 1].transitionDuration;
                yield return StartCoroutine(CrossfadeImages(prevImage, currentImage, crossfadeTime));
                prevImage.enabled = false;
            }

            // Skip-Indikator anzeigen (pulsierend)
            if (slide.allowSkip)
                StartCoroutine(PulseSkipIndicator());

            // Caption-Text eintippen (Typewriter-Effekt)
            if (!string.IsNullOrEmpty(slide.captionText))
                StartCoroutine(TypewriterEffect(slide.captionText));

            // --- KEN BURNS ANIMATION + WARTEN ---
            wantsSkip = false;
            float elapsed = 0f;
            while (elapsed < slide.duration)
            {
                if (skipAll) break;
                if (slide.allowSkip && wantsSkip)
                {
                    wantsSkip = false;
                    break;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / slide.duration);
                SetupSlideTransform(slide, t, currentRect);

                yield return null;
            }

            // Skip-Indikator verstecken
            skipText.color = new Color(1, 1, 1, 0);

            // Caption ausfaden
            if (!string.IsNullOrEmpty(slide.captionText))
                StartCoroutine(FadeTMPAlpha(captionText, captionText.color.a, 0f, 0.4f));

            // --- FADE OUT (Bild verschwindet) ---
            if (skipAll)
            {
                fadeOverlay.color = Color.black; // Sofort schwarz bei Skip
            }
            else if (i == data.slides.Count - 1)
            {
                // Nur das allerletzte Bild fadet wieder schwarz aus
                yield return StartCoroutine(FadeImage(fadeOverlay, 0f, 1f, data.fadeOutDuration));
            }
            else
            {
                if (slide.transitionToNext == CutsceneData.SlideTransition.DipToBlack)
                {
                    yield return StartCoroutine(FadeImage(fadeOverlay, 0f, 1f, slide.transitionDuration));
                    if (data.pauseBetweenSlides > 0)
                        yield return new WaitForSeconds(data.pauseBetweenSlides);
                    didDipToBlack = true;
                }
                else
                {
                    // Standbild vor dem nächsten Crossfade
                    if (data.pauseBetweenSlides > 0)
                        yield return new WaitForSeconds(data.pauseBetweenSlides);
                    didDipToBlack = false;
                }
            }

            // Aufräumen
            captionText.text = "";
            captionText.color = new Color(1, 1, 1, 0);
            // currentImage bleibt aktiviert (entweder unterm Schwarz oder für den Crossfade)
        }

        // --- Letterbox-Balken rausfahren ---
        if (data.showLetterbox && !skipAll)
            yield return StartCoroutine(AnimateLetterbox(false));

        // --- Musik ausfaden ---
        if (musicSource.isPlaying)
            yield return StartCoroutine(FadeAudio(musicSource, musicSource.volume, 0f, skipAll ? 0.3f : 1.5f));

        // ============================
        // === ZIEL-SZENE LADEN ===
        // ============================
        isPlaying = false;

        if (!string.IsNullOrEmpty(targetScene))
        {
            // Farbe für PixelSceneReveal setzen (damit das neue Level sauber aufgedeckt wird)
            PixelSceneReveal.globalTransitionColor = Color.black;
            PixelSceneReveal.useFadeToBlack = true; // HIER IST DIE LÖSUNG: Zwinge es zum Fade-In!
            SceneManager.sceneLoaded += OnTargetSceneLoaded;
            SceneManager.LoadScene(targetScene);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ========================================================
    // KEN BURNS & SLIDE-ANIMATIONEN
    // ========================================================

    /// <summary>
    /// Berechnet Position und Skalierung des Slide-Bildes basierend auf der Animation und dem Fortschritt (0-1).
    /// </summary>
    void SetupSlideTransform(CutsceneData.Slide slide, float t, RectTransform rect)
    {
        // Weicher Verlauf (Smoothstep) für cinematisches Feeling
        float smooth = t * t * (3f - 2f * t);

        Vector3 startScale = Vector3.one;
        Vector3 endScale = Vector3.one;
        Vector2 startPos = Vector2.zero;
        Vector2 endPos = Vector2.zero;

        float panAmount = slide.panSpeed * 200f;

        switch (slide.animation)
        {
            case CutsceneData.SlideAnimation.KenBurnsZoomIn:
                // Langsam reinzoomen, leicht nach rechts-oben driften
                startScale = Vector3.one * 1.02f;
                endScale = Vector3.one * slide.zoomIntensity;
                endPos = new Vector2(panAmount * 0.5f, panAmount * 0.3f);
                break;

            case CutsceneData.SlideAnimation.KenBurnsZoomOut:
                // Von nah nach weit, leicht driften
                startScale = Vector3.one * slide.zoomIntensity;
                endScale = Vector3.one * 1.02f;
                startPos = new Vector2(panAmount * 0.5f, panAmount * 0.3f);
                break;

            case CutsceneData.SlideAnimation.PanLeft:
                startScale = Vector3.one * 1.08f;
                endScale = startScale;
                startPos = new Vector2(panAmount, 0);
                endPos = new Vector2(-panAmount, 0);
                break;

            case CutsceneData.SlideAnimation.PanRight:
                startScale = Vector3.one * 1.08f;
                endScale = startScale;
                startPos = new Vector2(-panAmount, 0);
                endPos = new Vector2(panAmount, 0);
                break;

            case CutsceneData.SlideAnimation.PanUp:
                startScale = Vector3.one * 1.08f;
                endScale = startScale;
                startPos = new Vector2(0, -panAmount * 0.6f);
                endPos = new Vector2(0, panAmount * 0.6f);
                break;

            case CutsceneData.SlideAnimation.PanDown:
                startScale = Vector3.one * 1.08f;
                endScale = startScale;
                startPos = new Vector2(0, panAmount * 0.6f);
                endPos = new Vector2(0, -panAmount * 0.6f);
                break;

            case CutsceneData.SlideAnimation.Static:
                // Kein Effekt
                break;

            case CutsceneData.SlideAnimation.SlowShake:
                // Subtiles Zittern mit Perlin Noise
                startScale = Vector3.one * 1.04f;
                endScale = startScale;
                float shakeX = (Mathf.PerlinNoise(Time.time * 1.5f, 0f) - 0.5f) * 8f;
                float shakeY = (Mathf.PerlinNoise(0f, Time.time * 1.5f) - 0.5f) * 5f;
                rect.localScale = startScale;
                rect.anchoredPosition = new Vector2(shakeX, shakeY);
                return; // Skip das Lerp unten
        }

        rect.localScale = Vector3.Lerp(startScale, endScale, smooth);
        rect.anchoredPosition = Vector2.Lerp(startPos, endPos, smooth);
    }

    // ========================================================
    // HELPER COROUTINES
    // ========================================================

    /// <summary>
    /// Führt einen weichen Crossfade zwischen zwei Bildern durch.
    /// </summary>
    IEnumerator CrossfadeImages(Image fromImg, Image toImg, float duration)
    {
        toImg.color = new Color(1, 1, 1, 0);
        if (duration <= 0f)
        {
            toImg.color = Color.white;
            if (fromImg != null) fromImg.color = new Color(1, 1, 1, 0);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smooth = t * t * (3f - 2f * t);

            toImg.color = new Color(1, 1, 1, smooth);
            yield return null;
        }
        toImg.color = Color.white;
        if (fromImg != null) fromImg.color = new Color(1, 1, 1, 0);
    }

    /// <summary>
    /// Fadet ein Image von alpha 'from' nach 'to' über 'duration' Sekunden (Smoothstep).
    /// </summary>
    IEnumerator FadeImage(Image img, float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            Color c = img.color;
            c.a = to;
            img.color = c;
            yield break;
        }

        float elapsed = 0f;
        Color color = img.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smooth = t * t * (3f - 2f * t);
            color.a = Mathf.Lerp(from, to, smooth);
            img.color = color;
            yield return null;
        }
        color.a = to;
        img.color = color;
    }

    /// <summary>
    /// Fährt die Letterbox-Balken rein oder raus.
    /// </summary>
    IEnumerator AnimateLetterbox(bool show)
    {
        float targetHeight = data.letterboxSize * 1080f; // Basierend auf der Reference Resolution
        float from = show ? 0f : targetHeight;
        float to = show ? targetHeight : 0f;
        float duration = 0.8f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smooth = t * t * (3f - 2f * t);
            float h = Mathf.Lerp(from, to, smooth);
            topBar.rectTransform.sizeDelta = new Vector2(0, h);
            bottomBar.rectTransform.sizeDelta = new Vector2(0, h);
            yield return null;
        }
        topBar.rectTransform.sizeDelta = new Vector2(0, to);
        bottomBar.rectTransform.sizeDelta = new Vector2(0, to);
    }

    /// <summary>
    /// Typewriter-Effekt: Schreibt den Text Buchstabe für Buchstabe.
    /// </summary>
    IEnumerator TypewriterEffect(string text)
    {
        captionText.text = "";
        captionText.color = Color.white;

        for (int i = 0; i < text.Length; i++)
        {
            captionText.text += text[i];
            // Bei Leerzeichen keine Pause (fühlt sich natürlicher an)
            if (text[i] != ' ')
                yield return new WaitForSeconds(0.04f);
        }
    }

    /// <summary>
    /// Lässt den Skip-Indikator sanft pulsieren.
    /// </summary>
    IEnumerator PulseSkipIndicator()
    {
        float timer = 0f;
        // Erst nach 1.5 Sekunden einblenden (Spieler soll nicht aus Versehen skippen)
        yield return new WaitForSeconds(1.5f);

        while (isPlaying && skipText != null)
        {
            timer += Time.deltaTime;
            float alpha = 0.2f + 0.3f * (Mathf.Sin(timer * 2.5f) * 0.5f + 0.5f);
            skipText.color = new Color(1, 1, 1, alpha);
            yield return null;
        }

        if (skipText != null)
            skipText.color = new Color(1, 1, 1, 0);
    }

    /// <summary>
    /// Fadet eine AudioSource von 'from' nach 'to'.
    /// </summary>
    IEnumerator FadeAudio(AudioSource source, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        source.volume = to;
        if (to <= 0f) source.Stop();
    }

    /// <summary>
    /// Fadet den Alpha-Wert eines TextMeshPro-Textes.
    /// </summary>
    IEnumerator FadeTMPAlpha(TextMeshProUGUI tmp, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && tmp != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, Mathf.Lerp(from, to, t));
            yield return null;
        }
        if (tmp != null)
            tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, to);
    }

    /// <summary>
    /// Animiert ein einzelnes Staubpartikel (schwebt nach oben, wobbled, fadet).
    /// </summary>
    IEnumerator AnimateDustParticle(RectTransform rect, Image img)
    {
        // Zufällige Startverzögerung für natürlicheres Erscheinen
        yield return new WaitForSeconds(Random.Range(0f, 5f));

        float speedX = Random.Range(-3f, 3f);
        float speedY = Random.Range(1f, 5f);
        float wobbleFreq = Random.Range(0.3f, 1.5f);
        float wobbleAmp = Random.Range(1f, 4f);
        float baseAlpha = Random.Range(0.02f, 0.08f);

        while (isPlaying && rect != null)
        {
            float lifetime = Random.Range(6f, 14f);
            float elapsed = 0f;

            // Neue Startposition (unten auf dem Bildschirm)
            rect.anchoredPosition = new Vector2(
                Random.Range(-960f, 960f),
                Random.Range(-540f, -200f)
            );

            while (elapsed < lifetime && isPlaying && rect != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / lifetime;

                // Aufwärts driften + sanftes Wobble
                rect.anchoredPosition += new Vector2(
                    (speedX + Mathf.Sin(Time.time * wobbleFreq) * wobbleAmp) * Time.deltaTime,
                    speedY * Time.deltaTime
                );

                // Sanftes Ein-/Ausfaden
                float alpha = baseAlpha;
                if (t < 0.15f) alpha *= t / 0.15f;
                else if (t > 0.85f) alpha *= (1f - t) / 0.15f;
                img.color = new Color(1, 1, 1, alpha);

                yield return null;
            }

            if (img != null)
                img.color = new Color(1, 1, 1, 0);
        }
    }

    // ========================================================
    // CLEANUP
    // ========================================================

    void OnTargetSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnTargetSceneLoaded;
        // 1 Frame warten, damit PixelSceneReveal zuerst seine Arbeit machen kann
        StartCoroutine(DestroyAfterDelay());
    }

    IEnumerator DestroyAfterDelay()
    {
        yield return null;
        Destroy(gameObject);
    }
}
