using UnityEngine;

/// <summary>
/// Erzeugt einen Lichtschein, der vom Bildschirmrand hereinscheint,
/// als wäre direkt hinter dem Rand eine offene Tür mit hellem Licht.
/// Platziere das Objekt genau am linken oder rechten Rand des Levels.
/// </summary>
public class LevelPortal : MonoBehaviour
{
    public enum PortalType { Entrance, Exit }

    [Header("Portal-Typ")]
    [Tooltip("Entrance = Licht kommt von LINKS rein, Exit = Licht kommt von RECHTS rein")]
    public PortalType portalType = PortalType.Entrance;

    [Header("Farben")]
    public Color portalColor = new Color(0.3f, 0.8f, 1f, 0.4f);
    [Tooltip("Automatische Farbwahl: Cyan für Entrance, Gold für Exit")]
    public bool useAutoColor = false;

    [Header("Licht-Größe")]
    [Tooltip("Wie schmal der Lichtspalt an der Tür ist (Startwert am Rand). Je näher an Door Height, desto weniger Kegel.")]
    public float sourceHeight = 4f;
    [Tooltip("Wie hoch das Licht wird, wenn es sich ausbreitet (Endwert)")]
    public float doorHeight = 7f;
    [Tooltip("Wie weit das Licht in die Szene hineinreicht")]
    public float lightReach = 8f;

    [Header("Strahlen")]
    [Tooltip("Wie viele einzelne Lichtstrahlen von der Tür ausgehen")]
    public int rayCount = 5;
    [Tooltip("Wie breit sich die Strahlen auffächern (Grad). Kleiner = subtiler.")]
    public float fanSpread = 15f;

    [Header("Partikel")]
    public int particleAmount = 60;

    [Header("Animation")]
    public float pulseSpeed = 1.2f;
    public float minGlow = 0.4f;
    public float maxGlow = 1.0f;

    [Header("Sorting")]
    public int sortingOrder = 5;

    private SpriteRenderer edgeGlowRenderer;
    private SpriteRenderer[] rayRenderers;
    private ParticleSystem portalParticles;
    private float randomOffset;

    // Tracking für Live-Updates
    private float lastSourceHeight;
    private float lastDoorHeight;
    private float lastLightReach;
    private float lastFanSpread;
    private int lastRayCount;

    private static readonly Color entranceColor = new Color(0.4f, 0.75f, 1f, 0.45f);
    private static readonly Color exitColor = new Color(1f, 0.85f, 0.35f, 0.45f);

    void Start()
    {
        randomOffset = Random.Range(0f, 100f);

        if (useAutoColor)
            portalColor = (portalType == PortalType.Entrance) ? entranceColor : exitColor;

        lastSourceHeight = sourceHeight;
        lastDoorHeight = doorHeight;
        lastLightReach = lightReach;
        lastFanSpread = fanSpread;
        lastRayCount = rayCount;

        CreateEdgeGlow();
        CreateRays();
        CreateParticles();
    }

