using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(BoxCollider2D))]
public class PoisonMushroomTutorial : MonoBehaviour
{
    [Header("Referenzen")]
    [Tooltip("Der Pilz, der angeklickt werden soll.")]
    public PoisonMushroom targetMushroom;
    [Tooltip("UI-Element oder Sprite, das den Maus-Spieler auffordert zu klicken (z.B. ein Cursor oder Text). Wird automatisch eingeblendet.")]
    public GameObject mousePrompt;

    [Header("Zoom Einstellungen")]
    [Tooltip("Wie nah soll die Kamera ranzoomen? (Normal ist 5)")]
    public float zoomSize = 3.5f;
    [Tooltip("Wie lange dauert der Kameraflug hin und zurück?")]
    public float zoomDuration = 1.0f;
    
    [Header("Timing")]
    [Tooltip("Wie lange wartet das Skript nach der Explosion des Pilzes, bevor das Level neustartet?")]
    public float delayAfterExplosion = 1.5f;

    [Header("Debug")]
    [Tooltip("Wenn aktiv, wird das Tutorial beim Testen im Editor IMMER abgespielt, auch wenn es schon gespeichert wurde.")]
    public bool alwaysPlayInEditor = false;

    private bool hasTriggered = false;
    private const string TUTORIAL_PREF_KEY = "MushroomTutorialDone";

    void Start()
    {
        bool isDone = PlayerPrefs.GetInt(TUTORIAL_PREF_KEY, 0) == 1;
        
        #if UNITY_EDITOR
        if (alwaysPlayInEditor) isDone = false;
        #endif

        // Wenn das Tutorial schon mal erfolgreich gemacht wurde, direkt löschen
        if (isDone)
        {
            Destroy(gameObject);
            return;
        }

        if (mousePrompt != null)
        {
            mousePrompt.SetActive(false);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            StartCoroutine(TutorialSequence(other.gameObject));
        }
    }

    private IEnumerator TutorialSequence(GameObject player)
    {
        hasTriggered = true;

        if (targetMushroom == null)
        {
            Debug.LogError("PoisonMushroomTutorial: Kein Ziel-Pilz zugewiesen!");
            yield break;
        }

        // 1. Player einfrieren
        PlayerMovement pm = player.GetComponent<PlayerMovement>();
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        Animator anim = player.GetComponent<Animator>();
        if (anim == null) anim = player.GetComponentInChildren<Animator>();

        if (pm != null) pm.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
        if (anim != null)
        {
            anim.SetFloat("Speed", 0f); // Setze Walk-Animation zurück
            anim.speed = 0f; // Friert die Animation komplett ein!
        }

        // Text & Position für den Mouse Prompt anpassen
        if (mousePrompt != null)
        {
            // Positionierung Mitte Oben
            RectTransform rect = mousePrompt.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0, -50f); // Etwas Abstand nach oben
            }

            // Text setzen (falls TextMeshPro)
            var tmpText = mousePrompt.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmpText != null) tmpText.text = "Klick den Pilz!";

            // Text setzen (falls normales Unity UI Text)
            var normalText = mousePrompt.GetComponent<UnityEngine.UI.Text>();
            if (normalText != null) normalText.text = "Klick den Pilz!";
            
            // Falls es ein 3D Text Mesh Pro ist (kein UI)
            var tmp = mousePrompt.GetComponent<TMPro.TextMeshPro>();
            if (tmp != null) tmp.text = "Klick den Pilz!";
        }

        // 2. Kamera Zoom auf den Pilz
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.DoTutorialZoom(targetMushroom.transform, zoomSize, zoomDuration);
        }
        yield return new WaitForSeconds(zoomDuration);

        // 3. Aufforderung einblenden
        if (mousePrompt != null)
        {
            mousePrompt.SetActive(true);
        }

        // 4. Warten bis der Mausspieler den Pilz platzen lässt
        yield return new WaitUntil(() => targetMushroom.IsExploded);

        // 5. Aufforderung sofort ausblenden
        if (mousePrompt != null)
        {
            mousePrompt.SetActive(false);
        }

        // 6. Kamera zurücksetzen
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.ResetTutorialZoom(0.5f); // Schneller zurückzoomen
        }

        // 7. Spieler SOFORT wieder entfrieren, damit er direkt weiterspielen kann!
        if (pm != null) pm.enabled = true;
        if (anim != null) anim.speed = 1f;

        // 8. Speichern, dass wir das Tutorial gesehen haben!
        PlayerPrefs.SetInt(TUTORIAL_PREF_KEY, 1);
        PlayerPrefs.Save();

        // 9. Tutorial beenden
        Destroy(gameObject);
    }
}
