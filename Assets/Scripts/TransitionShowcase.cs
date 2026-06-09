using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Lege dieses Skript auf ein leeres GameObject.
/// Im Play-Modus drückst du die Pfeiltasten (Links/Rechts) um zwischen den Transitions zu wechseln
/// und SPACE um die aktuelle Transition abzuspielen!
/// </summary>
public class TransitionShowcase : MonoBehaviour
{
    [Header("Einstellungen")]
    [Tooltip("Wie lange jede Transition dauert")]
    public float transitionDuration = 1.5f;
    [Tooltip("Die Farbe der Transition")]
    public Color transitionColor = Color.black;

    // Internes
    private RawImage rawImage;
    private Texture2D tex;
    private Color[] pixels;
    private int width = 160;
    private int height = 90;
    private int currentIndex = 0;
    private bool isPlaying = false;
    private TextMeshProUGUI label;
    private TextMeshProUGUI instructions;

    // Alle verfügbaren Transitions
    private string[] transitionNames = new string[]
    {
        "1. Pixel Dissolve (Zufällig)",
        "2. Fade to Black",
        "3. Circle Wipe (Iris)",
        "4. Diamond Wipe (Diamant)",
        "5. Horizontal Blinds (Jalousie)",
        "6. Vertical Blinds (Vertikal)",
        "7. Checkerboard (Schachbrett)",
        "8. Diagonal Wipe (Diagonal)",
        "9. Clock Wipe (Uhrzeiger)",
        "10. Scanlines (Retro TV)",
        "11. Curtain Close (Vorhang)",
        "12. Pixelate (Verpixeln)",
        "13. Rain Drip (Regen)",
        "14. Spiral (Spirale)",
        "15. Star Wipe (Stern)",
        "16. Heart Wipe (Herz)",
        "17. TV Static (Rauschen)",
        "18. Slide Left (Links schieben)",
        "19. Slide Right (Rechts schieben)",
        "20. Slide Down (Runter schieben)",
        "21. Double Door (Doppeltür)",
        "22. Matrix Rain (Matrix)",
        "23. Wave Wipe (Welle)",
        "24. Zigzag Wipe (Zickzack)",
        "25. Mosaic (Mosaik)",
    };

    void Start()
    {
        // Canvas bauen
        GameObject canvasObj = new GameObject("TransitionShowcaseCanvas");
        canvasObj.transform.SetParent(transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        // RawImage für die Transition
        GameObject imgObj = new GameObject("TransitionImage");
        imgObj.transform.SetParent(canvasObj.transform, false);
        rawImage = imgObj.AddComponent<RawImage>();
        RectTransform rect = rawImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        // Textur
        tex = new Texture2D(width, height);
        tex.filterMode = FilterMode.Point;
        pixels = new Color[width * height];
        ClearPixels(Color.clear);
        rawImage.texture = tex;

        // Label oben
        GameObject labelObj = new GameObject("TransitionLabel");
        labelObj.transform.SetParent(canvasObj.transform, false);
        label = labelObj.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 42;
        label.color = Color.white;
        label.outlineWidth = 0.25f;
        label.outlineColor = Color.black;
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0, 1);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.pivot = new Vector2(0.5f, 1);
        labelRect.anchoredPosition = new Vector2(0, -30);
        labelRect.sizeDelta = new Vector2(0, 60);
        label.text = transitionNames[0];

        // Anweisungen unten
        GameObject instrObj = new GameObject("Instructions");
        instrObj.transform.SetParent(canvasObj.transform, false);
        instructions = instrObj.AddComponent<TextMeshProUGUI>();
        instructions.alignment = TextAlignmentOptions.Center;
        instructions.fontSize = 28;
        instructions.color = new Color(1f, 1f, 1f, 0.7f);
        RectTransform instrRect = instructions.rectTransform;
        instrRect.anchorMin = new Vector2(0, 0);
        instrRect.anchorMax = new Vector2(1, 0);
        instrRect.pivot = new Vector2(0.5f, 0);
        instrRect.anchoredPosition = new Vector2(0, 30);
        instrRect.sizeDelta = new Vector2(0, 50);
        instructions.text = "← → Wechseln  |  SPACE Abspielen";
    }

