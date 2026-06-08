using UnityEngine;
using TMPro;
using System.Collections;

public class TopScreenMessage : MonoBehaviour
{
    public static TopScreenMessage Instance;
    
    private TextMeshProUGUI textMesh;
    private CanvasGroup canvasGroup;
    private Coroutine currentCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Vollautomatisches Canvas erstellen
        GameObject canvasObj = new GameObject("TopScreenCanvas");
        canvasObj.transform.SetParent(transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // Ganz nach vorne über alles drüber
        
        canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        GameObject textObj = new GameObject("MessageText");
        textObj.transform.SetParent(canvasObj.transform, false);
        textMesh = textObj.AddComponent<TextMeshProUGUI>();
        
        // Schrift-Styling
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontSize = 40;
        textMesh.color = Color.white;
        
        // Retro Outline
        if (textMesh.fontMaterial != null)
        {
            textMesh.fontMaterial.EnableKeyword("OUTLINE_ON");
            textMesh.outlineWidth = 0.2f;
            textMesh.outlineColor = Color.black;
        }

        // Ganz oben am Bildschirm verankern
        RectTransform rect = textMesh.rectTransform;
        rect.anchorMin = new Vector2(0, 1); // Oben Links
        rect.anchorMax = new Vector2(1, 1); // Oben Rechts
        rect.pivot = new Vector2(0.5f, 1f); // Oben Mitte
        rect.anchoredPosition = new Vector2(0, -60f); // 60 Pixel vom oberen Rand entfernt
        rect.sizeDelta = new Vector2(0, 100f);
    }

    public static void Show(string message)
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("TopScreenMessageManager");
            go.AddComponent<TopScreenMessage>();
        }
        
        if (Instance.currentCoroutine != null)
            Instance.StopCoroutine(Instance.currentCoroutine);
            
        Instance.currentCoroutine = Instance.StartCoroutine(Instance.TypeMessage(message));
    }

    private IEnumerator TypeMessage(string message)
    {
        textMesh.text = "";
        canvasGroup.alpha = 1f;

        // Typewriter Effekt: Buchstabe für Buchstabe
        foreach (char c in message)
        {
            textMesh.text += c;
            yield return new WaitForSeconds(0.05f); // Tipp-Geschwindigkeit
        }

        // Warten, damit man es in Ruhe lesen kann (dynamisch je nach Textlänge)
        float waitTime = Mathf.Max(2f, message.Length * 0.08f);
        yield return new WaitForSeconds(waitTime);

        // Sanft ausblenden
        while (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= Time.deltaTime * 2f;
            yield return null;
        }
    }
}
