using UnityEngine;
using UnityEngine.InputSystem;

public class BossLever : MonoBehaviour
{
    [Header("Einstellungen")]
    [Tooltip("In welcher Phase ist dieser Hebel aktiv?")]
    public int activeInPhase = 1;
    
    [Tooltip("Ist das ein Freeze-Hebel für die Runen-Plattform in Phase 3?")]
    public bool isFreezeLever = false;

    [Tooltip("Kann der Hebel mehrfach benutzt werden? (Nur für Freeze-Hebel sinnvoll)")]
    public bool reusable = false;
    [Tooltip("Cooldown in Sekunden bis der Hebel erneut benutzt werden kann")]
    public float reuseCooldown = 5f;

    [Header("Visuals")]
    [Tooltip("Sprite für den Hebel im Ruhezustand")]
    public Sprite leverOff;
    [Tooltip("Sprite für den Hebel wenn gezogen")]
    public Sprite leverOn;

    private bool isUsed = false;
    private bool isPlayerNear = false;
    private SpriteRenderer sr;
    private float cooldownTimer = 0f;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null && leverOff != null) sr.sprite = leverOff;
    }

    void Update()
    {
        // Cooldown für wiederverwendbare Hebel
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f && reusable)
            {
                isUsed = false;
                if (sr != null && leverOff != null) sr.sprite = leverOff;
            }
        }

        if (!isPlayerNear || isUsed) return;

        // Nur in der richtigen Phase funktionieren!
        if (BossArenaManager.Instance != null && 
            BossArenaManager.Instance.GetCurrentPhase() != activeInPhase) return;

        // E drücken um den Hebel zu ziehen
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            ActivateLever();
        }
    }

    private void ActivateLever()
    {
        isUsed = true;

        // Sprite wechseln
        if (sr != null && leverOn != null) sr.sprite = leverOn;

        if (BossArenaManager.Instance == null) return;

        if (isFreezeLever)
        {
            // Phase 3: Runen-Plattform einfrieren!
            BossArenaManager.Instance.FreezeRunePlatform();
            
            if (reusable)
            {
                cooldownTimer = reuseCooldown;
            }
        }
        else
        {
            // Phase 1 & 2: Normaler Schaden am Boss
            BossArenaManager.Instance.OnLeverActivated();
        }
    }

    // ==========================================
    //  TRIGGER-ZONE (Spieler muss nah dran sein)
    // ==========================================

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            isPlayerNear = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            isPlayerNear = false;
    }
}
