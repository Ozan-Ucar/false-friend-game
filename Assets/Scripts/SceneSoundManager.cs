using UnityEngine;

public class SceneSoundManager : MonoBehaviour
{
    public static SceneSoundManager Instance;

    [Header("Background Audio")]
    public AudioClip backgroundMusic;
    [Range(0f, 1f)] public float musicVolume = 0.5f;

    public AudioClip ambienceSound;
    [Range(0f, 1f)] public float ambienceVolume = 0.5f;

    [Header("SFX - Desert Level")]
    [Tooltip("Zieh hier die mp3 für den Vogelkäfig rein!")]
    public AudioClip cageDropSound;
    [Range(0f, 1f)] public float cageDropVolume = 1f;
    [Tooltip("Verschiebt den Sound. 0 = exakt beim Aufprall. Negative Werte (z.B. -0.2) = Sound startet VOR dem Aufprall. Positive Werte = Sound startet DANACH.")]
    public float cageDropOffset = 0f;

    [Tooltip("Sound, wenn sich der Käfig nach der Bauphase in Luft auflöst")]
    public AudioClip cageFadeSound;
    [Range(0f, 1f)] public float cageFadeVolume = 1f;

    [Header("SFX - Player")]
    [Tooltip("Der Schritt-Sound beim Laufen. Wird automatisch hintereinander abgespielt.")]
    public AudioClip footstepSound;
    [Range(0f, 1f)] public float footstepVolume = 0.5f;

    [Header("SFX - Worm Construction")]
    [Tooltip("Sound, wenn ein Wurm platziert wird (Geist wird echt)")]
    public AudioClip placeWormSound;
    [Range(0f, 1f)] public float placeWormVolume = 1f;

    [Header("SFX - Worm Attacks")]
    [Tooltip("Angriffs-Sound für den normalen Sandwurm")]
    public AudioClip attackNormalWormSound;
    [Range(0f, 1f)] public float attackNormalWormVolume = 1f;

    [Tooltip("Angriffs-Sound für den mittleren (Mid) Sandwurm")]
    public AudioClip attackMidWormSound;
    [Range(0f, 1f)] public float attackMidWormVolume = 1f;

    [Tooltip("Angriffs-Sound für den großen (High) Sandwurm")]
    public AudioClip attackHighWormSound;
    [Range(0f, 1f)] public float attackHighWormVolume = 1f;

    [Header("SFX - Rolling Stone")]
    [Tooltip("Dieser Sound wird wiederholt (Loop), solange der Stein rollt")]
    public AudioClip stoneRollSound;
    [Range(0f, 1f)] public float stoneRollVolume = 1f;

    [Tooltip("Sound, wenn der Stein einen Spieler oder eine Wand trifft")]
    public AudioClip stoneHitSound;
    [Range(0f, 1f)] public float stoneHitVolume = 1f;

    [Header("SFX - Ambient Events")]
    [Tooltip("Sound für den Schrei des Falken")]
    public AudioClip falconSound;
    [Range(0f, 1f)] public float falconVolume = 0.5f;
    [Tooltip("Wie oft soll der Falke ungefähr schreien? (in Sekunden)")]
    public float falconInterval = 20f;

    private AudioSource musicSource;
    private AudioSource ambienceSource;
    private AudioSource sfxSource;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        if (backgroundMusic != null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.clip = backgroundMusic;
            musicSource.volume = musicVolume;
            musicSource.loop = true;
            musicSource.Play();
        }

        if (ambienceSound != null)
        {
            ambienceSource = gameObject.AddComponent<AudioSource>();
            ambienceSource.clip = ambienceSound;
            ambienceSource.volume = ambienceVolume;
            ambienceSource.loop = true;
            ambienceSource.Play();
        }

        // Der Lautsprecher für alle kurzen SFX
        sfxSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        // Starte die Falken-Endlosschleife
        StartCoroutine(FalconRoutine());
    }

    private System.Collections.IEnumerator FalconRoutine()
    {
        while (true)
        {
            // Ein bisschen Zufall einbauen (+/- 3 Sekunden), damit es natürlich klingt 
            // und nicht exakt wie eine gestellte Uhr.
            float randomWait = falconInterval + UnityEngine.Random.Range(-3f, 3f);
            
            yield return new WaitForSeconds(randomWait);

            if (falconSound != null)
            {
                // Spiele den Falken mit leichten Schwankungen im Pitch ab!
                PlaySFX(falconSound, falconVolume, true);
            }
        }
    }

    /// <summary>
    /// Allgemeine Methode, um einen Soundeffekt abzuspielen
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume = 1f, bool randomPitch = false)
    {
        if (clip == null) return;

        if (randomPitch)
            sfxSource.pitch = UnityEngine.Random.Range(0.85f, 1.15f);
        else
            sfxSource.pitch = 1f;

        sfxSource.PlayOneShot(clip, volume);
    }

    // ==========================================
    // SPEZIFISCHE SOUND-FUNKTIONEN (VON MIR PROGRAMMIERT)
    // ==========================================

    public void PlayCageDrop(float dropDuration)
    {
        // Wir berechnen die genaue Zeit: Falldauer + deine manuelle Verschiebung im Inspector
        float exactDelay = dropDuration + cageDropOffset;
        if (exactDelay < 0f) exactDelay = 0f; // Darf nicht negativ in die Zukunft gehen

        StartCoroutine(PlaySFXDelayed(cageDropSound, cageDropVolume, true, exactDelay)); 
    }

    public void PlayCageFade()
    {
        PlaySFX(cageFadeSound, cageFadeVolume, true);
    }

    public void PlayFootstep()
    {
        // TRUE = Random Pitch ist immer an für die Schritte!
        PlaySFX(footstepSound, footstepVolume, true); 
    }

    public void PlayPlaceWorm()
    {
        PlaySFX(placeWormSound, placeWormVolume, true);
    }

    public void PlayAttackNormal()
    {
        PlaySFX(attackNormalWormSound, attackNormalWormVolume, true);
    }

    public void PlayAttackMid()
    {
        PlaySFX(attackMidWormSound, attackMidWormVolume, true);
    }

    public void PlayAttackHigh()
    {
        PlaySFX(attackHighWormSound, attackHighWormVolume, true);
    }

    public void PlayStoneHit()
    {
        PlaySFX(stoneHitSound, stoneHitVolume, true);
    }

    private System.Collections.IEnumerator PlaySFXDelayed(AudioClip clip, float volume, bool randomPitch, float delay)
    {
        yield return new WaitForSeconds(delay);
        PlaySFX(clip, volume, randomPitch);
    }
}
