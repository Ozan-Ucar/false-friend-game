using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Ein sehr schlaues Credits-Skript:
/// Berechnet automatisch die Länge deines Textes, scrollt butterweich von unten nach oben durch,
/// und fadet am Ende (oder beim Drücken von Space/Escape) automatisch ins Schwarz und lädt das Menü!
/// </summary>
public class CreditsManager : MonoBehaviour
{
    [System.Serializable]
    public class CreditEntry
    {
        [Tooltip("Z.B. 'Game Designer' oder 'Musik'")]
        public string role;
        
        [Tooltip("Z.B. 'Max Mustermann' (Du kannst auch mehrere Zeilen für mehrere Personen nutzen!)")]
        [TextArea(1, 3)]
        public string names;
    }

    [Header("Inhalte (Hier deine Credits eintragen!)")]
    [Tooltip("Liste aller Leute, die mitgearbeitet haben")]
    public List<CreditEntry> creditsList = new List<CreditEntry>();

    [Header("Text Design")]
    [Tooltip("Welche Schriftart soll benutzt werden? (Optional, nutzt sonst Standard)")]
    public TMPro.TMP_FontAsset fontAsset;
    
    [Tooltip("Größe und Farbe für die Überschriften (z.B. 'Sound Designer')")]
    public int roleFontSize = 40;
    public Color roleColor = new Color(1f, 0.85f, 0f); // Gold

    [Tooltip("Größe und Farbe für die Namen")]
    public int nameFontSize = 55;
    public Color nameColor = Color.white;

    [Header("Scroll Einstellungen")]
    [Tooltip("Das Parent-Objekt, in dem alle deine Texte liegen (sollte ContentSizeFitter & VerticalLayoutGroup haben).")]
    public RectTransform creditsContent; 
    
    [Tooltip("Wie schnell rollt der Text nach oben?")]
    public float scrollSpeed = 80f;

    [Tooltip("Wie lange soll gewartet werden, bevor die Credits losrollen?")]
    public float startDelay = 2f;
    
    [Tooltip("Wie lange der Bildschirm am Ende schwarz bleibt, bevor das Hauptmenü lädt")]
    public float endDelay = 1.5f;

    [Header("Ende & Skip")]
    [Tooltip("Die Szene, die nach den Credits geladen wird (z.B. MainMenu)")]
    public string nextSceneName = "MainMenu";
    
    [Tooltip("Darf der Spieler die Credits mit Leertaste/Escape überspringen?")]
    public bool allowSkip = true;

    [Header("UI & Fade")]
    [Tooltip("Ein schwarzes Vollbild-Image, das am Anfang aus- und am Ende einblendet.")]
    public Image fadeOverlay;
    
    [Tooltip("Wie lange dauert das Fade-In / Fade-Out?")]
    public float fadeDuration = 2f;

    private bool isScrolling = false;
    private bool isEnding = false;
    private float targetY;

