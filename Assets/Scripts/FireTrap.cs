using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class FireTrap : MonoBehaviour
{
    public enum TrapLevel { Level1, Level2, Level3 }

    [Header("Trap Settings")]
    [Tooltip("Wähle das Level der Falle. Die entsprechende Animation wird automatisch genutzt.")]
    public TrapLevel trapLevel = TrapLevel.Level1;

    [Header("Animations-Namen")]
    public string animLevel1 = "FireTrapAnim";
    public string animLevel2 = "MidFireTrapAnim";
    public string animLevel3 = "HighFireTrapAnim";

    private string CurrentAnimationName
    {
        get
        {
            switch (trapLevel)
            {
                case TrapLevel.Level2: return animLevel2;
                case TrapLevel.Level3: return animLevel3;
                default: return animLevel1;
            }
        }
    }

    [Header("Damage Settings")]
    public int damage = 1;
    [Tooltip("Der Collider, der bestimmt, wo das Feuer Schaden macht. Am besten ein separates leeres Child-Objekt mit einem BoxCollider2D (Is Trigger) erstellen.")]
    public Collider2D damageCollider;
    
    [Header("Timing")]
    [Tooltip("Zeit (in Sekunden) nach dem Klick, bis die Animation startet")]
    public float preFireDelay = 0f;
    [Tooltip("Zeit (in Sekunden) NACH Animationsstart, bis das Feuer den Collider aktiviert (Schaden macht)")]
    public float damageStartDelay = 0.2f;
    [Tooltip("Wie lange das Feuer brennt und Schaden macht")]
    public float damageDuration = 1.0f;
    [Tooltip("Wie oft der Spieler Schaden nimmt, wenn er im Feuer stehen bleibt (in Sekunden)")]
    public float damageTickRate = 0.5f;
    [Tooltip("Zeit (in Sekunden) NACH Abschluss der Falle, bis sie wieder klickbar (passiv) wird")]
    public float cooldown = 1.0f;

    private Animator anim;
    private bool isFiring = false;
    private bool isDamageActive = false;
    private List<Collider2D> clickColliders = new List<Collider2D>();
    private float damageTimer = 0f;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        
        // Wir suchen alle Collider auf diesem Objekt (falls du mehrere BoxCollider für die Form hast)
        Collider2D[] allColliders = GetComponents<Collider2D>();
        
        foreach (Collider2D col in allColliders)
        {
            // Wir packen ALLE Collider in unsere Klick-Liste, die NICHT das Feuer sind
            if (col != damageCollider)
            {
                clickColliders.Add(col);
            }
        }

        // Fallback, falls irgendwie gar keiner gefunden wurde
        if (clickColliders.Count == 0) 
        {
            Collider2D first = GetComponent<Collider2D>();
            if (first != null) clickColliders.Add(first);
        }

        if (damageCollider != null)
        {
            damageCollider.enabled = false; // Zur Sicherheit am Anfang deaktivieren
        }
        else
        {
            Debug.LogWarning("FireTrap: Es wurde kein DamageCollider zugewiesen! Bitte im Inspector zuweisen.");
        }
    }

    private void Start()
    {
        if (anim != null)
        {
            anim.speed = 0;
            anim.Play(CurrentAnimationName, 0, 0f);
        }
    }

    private void Update()
    {
        // 1. Klick abfragen
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            
            // Prüfen, ob wir IRGENDEINEN der Klick-Collider getroffen haben
            bool clickedOnTrap = false;
            foreach (Collider2D col in clickColliders)
            {
                if (col != null && col.OverlapPoint(mousePos))
                {
                    clickedOnTrap = true;
                    break; // Sobald einer getroffen wurde, reicht uns das!
                }
            }

            if (clickedOnTrap)
            {
                if (!isFiring)
                {
                    StartCoroutine(FireSequence());
                }
                else
                {
                    Debug.Log("Klick ignoriert: Die Falle " + gameObject.name + " feuert gerade noch oder ist im Cooldown!");
                }
            }
        }

        // 2. Kontinuierlich auf Schaden prüfen, wenn das Feuer aktiv ist
        if (isDamageActive && damageCollider != null)
        {
            // Finde alle Collider, die sich im Feuer befinden
            ContactFilter2D filter = new ContactFilter2D();
            filter.NoFilter();
            List<Collider2D> results = new List<Collider2D>();
            damageCollider.Overlap(filter, results);

            bool playerInFire = false;

            foreach (Collider2D col in results)
            {
                if (col.CompareTag("Player"))
                {
                    playerInFire = true;
                    
                    // Sofortiger Schaden, wenn der Timer abgelaufen ist oder man neu ins Feuer läuft
                    if (damageTimer <= 0f)
                    {
                        HealthSystem health = col.GetComponent<HealthSystem>();
                        if (health != null) health.TakeDamage(damage);
                        
                        damageTimer = damageTickRate; // Timer für Folge-Schaden (Cooldown) setzen
                    }
                    break;
                }
            }

            // Cooldown-Timer aktualisieren
            if (damageTimer > 0)
            {
                damageTimer -= Time.deltaTime;
            }
            
            // Wenn der Spieler aus dem Feuer geht, setze den Timer sofort zurück, 
            // damit er bei erneutem Betreten direkt wieder Schaden bekommt
            if (!playerInFire && damageTimer > 0)
            {
                damageTimer = 0f; 
            }
        }
    }

    private IEnumerator FireSequence()
    {
        isFiring = true;

        if (preFireDelay > 0) yield return new WaitForSeconds(preFireDelay);

        ClickableHighlight highlight = GetComponent<ClickableHighlight>();
        if (highlight != null) highlight.isTriggered = true;

        float animLength = 0f;

        // 1. Animation starten
        if (anim != null)
        {
            anim.speed = 1;
            anim.Play(CurrentAnimationName, 0, 0f);
        }

        // WICHTIG: Einen Frame warten! Ansonsten gibt Unity die Länge der alten/vorherigen Animation zurück
        yield return null; 

        if (anim != null)
        {
            animLength = anim.GetCurrentAnimatorStateInfo(0).length;
        }

        float totalTime = Mathf.Max(animLength, damageStartDelay + damageDuration);
        
        float timeElapsed = 0f;
        bool damageStarted = false;
        bool damageEnded = false;
        bool animEnded = false;

        // Eine durchgängige Stoppuhr, die beides unabhängig voneinander regelt
        while (timeElapsed < totalTime)
        {
            timeElapsed += Time.deltaTime;

            // ANIMATION EXAKT BEENDEN: Sobald die exakte Animationslänge erreicht ist,
            // setzen wir sie auf Frame 0 zurück, EGAL wie lange der Schaden noch läuft!
            if (!animEnded && timeElapsed >= animLength)
            {
                if (anim != null)
                {
                    anim.Play(CurrentAnimationName, 0, 0f);
                    anim.speed = 0;
                }
                animEnded = true;
            }

            // Schaden AKTIVIEREN, wenn der Delay erreicht ist
            if (!damageStarted && timeElapsed >= damageStartDelay)
            {
                if (damageCollider != null) damageCollider.enabled = true;
                isDamageActive = true;
                damageTimer = 0f;
                damageStarted = true;
            }

            // Schaden DEAKTIVIEREN, wenn die Zeit abgelaufen ist
            if (damageStarted && !damageEnded && timeElapsed >= (damageStartDelay + damageDuration))
            {
                if (damageCollider != null) damageCollider.enabled = false;
                isDamageActive = false;
                damageEnded = true;
            }

            yield return null;
        }

        // Sicherheitshalber nochmal Schaden ausmachen
        if (damageCollider != null) damageCollider.enabled = false;
        isDamageActive = false;

        // Falls die Schleife aus irgendeinem Grund abbrach, Animation sicher zurücksetzen
        if (!animEnded && anim != null)
        {
            anim.Play(CurrentAnimationName, 0, 0f);
            anim.speed = 0;
        }

        // Cooldown abwarten, bevor die Falle wieder passiv wird
        if (cooldown > 0) yield return new WaitForSeconds(cooldown);

        isFiring = false;
        if (highlight != null) highlight.isTriggered = false;
    }

    // Für Testzwecke: Zeigt im Scene-Fenster farbig an, wann der DamageCollider aktiv ist
    private void OnDrawGizmos()
    {
        if (damageCollider != null)
        {
            // Rot, wenn Schaden gerade an ist. Durchsichtiges Rot, wenn er aus ist.
            Gizmos.color = (Application.isPlaying && isDamageActive) ? new Color(1f, 0f, 0f, 0.7f) : new Color(1f, 0f, 0f, 0.2f);
            
            // Zeichnet eine Box basierend auf dem Collider
            Gizmos.DrawCube(damageCollider.bounds.center, damageCollider.bounds.size);
        }
    }
}
