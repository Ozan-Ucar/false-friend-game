using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class ClickableHighlight : MonoBehaviour
{
    [Header("Highlight Settings")]
    public Color highlightColor = new Color(1f, 0.9f, 0f, 1f);
    [Range(0, 20)] public float normalWidth = 1.5f;
    [Range(0, 20)] public float hoverWidth = 10.0f;
    public float pulseSpeed = 3.0f;
    
    [Header("State")]
    public bool isTriggered = false; // Wenn wahr, wird das Highlight komplett deaktiviert

    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock propBlock;
    private bool isHovering = false;

    private void OnEnable()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        propBlock = new MaterialPropertyBlock();
        UpdateHighlight();
    }

    private void OnValidate()
    {
        UpdateHighlight();
    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            UpdateHighlight();
        }
    }

    public void UpdateHighlight()
    {
        if (spriteRenderer == null) return;
        if (propBlock == null) propBlock = new MaterialPropertyBlock();

        spriteRenderer.GetPropertyBlock(propBlock);

        // Wenn ausgelöst, Breite auf 0 setzen
        float target;
        float targetPulse;

        bool isJuicy = JuiceManager.Instance == null || JuiceManager.Instance.isJuicy;
        if (isTriggered || !isJuicy)
        {
            target = 0;
            targetPulse = 0;
        }
        else
        {
            bool activeHover = isHovering && Application.isPlaying;
            target = activeHover ? hoverWidth : normalWidth;
            targetPulse = activeHover ? 0.0f : 1.0f;
        }
        
        propBlock.SetFloat("_OutlineWidth", target);
        propBlock.SetColor("_HighlightColor", highlightColor);
        propBlock.SetFloat("_PulseSpeed", pulseSpeed);
        propBlock.SetFloat("_PulseAmount", targetPulse);

        spriteRenderer.SetPropertyBlock(propBlock);
    }

    private void OnMouseEnter() { isHovering = true; }
    private void OnMouseExit() { isHovering = false; }
}