    void Update()
    {
        if (isPlaying) return;

        if (Keyboard.current == null) return;

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            currentIndex = (currentIndex + 1) % transitionNames.Length;
            label.text = transitionNames[currentIndex];
        }
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            currentIndex--;
            if (currentIndex < 0) currentIndex = transitionNames.Length - 1;
            label.text = transitionNames[currentIndex];
        }
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            StartCoroutine(PlayTransition(currentIndex));
        }
    }

    // ==========================================
    //  HAUPT-DISPATCHER
    // ==========================================
    private IEnumerator PlayTransition(int index)
    {
        isPlaying = true;
        ClearPixels(Color.clear);
        ApplyTexture();

        // Phase 1: Einblenden (Bildschirm zudecken)
        yield return StartCoroutine(RunTransition(index, false));

        yield return new WaitForSeconds(0.3f);

        // Phase 2: Ausblenden (Bildschirm aufdecken)
        yield return StartCoroutine(RunTransition(index, true));

        ClearPixels(Color.clear);
        ApplyTexture();
        isPlaying = false;
    }

    private IEnumerator RunTransition(int index, bool reverse)
    {
        switch(index)
        {
            case 0:  yield return PixelDissolve(reverse); break;
            case 1:  yield return FadeToBlack(reverse); break;
            case 2:  yield return CircleWipe(reverse); break;
            case 3:  yield return DiamondWipe(reverse); break;
            case 4:  yield return HorizontalBlinds(reverse); break;
            case 5:  yield return VerticalBlinds(reverse); break;
            case 6:  yield return Checkerboard(reverse); break;
            case 7:  yield return DiagonalWipe(reverse); break;
            case 8:  yield return ClockWipe(reverse); break;
            case 9:  yield return Scanlines(reverse); break;
            case 10: yield return CurtainClose(reverse); break;
            case 11: yield return Pixelate(reverse); break;
            case 12: yield return RainDrip(reverse); break;
            case 13: yield return Spiral(reverse); break;
            case 14: yield return StarWipe(reverse); break;
            case 15: yield return HeartWipe(reverse); break;
            case 16: yield return TVStatic(reverse); break;
            case 17: yield return SlideLeft(reverse); break;
            case 18: yield return SlideRight(reverse); break;
            case 19: yield return SlideDown(reverse); break;
            case 20: yield return DoubleDoor(reverse); break;
            case 21: yield return MatrixRain(reverse); break;
            case 22: yield return WaveWipe(reverse); break;
            case 23: yield return ZigzagWipe(reverse); break;
            case 24: yield return Mosaic(reverse); break;
        }
    }

    // ==========================================
    //  HILFSFUNKTIONEN
    // ==========================================
    private void ClearPixels(Color c)
    {
        for (int i = 0; i < pixels.Length; i++) pixels[i] = c;
    }

    private void ApplyTexture()
    {
        tex.SetPixels(pixels);
        tex.Apply();
    }

    private void SetPixel(int x, int y, Color c)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
            pixels[y * width + x] = c;
    }

    private Color GetPixel(int x, int y)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
            return pixels[y * width + x];
        return Color.clear;
    }

    // ==========================================
    //  1. PIXEL DISSOLVE (Zufällig)
    // ==========================================
    private IEnumerator PixelDissolve(bool reverse)
    {
        List<int> indices = new List<int>();
        for (int i = 0; i < pixels.Length; i++) indices.Add(i);
        for (int i = 0; i < indices.Count; i++)
        {
            int r = Random.Range(i, indices.Count);
            int tmp = indices[i]; indices[i] = indices[r]; indices[r] = tmp;
        }

        ClearPixels(reverse ? transitionColor : Color.clear);

        float elapsed = 0;
        int done = 0;
        while (done < pixels.Length)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / transitionDuration);
            int target = Mathf.RoundToInt(p * pixels.Length);
            while (done < target && done < pixels.Length)
            {
                pixels[indices[done]] = reverse ? Color.clear : transitionColor;
                done++;
            }
            ApplyTexture();
            yield return null;
        }
    }

    // ==========================================
    //  2. FADE TO BLACK
    // ==========================================
    private IEnumerator FadeToBlack(bool reverse)
    {
        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / transitionDuration);
            if (reverse) p = 1f - p;
            Color c = transitionColor;
            c.a = p;
            ClearPixels(c);
            ApplyTexture();
            yield return null;
        }
    }

    // ==========================================
    //  3. CIRCLE WIPE (Iris)
    // ==========================================
    private IEnumerator CircleWipe(bool reverse)
    {
        float maxRadius = Mathf.Sqrt(width * width + height * height) * 0.5f;
        float cx = width * 0.5f, cy = height * 0.5f;
        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / transitionDuration);
            float radius = p * maxRadius;

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                bool covered;

                if (reverse)
                {
                    // Aufdecken: Schwarzer Kreis SCHRUMPFT von außen nach innen
                    covered = dist < (maxRadius - radius);
                }
                else
                {
                    // Zudecken: Schwarzer Kreis WÄCHST von innen nach außen
                    covered = dist < radius;
                }

                pixels[y * width + x] = covered ? transitionColor : Color.clear;
            }
            ApplyTexture();
            yield return null;
        }
    }

    // ==========================================
    //  4. DIAMOND WIPE (Diamant)
    // ==========================================
    private IEnumerator DiamondWipe(bool reverse)
    {
        float maxDist = width * 0.5f + height * 0.5f;
        float cx = width * 0.5f, cy = height * 0.5f;
        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / transitionDuration);
            float threshold = p * maxDist;

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float dist = Mathf.Abs(x - cx) + Mathf.Abs(y - cy);
                bool covered;
                if (reverse) covered = dist > (maxDist - threshold);
                else covered = dist < threshold;
                pixels[y * width + x] = covered ? transitionColor : Color.clear;
            }
            ApplyTexture();
            yield return null;
        }
    }

    // ==========================================
    //  5. HORIZONTAL BLINDS (Jalousie)
    // ==========================================
    private IEnumerator HorizontalBlinds(bool reverse)
    {
        int blindCount = 10;
        int blindHeight = height / blindCount;
        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / transitionDuration);
            if (reverse) p = 1f - p;

            for (int y = 0; y < height; y++)
            {
                int localY = y % blindHeight;
                bool covered = localY < (int)(p * blindHeight);
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = covered ? transitionColor : Color.clear;
            }
            ApplyTexture();
            yield return null;
        }
    }

    // ==========================================
    //  6. VERTICAL BLINDS (Vertikal)
    // ==========================================
    private IEnumerator VerticalBlinds(bool reverse)
    {
        int blindCount = 12;
        int blindWidth = width / blindCount;
        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / transitionDuration);
            if (reverse) p = 1f - p;

            for (int x = 0; x < width; x++)
            {
                int localX = x % blindWidth;
                bool covered = localX < (int)(p * blindWidth);
                for (int y = 0; y < height; y++)
                    pixels[y * width + x] = covered ? transitionColor : Color.clear;
            }
            ApplyTexture();
            yield return null;
        }
    }

    // ==========================================
    //  7. CHECKERBOARD (Schachbrett)
    // ==========================================
    private IEnumerator Checkerboard(bool reverse)
    {
        int cellSize = 8;
        int cellsX = (width + cellSize - 1) / cellSize;
        int cellsY = (height + cellSize - 1) / cellSize;
        int totalCells = cellsX * cellsY;

        // Hälfte der Zellen zuerst (wie ein echtes Schachbrett)
        List<Vector2Int> phase1 = new List<Vector2Int>();
        List<Vector2Int> phase2 = new List<Vector2Int>();
        for (int cy = 0; cy < cellsY; cy++)
        for (int cx = 0; cx < cellsX; cx++)
        {
            if ((cx + cy) % 2 == 0) phase1.Add(new Vector2Int(cx, cy));
            else phase2.Add(new Vector2Int(cx, cy));
        }

        ClearPixels(reverse ? transitionColor : Color.clear);
        Color targetCol = reverse ? Color.clear : transitionColor;

        // Phase 1: Schwarze Felder
        float half = transitionDuration * 0.5f;
        float elapsed = 0;
        int done = 0;
        while (done < phase1.Count)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / half);
            int target = Mathf.RoundToInt(p * phase1.Count);
            while (done < target && done < phase1.Count)
            {
                FillCell(phase1[done].x, phase1[done].y, cellSize, targetCol);
                done++;
            }
            ApplyTexture();
            yield return null;
        }

        // Phase 2: Weiße Felder
        elapsed = 0; done = 0;
        while (done < phase2.Count)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / half);
            int target = Mathf.RoundToInt(p * phase2.Count);
            while (done < target && done < phase2.Count)
            {
                FillCell(phase2[done].x, phase2[done].y, cellSize, targetCol);
                done++;
            }
            ApplyTexture();
            yield return null;
        }
    }

    private void FillCell(int cx, int cy, int cellSize, Color c)
    {
        for (int dy = 0; dy < cellSize; dy++)
        for (int dx = 0; dx < cellSize; dx++)
            SetPixel(cx * cellSize + dx, cy * cellSize + dy, c);
    }

    // ==========================================
    //  8. DIAGONAL WIPE
    // ==========================================
    private IEnumerator DiagonalWipe(bool reverse)
    {
        float maxDist = width + height;
        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / transitionDuration);
            float threshold = p * maxDist;

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float dist = x + (height - y);
                bool covered;
                if (reverse) covered = dist > (maxDist - threshold);
                else covered = dist < threshold;
                pixels[y * width + x] = covered ? transitionColor : Color.clear;
            }
            ApplyTexture();
            yield return null;
        }
    }

    // ==========================================
    //  9. CLOCK WIPE (Uhrzeiger)
    // ==========================================
    private IEnumerator ClockWipe(bool reverse)
    {
        float cx = width * 0.5f, cy = height * 0.5f;
        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / transitionDuration);
            float targetAngle = p * 360f;

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float angle = Mathf.Atan2(x - cx, cy - y) * Mathf.Rad2Deg;
                if (angle < 0) angle += 360f;
                bool covered;
                if (reverse) covered = angle > (360f - targetAngle);
                else covered = angle < targetAngle;
                pixels[y * width + x] = covered ? transitionColor : Color.clear;
            }
            ApplyTexture();
            yield return null;
        }
    }

    // ==========================================
    //  10. SCANLINES (Retro TV)
    // ==========================================
    private IEnumerator Scanlines(bool reverse)
    {
        ClearPixels(reverse ? transitionColor : Color.clear);
        Color targetCol = reverse ? Color.clear : transitionColor;

        // Gerade Zeilen von oben, ungerade von unten
        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / transitionDuration);

            int evenLine = (int)(p * height);
            int oddLine = height - 1 - (int)(p * height);

            for (int y = 0; y < height; y++)
            {
                bool covered = false;
                if (y % 2 == 0) covered = y < evenLine;
                else covered = y > oddLine;

                if (covered)
                    for (int x = 0; x < width; x++)
                        pixels[y * width + x] = targetCol;
            }
            ApplyTexture();
            yield return null;
        }
    }

    // ==========================================
    //  11. CURTAIN CLOSE (Vorhang)
    // ==========================================
    private IEnumerator CurtainClose(bool reverse)
    {
        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / transitionDuration);
            if (reverse) p = 1f - p;

            int halfCover = (int)(p * width * 0.5f);

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                bool covered = (x < halfCover) || (x >= width - halfCover);
                pixels[y * width + x] = covered ? transitionColor : Color.clear;
            }
            ApplyTexture();
            yield return null;
        }
    }

    // ==========================================
    //  12. PIXELATE (Verpixeln)
    // ==========================================
    private IEnumerator Pixelate(bool reverse)
    {
        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / transitionDuration);

            // Blockgröße wächst von 1 bis zum halben Bildschirm, dann wird alles schwarz
            float halfP = reverse ? (1f - p) : p;

            if (halfP > 0.9f)
            {
                ClearPixels(reverse ? Color.clear : transitionColor);
            }
            else
            {
                int blockSize = Mathf.Max(1, (int)(halfP * 20));
                for (int by = 0; by < height; by += blockSize)
                for (int bx = 0; bx < width; bx += blockSize)
                {
                    // Je weiter fortgeschritten, desto mehr Blöcke werden schwarz
                    float chance = halfP * 1.2f;
                    Color c = (Random.value < chance) ? transitionColor : Color.clear;
                    for (int dy = 0; dy < blockSize && by + dy < height; dy++)
                    for (int dx = 0; dx < blockSize && bx + dx < width; dx++)
                        SetPixel(bx + dx, by + dy, c);
                }
            }
            ApplyTexture();
            yield return null;
        }
    }

    // ==========================================
    //  13. RAIN DRIP (Regen)
    // ==========================================
    private IEnumerator RainDrip(bool reverse)
    {
        // Jede Spalte hat eine eigene zufällige Geschwindigkeit
        float[] columnSpeed = new float[width];
        for (int x = 0; x < width; x++)
            columnSpeed[x] = Random.Range(0.5f, 1.5f);

        ClearPixels(reverse ? transitionColor : Color.clear);

        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / transitionDuration);

            for (int x = 0; x < width; x++)
            {
                float colP = Mathf.Clamp01(p * columnSpeed[x] * 1.5f);
                int fillHeight = (int)(colP * height);

                for (int y = 0; y < height; y++)
                {
                    bool covered;
                    if (reverse) covered = y < (height - fillHeight);
                    else covered = y >= (height - fillHeight);
                    pixels[y * width + x] = covered ? transitionColor : Color.clear;
                }
            }
            ApplyTexture();
            yield return null;
        }
    }

    // ==========================================
    //  14. SPIRAL
    // ==========================================
    private IEnumerator Spiral(bool reverse)
    {
        float cx = width * 0.5f, cy = height * 0.5f;
        float maxRadius = Mathf.Sqrt(width * width + height * height) * 0.5f;
        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / transitionDuration);

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                float angle = Mathf.Atan2(y - cy, x - cx) * Mathf.Rad2Deg;
                if (angle < 0) angle += 360f;

                float spiralThreshold = (dist / maxRadius) + (angle / 360f) * 0.3f;
                bool covered;
                if (reverse) covered = spiralThreshold > (1f - p) * 1.3f;
                else covered = spiralThreshold < p * 1.3f;
                pixels[y * width + x] = covered ? transitionColor : Color.clear;
            }
            ApplyTexture();
            yield return null;
        }
    }

    // ==========================================
    //  15. STAR WIPE (Stern)
    // ==========================================
    private IEnumerator StarWipe(bool reverse)
    {
        float cx = width * 0.5f, cy = height * 0.5f;
        float maxR = Mathf.Max(width, height) * 0.6f;
        int points = 5;
        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / transitionDuration);
            float radius = p * maxR;

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx);
                
                // Stern-Form: Radius variiert je nach Winkel
                float starAngle = angle * points;
                float starR = radius * (0.5f + 0.5f * Mathf.Cos(starAngle));

                bool covered;
                if (reverse) covered = dist > (maxR * (0.5f + 0.5f * Mathf.Cos(starAngle)) - radius + maxR * 0.5f);
                else covered = dist < starR;
                pixels[y * width + x] = covered ? transitionColor : Color.clear;
            }
            ApplyTexture();
            yield return null;
        }
    }

    // ==========================================
    //  16. HEART WIPE (Herz)
    // ==========================================
    private IEnumerator HeartWipe(bool reverse)
    {
        float cx = width * 0.5f, cy = height * 0.5f;
        float maxScale = 1.5f;
        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / transitionDuration);
            float scale = p * maxScale;

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                // Herzformel normalisiert
                float nx = (x - cx) / (width * 0.3f * Mathf.Max(0.01f, scale));
                float ny = -(y - cy) / (height * 0.3f * Mathf.Max(0.01f, scale));

                float heart = (nx * nx + ny * ny - 1f);
                heart = heart * heart * heart - nx * nx * ny * ny * ny;

                bool covered;
                if (reverse) covered = heart > 0;
                else covered = heart < 0;
                pixels[y * width + x] = covered ? transitionColor : Color.clear;
            }
            ApplyTexture();
            yield return null;
        }
    }

    // ==========================================
    //  17. TV STATIC (Rauschen)
    // ==========================================
    private IEnumerator TVStatic(bool reverse)
    {
        ClearPixels(reverse ? transitionColor : Color.clear);

        float elapsed = 0;
        int totalPixels = pixels.Length;
        int pixelsDone = 0;

        // Zufällige Reihenfolge
        List<int> indices = new List<int>();
        for (int i = 0; i < totalPixels; i++) indices.Add(i);
        for (int i = 0; i < indices.Count; i++)
        {
            int r = Random.Range(i, indices.Count);
            int tmp = indices[i]; indices[i] = indices[r]; indices[r] = tmp;
        }

        Color target = reverse ? Color.clear : transitionColor;
        while (pixelsDone < totalPixels)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / transitionDuration);
            int targetPx = Mathf.RoundToInt(p * totalPixels);

            // Plus: Zufälliges Flimmern auf noch-nicht-gefüllten Pixeln
            for (int i = targetPx; i < totalPixels && i < targetPx + 200; i++)
            {
                pixels[indices[i]] = Random.value > 0.5f ? transitionColor : Color.clear;
            }

            while (pixelsDone < targetPx && pixelsDone < totalPixels)
            {
                pixels[indices[pixelsDone]] = target;
                pixelsDone++;
            }
            ApplyTexture();
            yield return null;
        }

        // Am Ende alles sauber aufräumen
        ClearPixels(reverse ? Color.clear : transitionColor);
        ApplyTexture();
    }

    // ==========================================
    //  18. SLIDE LEFT
    // ==========================================
    private IEnumerator SlideLeft(bool reverse)
    {
        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / transitionDuration);
            if (reverse) p = 1f - p;
            int edge = (int)(p * width);

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                pixels[y * width + x] = (x < edge) ? transitionColor : Color.clear;

            ApplyTexture();
            yield return null;
        }
    }

    // ==========================================
    //  19. SLIDE RIGHT
    // ==========================================
    private IEnumerator SlideRight(bool reverse)
    {
        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / transitionDuration);
            if (reverse) p = 1f - p;
            int edge = width - (int)(p * width);

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                pixels[y * width + x] = (x >= edge) ? transitionColor : Color.clear;

            ApplyTexture();
            yield return null;
        }
    }

    // ==========================================
    //  20. SLIDE DOWN
    // ==========================================
    private IEnumerator SlideDown(bool reverse)
    {
        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / transitionDuration);
            if (reverse) p = 1f - p;
            int edge = height - (int)(p * height);

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                pixels[y * width + x] = (y <= edge) ? transitionColor : Color.clear;

            ApplyTexture();
            yield return null;
        }
    }

    // ==========================================
    //  21. DOUBLE DOOR (Doppeltür)
    // ==========================================
    private IEnumerator DoubleDoor(bool reverse)
    {
        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / transitionDuration);
            if (reverse) p = 1f - p;

            int hCover = (int)(p * width * 0.5f);
            int vCover = (int)(p * height * 0.5f);

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                bool covered = (x < hCover || x >= width - hCover) ||
                               (y < vCover || y >= height - vCover);
                pixels[y * width + x] = covered ? transitionColor : Color.clear;
            }
            ApplyTexture();
            yield return null;
        }
    }

    // ==========================================
    //  22. MATRIX RAIN
    // ==========================================
    private IEnumerator MatrixRain(bool reverse)
    {
        float[] columnDelay = new float[width];
        for (int x = 0; x < width; x++)
            columnDelay[x] = Random.Range(0f, 0.5f);

        ClearPixels(reverse ? transitionColor : Color.clear);

        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;

            for (int x = 0; x < width; x++)
            {
                float colP = Mathf.Clamp01((elapsed - columnDelay[x]) / (transitionDuration * 0.7f));
                int fillY = (int)(colP * height);

                for (int y = 0; y < height; y++)
                {
                    int fromTop = height - 1 - y;
                    bool covered;
                    if (reverse) covered = fromTop >= fillY;
                    else covered = fromTop < fillY;
                    pixels[y * width + x] = covered ? transitionColor : Color.clear;
                }
            }
            ApplyTexture();
            yield return null;
        }
    }

    // ==========================================
    //  23. WAVE WIPE (Welle)
    // ==========================================
    private IEnumerator WaveWipe(bool reverse)
    {
        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / transitionDuration);

            for (int y = 0; y < height; y++)
            {
                float wave = Mathf.Sin((float)y / height * Mathf.PI * 4f) * width * 0.1f;
                float threshold = p * (width + width * 0.2f) + wave;

                for (int x = 0; x < width; x++)
                {
                    bool covered;
                    if (reverse) covered = x > (width - threshold);
                    else covered = x < threshold;
                    pixels[y * width + x] = covered ? transitionColor : Color.clear;
                }
            }
            ApplyTexture();
            yield return null;
        }
    }

    // ==========================================
    //  24. ZIGZAG WIPE
    // ==========================================
    private IEnumerator ZigzagWipe(bool reverse)
    {
        int zigHeight = 15;
        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / transitionDuration);

            for (int y = 0; y < height; y++)
            {
                int zigOffset = ((y / zigHeight) % 2 == 0) ? (y % zigHeight) : (zigHeight - y % zigHeight);
                float zigNorm = (float)zigOffset / zigHeight;
                float threshold = p * (width + zigHeight * 2) + zigNorm * zigHeight * 2 - zigHeight * 2;

                for (int x = 0; x < width; x++)
                {
                    bool covered;
                    if (reverse) covered = x > (width - threshold);
                    else covered = x < threshold;
                    pixels[y * width + x] = covered ? transitionColor : Color.clear;
                }
            }
            ApplyTexture();
            yield return null;
        }
    }

    // ==========================================
    //  25. MOSAIC (Mosaik)
    // ==========================================
    private IEnumerator Mosaic(bool reverse)
    {
        int cellSize = 10;
        int cellsX = (width + cellSize - 1) / cellSize;
        int cellsY = (height + cellSize - 1) / cellSize;
        int totalCells = cellsX * cellsY;

        // Zufällige Reihenfolge der Zellen
        List<Vector2Int> cells = new List<Vector2Int>();
        for (int cy = 0; cy < cellsY; cy++)
        for (int cx = 0; cx < cellsX; cx++)
            cells.Add(new Vector2Int(cx, cy));

        for (int i = 0; i < cells.Count; i++)
        {
            int r = Random.Range(i, cells.Count);
            Vector2Int tmp = cells[i]; cells[i] = cells[r]; cells[r] = tmp;
        }

        ClearPixels(reverse ? transitionColor : Color.clear);
        Color target = reverse ? Color.clear : transitionColor;

        float elapsed = 0;
        int done = 0;
        while (done < totalCells)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / transitionDuration);
            int targetCells = Mathf.RoundToInt(p * totalCells);

            while (done < targetCells && done < totalCells)
            {
                FillCell(cells[done].x, cells[done].y, cellSize, target);
                done++;
            }
            ApplyTexture();
            yield return null;
        }
    }
}
