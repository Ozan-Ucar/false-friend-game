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

        // Automatisches Finden & Laden von Musik & Kulisse passend zum aktuellen Level!
        AutoResolveLevelAudio();

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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInit()
    {
        if (Instance == null)
        {
            EnsureInstance();
        }
    }

    public static void EnsureInstance()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("SceneSoundManager");
            Instance = go.AddComponent<SceneSoundManager>();
        }
    }

    private void AutoResolveLevelAudio()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string sceneLower = sceneName.ToLower();

        // 0. House Level ONLY (HouseLevel)
        if (sceneLower.Contains("house"))
        {
            if (backgroundMusic == null) backgroundMusic = LoadResourceAudio("HomeBackgroundMusic");
        }
        // 1. Forest Level (ForestBackgroundmusic + ForestKulisse)
        else if (sceneLower.Contains("forest") || sceneLower.Contains("level01"))
        {
            if (backgroundMusic == null) backgroundMusic = LoadResourceAudio("ForestBackgroundmusic");
            if (ambienceSound == null) ambienceSound = LoadResourceAudio("ForestKulisse");
        }
        // 2. Dungeon Level (DungeonBackgroundMusic)
        else if (sceneLower.Contains("dungeon"))
        {
            if (backgroundMusic == null) backgroundMusic = LoadResourceAudio("DungeonBackgroundMusic");
        }
        // 3. Factory Level (FactoryBackground + FactoryKulisse)
        else if (sceneLower.Contains("factory"))
        {
            if (backgroundMusic == null) backgroundMusic = LoadResourceAudio("FactoryBackground");
            if (ambienceSound == null) ambienceSound = LoadResourceAudio("FactoryKulisse");
        }
        // 4. Stronghold Level (StrongholdBackgroundMusic)
        else if (sceneLower.Contains("stronghold"))
        {
            if (backgroundMusic == null) backgroundMusic = LoadResourceAudio("StrongholdBackgroundMusic");
        }
        // 5. Desert Level (Sandstorm Kulisse)
        else if (sceneLower.Contains("desert"))
        {
            if (ambienceSound == null) ambienceSound = LoadResourceAudio("sandstorm");
        }
        // 6. Water Level (WaterLevelKulisse)
        else if (sceneLower.Contains("water"))
        {
            if (ambienceSound == null) ambienceSound = LoadResourceAudio("WaterLevelKulisse");
        }
        // 7. Boss Arena (BossRoomBackgroundMusic)
        else if (sceneLower.Contains("boss"))
        {
            if (backgroundMusic == null) backgroundMusic = LoadResourceAudio("BossRoomBackgroundMusic");
        }
        // 8. Credit Scene (CreditBackgroundmusic)
        else if (sceneLower.Contains("credit"))
        {
            if (backgroundMusic == null) backgroundMusic = LoadResourceAudio("CreditBackgroundmusic");
        }
        // 9. Title Screen (TittlescreenMusic)
        else if (sceneLower.Contains("title") || sceneLower.Contains("main"))
        {
            if (backgroundMusic == null) backgroundMusic = LoadResourceAudio("TittlescreenMusic");
        }

        Debug.Log($"[SceneSoundManager] Szene: '{sceneName}' | Musik: {(backgroundMusic != null ? backgroundMusic.name : "NICHT GEFUNDEN")} | Kulisse: {(ambienceSound != null ? ambienceSound.name : "NICHT GEFUNDEN")}");
    }

    private AudioClip LoadResourceAudio(string resourceName)
    {
#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AudioClip");
        foreach (string g in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
            if (path.ToLower().Contains(resourceName.ToLower()))
            {
                AudioClip edClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (edClip != null) return edClip;
            }
        }
#endif

        // 1. Direkter Versuch
        AudioClip clip = Resources.Load<AudioClip>(resourceName);
        if (clip != null) return clip;

        // 2. Alle Clips aus dem Resources-Ordner laden und nach Teilstück des Namens durchsuchen
        AudioClip[] allClips = Resources.LoadAll<AudioClip>("");
        if (allClips != null)
        {
            foreach (var c in allClips)
            {
                if (c != null && c.name.ToLower().Contains(resourceName.ToLower()))
                {
                    return c;
                }
            }
        }
        return null;
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
