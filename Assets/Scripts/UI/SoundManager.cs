using UnityEngine;

/// <summary>
/// Central audio hub. Plays looping background music and stores three volume
/// channels (Music, Fallen, PlayerAnim) in PlayerPrefs so they persist and can
/// be read by gameplay SFX later. Lives across scene loads (singleton).
///
/// Setup: put this on a "SoundManager" GameObject in the menu scene and drag a
/// music AudioClip into "Background Music".
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Background Music")]
    [SerializeField] AudioClip backgroundMusic;
    [Range(0f, 1f)] [SerializeField] float defaultVolume = 0.75f;

    AudioSource musicSource;

    public float MusicVolume      { get; private set; }
    public float FallenVolume     { get; private set; }
    public float PlayerAnimVolume { get; private set; }

    const string KMusic  = "vol_music";
    const string KFallen = "vol_fallen";
    const string KAnim   = "vol_player_anim";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        MusicVolume      = PlayerPrefs.GetFloat(KMusic,  defaultVolume);
        FallenVolume     = PlayerPrefs.GetFloat(KFallen, defaultVolume);
        PlayerAnimVolume = PlayerPrefs.GetFloat(KAnim,   defaultVolume);

        if (backgroundMusic == null)
        {
            backgroundMusic = Resources.Load<AudioClip>("TittlescreenMusic");
            if (backgroundMusic == null) backgroundMusic = Resources.Load<AudioClip>("menu_music");
        }

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.clip        = backgroundMusic;
        musicSource.loop        = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f; // 2D Stereo Sound!
        musicSource.volume      = MusicVolume;
        if (backgroundMusic != null)
        {
            musicSource.Play();
            Debug.Log($"[SoundManager] Spiele Menü-Musik: '{backgroundMusic.name}' (Volume: {MusicVolume})");
        }
    }

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }

    public void PlayMusic()
    {
        if (musicSource != null)
        {
            musicSource.spatialBlend = 0f;
            musicSource.volume = MusicVolume;
            if (!musicSource.isPlaying)
            {
                musicSource.Play();
                Debug.Log($"[SoundManager] Menü-Musik fortgesetzt: '{musicSource.clip?.name}'");
            }
        }
    }

    public void SetMusicVolume(float v)
    {
        MusicVolume = v;
        if (musicSource != null) musicSource.volume = v;
        PlayerPrefs.SetFloat(KMusic, v);
    }

    public void SetFallenVolume(float v)
    {
        FallenVolume = v;
        PlayerPrefs.SetFloat(KFallen, v);
    }

    public void SetPlayerAnimVolume(float v)
    {
        PlayerAnimVolume = v;
        PlayerPrefs.SetFloat(KAnim, v);
    }
}
