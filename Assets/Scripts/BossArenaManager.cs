using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class BossArenaManager : MonoBehaviour
{
    public static BossArenaManager Instance;

    [Header("Phasen-Einstellungen")]
    [Tooltip("Wie viele Hebel müssen in Phase 1 gezogen werden?")]
    public int leversPhase1 = 3;
    [Tooltip("Wie viele Hebel müssen in Phase 2 gezogen werden?")]
    public int leversPhase2 = 4;

    [Header("Plattformen")]
    [Tooltip("Ziehe ALLE MovingPlatforms der Arena hier rein")]
    public MovingPlatform[] allPlatforms;
    
    [Header("Runen-Plattform (Phase 3)")]
    [Tooltip("Die spezielle Plattform mit der Rune")]
    public MovingPlatform runePlatform;
    [Tooltip("Wie lange die Runen-Plattform eingefroren wird (Sekunden)")]
    public float freezeDuration = 3f;

    [Header("Kappa Boss")]
    [Tooltip("Das Kappa-Sprite im Hintergrund (für Schaden-Animation)")]
    public SpriteRenderer kappaBossSprite;

    [Header("UI")]
    [Tooltip("Ein TextMeshPro-Feld für Phasen-Ankündigungen (optional)")]
    public TextMeshProUGUI phaseText;

    [Header("Debug (Zum Testen im Play-Modus)")]
    [Tooltip("Trage hier eine Phase (1, 2 oder 3) ein...")]
    [Range(1, 3)]
    public int debugPhase = 1;
    [Tooltip("...und setze dann diesen Haken, um sofort in diese Phase zu wechseln!")]
    public bool forcePhaseChange = false;

    [Header("Start Einstellungen")]
    [Tooltip("Soll der Kampf direkt beim Laden der Szene starten? (Wenn false, musst du 'BeginBossFight()' z.B. per DialogTrigger aufrufen)")]
    public bool startAutomatically = false;

    // Zustand
    private int currentPhase = 0;
    private int leversActivated = 0;
    private int leversNeeded = 0;
    private bool fightStarted = false;
    private bool fightEnded = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this); // Nur das Skript zerstören, nicht das ganze Objekt!
    }

    void Start()
    {
        if (startAutomatically)
        {
            StartPhase(1);
        }
    }

    public void BeginBossFight()
    {
        if (!fightStarted)
        {
            StartPhase(1);
        }
    }

    void Update()
    {
        // Erlaubt das manuelle Umschalten der Phase im Inspector!
        if (forcePhaseChange)
        {
            forcePhaseChange = false;
            StartPhase(debugPhase);
        }
    }

    // ==========================================
    //  PHASEN-SYSTEM
    // ==========================================

    public void StartPhase(int phase)
    {
        currentPhase = phase;
        leversActivated = 0;
        fightStarted = true;

        switch (phase)
        {
            case 1:
                leversNeeded = leversPhase1;
                break;
            case 2:
                leversNeeded = leversPhase2;
                break;
            case 3:
                leversNeeded = 0; // Phase 3 hat keine Hebel, nur die Rune!
                break;
        }

        // Alle Plattformen über die neue Phase informieren
        foreach (var platform in allPlatforms)
        {
            if (platform != null)
                platform.SetPhase(phase);
        }

        // Runen-Plattform nur in Phase 3 aktivieren
        if (runePlatform != null)
        {
            runePlatform.gameObject.SetActive(phase == 3);
            if (phase == 3) runePlatform.SetPhase(3);
        }

        // Phasen-Ankündigung anzeigen
        StartCoroutine(ShowPhaseAnnouncement(phase));
    }

    // ==========================================
    //  HEBEL WURDE GEZOGEN (vom BossLever aufgerufen)
    // ==========================================

    public void OnLeverActivated()
    {
        if (fightEnded) return;

        leversActivated++;

        // Kappa bekommt Schaden! (Visuelles Feedback)
        StartCoroutine(BossHitEffect());

        // Prüfen, ob genug Hebel für den Phasenwechsel gezogen wurden
        if (leversActivated >= leversNeeded)
        {
            if (currentPhase == 1)
            {
                StartCoroutine(TransitionToPhase(2));
            }
            else if (currentPhase == 2)
            {
                StartCoroutine(TransitionToPhase(3));
            }
        }
    }

    // ==========================================
    //  RUNEN-PLATTFORM EINFRIEREN (vom BossLever aufgerufen)
    // ==========================================

    public void FreezeRunePlatform()
    {
        if (runePlatform != null && currentPhase == 3)
        {
            runePlatform.Freeze(freezeDuration);
        }
    }

    // ==========================================
    //  RUNE AUFGEHOBEN -> BOSS BESIEGT!
    // ==========================================

    public void OnRuneCollected()
    {
        if (fightEnded) return;
        fightEnded = true;

        StartCoroutine(BossDefeated());
    }

    // ==========================================
    //  ANIMATIONEN & EFFEKTE
    // ==========================================

    private IEnumerator ShowPhaseAnnouncement(int phase)
    {
        if (phaseText == null) yield break;

        string text = "";
        switch (phase)
        {
            case 1: text = "PHASE 1"; break;
            case 2: text = "PHASE 2 - SCHNELLER!"; break;
            case 3: text = "FINALE PHASE - SCHNAPPT EUCH DIE RUNE!"; break;
        }

        phaseText.text = text;
        phaseText.gameObject.SetActive(true);
        phaseText.alpha = 0f;

        // Einblenden
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            phaseText.alpha = t;
            phaseText.fontSize = 50 + t * 20f;
            yield return null;
        }

        yield return new WaitForSeconds(1.5f);

        // Ausblenden
        t = 1f;
        while (t > 0f)
        {
            t -= Time.deltaTime * 2f;
            phaseText.alpha = t;
            yield return null;
        }

        phaseText.gameObject.SetActive(false);
    }

    private IEnumerator TransitionToPhase(int nextPhase)
    {
        // Kurze dramatische Pause
        if (phaseText != null)
        {
            phaseText.text = "...";
            phaseText.gameObject.SetActive(true);
            phaseText.alpha = 1f;
        }

        yield return new WaitForSeconds(1.5f);

        if (phaseText != null)
            phaseText.gameObject.SetActive(false);

        StartPhase(nextPhase);
    }

    private IEnumerator BossHitEffect()
    {
        if (kappaBossSprite == null) yield break;

        // Rot blinken bei Schaden (3x schnell)
        for (int i = 0; i < 3; i++)
        {
            kappaBossSprite.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            kappaBossSprite.color = Color.white;
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator BossDefeated()
    {
        // Alle Plattformen einfrieren
        foreach (var platform in allPlatforms)
        {
            if (platform != null && platform.gameObject.activeSelf)
                platform.Freeze(999f);
        }

        if (runePlatform != null)
            runePlatform.Freeze(999f);

        // Boss blinkt und verschwindet
        if (kappaBossSprite != null)
        {
            for (int i = 0; i < 10; i++)
            {
                kappaBossSprite.color = new Color(1, 1, 1, 0);
                yield return new WaitForSeconds(0.15f);
                kappaBossSprite.color = Color.white;
                yield return new WaitForSeconds(0.15f);
            }
            kappaBossSprite.gameObject.SetActive(false);
        }

        // Sieges-Text
        if (phaseText != null)
        {
            phaseText.text = "KAPPA BESIEGT!";
            phaseText.gameObject.SetActive(true);
            phaseText.alpha = 1f;
            phaseText.color = new Color(1f, 0.85f, 0f); // Gold!
            phaseText.fontSize = 72;
        }

        yield return new WaitForSeconds(4f);

        // Sucht den Spieler und startet seine Exit-Animation (die lädt dann die Cutscene!)
        PortalTransition pt = FindAnyObjectByType<PortalTransition>();
        if (pt != null)
        {
            pt.TriggerExit();
        }
        else
        {
            Debug.LogWarning("Boss besiegt, aber kein PortalTransition Skript in der Szene gefunden!");
        }
    }

    // ==========================================
    //  GETTER
    // ==========================================

    public int GetCurrentPhase() { return currentPhase; }
    public bool IsFightActive() { return fightStarted && !fightEnded; }
}
