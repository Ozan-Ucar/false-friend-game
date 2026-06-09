using UnityEngine;

public enum MovePattern
{
    BackAndForth,   // Hin und her (Richtung frei wählbar!)
    VerticalBob,    // Auf und ab (klassisch)
    Circular,       // Kreisbewegung
    SineWave,       // Horizontale Welle mit vertikalem Wobble
    Figure8,        // Achter-Form
    Erratic,        // Unberechenbar (Phase 3 Runen-Plattform!)
    Waypoints       // Folgt platzierten Punkten (für designed Levels!)
}

public class MovingPlatform : MonoBehaviour
{
    [Header("Bewegungs-Pattern")]
    public MovePattern pattern = MovePattern.BackAndForth;

    [Header("Geschwindigkeit & Größe")]
    [Tooltip("Grundgeschwindigkeit der Plattform")]
    public float baseSpeed = 2f;
    [Tooltip("Wie weit sich die Plattform bewegt (für BackAndForth/SineWave)")]
    public float moveDistance = 3f;
    [Tooltip("Nur für Circular/Figure8: Radius der Kreisbewegung")]
    public float radius = 2f;

    [Header("Waypoints (nur für Pattern 'Waypoints')")]
    [Tooltip("Erstelle leere GameObjects als Kinder und ziehe sie hier rein. Die Plattform fährt von Punkt zu Punkt!")]
    public Transform[] waypoints;
    [Tooltip("Wie lange die Plattform an jedem Punkt wartet (in Sekunden)")]
    public float waitAtPoint = 0.5f;
    [Tooltip("Soll die Plattform am Ende zurück zum Anfang fahren? (Loop)")]
    public bool loopWaypoints = true;

    [Header("Phasen-Aktivierung")]
    [Tooltip("In welchen Phasen ist diese Plattform aktiv?")]
    public bool activeInPhase1 = true;
    public bool activeInPhase2 = true;
    public bool activeInPhase3 = true;

    [Header("Phasen-Positionen")]
    [Tooltip("Wo die Plattform in den jeweiligen Phasen sein soll")]
    public Vector3 posPhase1;
    public Vector3 posPhase2;
    public Vector3 posPhase3;

    [Header("Phasen-Geschwindigkeiten")]
    [Tooltip("Geschwindigkeitsmultiplikator pro Phase")]
    public float phase1SpeedMult = 1.0f;
    public float phase2SpeedMult = 1.4f;
    public float phase3SpeedMult = 1.8f;

    // Internes
    private Vector3 currentCenter;
    private float timeOffset;
    private float currentSpeedMult = 1f;
    private bool isFrozen = false;
    private float frozenTimer = 0f;
    private SpriteRenderer sr;
    private Coroutine transitionRoutine;

    // Für Erratic-Pattern
    private Vector3 erraticTarget;
    private float erraticChangeTimer;
    private float erraticBoundsX = 5f;
    private float erraticBoundsY = 4f;

    // Für Waypoints
    private int currentWaypointIndex = 0;
    private bool waitingAtPoint = false;
    private float waitTimer = 0f;
    private int waypointDirection = 1; // 1 = vorwärts, -1 = rückwärts

    void Start()
    {
        // Initiale Position als Phase 1 (oder falls es in P1 nicht aktiv ist, etwas drunter)
        currentCenter = transform.position;
        sr = GetComponent<SpriteRenderer>();

        // Jede Plattform startet an einem zufälligen Punkt in ihrer Animation
        // damit nicht alle synchron laufen (sieht natürlicher aus!)
        timeOffset = Random.Range(0f, 10f);

        // Erratic: Erstes zufälliges Ziel setzen
        if (pattern == MovePattern.Erratic)
        {
            PickNewErraticTarget();
        }
    }

