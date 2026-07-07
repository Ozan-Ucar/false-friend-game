using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InteractableDoor : MonoBehaviour
{
    [Header("Tür Objekte")]
    public GameObject closedDoorObject;
    public GameObject openDoorObject;

    [Header("Übergangs-Effekt (Transition)")]
    [Tooltip("Wie viele Sekunden der Pixel-Effekt dauert.")]
    public float transitionDuration = 1.2f;
    [Tooltip("Der Verlauf der Geschwindigkeit (z.B. fängt langsam an, wird schneller).")]
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("Die Farbe der Pixel beim Szenenwechsel.")]
    public Color transitionColor = Color.black;
    
    [Header("Ziel")]
    [Tooltip("Der Name der Szene, in die diese Tür führt (exakte Schreibweise beachten!). Bleibt es leer, wird die aktuelle Szene neu geladen.")]
    public string targetSceneName = "";

    [Header("Cutscene (Optional)")]
    [Tooltip("Ziehe hier ein CutsceneData-Asset rein, um zwischen den Szenen eine Cutscene abzuspielen. Leer lassen = keine Cutscene.")]
    public CutsceneData cutsceneBeforeNextScene;

    private bool isOpen = false;
    private bool isPlayerNear = false;
    private bool hasTransitionStarted = false;

    void Awake()
    {
        UpdateDoorVisuals();
    }

    void Update()
    {
        if (hasTransitionStarted) return;

        // Wenn die Tür offen ist...
        if (isOpen)
        {
            // ...und Anni (Player) in der Nähe ist -> Level wechseln!
            if (isPlayerNear)
            {
                StartTransition();
                return;
            }

            // ...und Anni nicht da ist, checken wir ob Kappa noch wartet
            PetFollow kappa = FindAnyObjectByType<PetFollow>();
            if (kappa != null && kappa.currentState != PetFollow.PetState.WaitingAtTarget)
            {
                // Kappa ist nicht mehr im Warte-Modus (z.B. zurückgerufen) -> Tür zu!
                if (kappa.mySpeechBubble != null) kappa.mySpeechBubble.HideText();
                CloseDoor();
            }

            return; // Wir warten
        }

        // 1. Öffnen mit Taste 'E' (Falls man als Anni manuell öffnet)
        if (isPlayerNear && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Debug.Log("Tür per Taste E geöffnet!");
            OpenDoorVisualsOnly();
            StartTransition();
        }

        // 2. Öffnen per Mausklick (Egal wo der Spieler steht!)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
            
            // Prüfen ob wir auf den Collider der Tür geklickt haben
            Collider2D myCollider = GetComponent<Collider2D>();
            if (myCollider != null && myCollider.OverlapPoint(mouseWorldPos))
            {
                Debug.Log("Tür wurde erfolgreich mit der Maus angeklickt!");
                
                // Sucht Kappa (das Pet) in der Szene
                PetFollow kappa = FindAnyObjectByType<PetFollow>();
                if (kappa != null)
                {
                    // Kappa geht zur Tür
                    kappa.WalkTo(transform.position.x, () => {
                        StartCoroutine(OpenDoorAndSpeak(kappa));
                    });
                }
                else
                {
                    // Falls kein Spieler gefunden, direkt öffnen und wechseln
                    OpenDoorVisualsOnly();
                    StartTransition();
                }
            }
        }
    }

    private System.Collections.IEnumerator OpenDoorAndSpeak(PetFollow kappa)
    {
        // Tür geht optisch auf!
        OpenDoorVisualsOnly();

        // 1 Sekunde warten
        yield return new WaitForSeconds(1f);

        // Wenn er angekommen ist, spreche:
        SpeechBubble bubble = kappa.mySpeechBubble;
        if (bubble == null) bubble = kappa.GetComponentInChildren<SpeechBubble>(true);

        if (bubble != null)
        {
            bubble.ShowText("Anni komm,\nich warte auf dich!", true);
        }
        else
        {
            Debug.LogWarning("Kappa hat keine SpeechBubble-Komponente! Bitte ziehe das SpeechBubble-Skript direkt in das 'My Speech Bubble' Feld von PetFollow.");
        }
    }

    private void OpenDoorVisualsOnly()
    {
        if (!isOpen)
        {
            isOpen = true;
            UpdateDoorVisuals();
        }
    }

    private void CloseDoor()
    {
        if (isOpen)
        {
            isOpen = false;
            UpdateDoorVisuals();
        }
    }

    private void StartTransition()
    {
        if (hasTransitionStarted) return;
        hasTransitionStarted = true;
        StartCoroutine(ShowStarsThenTransition());
    }

    private System.Collections.IEnumerator ShowStarsThenTransition()
    {
        // Stars anzeigen
        if (StarManager.Instance != null)
        {
            StarManager.Instance.ShowEndScreen();
            
            // 3 Sekunden warten, damit der Spieler die Sterne sehen kann
            yield return new WaitForSeconds(3f);
        }

        // Dann den Level-Wechsel starten
        yield return StartCoroutine(TransitionAndChangeScene());
    }

    private void UpdateDoorVisuals()
    {
        if (closedDoorObject != null) closedDoorObject.SetActive(!isOpen);
        if (openDoorObject != null) openDoorObject.SetActive(isOpen);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) isPlayerNear = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) isPlayerNear = false;
    }

    private IEnumerator TransitionAndChangeScene()
    {
        yield return new WaitForSeconds(0.5f);

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
        // Je kleiner die Zahlen, desto dicker die Pixel!
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
        float safeDuration = Mathf.Max(0.01f, transitionDuration); // Verhindert Crashs bei 0 Sekunden
        float elapsed = 0f;
        int totalPixels = pixels.Length;
        int pixelsColored = 0;

        // Farbe an das neue Level übergeben, damit es mit der gleichen Farbe startet!
        PixelSceneReveal.globalTransitionColor = transitionColor;
        PixelSceneReveal.useFadeToBlack = false;

        // Die Schleife läuft exakt so lange, bis wirklich jeder einzelne Pixel schwarz ist!
        while (pixelsColored < totalPixels)
        {
            elapsed += Time.deltaTime;
            
            float linearProgress = Mathf.Clamp01(elapsed / safeDuration);
            float curvedProgress = Mathf.Clamp01(transitionCurve.Evaluate(linearProgress));

            // Falls die Zeit abgelaufen ist oder die Kurve nicht bis 1 geht, erzwingen wir 100%
            if (elapsed >= safeDuration) curvedProgress = 1f;

            int targetPixels = Mathf.RoundToInt(curvedProgress * totalPixels);

            bool changed = false;
            while (pixelsColored < targetPixels && pixelsColored < totalPixels)
            {
                pixels[pixelIndices[pixelsColored]] = transitionColor;
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

        // In der exakt selben Millisekunde, in der der Bildschirm 100% schwarz ist:
        // Wenn eine Cutscene zugewiesen ist, spielen wir sie ab. Sonst direkt die Szene laden!
        string sceneToLoad = !string.IsNullOrEmpty(targetSceneName)
            ? targetSceneName
            : SceneManager.GetActiveScene().name;

        if (cutsceneBeforeNextScene != null && cutsceneBeforeNextScene.slides != null && cutsceneBeforeNextScene.slides.Count > 0)
        {
            CutscenePlayer.pendingCutscene = cutsceneBeforeNextScene;
            CutscenePlayer.pendingTargetScene = sceneToLoad;
            CutscenePlayer.Play();
        }
        else
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}
