using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class InstantKillTrigger : MonoBehaviour
{
    [Header("Instant Kill Einstellungen")]
    [Tooltip("Soll der Trigger den Spieler auch töten, wenn er eigentlich im God Mode ist? (z.B. für bodenlose Abgründe)")]
    public bool bypassGodMode = false;
    
    [Tooltip("Soll der starke Kamera-Zoom beim Tod hier deaktiviert werden?")]
    public bool disableDeathZoom = true;

    [Tooltip("Wie schnell soll die Szene neustarten? (Standard beim normalen Tod ist meistens 3 Sekunden)")]
    public float customRestartDelay = 0.5f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryKill(collision.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryKill(collision.gameObject);
    }

    private void TryKill(GameObject target)
    {
        if (target.CompareTag("Player"))
        {
            HealthSystem health = target.GetComponent<HealthSystem>();
            if (health != null)
            {
                if (bypassGodMode)
                {
                    // Schaltet den God Mode kurz aus, damit der Spieler sicher stirbt
                    health.godMode = false; 
                }
                
                // Ruft die neue InstantKill Funktion auf, die Unverwundbarkeit (I-Frames) ignoriert
                health.InstantKill(disableDeathZoom, customRestartDelay);
            }
        }
    }
}