    IEnumerator Start()
    {
        if (creditsContent == null)
        {
            Debug.LogError("CreditsManager: Du hast das 'Credits Content' Feld nicht zugewiesen!");
            yield break;
        }

        // --- AUTOMATISCHES LAYOUT SETUP ---
        // Zerstöre alle alten Texte im Container
        foreach (Transform child in creditsContent)
        {
            Destroy(child.gameObject);
        }

        // Zentriere das Credits-Objekt an sich
        creditsContent.anchorMin = new Vector2(0.5f, 0f); // Anchor am unteren Bildschirmrand
        creditsContent.anchorMax = new Vector2(0.5f, 0f);
        creditsContent.pivot = new Vector2(0.5f, 1f); // Pivot an der Oberkante des Textes
        creditsContent.sizeDelta = new Vector2(1200f, creditsContent.sizeDelta.y); // Feste Breite

        var csf = creditsContent.GetComponent<UnityEngine.UI.ContentSizeFitter>();
        if (csf == null) csf = creditsContent.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        csf.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
        
        // Vertikales Layout (sicherstellen, dass es an ist, falls es doch mehrere Kinder gibt)
        var vlg = creditsContent.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        if (vlg == null) vlg = creditsContent.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;

        // --- NEU: EIN EINZIGES TEXT-OBJEKT FÜR PERFEKTES MOVIE-FEELING ---
        // Anstatt 50 Objekte zu spawnen, machen wir EINEN Text mit Rich-Text (wie in HTML).
        // Das garantiert perfektes Aussehen ohne Überlappungen!
        GameObject textObj = new GameObject("All_Credits_Text");
        textObj.transform.SetParent(creditsContent, false);
        
        var allText = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        allText.alignment = TMPro.TextAlignmentOptions.Center;
        if (fontAsset != null) allText.font = fontAsset;

        // Rich-Text zusammenbauen
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        string hexRole = ColorUtility.ToHtmlStringRGB(roleColor);
        string hexName = ColorUtility.ToHtmlStringRGB(nameColor);

        // Oben ein bisschen Platz lassen
        sb.Append("\n\n");

        foreach (var entry in creditsList)
        {
            if (!string.IsNullOrEmpty(entry.role))
            {
                // Rolle (z.B. SOUND DESIGNER) in kleinerer Schrift und Farbe
                sb.Append($"<color=#{hexRole}><size={roleFontSize}>{entry.role.ToUpper()}</size></color>\n");
            }

            if (!string.IsNullOrEmpty(entry.names))
            {
                // Name in großer weißer Schrift, danach große Lücke
                sb.Append($"<color=#{hexName}><size={nameFontSize}>{entry.names}</size></color>\n\n\n\n");
            }
        }

        allText.text = sb.ToString();

        // Ein Frame warten, damit Unity die Höhe des Textes berechnen kann
        yield return null;

        // 1. Schlaues Setup: Berechne exakt die Höhe des Bildschirms und des Textes!
        Canvas canvas = GetComponentInParent<Canvas>();
        float screenHeight = canvas != null ? canvas.GetComponent<RectTransform>().rect.height : Screen.height;
        
        // Zwingt Unity, die Höhe des Textes sofort zu berechnen (falls ContentSizeFitter aktiv ist)
        LayoutRebuilder.ForceRebuildLayoutImmediate(creditsContent);
        
        // Starte exakt unterhalb des sichtbaren Bildschirms (da Pivot oben und Anchor unten ist, ist Y = 0 exakt unten drunter)
        creditsContent.anchoredPosition = new Vector2(creditsContent.anchoredPosition.x, 0f);
        
        // Ziel: Wenn das komplette Text-Feld (Höhe) + Bildschirmhöhe durchgescrollt ist
        targetY = creditsContent.rect.height + screenHeight + 100f; 

        // 2. Am Anfang ist der Bildschirm schwarz, fadet langsam auf
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            fadeOverlay.color = Color.black;
            yield return StartCoroutine(FadeImage(fadeOverlay, 1f, 0f, fadeDuration));
        }

        // 3. Kurz warten vor dem Start (dramatische Pause)
        yield return new WaitForSeconds(startDelay);
        isScrolling = true;
    }

    void Update()
    {
        if (isScrolling && !isEnding)
        {
            // Credits butterweich nach oben schieben
            creditsContent.anchoredPosition += Vector2.up * scrollSpeed * Time.deltaTime;

            // Prüfen ob wir das berechnete Ende erreicht haben
            if (creditsContent.anchoredPosition.y >= targetY)
            {
                EndCredits();
            }

            // Skip-Funktion mit dem neuen Input System
            if (allowSkip && Keyboard.current != null)
            {
                if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    EndCredits();
                }
            }
        }
    }

    public void EndCredits()
    {
        if (isEnding) return;
        isEnding = true;
        StartCoroutine(EndRoutine());
    }

    private IEnumerator EndRoutine()
    {
        // 1. Bildschirm langsam ins Schwarz faden
        if (fadeOverlay != null)
        {
            yield return StartCoroutine(FadeImage(fadeOverlay, 0f, 1f, fadeDuration));
        }

        // Noch ein kurzer Moment Stille für dramatischen Effekt
        yield return new WaitForSeconds(endDelay);

        // 2. Nächste Szene laden
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("CreditsManager: Keine Next Scene eingetragen!");
        }
    }

    private IEnumerator FadeImage(Image img, float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // SmoothStep für cinematischen, weichen Verlauf
            float smooth = t * t * (3f - 2f * t);
            
            img.color = new Color(0, 0, 0, Mathf.Lerp(startAlpha, endAlpha, smooth));
            yield return null;
        }
        img.color = new Color(0, 0, 0, endAlpha);
    }
}
