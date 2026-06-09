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

    public static void Show(string message, TMP_FontAsset customFont, float customFontSize, Color customColor, float displayDuration, float outlineWidth, Color outlineColor)
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("TopScreenMessageManager");
            go.AddComponent<TopScreenMessage>();
        }
        
        // Schriftart, Größe und Farbe anwenden
        if (customFont != null)
        {
            Instance.textMesh.font = customFont;
        }
        Instance.textMesh.fontSize = customFontSize;
        Instance.textMesh.color = customColor;

        // Outline anpassen
        if (Instance.textMesh.fontMaterial != null)
        {
            if (outlineWidth > 0f)
            {
                Instance.textMesh.fontMaterial.EnableKeyword("OUTLINE_ON");
                Instance.textMesh.outlineWidth = outlineWidth;
                Instance.textMesh.outlineColor = outlineColor;
            }
            else
            {
                Instance.textMesh.fontMaterial.DisableKeyword("OUTLINE_ON");
                Instance.textMesh.outlineWidth = 0f;
            }
        }

        if (Instance.currentCoroutine != null)
            Instance.StopCoroutine(Instance.currentCoroutine);
            
        Instance.currentCoroutine = Instance.StartCoroutine(Instance.TypeMessage(message, displayDuration));
    }

    private IEnumerator TypeMessage(string message, float displayDuration)
    {
        textMesh.text = "";
        canvasGroup.alpha = 1f;

        // Typewriter Effekt: Buchstabe für Buchstabe
        foreach (char c in message)
        {
            textMesh.text += c;
            yield return new WaitForSeconds(0.05f); // Tipp-Geschwindigkeit
        }

        // Warten (angepasste Dauer vom Trigger)
        yield return new WaitForSeconds(displayDuration);

        // Sanft ausblenden
        while (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= Time.deltaTime * 2f;
            yield return null;
        }
    }
}