    void Update()
    {
        // Freeze-Timer runterzählen
        if (isFrozen)
        {
            frozenTimer -= Time.deltaTime;
            if (frozenTimer <= 0f)
            {
                isFrozen = false;
                // Visuelle Rückmeldung: Farbe zurücksetzen
                if (sr != null) sr.color = Color.white;
            }
            return; // Plattform bewegt sich NICHT, wenn gefroren!
        }

        float speed = baseSpeed * currentSpeedMult;
        float t = (Time.time + timeOffset) * speed;

        switch (pattern)
        {
            case MovePattern.BackAndForth:
                MoveBackAndForth(t);
                break;
            case MovePattern.VerticalBob:
                MoveVerticalBob(t);
                break;
            case MovePattern.Circular:
                MoveCircular(t);
                break;
            case MovePattern.SineWave:
                MoveSineWave(t);
                break;
            case MovePattern.Figure8:
                MoveFigure8(t);
                break;
            case MovePattern.Erratic:
                MoveErratic(speed);
                break;
            case MovePattern.Waypoints:
                MoveWaypoints(speed);
                break;
        }
    }

    // ==========================================
    //  BEWEGUNGS-PATTERNS
    // ==========================================

    private void MoveBackAndForth(float t)
    {
        float offset = Mathf.Sin(t) * moveDistance;
        transform.position = currentCenter + Vector3.right * offset;
    }

    private void MoveVerticalBob(float t)
    {
        float offset = Mathf.Sin(t) * moveDistance;
        transform.position = currentCenter + Vector3.up * offset;
    }

    private void MoveCircular(float t)
    {
        float x = Mathf.Cos(t) * radius;
        float y = Mathf.Sin(t) * radius;
        transform.position = currentCenter + new Vector3(x, y, 0);
    }

    private void MoveSineWave(float t)
    {
        float x = Mathf.Sin(t) * moveDistance;
        float y = Mathf.Sin(t * 2f) * (moveDistance * 0.5f);
        transform.position = currentCenter + new Vector3(x, y, 0);
    }

    private void MoveFigure8(float t)
    {
        float x = Mathf.Sin(t) * radius;
        float y = Mathf.Sin(t * 2f) * (radius * 0.5f);
        transform.position = currentCenter + new Vector3(x, y, 0);
    }

    private void MoveErratic(float speed)
    {
        // Unberechenbares Zickzack! Bewegt sich auf ein zufälliges Ziel zu,
        // und wählt dann ein neues.
        transform.position = Vector3.MoveTowards(
            transform.position, erraticTarget, speed * Time.deltaTime
        );

        // Wenn wir nah am Ziel sind ODER der Timer abgelaufen ist -> neues Ziel
        erraticChangeTimer -= Time.deltaTime;
        if (Vector3.Distance(transform.position, erraticTarget) < 0.2f || erraticChangeTimer <= 0f)
        {
            PickNewErraticTarget();
        }
    }

    private void PickNewErraticTarget()
    {
        erraticTarget = currentCenter + new Vector3(
            Random.Range(-erraticBoundsX, erraticBoundsX),
            Random.Range(-erraticBoundsY, erraticBoundsY),
            0
        );
        erraticChangeTimer = Random.Range(0.8f, 2.0f);
    }

