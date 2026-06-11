using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class GodRay2D : MonoBehaviour
{
    public enum RayStyle { Forest, DungeonGrate, DesertSun }

    [Header("Stil")]
    [Tooltip("Forest = weich & wolkig, Dungeon = staubige Lichtkegel, DesertSun = gleißender Sonnenstrahl")]
    public RayStyle style = RayStyle.Forest;

    [Header("Aussehen")]
    [Tooltip("Die Farbe und Helligkeit des Lichtstrahls")]
    public Color rayColor = new Color(1f, 0.95f, 0.6f, 0.25f);
    public float width = 5f;
    public float length = 15f;

    [Header("Positionierung")]
    [Tooltip("Der Winkel des Sonnenstrahls (z.B. -45 für schräg von oben rechts)")]
    public float angle = -30f;

    [Header("Form (Cone / Kegel)")]
    [Range(0f, 1f)]
    [Tooltip("Wie schmal ist der Strahl oben an der Lichtquelle? (1 = Rechteck, 0.1 = extremer Kegel)")]
    public float topWidthPercent = 0.2f;
    [Range(0f, 0.5f)]
    [Tooltip("Wie weich sind die Außenkanten des Strahls?")]
    public float edgeSoftness = 0.15f;

    [Header("Strahlen-Muster")]
    [Tooltip("Forest: Anzahl der Äste / Dungeon: Anzahl der Gitterstäbe")]
    public float stripeDensity = 10f;
    [Tooltip("Wie scharf abgegrenzt sind die einzelnen Strahlen?")]
    public float stripeContrast = 3.0f;

    [Header("Animation")]
    [Tooltip("Soll sich der Strahl im Wind wiegen?")]
    public bool animateSway = true;
    public float swayAmount = 1.5f;
    public float swaySpeed = 0.3f;
    
    [Tooltip("Soll der Strahl sanft auf- und abblenden?")]
    public bool animateFade = true;
    public float fadeSpeed = 0.5f;
    [Tooltip("Tiefster Alpha-Wert beim Ausblenden (0 bis 1)")]
    public float minAlpha = 0.1f;
    [Tooltip("Höchster Alpha-Wert beim Einblenden (0 bis 1)")]
    public float maxAlpha = 0.5f;

    private SpriteRenderer sr;
    private float initialAngle;
    private float randomOffset;

    // Für Live-Updates im Play-Mode
    private RayStyle lastStyle;
    private float lastStripeDensity;
    private float lastStripeContrast;
    private float lastTopWidth;
    private float lastEdgeSoftness;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        randomOffset = Random.Range(0f, 100f);

        lastStyle = style;
        lastStripeDensity = stripeDensity;
        lastStripeContrast = stripeContrast;
        lastTopWidth = topWidthPercent;
        lastEdgeSoftness = edgeSoftness;

        GenerateRay();
    }

    void GenerateRay()
    {
        int texSize = 1024; // Ultra-HD Auflösung gegen Pixeligkeit
        Texture2D tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < texSize; y++)
        {
            float normY = (float)y / (texSize - 1); // 0 = Unten (breit), 1 = Oben (schmal)
            
            // Helligkeitsverlauf von Oben (1) nach Unten (0)
            float alphaY = Mathf.Lerp(0f, 1f, normY);
            alphaY = alphaY * alphaY; // Weicherer Verlauf
            
            // Kegel-Form (Cone) berechnen
            float currentWidthPercent = Mathf.Lerp(1f, topWidthPercent, normY);
            float currentRadius = (texSize / 2f) * currentWidthPercent;
            float center = texSize / 2f;

            for (int x = 0; x < texSize; x++)
            {
                float distFromCenter = x - center;
                
                // Wenn wir komplett außerhalb des Kegels sind -> unsichtbar
                if (Mathf.Abs(distFromCenter) > currentRadius)
                {
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0f));
                    continue;
                }

                // X in einen Wert von 0 bis 1 umrechnen (entlang der aktuellen Breite)
                float normalizedX = (currentRadius == 0) ? 0 : distFromCenter / currentRadius;
                float u = (normalizedX + 1f) * 0.5f;

                float intensity = 1f;

                if (style == RayStyle.Forest)
                {
                    // Forest: Organisches Perlin Noise
                    float n = Mathf.PerlinNoise(u * stripeDensity + randomOffset, randomOffset);
                    intensity = Mathf.Clamp01((n - 0.3f) * stripeContrast);
                }
                else if (style == RayStyle.DungeonGrate)
                {
                    // Dungeon: Natürlicheres, staubiges Licht (nicht mehr perfekt künstlich!)
                    // Wir mixen dicke und dünne Streifen durch zwei Noise-Layer
                    float n1 = Mathf.PerlinNoise(u * stripeDensity + randomOffset, randomOffset);
                    float n2 = Mathf.PerlinNoise(u * (stripeDensity * 2.5f) - randomOffset, randomOffset);
                    
                    float combinedNoise = (n1 * 0.7f) + (n2 * 0.3f);
                    intensity = Mathf.Clamp01((combinedNoise - 0.35f) * stripeContrast); 
                }
                else if (style == RayStyle.DesertSun)
                {
                    // Desert Sun: Gleißende Wüstensonne. Ein massiver, heller Kern, der sanft ausfadet.
                    // Wir ignorieren hier die Streifen und fokussieren uns auf einen extrem hellen Center-Glow
                    float coreGlow = 1f - Mathf.Pow(Mathf.Abs(normalizedX), 1.5f); // Hell in der Mitte, fällt zur Seite ab
                    
                    // Sehr feines, weiches Flimmern für heißen Staub in der Luft
                    float n = Mathf.PerlinNoise(u * stripeDensity + randomOffset, normY * 5f + randomOffset);
                    float heatDust = Mathf.Lerp(0.8f, 1.2f, n);
                    
                    // Contrast lässt uns steuern, wie "scharf" der Strahl ist
                    intensity = Mathf.Clamp01(coreGlow * heatDust * stripeContrast);
                }

                // Weiche Außenkanten des Strahls (Edge Blend)
                float edgeMask = 1f;
                float edgePixels = texSize * edgeSoftness;
                float distFromEdge = currentRadius - Mathf.Abs(distFromCenter);
                if (distFromEdge < edgePixels)
                {
                    edgeMask = distFromEdge / Mathf.Max(1f, edgePixels);
                }

                float finalAlpha = alphaY * intensity * edgeMask;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, finalAlpha));
            }
        }
        tex.Apply();

        // Pivot (0.5, 1.0) bedeutet, es hängt von oben herab.
        Sprite newSprite = Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 1.0f), texSize);
        sr.sprite = newSprite;
        sr.color = rayColor;

        transform.localScale = new Vector3(width, length, 1f);
    }

    void Update()
    {
        // 1. Live-Update für Größe
        transform.localScale = new Vector3(width, length, 1f);

        // 2. Live-Update für Textur
        if (style != lastStyle || 
            stripeDensity != lastStripeDensity || 
            stripeContrast != lastStripeContrast || 
            topWidthPercent != lastTopWidth ||
            edgeSoftness != lastEdgeSoftness)
        {
            lastStyle = style;
            lastStripeDensity = stripeDensity;
            lastStripeContrast = stripeContrast;
            lastTopWidth = topWidthPercent;
            lastEdgeSoftness = edgeSoftness;
            
            if (sr.sprite != null)
            {
                Destroy(sr.sprite.texture);
                Destroy(sr.sprite);
            }
            GenerateRay();
        }

        // 3. Sway Animation & Winkel
        if (animateSway)
        {
            float angleOffset = Mathf.Sin((Time.time + randomOffset) * swaySpeed) * swayAmount;
            transform.rotation = Quaternion.Euler(0, 0, angle + angleOffset);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        // 4. Fade Animation & Farb-Update
        if (animateFade)
        {
            float pulse = (Mathf.Sin((Time.time + randomOffset) * fadeSpeed) + 1f) * 0.5f; 
            float currentAlpha = Mathf.Lerp(minAlpha, maxAlpha, pulse);
            
            Color c = rayColor;
            c.a = currentAlpha;
            sr.color = c;
        }
        else
        {
            sr.color = rayColor;
        }
    }
}
