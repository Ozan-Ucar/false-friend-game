using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class MirrorInteract : MonoBehaviour
{
    [Header("Ziel-Szene")]
    [Tooltip("Der exakte Name der Szene, in die der Spiegel führt.")]
    public string targetSceneName = "NameDerSzeneHierEintragen";

    [Header("Cutscene (Optional)")]
    [Tooltip("Ziehe hier ein CutsceneData-Asset rein, um zwischen den Szenen eine Cutscene abzuspielen. Leer lassen = keine Cutscene.")]
    public CutsceneData cutsceneBeforeNextScene;

    [Header("Übergangs-Effekt (Transition)")]
    [Tooltip("Wie viele Sekunden der Pixel-Effekt dauert.")]
    public float transitionDuration = 1.2f;
    [Tooltip("Der Verlauf der Geschwindigkeit (z.B. fängt langsam an, wird schneller).")]
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("Die Farbe der Pixel beim Szenenwechsel.")]
    public Color transitionColor = Color.black;

    private bool hasTransitionStarted = false;

    void Update()
    {
        if (hasTransitionStarted) return;

        // Prüfen ob die linke Maustaste gedrückt wurde
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Position der Maus in die Welt-Koordinaten umrechnen
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
            
            // Prüfen, ob genau auf den Collider dieses Spiegels geklickt wurde
            Collider2D myCollider = GetComponent<Collider2D>();
            if (myCollider != null && myCollider.OverlapPoint(mouseWorldPos))
            {
                Interact();
            }
        }
    }

    private void Interact()
    {
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            Debug.Log("Spiegel wurde angeklickt! Starte Transition...");
            hasTransitionStarted = true;
            StartCoroutine(TransitionAndChangeScene());
        }
        else
        {
            Debug.LogWarning("Spiegel: Du hast noch keinen Szenen-Namen im Inspector eingetragen!");
        }
    }

    private IEnumerator TransitionAndChangeScene()
    {
        // 1. Canvas für den Vollbild-Effekt erstellen
        GameObject canvasObj = new GameObject("PixelTransitionCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        // 2. Ein RawImage für unsere generierte Pixel-Textur
        GameObject rawImageObj = new GameObject("PixelScreen");
        rawImageObj.transform.SetParent(canvasObj.transform, false);
        RawImage rawImage = rawImageObj.AddComponent<RawImage>();
        
        RectTransform rect = rawImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        // 3. Eine extrem niedrig auflösende 8-Bit Textur erstellen (z.B. 40x25 Pixel)
        int width = 40;
        int height = 25;
        Texture2D tex = new Texture2D(width, height);
        tex.filterMode = FilterMode.Point; // Wichtig: Macht die Pixel knackscharf!
        
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;
        tex.SetPixels(pixels);
        tex.Apply();
        
        rawImage.texture = tex;

        // 4. Wir erstellen eine Liste aller Pixel und mischen sie zufällig
        System.Collections.Generic.List<int> pixelIndices = new System.Collections.Generic.List<int>();
        for (int i = 0; i < pixels.Length; i++) pixelIndices.Add(i);
        
        for (int i = 0; i < pixelIndices.Count; i++)
        {
            int temp = pixelIndices[i];
            int randomIndex = Random.Range(i, pixelIndices.Count);
            pixelIndices[i] = pixelIndices[randomIndex];
            pixelIndices[randomIndex] = temp;
        }

        // 5. Animation: Den Bildschirm langsam Pixel für Pixel schwärzen
        float safeDuration = Mathf.Max(0.01f, transitionDuration);
        float elapsed = 0f;
        int totalPixels = pixels.Length;
        int pixelsColored = 0;

        // Farbe an das neue Level übergeben, falls du das Skript PixelSceneReveal benutzt
        PixelSceneReveal.globalTransitionColor = transitionColor;
        PixelSceneReveal.useFadeToBlack = false; // Wir wollen hier den Pixel-Effekt!

        while (pixelsColored < totalPixels)
        {
            elapsed += Time.deltaTime;
            
            float linearProgress = Mathf.Clamp01(elapsed / safeDuration);
            float curvedProgress = Mathf.Clamp01(transitionCurve.Evaluate(linearProgress));

            if (elapsed >= safeDuration) curvedProgress = 1f;

            int targetPixels = Mathf.RoundToInt(curvedProgress * totalPixels);

            bool changed = false;
            while (pixelsColored < targetPixels && pixelsColored < totalPixels)
            {
                pixels[pixelIndices[pixelsColored]] = transitionColor;
                pixelsColored++;
                changed = true;
            }

            if (changed)
            {
                tex.SetPixels(pixels);
                tex.Apply();
            }
            yield return null;
        }

        // In der exakt selben Millisekunde, in der der Bildschirm 100% schwarz ist, wechseln wir die Szene!
        if (cutsceneBeforeNextScene != null && cutsceneBeforeNextScene.slides != null && cutsceneBeforeNextScene.slides.Count > 0)
        {
            CutscenePlayer.pendingCutscene = cutsceneBeforeNextScene;
            CutscenePlayer.pendingTargetScene = targetSceneName;
            CutscenePlayer.Play();
        }
        else
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }
}
