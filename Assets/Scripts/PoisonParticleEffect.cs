using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class PoisonParticleEffect : MonoBehaviour
{
    [Header("Farbe & Transparenz")]
    public Color cloudColor = new Color(0.2f, 1f, 0.2f, 0.2f);
    public Color endColor = new Color(0f, 0.5f, 0f, 0f);

    [Header("Größe & Lebenszeit")]
    public float minSize = 0.8f;
    public float maxSize = 1.5f;
    public float minLifetime = 1.5f;
    public float maxLifetime = 2.5f;

    [Header("Emission & Form")]
    public int emissionRate = 35;
    public float cloudRadius = 0.8f;

    [Header("Bewegung (Wabern)")]
    public float noiseStrength = 0.15f;
    public float noiseFrequency = 0.5f;

    [Header("Optik & Textur")]
    public Sprite cloudSprite; // Zieh hier dein scharfes Kreis-Sprite rein!
    public int sortingOrder = 5;

    [Header("Testen")]
    public bool replayEffect;

    void Awake() { SetupParticles(); }
    void Update() { if (replayEffect) { replayEffect = false; SetupParticles(); } }
    void OnValidate() { if (Application.isPlaying) SetupParticles(); }

    public void SetupParticles()
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        var main = ps.main;
        main.duration = 0.5f; // Alles passiert in einer halben Sekunde
        main.loop = false;    // Nur einmal abspielen
        main.startLifetime = new ParticleSystem.MinMaxCurve(minLifetime, maxLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = cloudColor; 
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0; // Kein dauerhafter Fluss
        // Starker Burst am Anfang
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, (short)emissionRate) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = cloudRadius;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(cloudColor, 0.0f), new GradientColorKey(cloudColor, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0, 0.0f), new GradientAlphaKey(cloudColor.a, 0.2f), new GradientAlphaKey(0, 1.0f) }
        );
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0.2f); curve.AddKey(0.2f, 1f); curve.AddKey(1f, 0.8f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = noiseStrength;
        noise.frequency = noiseFrequency;

        // === TEXTURE / SPRITE LOGIK ===
        var textureSheet = ps.textureSheetAnimation;
        if (cloudSprite != null)
        {
            textureSheet.enabled = true;
            textureSheet.mode = ParticleSystemAnimationMode.Sprites;
            // Falls das Array leer ist oder wir ein neues Sprite setzen wollen:
            if (textureSheet.spriteCount == 0 || textureSheet.GetSprite(0) != cloudSprite)
            {
                textureSheet.SetSprite(0, cloudSprite);
            }
        }
        else
        {
            textureSheet.enabled = false;
        }

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = sortingOrder;

        Shader particleShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit");
        if (particleShader == null) particleShader = Shader.Find("Sprites/Default");
        renderer.material = new Material(particleShader);

        #if UNITY_EDITOR
        if (cloudSprite == null)
        {
            Texture2D circleTexture = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Texture2D>("UI/Skin/Knob.psd");
            if (circleTexture != null) renderer.material.mainTexture = circleTexture;
        }
        #endif

        ps.Play();
    }
}
