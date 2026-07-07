using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider2D))]
public class DialogTrigger : MonoBehaviour
{
    [Header("Dialog Content")]
    [Tooltip("Die Sätze, die in diesem Dialog gesprochen werden.")]
    public List<DialogLine> dialogLines = new List<DialogLine>();

    [Header("Settings")]
    [Tooltip("Soll dieser Dialog nur ein einziges Mal abgespielt werden? (Wird beim Tod/Neustart wieder resettet)")]
    public bool playOnce = true;

    [Tooltip("Soll DAUERHAFT gespeichert werden, dass er gespielt wurde? (Bleibt auch nach dem Tod oder Spiel-Neustart weg!)")]
    public bool saveProgress = false;
    
    [Tooltip("Eine einmalige ID für diesen Dialog (z.B. 'Start_Wald'). Wenn du es leer lässt, generiert das Skript automatisch eine ID!")]
    public string uniqueID = "";

    [Header("Events (Optional)")]
    [Tooltip("Was soll passieren, NACHDEM der Dialog komplett zu Ende gelesen wurde? (Zieht hier z.B. den BossArenaManager rein und wählt 'BeginBossFight'!)")]
    public UnityEngine.Events.UnityEvent onDialogFinished;

    private bool hasPlayed = false;
    private bool eventFired = false;
    
    // STATIC bedeutet: Speichert sich Werte über alle Tode und Levelwechsel hinweg, 
    // aber leert sich komplett, sobald das Spiel ausgemacht wird!
    private static System.Collections.Generic.HashSet<string> sessionDialogsDone = new System.Collections.Generic.HashSet<string>();

    private void Awake()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null) col.isTrigger = true;

        // Automatische ID generieren, falls der Nutzer keine eingetragen hat
        if (string.IsNullOrEmpty(uniqueID))
        {
            uniqueID = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "_" + gameObject.name;
        }

        // Prüfen, ob dieser Dialog in dieser SPIELSITZUNG schon markiert wurde
        if (saveProgress && sessionDialogsDone.Contains(uniqueID))
        {
            hasPlayed = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Wenn schon gespielt (egal ob nur für diese Runde oder dauerhaft gespeichert)
            if (playOnce && hasPlayed)
            {
                // SEHR WICHTIG: Wenn der Dialog (z.B. nach dem Tod) übersprungen wird,
                // MUSS das Event (z.B. Bosskampf Start) trotzdem direkt ausgeführt werden!
                if (!eventFired)
                {
                    eventFired = true;
                    onDialogFinished?.Invoke();
                }
                return;
            }

            if (DialogManager.Instance != null && !DialogManager.Instance.IsDialogActive)
            {
                hasPlayed = true;
                
                // Für diese Spielsitzung speichern!
                if (saveProgress)
                {
                    sessionDialogsDone.Add(uniqueID);
                }

                // Startet den Dialog und führt das Event am ENDE des Dialogs aus
                DialogManager.Instance.StartDialog(dialogLines, () => {
                    if (!eventFired)
                    {
                        eventFired = true;
                        onDialogFinished?.Invoke();
                    }
                });
            }
        }
    }
}