    private void MoveWaypoints(float speed)
    {
        if (waypoints == null || waypoints.Length == 0) return;

        // Warten am Punkt?
        if (waitingAtPoint)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f) waitingAtPoint = false;
            return;
        }

        // Zum nächsten Punkt fahren
        Transform target = waypoints[currentWaypointIndex];
        if (target == null) return;

        transform.position = Vector3.MoveTowards(
            transform.position, target.position, speed * Time.deltaTime
        );

        // Angekommen?
        if (Vector3.Distance(transform.position, target.position) < 0.05f)
        {
            transform.position = target.position;

            // Kurz warten
            waitingAtPoint = true;
            waitTimer = waitAtPoint;

            // Nächsten Punkt berechnen
            if (loopWaypoints)
            {
                // Loop: Am Ende wieder von vorne
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            }
            else
            {
                // Ping-Pong: Am Ende umdrehen
                currentWaypointIndex += waypointDirection;
                if (currentWaypointIndex >= waypoints.Length || currentWaypointIndex < 0)
                {
                    waypointDirection *= -1;
                    currentWaypointIndex += waypointDirection * 2;
                }
            }
        }
    }

    // ==========================================
    //  PHASEN-STEUERUNG (vom BossArenaManager aufgerufen)
    // ==========================================

    public void SetPhase(int phase, float transitionDuration = 3f)
    {
        // Geschwindigkeit anpassen
        switch (phase)
        {
            case 1: currentSpeedMult = phase1SpeedMult; break;
            case 2: currentSpeedMult = phase2SpeedMult; break;
            case 3: currentSpeedMult = phase3SpeedMult; break;
        }

        // Soll die Plattform in dieser Phase aktiv sein?
        bool shouldBeActive = false;
        Vector3 targetPos = currentCenter;

        switch (phase)
        {
            case 1: 
                shouldBeActive = activeInPhase1; 
                targetPos = posPhase1;
                break;
            case 2: 
                shouldBeActive = activeInPhase2; 
                targetPos = posPhase2;
                break;
            case 3: 
                shouldBeActive = activeInPhase3; 
                targetPos = posPhase3;
                break;
        }

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(TransitionToPhase(targetPos, shouldBeActive, transitionDuration));
    }

    private System.Collections.IEnumerator TransitionToPhase(Vector3 targetPos, bool shouldBeActive, float duration)
    {
        // Wenn die Plattform neu dazu kommt, stellen wir sie "unter den Bildschirm" und ziehen sie hoch!
        if (shouldBeActive && !gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            currentCenter = targetPos + Vector3.down * 15f; 
        }

        Vector3 start = currentCenter;

        if (!shouldBeActive)
        {
            // ===== ABSTURZ-ANIMATION (Vibrieren + Fallen) =====
            
            // 1. Vibrieren (ca 1 Sekunde)
            float vibTime = 1.0f;
            float elapsedVib = 0f;
            while (elapsedVib < vibTime)
            {
                elapsedVib += Time.deltaTime;
                // Schnelles Wackeln auf der X- und Y-Achse
                float shakeX = Random.Range(-0.15f, 0.15f);
                float shakeY = Random.Range(-0.1f, 0.1f);
                currentCenter = start + new Vector3(shakeX, shakeY, 0);
                yield return null;
            }

            // 2. Straight nach unten fallen (schnell!)
            currentCenter = start; // Zurücksetzen nach Vibrieren
            float fallDuration = 1.0f; // Fällt sehr schnell
            float elapsedFall = 0f;
            Vector3 fallTarget = currentCenter + Vector3.down * 20f;

            while (elapsedFall < fallDuration)
            {
                elapsedFall += Time.deltaTime;
                // Beschleunigtes Fallen (Ease-In)
                float t = elapsedFall / fallDuration;
                t = t * t; // Quadratisch, wird immer schneller
                
                currentCenter = Vector3.Lerp(start, fallTarget, t);
                yield return null;
            }

            // Abschalten
            gameObject.SetActive(false);
        }
        else
        {
            // ===== NORMALE BEWEGUNG (Sanft rübergleiten) =====
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Smoother ease-in-out
                t = t * t * (3f - 2f * t); 
                
                currentCenter = Vector3.Lerp(start, targetPos, t);
                yield return null;
            }

            currentCenter = targetPos;
        }
    }

    // ==========================================
    //  FREEZE-FUNKTION (für die Runen-Plattform!)
    // ==========================================

    public void Freeze(float duration)
    {
        isFrozen = true;
        frozenTimer = duration;

        // Visuelle Rückmeldung: Plattform wird blau wenn gefroren!
        if (sr != null) sr.color = new Color(0.5f, 0.8f, 1f, 1f);
    }

    // ==========================================
    //  SPIELER MITNEHMEN (damit er nicht von der Plattform rutscht)
    // ==========================================

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Spieler wird Kind der Plattform -> bewegt sich automatisch mit!
            collision.transform.SetParent(transform);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Spieler springt ab -> wieder unabhängig
            collision.transform.SetParent(null);
        }
    }
}
