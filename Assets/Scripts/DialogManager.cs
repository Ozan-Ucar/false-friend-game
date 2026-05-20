using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public enum SpeakerSide { Left, Right }

[System.Serializable]
public struct DialogLine
{
    [Tooltip("Der Name des Sprechers")]
    public string speakerName;
    
    [Tooltip("Der gesprochene Text")]
    [TextArea(3, 5)]
    public string text;
    
    [Tooltip("Das Bild (Sprite) der Person")]
    public Sprite speakerSprite;
    
    [Tooltip("Soll die Person von links oder rechts ins Bild kommen?")]
    public SpeakerSide side;
}

public class DialogManager : MonoBehaviour
{
    public static DialogManager Instance;

    [Header("UI References")]
    [Tooltip("Das Haupt-Panel (inkl. Hintergrund), das von unten hochfährt")]
    public RectTransform dialogBox; 
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogText;
    
    [Header("Portraits (Bilder der Sprecher)")]
    [Tooltip("Das Parent-Objekt für das linke Bild (für die Slide-Animation)")]
    public RectTransform leftPortraitPivot;
    public Image leftPortraitImage;
    
    [Tooltip("Das Parent-Objekt für das rechte Bild (für die Slide-Animation)")]
    public RectTransform rightPortraitPivot;
    public Image rightPortraitImage;

    [Header("Animation Settings")]
    public float boxSlideDuration = 0.8f; // Verlangsamt für kinomatischen Look (vorher 0.4f)
    public float portraitSlideDuration = 1.2f; // Verlangsamt für kinomatischen Look (vorher 0.7f)
    public float typeSpeed = 0.02f;

    [Header("Positions")]
    [Tooltip("Die genaue Position der Dialogbox, wenn sie versteckt (ausgeblendet) ist.")]
    public Vector2 boxHiddenPosition = new Vector2(0f, -500f);
    [Tooltip("Die genaue Position der Dialogbox, wenn sie sichtbar (eingeblendet) ist.")]
    public Vector2 boxVisiblePosition = new Vector2(0f, 0f);
    [Tooltip("Wie weit links/rechts das Portrait im versteckten Zustand aus dem Bildschirm geschoben wird (positiver Wert).")]
    public float portraitHiddenOffsetX = 1200f; 
    
    public bool IsDialogActive { get; private set; }

    private Queue<DialogLine> linesQueue;
    private DialogLine currentLine;
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    // Gespeicherte Original-Positionen aus dem Unity Editor
    private float leftPortraitVisibleX;
    private float rightPortraitVisibleX;

    // Cooldown, damit man nicht sofort weiterklicken kann
    private float skipCooldownTimer = 0f;

    // Schutz flag, damit man während der Slide-Animation nichts drücken kann
    private bool isBoxSliding = false;

    // Referenzen auf aktive Rutsch-Coroutinen (um Konflikte zu verhindern!)
    private Coroutine leftPortraitCoroutine;
    private Coroutine rightPortraitCoroutine;

    private void Awake()
    {
        // Singleton Setup
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        linesQueue = new Queue<DialogLine>();

        // AUTOMATISCHER FALLBACK: Falls die Pivot-Slots im Inspector leer gelassen wurden,
        // nutzen wir einfach direkt die RectTransforms der Bilder selbst!
        if (leftPortraitPivot == null && leftPortraitImage != null)
        {
            leftPortraitPivot = leftPortraitImage.rectTransform;
        }
        if (rightPortraitPivot == null && rightPortraitImage != null)
        {
            rightPortraitPivot = rightPortraitImage.rectTransform;
        }
        
        // Original-Positionen speichern (damit du sie im Editor platzieren kannst, wo du willst!)
        if (leftPortraitPivot != null) leftPortraitVisibleX = leftPortraitPivot.anchoredPosition.x;
        if (rightPortraitPivot != null) rightPortraitVisibleX = rightPortraitPivot.anchoredPosition.x;

        // Sicherheitsprüfung: Falls Unity im Inspector '0' eingetragen hat, setzen wir gute Standardwerte!
        if (typeSpeed <= 0f)
        {
            typeSpeed = 0.02f;
        }

        if (portraitHiddenOffsetX == 0f)
        {
            portraitHiddenOffsetX = 1200f;
        }

        if (boxSlideDuration <= 0f)
        {
            boxSlideDuration = 0.8f;
        }

        if (portraitSlideDuration <= 0f)
        {
            portraitSlideDuration = 1.2f;
        }

        // UI initial verstecken
        if (dialogBox != null)
        {
            dialogBox.anchoredPosition = boxHiddenPosition;
            dialogBox.gameObject.SetActive(false);
        }

        HidePortraitInstant(leftPortraitPivot, true);
        HidePortraitInstant(rightPortraitPivot, false);
    }