    /// <summary>
    /// Ein heller, vertikaler Lichtstreifen direkt am Rand (die "Türspalte")
    /// </summary>
    void CreateEdgeGlow()
    {
        GameObject obj = new GameObject("EdgeGlow");
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = Vector3.zero;

        edgeGlowRenderer = obj.AddComponent<SpriteRenderer>();
        edgeGlowRenderer.sortingOrder = sortingOrder + 1;

        // KEGEL-FORM: Links (Tür) schmal, rechts (ins Level) breit
        int texW = 128;
        int texH = 128;
        Texture2D tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        // Verhältnis: Wie schmal ist die Quelle im Vergleich zur vollen Höhe?
        float sourceRatio = Mathf.Clamp01(sourceHeight / Mathf.Max(doorHeight, 0.01f));

        // ASYMMETRISCHER KEGEL: Unten gerade, nur oben fächert sich auf!
        // Unterer Rand ist fix: bei allen X-Positionen gleich
        // Oberer Rand: Bei x=0 (Tür) = sourceHeight, bei x=1 (weit) = doorHeight
        float bottomBound = 0.5f - sourceRatio * 0.5f; // Feste Unterkante

        for (int y = 0; y < texH; y++)
        {
            float ny = (float)y / (texH - 1); // 0 = unten, 1 = oben

            for (int x = 0; x < texW; x++)
            {
                float nx = (float)x / (texW - 1); // 0 = Tür (Rand), 1 = weit weg

                // Oberer Rand expandiert von sourceRatio nach 1.0
                float topBound = Mathf.Lerp(0.5f + sourceRatio * 0.5f, 1.0f, nx);

                // Weicher Übergang am oberen und unteren Rand
                float softness = 0.05f;
                float insideBottom = Mathf.SmoothStep(0f, 1f, (ny - bottomBound + softness) / (softness * 2f));
                float insideTop = Mathf.SmoothStep(0f, 1f, (topBound - ny + softness) / (softness * 2f));
                float insideCone = insideBottom * insideTop;

                // Horizontal: Tür hell, nach rechts ausfadend
                float alphaH = 1f - nx;
                alphaH = Mathf.Pow(alphaH, 1.2f);

                float finalAlpha = insideCone * alphaH;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, finalAlpha));
            }
        }
        tex.Apply();

        // Pivot links-mitte, damit der Glow genau vom Rand ausgeht
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, texW, texH), new Vector2(0f, 0.5f), texH);
        edgeGlowRenderer.sprite = sprite;
        edgeGlowRenderer.color = portalColor;

        float direction = (portalType == PortalType.Entrance) ? 1f : -1f;
        obj.transform.localScale = new Vector3(lightReach * direction, doorHeight, 1f);
    }

    /// <summary>
    /// Fächerförmige Lichtstrahlen, die von der Türspalte nach innen scheinen
    /// </summary>
    void CreateRays()
    {
        rayRenderers = new SpriteRenderer[rayCount];
        float direction = (portalType == PortalType.Entrance) ? 1f : -1f;

        for (int i = 0; i < rayCount; i++)
        {
            GameObject rayObj = new GameObject("DoorRay_" + i);
            rayObj.transform.SetParent(transform, false);
            rayObj.transform.localPosition = Vector3.zero;

            // Strahlen fächern sich auf
            float t = (rayCount <= 1) ? 0.5f : (float)i / (rayCount - 1);
            float angle = Mathf.Lerp(-fanSpread, fanSpread, t);

            // Bei Exit: Strahlen zeigen nach links (180° gedreht)
            if (portalType == PortalType.Exit)
                angle = 180f + angle;

            rayObj.transform.localRotation = Quaternion.Euler(0, 0, angle);

            SpriteRenderer sr = rayObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortingOrder;

            // Strahl-Textur: von links (hell) nach rechts (transparent), mit weichen Rändern
            int texW = 128;
            int texH = 16;
            Texture2D tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            float seed = Random.Range(0f, 50f);

            for (int y = 0; y < texH; y++)
            {
                float ny = (float)y / (texH - 1);
                // Vertikal: Ränder weich
                float alphaV = 1f - Mathf.Abs(ny - 0.5f) * 2f;
                alphaV = Mathf.Pow(alphaV, 1.2f);

                for (int x = 0; x < texW; x++)
                {
                    float nx = (float)x / (texW - 1);
                    // Horizontal: Links hell, nach rechts ausfadend
                    float alphaH = 1f - nx;
                    alphaH = Mathf.Pow(alphaH, 2f);

                    // Etwas Noise für natürliche Variation
                    float noise = Mathf.PerlinNoise(nx * 5f + seed, ny * 2f + seed);
                    noise = Mathf.Lerp(0.7f, 1f, noise);

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alphaV * alphaH * noise));
                }
            }
            tex.Apply();

            // Pivot links-mitte: Strahl geht vom Rand nach rechts raus
            Sprite raySprite = Sprite.Create(tex, new Rect(0, 0, texW, texH), new Vector2(0f, 0.5f), texH);
            sr.sprite = raySprite;

            Color rayCol = portalColor;
            rayCol.a = portalColor.a * Random.Range(0.4f, 0.9f);
            sr.color = rayCol;

            float rayLength = lightReach * Random.Range(0.7f, 1.1f);
            float rayThickness = Random.Range(0.5f, 1.2f);
            rayObj.transform.localScale = new Vector3(rayLength, rayThickness, 1f);

            rayRenderers[i] = sr;
        }
    }

    /// <summary>
    /// Schwebende Staubpartikel im Lichtschein
    /// </summary>
    void CreateParticles()
    {
        GameObject particleObj = new GameObject("DoorLightParticles");
        particleObj.transform.SetParent(transform, false);

        float direction = (portalType == PortalType.Entrance) ? 1f : -1f;
        particleObj.transform.localPosition = new Vector3(lightReach * 0.3f * direction, 0, 0);

        portalParticles = particleObj.AddComponent<ParticleSystem>();
        portalParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = portalParticles.main;
        main.duration = 5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
        main.startColor = new Color(portalColor.r, portalColor.g, portalColor.b, 0.5f);
        main.maxParticles = particleAmount;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = portalParticles.emission;
        emission.rateOverTime = particleAmount / 3f;

        var shape = portalParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(lightReach * 0.5f, doorHeight * 0.7f, 0.1f);

        var velocity = portalParticles.velocityOverLifetime;
        velocity.enabled = true;
        // Partikel treiben langsam in Lichtrichtung
        velocity.x = new ParticleSystem.MinMaxCurve(0.05f * direction, 0.2f * direction);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.05f, 0.08f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var colorOverLifetime = portalParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.2f),
                new GradientAlphaKey(0.8f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        var renderer = portalParticles.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = sortingOrder + 2;
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.mainTexture = GenerateSoftCircle();
        renderer.material = mat;

        portalParticles.Play();
    }

    Texture2D GenerateSoftCircle()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float center = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = 1f - Mathf.Clamp01(dist / (center));
                alpha *= alpha;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        tex.Apply();
        return tex;
    }

    void Update()
    {
        if (useAutoColor)
            portalColor = (portalType == PortalType.Entrance) ? entranceColor : exitColor;

        // === LIVE-REBUILD: Kegel-Form neu zeichnen wenn sich Größe ändert ===
        bool shapeChanged = (sourceHeight != lastSourceHeight || doorHeight != lastDoorHeight || lightReach != lastLightReach);
        bool raysChanged = (fanSpread != lastFanSpread || rayCount != lastRayCount);

        if (shapeChanged)
        {
            lastSourceHeight = sourceHeight;
            lastDoorHeight = doorHeight;
            lastLightReach = lightReach;

            // Edge Glow Textur neu bauen
            if (edgeGlowRenderer != null && edgeGlowRenderer.sprite != null)
            {
                Destroy(edgeGlowRenderer.sprite.texture);
                Destroy(edgeGlowRenderer.sprite);
                Destroy(edgeGlowRenderer.gameObject);
            }
            CreateEdgeGlow();
        }

        if (shapeChanged || raysChanged)
        {
            lastFanSpread = fanSpread;
            lastRayCount = rayCount;

            // Alte Strahlen aufräumen
            if (rayRenderers != null)
            {
                foreach (var sr in rayRenderers)
                {
                    if (sr != null)
                    {
                        if (sr.sprite != null)
                        {
                            Destroy(sr.sprite.texture);
                            Destroy(sr.sprite);
                        }
                        Destroy(sr.gameObject);
                    }
                }
            }
            CreateRays();
        }

        // === LIVE-UPDATE: Partikel-Farbe ===
        if (portalParticles != null)
        {
            var main = portalParticles.main;
            main.startColor = new Color(portalColor.r, portalColor.g, portalColor.b, 0.5f);
        }

        // === ANIMATION ===
        float dir = (portalType == PortalType.Entrance) ? 1f : -1f;

        // Pulsierender Edge-Glow
        if (edgeGlowRenderer != null)
        {
            float pulse = (Mathf.Sin((Time.time + randomOffset) * pulseSpeed) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(minGlow, maxGlow, pulse);

            Color c = portalColor;
            c.a = alpha * portalColor.a * 1.5f;
            edgeGlowRenderer.color = c;

            // Leichtes "Atmen" der Lichtbreite
            float breathe = 1f + Mathf.Sin((Time.time + randomOffset) * pulseSpeed * 0.8f) * 0.06f;
            edgeGlowRenderer.transform.localScale = new Vector3(lightReach * dir * breathe, doorHeight * breathe, 1f);
        }

        // Strahlen pulsieren versetzt + Farbe live
        if (rayRenderers != null)
        {
            for (int i = 0; i < rayRenderers.Length; i++)
            {
                if (rayRenderers[i] == null) continue;
                float offset = i * 1.5f;
                float pulse = (Mathf.Sin((Time.time + randomOffset + offset) * pulseSpeed * 0.5f) + 1f) * 0.5f;
                float alpha = Mathf.Lerp(minGlow * 0.3f, portalColor.a, pulse);

                Color c = portalColor;
                c.a = alpha;
                rayRenderers[i].color = c;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = (portalType == PortalType.Entrance)
            ? new Color(0.3f, 0.7f, 1f, 0.4f)
            : new Color(1f, 0.85f, 0.3f, 0.4f);

        float dir = (portalType == PortalType.Entrance) ? 1f : -1f;

        // Trapez-Form: Unten gerade, nur oben geht auf
        float bottomY = -sourceHeight * 0.5f;
        Vector3 topLeft = transform.position + new Vector3(0, sourceHeight * 0.5f, 0);
        Vector3 bottomLeft = transform.position + new Vector3(0, bottomY, 0);
        Vector3 topRight = transform.position + new Vector3(lightReach * dir, doorHeight * 0.5f, 0);
        Vector3 bottomRight = transform.position + new Vector3(lightReach * dir, bottomY, 0);

        Gizmos.DrawLine(topLeft, topRight);       // Obere Kante (schräg)
        Gizmos.DrawLine(bottomLeft, bottomRight); // Untere Kante (gerade!)
        Gizmos.DrawLine(topRight, bottomRight);

        // Tür-Linie am Rand
        Gizmos.color = Color.white;
        Gizmos.DrawLine(topLeft, bottomLeft);
    }
}
