using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class PixelRainEffect : MonoBehaviour
{
    [Header("Rain Settings")]
    [Tooltip("Wie viele Tropfen pro Sekunde fallen?")]
    public float rainIntensity = 400f;
    
    [Tooltip("Wie schnell fallen die Tropfen nach unten?")]
    public float fallSpeed = 25f;
    
    [Tooltip("Windgeschwindigkeit (Positiv = nach rechts, Negativ = nach links)")]
    public float windSpeed = 3f;
    
    [Tooltip("Farbe und Transparenz der Tropfen (Alpha-Wert anpassen für Durchsichtigkeit!)")]
    public Color rainColor = new Color(0.8f, 0.9f, 1.0f, 0.4f);
    
    [Header("Drop Shape")]
    [Tooltip("Wie lang sollen die Regentropfen visuell gezogen werden?")]
    public float dropLength = 0.05f;
    
    [Tooltip("Wie breit/dick sollen die Tropfen sein?")]
    public float dropWidth = 0.05f;

    [Header("Spawning")]
    [Tooltip("Wie weit über der Kamera soll der Regen spawnen?")]
    public float spawnHeightAboveCamera = 15f;
    [Tooltip("Wie breit ist die Wolke? Sollte deutlich breiter als der Bildschirm sein, wegen dem Wind.")]
    public float spawnWidth = 50f;

    [Header("Rendering")]
    [Tooltip("Der Sorting Layer Name (z.B. 'Default', 'Foreground')")]
    public string sortingLayerName = "Default";
    [Tooltip("Die Order in Layer (höhere Zahl = weiter im Vordergrund)")]
    public int sortingOrder = 900;

    private ParticleSystem ps;
    private ParticleSystemRenderer psRenderer;
    private Material pixelMaterial;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        psRenderer = GetComponent<ParticleSystemRenderer>();

        // Erstelle eine 1x1 Pixel Textur, damit es echten Pixelart-Look hat (keine verschwommenen Ränder!)
        Texture2D pixelTex = new Texture2D(1, 1);
        pixelTex.SetPixel(0, 0, Color.white);
        pixelTex.filterMode = FilterMode.Point; // WICHTIG für Pixelart!
        pixelTex.Apply();

        // Erstelle ein Standard-Sprite-Material, das perfekt für knackscharfe Pixel ist
        pixelMaterial = new Material(Shader.Find("Sprites/Default"));
        pixelMaterial.mainTexture = pixelTex;

        // Renderer konfigurieren
        psRenderer.renderMode = ParticleSystemRenderMode.Stretch;
        psRenderer.material = pixelMaterial;
    }

    void Update()
    {
        ApplySettings();
        FollowCamera();
    }

    void ApplySettings()
    {
        if (ps == null) return;

        // 1. Main Module
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // Regen bleibt in der Welt, wenn die Kamera sich bewegt
        
        // Da wir VelocityOverLifetime nutzen, setzen wir StartSpeed auf 0
        main.startSpeed = 0f;
        main.startSize = dropWidth;
        main.startColor = rainColor;
        main.startLifetime = 3f; // Lang genug, um durchs Bild zu fallen
        main.maxParticles = 5000;
        
        main.useUnscaledTime = false;

        // 2. Emission
        var emission = ps.emission;
        emission.rateOverTime = rainIntensity;

        // 3. Shape (Wo der Regen entsteht)
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(spawnWidth, 1f, 1f);

        // 4. Velocity (Wind & Fallrichtung)
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = windSpeed;
        velocity.y = -fallSpeed; // Nach unten
        velocity.z = 0f;

        // 5. Renderer Scale (Wie lang die Tropfen visuell gezogen werden)
        psRenderer.lengthScale = 0f;
        // Wir nutzen den Velocity Scale! Da die Partikel schnell fallen, strecken sie sich entsprechend ihrer Geschwindigkeit.
        psRenderer.velocityScale = dropLength; 

        // 6. Sorting (damit man es live anpassen kann)
        psRenderer.sortingLayerName = sortingLayerName;
        psRenderer.sortingOrder = sortingOrder;
    }

    void FollowCamera()
    {
        if (Camera.main != null)
        {
            Vector3 camPos = Camera.main.transform.position;
            // Positioniere den Spawner über der Kamera, Z = 0 damit er in der 2D Welt sichtbar ist
            transform.position = new Vector3(camPos.x, camPos.y + spawnHeightAboveCamera, 0f);
        }
    }

    void OnDestroy()
    {
        // Aufräumen, um Memory Leaks zu vermeiden
        if (pixelMaterial != null)
        {
            if (pixelMaterial.mainTexture != null) Destroy(pixelMaterial.mainTexture);
            Destroy(pixelMaterial);
        }
    }
}
