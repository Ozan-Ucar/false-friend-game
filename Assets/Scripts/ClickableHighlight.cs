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
    private float currentWidth;
    private float currentPulse;

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

        float targetWidth;
        float targetPulseAmount;

        if (isTriggered)
        {
            targetWidth = 0;
            targetPulseAmount = 0;
        }
        else
        {
            bool activeHover = isHovering && Application.isPlaying;
            targetWidth = activeHover ? hoverWidth : normalWidth;
            targetPulseAmount = activeHover ? 0.0f : 1.0f;
        }
        
        if (Application.isPlaying)
        {
            // Weicher Übergang (Lerp), damit es beim Loslassen sanft abschwillt
            currentWidth = Mathf.Lerp(currentWidth, targetWidth, Time.deltaTime * 10f);
            currentPulse = Mathf.Lerp(currentPulse, targetPulseAmount, Time.deltaTime * 10f);
        }
        else
        {
            currentWidth = targetWidth;
            currentPulse = targetPulseAmount;
        }

        propBlock.SetFloat("_OutlineWidth", currentWidth);
        propBlock.SetColor("_HighlightColor", highlightColor);
        propBlock.SetFloat("_PulseSpeed", pulseSpeed);
        propBlock.SetFloat("_PulseAmount", currentPulse);

        spriteRenderer.SetPropertyBlock(propBlock);
    }

    private void OnMouseEnter() { isHovering = true; }
    private void OnMouseExit() { isHovering = false; }
}
