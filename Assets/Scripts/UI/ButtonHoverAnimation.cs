using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonHoverAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] float hoverScale    = 1.08f;
    [SerializeField] float pressScale    = 0.94f;
    [SerializeField] float animDuration  = 0.12f;

    Vector3 originalScale;
    Coroutine current;

    void Awake() => originalScale = transform.localScale;

    public void OnPointerEnter(PointerEventData _) => Animate(originalScale * hoverScale);
    public void OnPointerExit(PointerEventData _)  => Animate(originalScale);
    public void OnPointerDown(PointerEventData _)  => Animate(originalScale * pressScale);
    public void OnPointerUp(PointerEventData _)    => Animate(originalScale * hoverScale);

    void Animate(Vector3 target)
    {
        if (current != null) StopCoroutine(current);
        current = StartCoroutine(ScaleTo(target));
    }

    IEnumerator ScaleTo(Vector3 target)
    {
        Vector3 start = transform.localScale;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / animDuration;
            transform.localScale = Vector3.Lerp(start, target, EaseOut(t));
            yield return null;
        }

        transform.localScale = target;
    }

    static float EaseOut(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - (1f - t) * (1f - t);
    }
}
