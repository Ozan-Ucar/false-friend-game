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
        float playerY = transform.position.y;

        // 3. Schwerkraft aus, Geschwindigkeit stoppen
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        // 4. Walk-Animation an, Blickrichtung nach rechts (zum Ausgang)
        if (anim != null) anim.SetBool("isWalking", true);
        if (sr != null) sr.flipX = false;

        // 5. Sanft rauslaufen
        while (transform.position.x < targetX)
        {
            float newX = transform.position.x + walkSpeed * Time.deltaTime;
            transform.position = new Vector3(newX, playerY, transform.position.z);
            
            if (anim != null)
            {
                anim.SetBool("isGrounded", true);
                anim.SetBool("isWalking", true);
                anim.SetFloat("yVelocity", 0f);
                anim.SetBool("isClimbing", false);
            }
            yield return null;
        }

        // 6. Animation stoppen
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
        GameObject canvasObj = new GameObject("PixelTransitionCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        // 2. Ein RawImage für unsere generierte Pixel-Textur
        GameObject rawImageObj = new GameObject("PixelScreen");
        rawImageObj.transform.SetParent(canvasObj.transform, false);
        RawImage rawImage = rawImageObj.AddComponent<RawImage>();
        
        RectTransform rect = rawImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        // 3. Eine extrem niedrig auflösende 8-Bit Textur erstellen (z.B. 40x25 Pixel)
        int width = 40;
        int height = 25;
        Texture2D tex = new Texture2D(width, height);
        tex.filterMode = FilterMode.Point; // Wichtig: Macht die Pixel knackscharf!
        
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;
        tex.SetPixels(pixels);
        tex.Apply();
        
        rawImage.texture = tex;

        // 4. Wir erstellen eine Liste aller Pixel und mischen sie zufällig
        System.Collections.Generic.List<int> pixelIndices = new System.Collections.Generic.List<int>();
        for (int i = 0; i < pixels.Length; i++) pixelIndices.Add(i);
        
        for (int i = 0; i < pixelIndices.Count; i++)
        {
            int temp = pixelIndices[i];
            int randomIndex = Random.Range(i, pixelIndices.Count);
            pixelIndices[i] = pixelIndices[randomIndex];
            pixelIndices[randomIndex] = temp;
        }

        // 5. Animation: Den Bildschirm langsam Pixel für Pixel schwärzen
        float duration = 1.2f;
        float elapsed = 0f;
        int totalPixels = pixels.Length;
        int pixelsColored = 0;

        // Farbe an das neue Level übergeben, damit es mit der gleichen Farbe startet!
        PixelSceneReveal.globalTransitionColor = Color.black;

        // Die Schleife läuft exakt so lange, bis wirklich jeder einzelne Pixel schwarz ist!
        while (pixelsColored < totalPixels)
        {
            elapsed += Time.deltaTime;
            
            float linearProgress = Mathf.Clamp01(elapsed / duration);
            // S-Kurve für weichen Verlauf
            float curvedProgress = linearProgress * linearProgress * (3f - 2f * linearProgress); 

            if (elapsed >= duration) curvedProgress = 1f;

            int targetPixels = Mathf.RoundToInt(curvedProgress * totalPixels);

            bool changed = false;
            while (pixelsColored < targetPixels && pixelsColored < totalPixels)
            {
                pixels[pixelIndices[pixelsColored]] = Color.black;
                pixelsColored++;
                changed = true;
            }

            if (changed)
            {
                tex.SetPixels(pixels);
                tex.Apply();
            }
            yield return null;
        }

        // In der exakt selben Millisekunde, in der der Bildschirm 100% schwarz ist, wechseln wir die Szene!
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
    }
}
