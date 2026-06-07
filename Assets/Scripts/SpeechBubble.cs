using UnityEngine;
using TMPro;
using System.Collections;

public class SpeechBubble : MonoBehaviour
{
    [Tooltip("Ziehe hier das TextMeshPro-Objekt rein, das in der Sprechblase liegt")]
    public TextMeshProUGUI bubbleText;
    
    [Tooltip("Ziehe hier das übergeordnete Canvas oder Panel der Sprechblase rein")]
    public GameObject bubbleGraphic;
    
    [Tooltip("Wie lange die Sprechblase sichtbar bleibt")]
    public float displayTime = 3f;

    [Header("Animation")]
    [Tooltip("Wie schnell die Blase wächst und schrumpft")]
    public float animationSpeed = 6f;
    public AnimationCurve popCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Follow Settings")]
    [Tooltip("Wie weich die Blase folgt (kleiner = weicher, ca. 10 ist gut)")]
    public float followSpeed = 10f;
    [Tooltip("Wie hoch über dem Spieler soll die Blase schweben?")]
    public Vector3 offset = new Vector3(0, 1.5f, 0);

    private Transform bubbleCanvas;
    private Coroutine currentRoutine;
    private Vector3 originalScale = Vector3.one;

    void Start()
    {
        // Verhindert den "New Text" Platzhalter
        if (bubbleText != null)
        {
            bubbleText.text = "";
            bubbleText.gameObject.SetActive(false);
        }

        if (bubbleGraphic != null)
        {
            // Falls der Nutzer vergessen hat, den Text ins Bild zu ziehen, reparieren wir das heimlich!
            if (bubbleText != null && bubbleText.transform.parent != bubbleGraphic.transform)
            {
                bubbleText.transform.SetParent(bubbleGraphic.transform, true);
            }

            originalScale = bubbleGraphic.transform.localScale;
            bubbleGraphic.transform.localScale = Vector3.zero;
            bubbleGraphic.SetActive(false);

            // Wir suchen uns automatisch das übergeordnete Canvas
            bubbleCanvas = bubbleGraphic.transform.parent;

            // Wir lösen das Canvas vom Spieler, damit es sich frei und weich bewegen kann
            if (bubbleCanvas != null)
            {
                bubbleCanvas.SetParent(null);
            }
        }
    }

    void LateUpdate()
    {
        if (bubbleCanvas != null)
        {
            Vector3 targetPos = transform.position + offset;
            
            // Wenn unsichtbar: Direkt anheften, damit sie beim Aufpoppen sofort am richtigen Ort ist
            if (bubbleGraphic != null && !bubbleGraphic.activeSelf)
            {
                bubbleCanvas.position = targetPos;
            }
            else
            {
                // Wenn sichtbar: Schön weich hinterherziehen!
                bubbleCanvas.position = Vector3.Lerp(bubbleCanvas.position, targetPos, Time.deltaTime * followSpeed);
            }
        }
    }

    public void ShowText(string text)
    {
        if (bubbleGraphic == null || bubbleText == null)
        {
            Debug.LogError("SpeechBubble: Du hast vergessen, den Text oder die Grafik im Inspector zuzuweisen!");
            return;
        }

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);
            
        currentRoutine = StartCoroutine(ShowRoutine(text));
    }

    private IEnumerator ShowRoutine(string text)
    {
        bubbleText.gameObject.SetActive(true);
        bubbleText.text = text;
        bubbleGraphic.SetActive(true);

        // 1. Aufpoppen (Scale 0 -> 1)
        float progress = 0f;
        while (progress < 1f)
        {
            progress += Time.deltaTime * animationSpeed;
            float scale = popCurve.Evaluate(progress);
            bubbleGraphic.transform.localScale = originalScale * scale;
            yield return null;
        }
        bubbleGraphic.transform.localScale = originalScale;

        // 2. Warten, bis der Text gelesen wurde
        yield return new WaitForSeconds(displayTime);

        // 3. Zupoppen (Scale 1 -> 0)
        progress = 0f;
        while (progress < 1f)
        {
            progress += Time.deltaTime * animationSpeed;
            // Kurve rückwärts abspielen!
            float scale = popCurve.Evaluate(1f - progress);
            bubbleGraphic.transform.localScale = originalScale * scale;
            yield return null;
        }

        bubbleGraphic.transform.localScale = Vector3.zero;
        bubbleGraphic.SetActive(false);
        bubbleText.text = "";
        bubbleText.gameObject.SetActive(false);
    }
}