    private void Update()
    {
        // Wenn kein Dialog aktiv ist ODER die Box gerade slidet, keine Eingaben erlauben!
        if (!IsDialogActive || isBoxSliding) return;

        // Cooldown verringern (unscaledDeltaTime, falls das Spiel pausiert ist)
        if (skipCooldownTimer > 0f)
        {
            skipCooldownTimer -= Time.unscaledDeltaTime;
        }

        // Weiter per Klick oder Leertaste (Kompatibel mit dem neuen Input System)
        bool nextPressed = (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
                           (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

        if (nextPressed)
        {
            if (isTyping)
            {
                // Typing überspringen und direkt ganzen Text anzeigen
                StopCoroutine(typingCoroutine);
                dialogText.text = currentLine.text;
                isTyping = false;
                
                // Sobald der ganze Text da ist, startet der 2-Sekunden-Cooldown
                skipCooldownTimer = 2.0f;
            }
            else
            {
                // Erst weitergehen, wenn der Cooldown abgelaufen ist
                if (skipCooldownTimer <= 0f)
                {
                    DisplayNextLine();
                }
            }
        }
    }

    public void StartDialog(List<DialogLine> lines)
    {
        if (IsDialogActive) return;

        IsDialogActive = true;
        linesQueue.Clear();
        foreach (var line in lines) linesQueue.Enqueue(line);

        // Vor dem Hochfahren: Text leeren und den ersten Sprechernamen anzeigen (kein Platzhalter!)
        if (dialogText != null) dialogText.text = "";
        if (nameText != null && lines.Count > 0)
        {
            nameText.text = lines[0].speakerName;
        }

        // Optional: Spiel pausieren während des Dialogs
        // Time.timeScale = 0f;

        isBoxSliding = true; // Slide startet
        dialogBox.gameObject.SetActive(true);
        
        // Aktive Portrait-Coroutinen stoppen, falls noch welche laufen
        if (leftPortraitCoroutine != null) StopCoroutine(leftPortraitCoroutine);
        if (rightPortraitCoroutine != null) StopCoroutine(rightPortraitCoroutine);
        
        StopAllCoroutines();
        
        // Box von unten hochfahren lassen, dann erst ersten Satz zeigen
        StartCoroutine(SlideBox(boxVisiblePosition, boxSlideDuration, () => {
            isBoxSliding = false; // Slide beendet, Eingabe wieder erlaubt!
            DisplayNextLine();
        }));
    }

    private void DisplayNextLine()
    {
        if (linesQueue.Count == 0)
        {
            EndDialog();
            return;
        }

        currentLine = linesQueue.Dequeue();
        nameText.text = currentLine.speakerName;

        // Portraits animieren
        if (currentLine.side == SpeakerSide.Left)
        {
            if (currentLine.speakerSprite != null) 
            {
                leftPortraitImage.sprite = currentLine.speakerSprite;
                leftPortraitImage.gameObject.SetActive(true);
            }
            else
            {
                leftPortraitImage.gameObject.SetActive(false);
            }

            // Portraits animieren
            float offset = Mathf.Abs(portraitHiddenOffsetX);
            
            if (leftPortraitCoroutine != null) StopCoroutine(leftPortraitCoroutine);
            leftPortraitCoroutine = StartCoroutine(SlidePortrait(leftPortraitPivot, leftPortraitVisibleX, portraitSlideDuration, false)); 
            
            if (rightPortraitCoroutine != null) StopCoroutine(rightPortraitCoroutine);
            rightPortraitCoroutine = StartCoroutine(SlidePortrait(rightPortraitPivot, rightPortraitVisibleX + offset, portraitSlideDuration, true)); 
        }
        else
        {
            if (currentLine.speakerSprite != null) 
            {
                rightPortraitImage.sprite = currentLine.speakerSprite;
                rightPortraitImage.gameObject.SetActive(true);
            }
            else
            {
                rightPortraitImage.gameObject.SetActive(false);
            }

            // Rechts reinfahren (auf seine originale X-Position), Links rausfahren
            float offset = Mathf.Abs(portraitHiddenOffsetX);
            
            if (rightPortraitCoroutine != null) StopCoroutine(rightPortraitCoroutine);
            rightPortraitCoroutine = StartCoroutine(SlidePortrait(rightPortraitPivot, rightPortraitVisibleX, portraitSlideDuration, false)); 
            
            if (leftPortraitCoroutine != null) StopCoroutine(leftPortraitCoroutine);
            leftPortraitCoroutine = StartCoroutine(SlidePortrait(leftPortraitPivot, leftPortraitVisibleX - offset, portraitSlideDuration, true)); 
        }

        // Typewriter Effekt starten
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeSentence(currentLine.text));
    }

    private IEnumerator TypeSentence(string sentence)
    {
        dialogText.text = "";
        isTyping = true;
        
        // unscaledDeltaTime bzw. WaitForSecondsRealtime, falls das Spiel pausiert ist
        foreach (char letter in sentence.ToCharArray())
        {
            dialogText.text += letter;
            yield return new WaitForSecondsRealtime(typeSpeed);
        }

        isTyping = false;
        
        // Text wurde komplett natürlich fertig geschrieben -> 2 Sekunden Cooldown starten
        skipCooldownTimer = 2.0f;
    }

    private void EndDialog()
    {
        isBoxSliding = true; // Box fährt runter, Eingabe sperren

        float offset = Mathf.Abs(portraitHiddenOffsetX);

        // Jedes Element rutscht mit seiner EIGENEN Geschwindigkeit aus dem Inspector!
        if (leftPortraitCoroutine != null) StopCoroutine(leftPortraitCoroutine);
        leftPortraitCoroutine = StartCoroutine(SlidePortrait(leftPortraitPivot, leftPortraitVisibleX - offset, portraitSlideDuration, false));
        
        if (rightPortraitCoroutine != null) StopCoroutine(rightPortraitCoroutine);
        rightPortraitCoroutine = StartCoroutine(SlidePortrait(rightPortraitPivot, rightPortraitVisibleX + offset, portraitSlideDuration, false));

        // Box rutscht mit ihrer eigenen Geschwindigkeit runter
        StartCoroutine(SlideBox(boxHiddenPosition, boxSlideDuration, null));

        // Wir warten exakt die LÄNGSTE Dauer ab, bevor wir alles ausschalten.
        // Dadurch kannst du im Inspector die Geschwindigkeiten völlig unabhängig voneinander einstellen!
        float maxDuration = Mathf.Max(boxSlideDuration, portraitSlideDuration);
        StartCoroutine(CleanupDialogUI(maxDuration));
    }

    private IEnumerator CleanupDialogUI(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        
        isBoxSliding = false;
        
        // Beide Portraits UND ihre Bilder am Ende der Animation absolut sicher deaktivieren
        if (leftPortraitPivot != null) leftPortraitPivot.gameObject.SetActive(false);
        if (rightPortraitPivot != null) rightPortraitPivot.gameObject.SetActive(false);
        if (leftPortraitImage != null) leftPortraitImage.gameObject.SetActive(false);
        if (rightPortraitImage != null) rightPortraitImage.gameObject.SetActive(false);

        dialogBox.gameObject.SetActive(false);
        IsDialogActive = false;
    }

    // --- Clean Animations (Cubic Ease-Out) ---

    private void HidePortraitInstant(RectTransform rt, bool isLeft)
    {
        if (rt == null) return;
        Vector2 pos = rt.anchoredPosition;
        float offset = Mathf.Abs(portraitHiddenOffsetX);
        // Wir nehmen die Original-Position und verschieben sie nach außen
        pos.x = isLeft ? leftPortraitVisibleX - offset : rightPortraitVisibleX + offset;
        rt.anchoredPosition = pos;
        rt.gameObject.SetActive(false); // Direkt komplett deaktivieren
    }

    private IEnumerator SlideBox(Vector2 targetPos, float duration, System.Action onComplete = null)
    {
        float elapsed = 0f;
        Vector2 startPos = dialogBox.anchoredPosition;
        
        // Dynamisch entscheiden: Ease-Out beim Reinkommen (weiches Bremsen), Ease-In beim Rausgehen (schneller Abflug)
        bool isHiding = (targetPos == boxHiddenPosition);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // Ease-In (t^3) zum Verschwinden, Ease-Out (1 - (1-t)^3) zum Erscheinen
            float curveT = isHiding ? (t * t * t) : (1f - Mathf.Pow(1f - t, 3f));

            dialogBox.anchoredPosition = Vector2.Lerp(startPos, targetPos, curveT);
            yield return null;
        }

        dialogBox.anchoredPosition = targetPos;
        onComplete?.Invoke();
    }

    private IEnumerator SlidePortrait(RectTransform rt, float targetX, float duration, bool hideAtEnd)
    {
        if (rt == null) yield break;

        // Wenn es reinfährt, sofort sichtbar machen
        if (!hideAtEnd) rt.gameObject.SetActive(true);

        float elapsed = 0f;
        Vector2 startPos = rt.anchoredPosition;
        Vector2 targetPos = new Vector2(targetX, startPos.y);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // Ease-In (t^3) beim Rausgehen, Ease-Out beim Reinkommen für organische Dynamik!
            float curveT = hideAtEnd ? (t * t * t) : (1f - Mathf.Pow(1f - t, 3f));

            rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, curveT);
            yield return null;
        }

        rt.anchoredPosition = targetPos;

        // Wenn es rausfährt, am Ende komplett deaktivieren
        if (hideAtEnd) rt.gameObject.SetActive(false);
    }
}
