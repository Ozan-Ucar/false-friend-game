using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class SimpleTextTrigger : MonoBehaviour
{
    [Header("Der Text")]
    [TextArea(2, 4)]
    public string messageText = "Hallo Welt!";
    
    [Header("Einstellungen")]
    [Tooltip("Haken AN = Text erscheint oben am Bildschirm (Typewriter).\nHaken AUS = Sprechblase über dem Spieler (Sofort).")]
    public bool useTopScreen = true;
    
    [Tooltip("Soll der Text nur ein einziges Mal angezeigt werden?")]
    public bool playOnce = true;
    
    [Tooltip("Wenn PlayOnce AUS ist: Wie viele Sekunden warten, bis der Trigger wieder auslösen darf?")]
    public float triggerCooldown = 2f;

    [Header("Top Screen Styling (nur wenn Top Screen an ist)")]
    [Tooltip("Lass dieses Feld leer, um die Standard-Schriftart zu nutzen.")]
    public TMPro.TMP_FontAsset topScreenFont;
    [Tooltip("Die Schriftgröße für den Text oben.")]
    public float topScreenFontSize = 40f;
    [Tooltip("Die Schriftfarbe.")]
    public Color topScreenColor = Color.white;
    [Tooltip("Die Farbe der Umrandung (Outline).")]
    public Color topScreenOutlineColor = Color.black;
    [Tooltip("Dicke der Umrandung. Setze auf 0, um die Outline komplett auszuschalten.")]
    public float topScreenOutlineWidth = 0.2f;
    [Tooltip("Wie viele Sekunden soll der Text sichtbar bleiben, nachdem er zu Ende getippt wurde?")]
    public float displayDuration = 3f;
    
    private bool hasPlayed = false;
    private float lastTriggerTime = -999f;

    private void Awake()
    {
        // Sicherstellen, dass es wirklich ein Trigger ist
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Wir reagieren nur auf den Spieler!
        if (collision.CompareTag("Player"))
        {
            if (playOnce && hasPlayed) return;
            if (!playOnce && Time.time < lastTriggerTime + triggerCooldown) return;

            hasPlayed = true;
            lastTriggerTime = Time.time;
            
            if (useTopScreen)
            {
                // Nutzt unser vollautomatisches Skript mit den neuen Styling-Parametern
                TopScreenMessage.Show(messageText, topScreenFont, topScreenFontSize, topScreenColor, displayDuration, topScreenOutlineWidth, topScreenOutlineColor);
            }
            else
            {
                // Sucht das Sprechblasen-Skript im Spieler
                SpeechBubble bubble = collision.GetComponentInChildren<SpeechBubble>();
                if (bubble != null)
                {
                    bubble.ShowText(messageText);
                }
                else
                {
                    Debug.LogError("Der Spieler hat kein SpeechBubble-Skript! Hast du vergessen, es auf den Player zu ziehen?");
                }
            }
        }
    }
}
