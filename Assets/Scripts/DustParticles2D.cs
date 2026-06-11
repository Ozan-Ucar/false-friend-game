using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class DustParticles2D : MonoBehaviour
{
    [Header("Bereich & Dichte")]
    [Tooltip("Wie groß soll die Box sein, in der Staub entsteht?")]
    public Vector2 spawnArea = new Vector2(10f, 10f);
    [Tooltip("Wie viele Staubkörner sollen gleichzeitig maximal existieren?")]
    public int maxParticles = 100;
    [Tooltip("Wie viele Staubkörner pro Sekunde gespawnt werden sollen (Höher = dichter)")]
    public float emissionRate = 20f;

    [Header("Form der Partikel")]
    [Tooltip("Lass es leer für einen schönen runden, weichen Lichtpunkt. Oder ziehe deine eigene Textur (z.B. ein Blatt oder Stern) rein!")]
    public Texture2D customTexture;
    public float particleSizeMin = 0.02f;
    public float particleSizeMax = 0.08f;
    
    [Header("Farbe & Verhalten")]
    public Color dustColor = new Color(1f, 1f, 1f, 0.6f);
    public float lifeTimeMin = 3f;
    public float lifeTimeMax = 7f;

    private ParticleSystem ps;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        SetupParticles();
    }

    void SetupParticles()
    {
        // System sicher stoppen bevor wir es ändern
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifeTimeMin, lifeTimeMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(particleSizeMin, particleSizeMax);
        main.startColor = dustColor;
        main.maxParticles = maxParticles;
        // WICHTIG: World Space bedeutet, du kannst das Objekt bewegen und die Partikel bleiben in der Luft hängen
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = emissionRate;

        // Form = Rechteck (damit der Staub im ganzen Raum ist)
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(spawnArea.x, spawnArea.y, 1f);

        // Sanftes Schweben
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.05f, 0.1f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        // Sanftes Auf- und Abblenden (Opacity)
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;

        // --- RENDERER & MATERIAL (Die Form bestimmen) ---
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 10; // Steht eher im Vordergrund
        
        Material mat = new Material(Shader.Find("Sprites/Default"));
        if (customTexture != null)
        {
            // Nutze die vom User reingezogene Textur
            mat.mainTexture = customTexture;
        }
        else
        {
            // Mache automatisch eine super schöne runde Staub-Kugel
            mat.mainTexture = GenerateSoftCircle();
        }
        renderer.material = mat;

        ps.Play();
    }

    // Generiert per Code einen schönen weichen, leuchtenden, runden Punkt
    Texture2D GenerateSoftCircle()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        
        float center = size / 2f;
        float radius = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                // Je weiter weg vom Zentrum, desto transparenter
                float alpha = 1f - Mathf.Clamp01(dist / radius);
                
                // Wir quadrieren den Alpha-Wert, damit die Ränder weicher ausfaden
                alpha = alpha * alpha; 
                
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return tex;
    }

    // Damit du im Editor siehst, wo der Staub spawnt
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 1, 0, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(spawnArea.x, spawnArea.y, 1f));
    }
}
