using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PixelSceneReveal : MonoBehaviour
{
    // Merkt sich die Farbe der Tür, durch die wir gegangen sind
    public static Color globalTransitionColor = Color.black;

    // Magie: Diese Funktion zwingt Unity dazu, dieses Skript bei JEDEM Laden einer Szene
    // vollautomatisch im Hintergrund auszuführen! Du musst das Skript nirgendwo draufziehen!
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Spawnt das Skript in der neuen Szene
        GameObject go = new GameObject("AutoPixelSceneReveal");
        go.AddComponent<PixelSceneReveal>();
    }

    void Start()
    {
        // Startet sofort die Aufdeck-Animation
        StartCoroutine(RevealScene());
    }

    private IEnumerator RevealScene()
    {
        // 1. Canvas erstellen
        GameObject canvasObj = new GameObject("PixelRevealCanvas");
        canvasObj.transform.SetParent(this.transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        // 2. RawImage erstellen
        GameObject rawImageObj = new GameObject("PixelScreen");
        rawImageObj.transform.SetParent(canvasObj.transform, false);
        RawImage rawImage = rawImageObj.AddComponent<RawImage>();
        
        RectTransform rect = rawImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        // 3. Textur (Diesmal starten wir komplett schwarz!)
        int width = 40;
        int height = 25;
        Texture2D tex = new Texture2D(width, height);
        tex.filterMode = FilterMode.Point;
        
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = globalTransitionColor;
        tex.SetPixels(pixels);
        tex.Apply();
        
        rawImage.texture = tex;

        // 4. Pixel mischen
        List<int> pixelIndices = new List<int>();
        for (int i = 0; i < pixels.Length; i++) pixelIndices.Add(i);
        
        for (int i = 0; i < pixelIndices.Count; i++)
        {
            int temp = pixelIndices[i];
            int randomIndex = Random.Range(i, pixelIndices.Count);
            pixelIndices[i] = pixelIndices[randomIndex];
            pixelIndices[randomIndex] = temp;
        }

        // 5. Animation: Den schwarzen Bildschirm rückgängig Pixel für Pixel aufdecken
        float duration = 1.2f; // Die gleiche Zeit wie bei der Tür
        float elapsed = 0f;
        int totalPixels = pixels.Length;
        int pixelsCleared = 0;

        while (pixelsCleared < totalPixels)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            
            // Ein weicher Verlauf, damit es sich angenehm anfühlt
            float curvedProgress = progress * (2f - progress); 
            if (elapsed >= duration) curvedProgress = 1f;

            int targetPixels = Mathf.RoundToInt(curvedProgress * totalPixels);

            bool changed = false;
            while (pixelsCleared < targetPixels && pixelsCleared < totalPixels)
            {
                // Hier ist der Trick: Wir setzen den schwarzen Pixel auf Color.clear (durchsichtig)!
                pixels[pixelIndices[pixelsCleared]] = Color.clear;
                pixelsCleared++;
                changed = true;
            }

            if (changed)
            {
                tex.SetPixels(pixels);
                tex.Apply();
            }
            yield return null;
        }

        // Sobald das Bild zu 100% freigelegt ist, zerstört sich dieses Skript selbst und räumt auf!
        Destroy(gameObject);
    }
}
