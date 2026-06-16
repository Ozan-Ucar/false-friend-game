using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Verwaltet die Stars pro Level. Zeigt am Ende der Runde an, wie viele gesammelt wurden.
/// Platziere dieses Skript auf einem leeren GameObject in der Szene.
/// </summary>
public class StarManager : MonoBehaviour
{
    public static StarManager Instance { get; private set; }

    [Header("Stars in diesem Level")]
    [Tooltip("Wie viele Stars es in diesem Level gibt (Standard: 3)")]
    public int totalStars = 3;

    [Header("Sound")]
    [Tooltip("Sound beim Einsammeln eines Sterns")]
    public AudioClip starCollectSound;
    [Range(0f, 1f)] public float starCollectVolume = 1f;

    [Header("End-Screen UI")]
    [Tooltip("Das gesamte UI Panel, das am Ende eingeblendet wird")]
    public GameObject endScreenPanel;
    [Tooltip("Die 3 Star-Images im End-Screen (von links nach rechts)")]
    public Image[] starImages;
    [Tooltip("Sprite fuer einen eingesammelten Stern")]
    public Sprite starFilledSprite;
    [Tooltip("Sprite fuer einen NICHT eingesammelten Stern (grau/leer)")]
    public Sprite starEmptySprite;

    [Header("End-Screen Animation")]
    [Tooltip("Wie lange zwischen jedem Stern-Aufdecken gewartet wird")]
    public float starRevealDelay = 0.5f;
    [Tooltip("Wie gross der Stern beim Aufploppen wird (Bounce)")]
    public float starBounceScale = 1.5f;

    [Header("In-Game HUD (Optional)")]
    [Tooltip("Text der live anzeigt: 1/3, 2/3 etc. Kann leer bleiben.")]
    public Text starCounterText;

    private int collectedStars = 0;
    private AudioSource audioSource;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // End-Screen am Anfang verstecken
        if (endScreenPanel != null)
        {
            endScreenPanel.SetActive(false);
        }

        UpdateHUD();
    }

    /// <summary>
    /// Wird von StarPickup aufgerufen, wenn ein Stern eingesammelt wird.
    /// </summary>
    public void CollectStar()
    {
        collectedStars++;
        collectedStars = Mathf.Min(collectedStars, totalStars);

        // Sound abspielen
        if (starCollectSound != null && audioSource != null)
        {
            audioSource.pitch = 0.9f + collectedStars * 0.1f; // Jeder Stern klingt etwas hoeher!
            audioSource.PlayOneShot(starCollectSound, starCollectVolume);
        }

        UpdateHUD();
    }

    /// <summary>
    /// Gibt zurueck, wie viele Stars eingesammelt wurden.
    /// </summary>
    public int GetCollectedStars()
    {
        return collectedStars;
    }

    /// <summary>
    /// Zeigt den End-Screen mit den gesammelten Sternen an.
    /// Ruf diese Methode auf, wenn der Level vorbei ist (z.B. aus InteractableDoor).
    /// </summary>
    public void ShowEndScreen()
    {
        if (endScreenPanel != null)
        {
            endScreenPanel.SetActive(true);
            StartCoroutine(RevealStarsAnimation());
        }
    }

    private IEnumerator RevealStarsAnimation()
    {
        // Panel von klein nach gross animieren
        RectTransform panelRect = endScreenPanel.GetComponent<RectTransform>();
        float growDuration = 0.4f;
        float elapsed = 0f;

        panelRect.localScale = Vector3.zero;

        while (elapsed < growDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / growDuration;
            // Overshoot: springt leicht ueber 1.0 hinaus
            float scale = 1f + 0.15f * Mathf.Sin(t * Mathf.PI);
            scale *= Mathf.Clamp01(t * 1.2f);
            panelRect.localScale = Vector3.one * scale;
            yield return null;
        }
        panelRect.localScale = Vector3.one;

        // Erst alle Sterne auf leer setzen
        if (starImages != null)
        {
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] != null)
                {
                    starImages[i].sprite = starEmptySprite;
                    starImages[i].transform.localScale = Vector3.one;
                    starImages[i].color = new Color(1f, 1f, 1f, 0.3f);
                }
            }
        }

        yield return new WaitForSeconds(0.3f);

        // Jetzt einen nach dem anderen aufdecken
        for (int i = 0; i < collectedStars && i < starImages.Length; i++)
        {
            if (starImages[i] != null)
            {
                starImages[i].sprite = starFilledSprite;
                starImages[i].color = Color.white;

                // Bounce Animation
                yield return StartCoroutine(BounceIn(starImages[i].transform));
            }

            yield return new WaitForSeconds(starRevealDelay);
        }
    }

    private IEnumerator BounceIn(Transform target)
    {
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            // Overshoot curve: Springt ueber 1.0 hinaus und kommt zurueck
            float scale;
            if (t < 0.6f)
            {
                // Hochspringen
                scale = Mathf.Lerp(0f, starBounceScale, t / 0.6f);
            }
            else
            {
                // Zurueckfedern
                scale = Mathf.Lerp(starBounceScale, 1f, (t - 0.6f) / 0.4f);
            }

            target.localScale = Vector3.one * scale;
            yield return null;
        }

        target.localScale = Vector3.one;
    }

    private void UpdateHUD()
    {
        if (starCounterText != null)
        {
            starCounterText.text = collectedStars + " / " + totalStars;
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
