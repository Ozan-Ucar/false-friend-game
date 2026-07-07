using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Legt dieses Skript auf den PLAYER.
/// - Bei Szenenstart: Spieler läuft automatisch vom Entrance-Portal ins Level rein.
/// - Bei Berührung des Exit-Portals: Spieler läuft automatisch aus dem Bild raus.
/// 
/// Sucht sich die Portale automatisch per LevelPortal-Komponente in der Szene.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PortalTransition : MonoBehaviour
{
    [Header("Einstellungen")]
    [Tooltip("Wie schnell der Spieler beim Ein-/Auslaufen läuft")]
    public float walkSpeed = 5f;
    [Tooltip("Wie weit außerhalb des Portals der Spieler startet/endet (in Unity-Units)")]
    public float offscreenOffset = 3f;
    [Tooltip("Wie weit der Spieler nach dem Eingang ins Level reinläuft")]
    public float walkInDistance = 3f;
    [Tooltip("Höhen-Versatz zum Portal (0 = genau auf Portal-Höhe, negativ = tiefer, positiv = höher)")]
    public float spawnYOffset = 0f;
    [Tooltip("Wie lange nach dem Betreten des Exit-Portals gewartet wird, bevor die nächste Szene lädt")]
    public float sceneLoadDelay = 1.0f;
    [Tooltip("Name der nächsten Szene (leer lassen, wenn nichts geladen werden soll)")]
    public string nextSceneName = "";

    [Header("Cutscene (Optional)")]
    [Tooltip("Ziehe hier ein CutsceneData-Asset rein, um zwischen den Szenen eine Cutscene abzuspielen. Leer lassen = keine Cutscene.")]
    public CutsceneData cutsceneBeforeNextScene;

    [Header("Optional: Manuell zuweisen")]
    [Tooltip("Lass leer = wird automatisch gesucht")]
    public LevelPortal entrancePortal;
    [Tooltip("Lass leer = wird automatisch gesucht")]
    public LevelPortal exitPortal;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private PlayerMovement playerMovement;
    private bool isInTransition = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        playerMovement = GetComponent<PlayerMovement>();

        // Portale automatisch finden wenn nicht manuell zugewiesen
        if (entrancePortal == null || exitPortal == null)
        {
            LevelPortal[] portals = FindObjectsByType<LevelPortal>(FindObjectsSortMode.None);
            foreach (LevelPortal p in portals)
            {
                if (p.portalType == LevelPortal.PortalType.Entrance && entrancePortal == null)
                    entrancePortal = p;
                else if (p.portalType == LevelPortal.PortalType.Exit && exitPortal == null)
                    exitPortal = p;
            }
        }

        // Entrance-Animation starten
        if (entrancePortal != null)
        {
            StartCoroutine(EntranceSequence());
        }
    }

    IEnumerator EntranceSequence()
    {
        isInTransition = true;

        // 1. Player-Input deaktivieren
        if (playerMovement != null) playerMovement.enabled = false;

        // 2. Spieler außerhalb des Bildschirms positionieren (links vom Entrance)
        float startX = entrancePortal.transform.position.x - offscreenOffset;
        float targetX = entrancePortal.transform.position.x + walkInDistance;
        float playerY = entrancePortal.transform.position.y + spawnYOffset;

        transform.position = new Vector3(startX, playerY, transform.position.z);

        // 3. Schwerkraft kurz aus, damit er nicht fällt während er reinläuft
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        // 4. Walk-Animation an, Blickrichtung nach rechts
        if (anim != null) anim.SetBool("isWalking", true);
        if (sr != null) sr.flipX = false;

        // 5. Sanft reinlaufen
        while (transform.position.x < targetX)
        {
            float newX = transform.position.x + walkSpeed * Time.deltaTime;
            transform.position = new Vector3(newX, playerY, transform.position.z);
            
            // Animator denkt sonst er fällt, weil kein Bodenkontakt
            if (anim != null)
            {
                anim.SetBool("isGrounded", true);
                anim.SetBool("isWalking", true);
                anim.SetFloat("yVelocity", 0f);
                anim.SetBool("isClimbing", false);
            }
            yield return null;
        }

        // 6. Walk-Animation aus, Physik wieder an, Input wieder an
        if (anim != null)
        {
            anim.SetBool("isWalking", false);
            anim.SetBool("isGrounded", true);
        }
        rb.gravityScale = originalGravity;
        rb.linearVelocity = Vector2.zero;

        if (playerMovement != null) playerMovement.enabled = true;

        // PlayerInput neu starten, damit die Steuerung wieder funktioniert
        var playerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput != null)
        {
            playerInput.enabled = false;
            playerInput.enabled = true;
        }

        isInTransition = false;
    }

    IEnumerator ExitSequence()
    {
        isInTransition = true;

        // 1. Player-Input sofort deaktivieren
        if (playerMovement != null) playerMovement.enabled = false;

        // 2. Zielposition: rechts außerhalb des Bildschirms
        float targetX = exitPortal.transform.position.x + offscreenOffset;

        // 4. Blickrichtung nach rechts (zum Ausgang)
        if (sr != null) sr.flipX = false;

        // 5. Sanft rauslaufen (mit echter Physik, damit er runterfällt, falls er in der Luft ist!)
        while (transform.position.x < targetX)
        {
            // Er läuft nach rechts, fällt aber ganz normal nach unten
            rb.linearVelocity = new Vector2(walkSpeed, rb.linearVelocity.y);
            
            if (anim != null)
            {
                // Animation dynamisch anpassen: Wenn er in der Luft ist, spielt er die Fall-Animation, sonst Walk.
                bool isGrounded = Mathf.Abs(rb.linearVelocity.y) < 0.05f;
                anim.SetBool("isGrounded", isGrounded);
                anim.SetBool("isWalking", isGrounded); // Nur Walk-Animation, wenn er auf dem Boden ist
                anim.SetFloat("yVelocity", rb.linearVelocity.y);
                anim.SetBool("isClimbing", false);
            }
            
            // Aufs Physik-Update warten, wenn wir Velocity setzen!
            yield return new WaitForFixedUpdate();
        }

        // 6. Stop
        rb.linearVelocity = Vector2.zero;
        if (anim != null) anim.SetBool("isWalking", false);

        // 7. Stars anzeigen
        if (StarManager.Instance != null)
        {
            StarManager.Instance.ShowEndScreen();
            yield return new WaitForSeconds(3f);
        }

        // 8. Nächste Szene laden
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            if (sceneLoadDelay > 0) yield return new WaitForSeconds(sceneLoadDelay);
            yield return StartCoroutine(PixelTransitionAndChangeScene());
        }
        else
        {
            Debug.Log("Exit-Transition abgeschlossen! Keine nächste Szene angegeben.");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Wenn der Spieler das Exit-Portal berührt
        if (isInTransition) return;
        if (exitPortal == null) return;

        // Prüfen ob der berührte Collider zum Exit-Portal gehört
        if (other.transform == exitPortal.transform || other.transform.IsChildOf(exitPortal.transform))
        {
            StartCoroutine(ExitSequence());
        }
    }

    /// <summary>
    /// Kann von außen aufgerufen werden, z.B. von einem Trigger-Objekt am Ausgang
    /// </summary>
    public void TriggerExit()
    {
        if (!isInTransition && exitPortal != null)
        {
            StartCoroutine(ExitSequence());
        }
    }

    /// <summary>
    /// Gibt zurück ob gerade eine Transition läuft (für andere Skripte)
    /// </summary>
    public bool IsInTransition()
    {
        return isInTransition;
    }

    private IEnumerator PixelTransitionAndChangeScene()
    {
        // 1. Canvas für den Vollbild-Effekt erstellen
        GameObject canvasObj = new GameObject("FadeTransitionCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        // 2. Ein normales Image für den Fade
        GameObject imgObj = new GameObject("FadeScreen");
        imgObj.transform.SetParent(canvasObj.transform, false);
        Image image = imgObj.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0); // Start transparent
        
        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        // 3. Animation: Fade Out (Schwarz werden)
        float duration = 1.2f;
        float elapsed = 0f;

        // WICHTIG: Sag der neuen Szene, dass sie faden soll!
        PixelSceneReveal.globalTransitionColor = Color.black;
        PixelSceneReveal.useFadeToBlack = true;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float linearProgress = Mathf.Clamp01(elapsed / duration);
            float curvedProgress = linearProgress * linearProgress * (3f - 2f * linearProgress);
            image.color = new Color(0, 0, 0, curvedProgress);
            yield return null;
        }

        image.color = Color.black;

        // In der exakt selben Millisekunde, in der der Bildschirm 100% schwarz ist:
        // Wenn eine Cutscene zugewiesen ist, spielen wir sie ab. Sonst direkt die Szene laden!
        if (cutsceneBeforeNextScene != null && cutsceneBeforeNextScene.slides != null && cutsceneBeforeNextScene.slides.Count > 0)
        {
            CutscenePlayer.pendingCutscene = cutsceneBeforeNextScene;
            CutscenePlayer.pendingTargetScene = nextSceneName;
            CutscenePlayer.Play();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
        }
    }
}
