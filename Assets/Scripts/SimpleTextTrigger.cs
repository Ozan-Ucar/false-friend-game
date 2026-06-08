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
    
    private bool hasPlayed = false;

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

            hasPlayed = true;
            
            if (useTopScreen)
            {
                // Nutzt unser vollautomatisches Skript
                TopScreenMessage.Show(messageText);
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
