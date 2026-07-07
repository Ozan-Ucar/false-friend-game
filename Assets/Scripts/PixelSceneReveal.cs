using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PixelSceneReveal : MonoBehaviour
{
    // Merkt sich die Farbe der Tür, durch die wir gegangen sind
    public static Color globalTransitionColor = Color.black;
    public static bool useFadeToBlack = true; // Standard ist ab jetzt Fade to Black!

    private Canvas _canvas;
    private Image _fadeImage;
    private RawImage _pixelImage;
    private Texture2D _pixelTex;
    private Color[] _pixels;
    private List<int> _pixelIndices;

    // Magie: Diese Funktion zwingt Unity dazu, dieses Skript bei JEDEM Laden einer Szene
    // vollautomatisch im Hintergrund auszuführen! Du musst das Skript nirgendwo draufziehen!
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (TransitionShowcase.lastTransitionIndex != -1) return;

        GameObject go = new GameObject("AutoPixelSceneReveal");
        go.AddComponent<PixelSceneReveal>();
    }

    void Awake()
    {
        // WICHTIG: Das Canvas muss in Awake() erstellt werden!
        // Awake() läuft ab, BEVOR Unity den allerersten Frame der neuen Szene rendert.
        // Das verhindert den "1-Frame-Flash", bei dem die Szene kurz hell aufblitzt!
        GameObject canvasObj = new GameObject("PixelRevealCanvas");
        canvasObj.transform.SetParent(this.transform);
        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 999;

        if (useFadeToBlack)
        {
            GameObject imgObj = new GameObject("FadeScreen");
            imgObj.transform.SetParent(canvasObj.transform, false);
            _fadeImage = imgObj.AddComponent<Image>();
            _fadeImage.color = globalTransitionColor;
            
            RectTransform rect = _fadeImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
        }
        else
        {
            GameObject rawImageObj = new GameObject("PixelScreen");
            rawImageObj.transform.SetParent(canvasObj.transform, false);
            _pixelImage = rawImageObj.AddComponent<RawImage>();
            
            RectTransform rect = _pixelImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            int width = 40;
            int height = 25;
            _pixelTex = new Texture2D(width, height);
            _pixelTex.filterMode = FilterMode.Point;
            
            _pixels = new Color[width * height];
            for (int i = 0; i < _pixels.Length; i++) _pixels[i] = globalTransitionColor;
            _pixelTex.SetPixels(_pixels);
            _pixelTex.Apply();
            
            _pixelImage.texture = _pixelTex;

            _pixelIndices = new List<int>();
            for (int i = 0; i < _pixels.Length; i++) _pixelIndices.Add(i);
            
            for (int i = 0; i < _pixelIndices.Count; i++)
            {
                int temp = _pixelIndices[i];
                int randomIndex = Random.Range(i, _pixelIndices.Count);
                _pixelIndices[i] = _pixelIndices[randomIndex];
                _pixelIndices[randomIndex] = temp;
            }
        }
    }

    void Start()
    {
        StartCoroutine(RevealScene());
    }

    private IEnumerator RevealScene()
    {
        if (useFadeToBlack)
        {
            yield return StartCoroutine(FadeReveal());
            useFadeToBlack = true;
            Destroy(gameObject);
            yield break;
        }

        float duration = 1.2f; 
        float elapsed = 0f;
        int totalPixels = _pixels.Length;
        int pixelsCleared = 0;

        while (pixelsCleared < totalPixels)
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            elapsed += dt;
            
            float progress = Mathf.Clamp01(elapsed / duration);
            float curvedProgress = progress * (2f - progress); 
            if (elapsed >= duration) curvedProgress = 1f;

            int targetPixels = Mathf.RoundToInt(curvedProgress * totalPixels);

            bool changed = false;
            while (pixelsCleared < targetPixels && pixelsCleared < totalPixels)
            {
                _pixels[_pixelIndices[pixelsCleared]] = Color.clear;
                pixelsCleared++;
                changed = true;
            }

            if (changed)
            {
                _pixelTex.SetPixels(_pixels);
                _pixelTex.Apply();
            }
            yield return null;
        }

        useFadeToBlack = true;
        Destroy(gameObject);
    }

    private IEnumerator FadeReveal()
    {
        float duration = 1.2f; 
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            elapsed += dt;
            
            float t = Mathf.Clamp01(elapsed / duration);
            float curvedProgress = t * t * (3f - 2f * t); 
            
            _fadeImage.color = new Color(globalTransitionColor.r, globalTransitionColor.g, globalTransitionColor.b, 1f - curvedProgress);
            yield return null;
        }
    }
}
