using UnityEngine;

/// <summary>
/// Erzeugt automatisch eine weiche, pulsierende Leucht-Aura hinter dem Sprite.
/// Einfach auf den Diamanten ziehen - fertig!
/// </summary>
public class SpriteAura : MonoBehaviour
{
    [Header("Aura Settings")]
    [Tooltip("Farbe der Aura")]
    public Color auraColor = new Color(0.2f, 0.8f, 1f, 0.4f);
    
    [Tooltip("Wie viel groesser die Aura ist als das Sprite (1.5 = 50% groesser)")]
    public float auraScale = 1.5f;
    
    [Tooltip("Wie schnell die Aura pulsiert")]
    public float pulseSpeed = 2f;
    
    [Tooltip("Wie stark die Aura pulsiert (0 = gar nicht, 0.3 = leicht)")]
    public float pulseAmount = 0.15f;

    private SpriteRenderer auraRenderer;
    private float baseScale;

    void Start()
    {
        CreateAura();
    }

    void CreateAura()
    {
        // 1. Hole das Original-Sprite
        SpriteRenderer original = GetComponent<SpriteRenderer>();
        if (original == null || original.sprite == null) return;

        // 2. Erstelle ein Kind-Objekt fuer die Aura
        GameObject auraObj = new GameObject("_Aura");
        auraObj.transform.SetParent(transform, false);
        auraObj.transform.localPosition = Vector3.zero;
        auraObj.transform.localRotation = Quaternion.identity;
        
        baseScale = auraScale;
        auraObj.transform.localScale = Vector3.one * baseScale;

        // 3. Fuege einen SpriteRenderer hinzu
        auraRenderer = auraObj.AddComponent<SpriteRenderer>();
        auraRenderer.sprite = original.sprite;
        auraRenderer.color = auraColor;
        auraRenderer.material = new Material(Shader.Find("Sprites/Default"));
        
        // Additives Blending: Das Sprite wird zu reinem LICHT
        // Es verdeckt nichts, sondern leuchtet einfach durch
        auraRenderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        auraRenderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);

        // 4. Sorting: Aura HINTER dem Diamanten
        auraRenderer.sortingLayerName = original.sortingLayerName;
        auraRenderer.sortingOrder = original.sortingOrder - 1;
    }

    void Update()
    {
        if (auraRenderer == null) return;

        // Sanftes Pulsieren
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        auraRenderer.transform.localScale = Vector3.one * baseScale * pulse;
        
        // Farbe live updaten (damit man im Inspector spielen kann)
        auraRenderer.color = auraColor;
    }
}
