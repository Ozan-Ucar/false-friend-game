using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Hoch-optimierter, ladefreier prozeduraler Sound-Synthesizer für ALLE Fallen, Dialoge, Spieler-Treffer & Aktionen.
/// Alle AudioClips werden einmalig vorberechnet (gecacht), damit das Abspielen mit 0 ms Verzögerung 
/// EXAKTIM FRAME des Aufpralls / Schusses erfolgt!
/// </summary>
public class ProceduralTrapSFX : MonoBehaviour
{
    public static ProceduralTrapSFX Instance;

    private AudioSource audioSource;
    private AudioSource rollingLoopSource;

    // Gecachte Clips für 0ms Latenz & perfekte Synchronität!
    private AudioClip arrowClip;
    private AudioClip ceilingSlamClip;
    private AudioClip ceilingFallClip;
    private AudioClip mushroomPopClip;
    private AudioClip poisonGasClip;
    private AudioClip fireBurstClip;
    private AudioClip stoneCrashClip;
    private AudioClip stoneRollLoopClip;
    private AudioClip wormPlaceClip;
    private AudioClip wormAttackClip;
    private AudioClip lightningClip;
    private AudioClip laserClip;
    private AudioClip laserWarningClip;
    private AudioClip cannonClip;
    private AudioClip lightningRod2FileClip;
    private AudioClip bounceClip;
    private AudioClip playerHurtClip;
    private AudioClip playerDamageFileClip;
    private AudioClip playerDeathClip;
    private AudioClip dialogBlipClip;
    private AudioClip waterHandClip;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeClips();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        rollingLoopSource = gameObject.AddComponent<AudioSource>();
        rollingLoopSource.playOnAwake = false;
        rollingLoopSource.loop = true;
    }

    private void InitializeClips()
    {
        if (arrowFileClip == null) arrowFileClip = LoadResourceAudio("ArrowSfx");
        if (arrowFileClip == null) arrowFileClip = LoadResourceAudio("ArrowTrap");
        if (arrowClip == null) arrowClip = GenerateArrowShotClip();
        if (ceilingSlamClip == null) ceilingSlamClip = GenerateCeilingSlamClip();
        if (ceilingTrapFileClip == null) ceilingTrapFileClip = LoadResourceAudio("CeilingTrapSfx");
        if (ceilingFallClip == null) ceilingFallClip = GenerateCeilingFallClip();
        if (mushroomPopClip == null) mushroomPopClip = GenerateMushroomPopClip();
        if (mushroomExplosionFileClip == null) mushroomExplosionFileClip = LoadResourceAudio("PilzExplosionSFX");
        if (poisonGasClip == null) poisonGasClip = GeneratePoisonGasClip();
        if (fireBurstClip == null) fireBurstClip = GenerateFireBurstClip();
        if (fireTrapFileClip == null) fireTrapFileClip = LoadResourceAudio("FireTrapSound");
        if (stoneCrashClip == null) stoneCrashClip = GenerateStoneCrashClip();
        if (stoneRollLoopClip == null) stoneRollLoopClip = GenerateStoneRollLoopClip();
        if (wormPlaceClip == null) wormPlaceClip = GenerateWormPlaceClip();
        if (wormAttackClip == null) wormAttackClip = GenerateWormAttackClip();
        if (lightningClip == null) lightningClip = GenerateLightningClip();
        if (lightningClips == null || lightningClips.Length == 0)
        {
            System.Collections.Generic.List<AudioClip> lClips = new System.Collections.Generic.List<AudioClip>();
            for (int i = 1; i <= 3; i++)
            {
                AudioClip c = LoadResourceAudio("LightningRod" + i);
                if (c != null) lClips.Add(c);
            }
            if (lClips.Count > 0) lightningClips = lClips.ToArray();
        }
        if (laserClip == null) laserClip = GenerateLaserClip();
        if (laserWarningClip == null) laserWarningClip = GenerateLaserWarningClip();
        if (cannonClip == null) cannonClip = GenerateCannonClip();
        if (bounceClip == null) bounceClip = GenerateBounceClip();
        if (playerHurtClip == null) playerHurtClip = GeneratePlayerHurtClip();
        if (playerDeathClip == null) playerDeathClip = GeneratePlayerDeathClip();
        if (dialogBlipClip == null) dialogBlipClip = GenerateDialogBlipClip();
        if (waterHandClip == null) waterHandClip = LoadResourceAudio("WaterhandTrap");
    }

    private void Update()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame) PlayArrowTrapSound();
            if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame) PlayCeilingTrapSound();
            if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame) { PlayPoisonMushroomSound(); PlayPoisonGasSound(); }
            if (Keyboard.current.digit4Key.wasPressedThisFrame || Keyboard.current.numpad4Key.wasPressedThisFrame) PlayFireTrapSound();
            if (Keyboard.current.digit5Key.wasPressedThisFrame || Keyboard.current.numpad5Key.wasPressedThisFrame) PlayRollingStoneSound();
            if (Keyboard.current.digit6Key.wasPressedThisFrame || Keyboard.current.numpad6Key.wasPressedThisFrame) PlayPlaceWormSound();
            if (Keyboard.current.digit7Key.wasPressedThisFrame || Keyboard.current.numpad7Key.wasPressedThisFrame) PlaySandwormAttackSound();
            if (Keyboard.current.digit8Key.wasPressedThisFrame || Keyboard.current.numpad8Key.wasPressedThisFrame) PlayLightningSound();
            if (Keyboard.current.digit9Key.wasPressedThisFrame || Keyboard.current.numpad9Key.wasPressedThisFrame) PlayLaserSound();
        }
    }



    private static void EnsureInstance()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("ProceduralTrapSFX");
            Instance = go.AddComponent<ProceduralTrapSFX>();
        }
        else if (Instance.arrowClip == null)
        {
            Instance.InitializeClips();
        }
    }

    private AudioClip arrowFileClip;

    // ==========================================
    // 1. PFEILFALLE (Arrow Trap - Echter Sehnenschlag & Luftzischen)
    // ==========================================
    public static void PlayArrowTrapSound()
    {
        EnsureInstance();
        if (Instance.audioSource != null)
        {
            if (Instance.arrowFileClip != null)
            {
                Instance.audioSource.PlayOneShot(Instance.arrowFileClip, 1.0f);
            }
            else if (Instance.arrowClip != null)
            {
                Instance.audioSource.PlayOneShot(Instance.arrowClip, 0.95f);
            }
        }
    }

    private static AudioClip GenerateArrowShotClip()
    {
        int sampleRate = 44100;
        float duration = 0.24f; // Länger & knackiger!
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];

        float phase = 0f;
        float lastNoise = 0f;

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;

            // 1. Harter Sehnenschlag / Armbrust-Auslöser (Frequenz-Absacker 450 Hz -> 85 Hz)
            float stringFreq = Mathf.Lerp(450f, 85f, t * 2.8f);
            phase += 2f * Mathf.PI * stringFreq / sampleRate;
            float stringSnap = Mathf.Sin(phase) * Mathf.Exp(-t * 16f);

            // 2. Mechanischer Auslöser-Knack (Holz/Eisen-Klick t < 0.012s)
            float mechClick = (t < 0.012f) ? Mathf.Sin(2f * Mathf.PI * 2600f * t) * 0.9f : 0f;

            // 3. Pfeil-Durchschneiden der Luft ("SWHOOOOSH!")
            float rawNoise = Random.value * 2f - 1f;
            float airWhoosh = (rawNoise - lastNoise * 0.65f) * 0.5f;
            lastNoise = rawNoise;

            float whooshEnvelope = Mathf.Sin(t * Mathf.PI) * Mathf.Exp(-t * 6.5f);

            data[i] = Mathf.Clamp((mechClick * 0.45f + stringSnap * 0.55f + airWhoosh * whooshEnvelope * 0.75f), -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("ArrowTrapSFX", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip ceilingTrapFileClip;

    // ==========================================
    // 2. DECKENFALLE (Ceiling Slam & Fall - Eigenes Audio CeilingTrapSfx)
    // ==========================================
    public static void PlayCeilingTrapSound()
    {
        EnsureInstance();
        if (Instance.audioSource != null)
        {
            if (Instance.ceilingTrapFileClip != null)
            {
                Instance.audioSource.PlayOneShot(Instance.ceilingTrapFileClip, 1.0f);
            }
            else if (Instance.ceilingSlamClip != null)
            {
                Instance.audioSource.PlayOneShot(Instance.ceilingSlamClip, 1.0f);
            }
        }
    }

    public static void PlayCeilingFallSound()
    {
        EnsureInstance();
        Instance.audioSource.PlayOneShot(Instance.ceilingFallClip, 0.7f);
    }

    private static AudioClip GenerateCeilingFallClip()
    {
        int sampleRate = 44100;
        float duration = 0.18f;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];

        float phase = 0f;
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float freq = Mathf.Lerp(300f, 80f, t);
            phase += 2f * Mathf.PI * freq / sampleRate;

            float tone = Mathf.Sin(phase);
            float envelope = Mathf.Sin(t * Mathf.PI);

            data[i] = tone * envelope * 0.4f;
        }

        AudioClip clip = AudioClip.Create("CeilingFallSFX", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip GenerateCeilingSlamClip()
    {
        int sampleRate = 44100;
        float duration = 0.35f;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];

        float phase = 0f;
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float freq = Mathf.Lerp(75f, 25f, t);
            phase += 2f * Mathf.PI * freq / sampleRate;

            float tone = Mathf.Sin(phase);
            float crunch = (Random.value * 2f - 1f) * Mathf.Exp(-t * 10f);
            float envelope = Mathf.Pow(1f - t, 2.2f);

            data[i] = Mathf.Clamp((tone * 0.75f + crunch * 0.6f) * envelope, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("CeilingSlamSFX", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip mushroomExplosionFileClip;

    // ==========================================
    // 3. GIFTPILZ (1. Platzen / Explosion + 2. Giftwolke Hiss)
    // ==========================================
    public static void PlayPoisonMushroomSound()
    {
        EnsureInstance();
        if (Instance.audioSource != null)
        {
            if (Instance.mushroomExplosionFileClip != null)
            {
                Instance.audioSource.PlayOneShot(Instance.mushroomExplosionFileClip, 1.0f);
            }
            else if (Instance.mushroomPopClip != null)
            {
                Instance.audioSource.PlayOneShot(Instance.mushroomPopClip, 0.95f);
            }
        }
    }

    public static void PlayPoisonGasSound()
    {
        EnsureInstance();
        Instance.audioSource.PlayOneShot(Instance.poisonGasClip, 0.85f);
    }

    private static AudioClip GenerateMushroomPopClip()
    {
        int sampleRate = 44100;
        float duration = 0.38f; // Länger und mächtiger!
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];

        float phase = 0f;
        float lastNoise = 0f;

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            // Wuchtiger Sub-Bass Punch (260 Hz -> 32 Hz)
            float freq = Mathf.Lerp(260f, 32f, t * t);
            phase += 2f * Mathf.PI * freq / sampleRate;
            float subBass = Mathf.Sin(phase);

            // Explosiver Detonations-Crunch (Tiefpass-Rauschen am Start)
            float rawNoise = Random.value * 2f - 1f;
            float filteredNoise = (rawNoise + lastNoise * 0.7f) * 0.5f;
            lastNoise = filteredNoise;
            float explosionCrack = filteredNoise * Mathf.Exp(-t * 12f);

            float envelope = Mathf.Pow(1f - t, 2.4f);

            data[i] = Mathf.Clamp((subBass * 0.7f + explosionCrack * 0.65f) * envelope * 1.15f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("MushroomPopSFX", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip GeneratePoisonGasClip()
    {
        int sampleRate = 44100;
        float duration = 2.2f;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];

        float lastSample = 0f;
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float whiteNoise = Random.value * 2f - 1f;
            float filteredNoise = (whiteNoise + lastSample * 0.85f) * 0.35f;
            lastSample = filteredNoise;

            float attack = Mathf.Clamp01(t / 0.08f);
            float fadeOut = Mathf.Pow(1f - t, 2.8f);

            data[i] = filteredNoise * attack * fadeOut;
        }

        AudioClip clip = AudioClip.Create("PoisonGasSFX", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip fireTrapFileClip;

    // ==========================================
    // 4. FEUERFALLE (Fire Burst - Eigenes Audio FireTrapSound)
    // ==========================================
    public static void PlayFireTrapSound()
    {
        EnsureInstance();
        if (Instance.audioSource != null)
        {
            if (Instance.fireTrapFileClip != null)
            {
                Instance.audioSource.PlayOneShot(Instance.fireTrapFileClip, 1.0f);
            }
            else if (Instance.fireBurstClip != null)
            {
                Instance.audioSource.PlayOneShot(Instance.fireBurstClip, 0.88f);
            }
        }
    }

    private static AudioClip GenerateFireBurstClip()
    {
        int sampleRate = 44100;
        float duration = 0.32f;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];

        float lastSample = 0f;
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float whiteNoise = Random.value * 2f - 1f;
            float filteredNoise = (whiteNoise + lastSample * 0.6f) * 0.5f;
            lastSample = filteredNoise;

            float envelope = (t < 0.1f) ? (t / 0.1f) : Mathf.Exp(-(t - 0.1f) * 4f);
            data[i] = filteredNoise * envelope * 0.85f;
        }

        AudioClip clip = AudioClip.Create("FireBurstSFX", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // ==========================================
    // 5. ROLLENDER STEIN (Roll Loop & Crash)
    // ==========================================
    private float stoneRollTimer = 0f;
    private bool isStoneRollActive = false;
    private bool hasFinishedRollFade = false;

    public static void ResetStoneRollState()
    {
        EnsureInstance();
        Instance.isStoneRollActive = false;
        Instance.hasFinishedRollFade = false;
        Instance.stoneRollTimer = 0f;
        if (Instance.rollingLoopSource != null)
        {
            Instance.rollingLoopSource.Stop();
        }
    }

    public static void PlayRollingStoneSound()
    {
        EnsureInstance();
        Instance.audioSource.PlayOneShot(Instance.stoneCrashClip, 0.55f);
    }

    public static void StartStoneRollLoop(float speedRatio = 1f)
    {
        EnsureInstance();
        if (Instance.hasFinishedRollFade) return; // Nach dem 4s-Fadeout NIEMALS neu starten!

        if (Instance.rollingLoopSource != null)
        {
            if (!Instance.isStoneRollActive || !Instance.rollingLoopSource.isPlaying)
            {
                Instance.rollingLoopSource.clip = Instance.stoneRollLoopClip;
                Instance.rollingLoopSource.Play();
                Instance.isStoneRollActive = true;
                Instance.stoneRollTimer = 0f;
            }

            Instance.stoneRollTimer += Time.deltaTime;

            if (Instance.stoneRollTimer >= 4.0f)
            {
                Instance.rollingLoopSource.Stop();
                Instance.isStoneRollActive = false;
                Instance.hasFinishedRollFade = true; // Sperrt weiteres Abspielen für diesen Stein!
                return;
            }

            // Ausblenden (Fade-Out) in den letzten 1,0 Sekunden (von Sekunde 3.0 bis 4.0)
            float fadeFactor = 1f;
            if (Instance.stoneRollTimer > 3.0f)
            {
                fadeFactor = Mathf.Clamp01((4.0f - Instance.stoneRollTimer) / 1.0f);
            }

            Instance.rollingLoopSource.pitch = Mathf.Lerp(0.6f, 1.3f, speedRatio);
            float baseVol = Mathf.Lerp(0.12f, 0.45f, speedRatio);
            Instance.rollingLoopSource.volume = baseVol * fadeFactor;
        }
    }

    public static void StopStoneRollLoop()
    {
        if (Instance != null)
        {
            Instance.isStoneRollActive = false;
            Instance.hasFinishedRollFade = false;
            Instance.stoneRollTimer = 0f;
            if (Instance.rollingLoopSource != null)
            {
                Instance.rollingLoopSource.Stop();
            }
        }
    }

    private static AudioClip GenerateStoneRollLoopClip()
    {
        int sampleRate = 44100;
        float duration = 1.0f;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];

        float phase = 0f;
        float lastNoise = 0f;

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            
            // 1. Schweres Stein-Gewicht (Tieffrequentes unregelmäßiges Rumpeln 42 Hz - 78 Hz)
            float rumbleFreq = 42f + Mathf.Sin(t * Mathf.PI * 4f) * 20f + Mathf.Sin(t * Mathf.PI * 9f) * 12f;
            phase += 2f * Mathf.PI * rumbleFreq / sampleRate;
            float heavyWeight = Mathf.Sin(phase);

            // 2. Realistische Reibung & Kies-Gleiten auf dem Boden (Gefiltertes Rauschen)
            float whiteNoise = Random.value * 2f - 1f;
            float gravelFriction = (whiteNoise + lastNoise * 0.82f) * 0.45f;
            lastNoise = gravelFriction;

            // 3. Organische Boden-Unebenheiten (Bumps)
            float surfaceBump = 1f + Mathf.Sin(t * Mathf.PI * 2f) * 0.25f;

            data[i] = Mathf.Clamp((heavyWeight * 0.55f + gravelFriction * 0.45f) * surfaceBump * 0.6f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("StoneRollLoopSFX", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip GenerateStoneCrashClip()
    {
        int sampleRate = 44100;
        float duration = 0.38f;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];

        float phase = 0f;
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float freq = Mathf.Lerp(130f, 35f, t);
            phase += 2f * Mathf.PI * freq / sampleRate;

            float rumble = Mathf.Sin(phase);
            float impactNoise = (Random.value * 2f - 1f) * Mathf.Exp(-t * 7f);
            float envelope = Mathf.Exp(-t * 4.2f);

            data[i] = Mathf.Clamp((rumble * 0.55f + impactNoise * 0.65f) * envelope, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("StoneCrashSFX", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // ==========================================
    // 6. SANDWURM (1. Platzieren & 2. Wühlen im Sand)
    // ==========================================
    public static void PlayPlaceWormSound()
    {
        EnsureInstance();
        Instance.audioSource.PlayOneShot(Instance.wormPlaceClip, 0.95f);
    }

    public static void PlaySandwormAttackSound()
    {
        EnsureInstance();
        Instance.audioSource.PlayOneShot(Instance.wormAttackClip, 1.0f);
    }

    private static AudioClip GenerateWormPlaceClip()
    {
        int sampleRate = 44100;
        float duration = 0.28f;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];

        float phase = 0f;
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float freq = Mathf.Lerp(180f, 40f, t);
            phase += 2f * Mathf.PI * freq / sampleRate;

            float tone = Mathf.Sin(phase);
            float sandNoise = (Random.value * 2f - 1f) * 0.5f;
            float envelope = Mathf.Sin(t * Mathf.PI);

            data[i] = (tone * 0.5f + sandNoise * 0.5f) * envelope;
        }

        AudioClip clip = AudioClip.Create("WormPlaceSFX", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip GenerateWormAttackClip()
    {
        int sampleRate = 44100;
        float duration = 1.6f;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];

        float lastSample = 0f;
        float phase = 0f;

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float whiteNoise = Random.value * 2f - 1f;
            float sandHiss = (whiteNoise + lastSample * 0.78f) * 0.5f;
            lastSample = sandHiss;

            float rumbleFreq = 45f + Mathf.Sin(t * Mathf.PI * 5f) * 40f;
            phase += 2f * Mathf.PI * rumbleFreq / sampleRate;
            float earthRumble = Mathf.Sin(phase) * 0.5f;

            float envelope = (t < 0.2f) ? (t / 0.2f) : Mathf.Pow(1f - t, 1.8f);

            data[i] = Mathf.Clamp((sandHiss * 0.65f + earthRumble * 0.35f) * envelope, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("WormAttackSFX", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // ==========================================
    // 7. BLITZFALLE (Lightning Rod - 4 Random Variations)
    // ==========================================
    private AudioClip[] lightningClips;

    public static void PlayLightningSound()
    {
        EnsureInstance();
        if (Instance.audioSource != null)
        {
            if (Instance.lightningClips != null && Instance.lightningClips.Length > 0)
            {
                // Zufälligen Sound aus den 4 Blitzfalle-Sounds abspielen!
                AudioClip randomClip = Instance.lightningClips[Random.Range(0, Instance.lightningClips.Length)];
                Instance.audioSource.PlayOneShot(randomClip, 1.0f);
            }
            else if (Instance.lightningClip != null)
            {
                Instance.audioSource.PlayOneShot(Instance.lightningClip, 1.0f);
            }
        }
    }

    private static AudioClip GenerateLightningClip()
    {
        int sampleRate = 44100;
        float duration = 0.45f;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];

        float phase = 0f;
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float crackle = (Random.value * 2f - 1f) * (t < 0.12f ? 1.2f : 0.35f);

            float freq = Mathf.Lerp(1800f, 35f, t);
            phase += 2f * Mathf.PI * freq / sampleRate;
            float tone = Mathf.Sin(phase);

            float envelope = Mathf.Exp(-t * 4.8f);
            data[i] = Mathf.Clamp((crackle * 0.65f + tone * 0.45f) * envelope, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("LightningSFX", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // ==========================================
    // 8. LASERKANONE (Laser Beam & Warning Beep)
    // ==========================================
    public static void PlayLaserSound()
    {
        EnsureInstance();
        if (Instance.lightningRod2FileClip == null) Instance.lightningRod2FileClip = LoadResourceAudio("LightningRod2");

        if (Instance.lightningRod2FileClip != null)
            Instance.audioSource.PlayOneShot(Instance.lightningRod2FileClip, 1.0f);
        else if (Instance.laserClip != null)
            Instance.audioSource.PlayOneShot(Instance.laserClip, 0.9f);
    }

    public static void PlayLaserWarningSound()
    {
        EnsureInstance();
        Instance.audioSource.PlayOneShot(Instance.laserWarningClip, 0.6f);
    }

    private static AudioClip GenerateLaserWarningClip()
    {
        int sampleRate = 44100;
        float duration = 0.08f;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];

        float phase = 0f;
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            phase += 2f * Mathf.PI * 1600f / sampleRate;
            float tone = Mathf.Sin(phase);
            float envelope = Mathf.Sin(t * Mathf.PI);

            data[i] = tone * envelope * 0.5f;
        }

        AudioClip clip = AudioClip.Create("LaserWarningSFX", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip GenerateLaserClip()
    {
        int sampleRate = 44100;
        float duration = 0.22f;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];

        float phase = 0f;
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float freq = Mathf.Lerp(2800f, 320f, t * t);
            phase += 2f * Mathf.PI * freq / sampleRate;

            float tone = Mathf.Sign(Mathf.Sin(phase));
            float envelope = 1f - t;

            data[i] = tone * envelope * 0.45f;
        }

        AudioClip clip = AudioClip.Create("LaserSFX", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // ==========================================
    // 9. KANONE (Mouse Cannon - "BOOM!")
    // ==========================================
    public static void PlayCannonSound()
    {
        EnsureInstance();
        if (Instance.mushroomExplosionFileClip == null) Instance.mushroomExplosionFileClip = LoadResourceAudio("PilzExplosionSFX");

        if (Instance.mushroomExplosionFileClip != null)
            Instance.audioSource.PlayOneShot(Instance.mushroomExplosionFileClip, 1.0f);
        else if (Instance.cannonClip != null)
            Instance.audioSource.PlayOneShot(Instance.cannonClip, 1.0f);
    }

    private static AudioClip GenerateCannonClip()
    {
        int sampleRate = 44100;
        float duration = 0.45f;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];

        float phase = 0f;
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float freq = Mathf.Lerp(90f, 25f, t);
            phase += 2f * Mathf.PI * freq / sampleRate;

            float subBass = Mathf.Sin(phase);
            float explosion = (Random.value * 2f - 1f) * Mathf.Exp(-t * 9f);
            float envelope = Mathf.Exp(-t * 4.5f);

            data[i] = Mathf.Clamp((subBass * 0.7f + explosion * 0.6f) * envelope, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("CannonSFX", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // ==========================================
    // 10. SPRUNGPILZ (Mushroom Jumppad - "BOING!")
    // ==========================================
    public static void PlayBounceSound()
    {
        EnsureInstance();
        Instance.audioSource.PlayOneShot(Instance.bounceClip, 0.88f);
    }

    private static AudioClip GenerateBounceClip()
    {
        int sampleRate = 44100;
        float duration = 0.22f;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];

        float phase = 0f;
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float freq = Mathf.Lerp(140f, 580f, Mathf.Sqrt(t));
            phase += 2f * Mathf.PI * freq / sampleRate;

            float tone = Mathf.Sin(phase);
            float envelope = Mathf.Sin(t * Mathf.PI);

            data[i] = tone * envelope * 0.75f;
        }

        AudioClip clip = AudioClip.Create("BounceSFX", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // ==========================================
    // 11. SPIELER (Treffer & Tod)
    // ==========================================
    public static void PlayPlayerHurtSound()
    {
        EnsureInstance();
        if (Instance.playerDamageFileClip == null) Instance.playerDamageFileClip = LoadResourceAudio("PlayerDamageSfx");

        if (Instance.playerDamageFileClip != null)
            Instance.audioSource.PlayOneShot(Instance.playerDamageFileClip, 1.0f);
        else if (Instance.playerHurtClip != null)
            Instance.audioSource.PlayOneShot(Instance.playerHurtClip, 0.85f);
    }

    public static void PlayPlayerDeathSound()
    {
        EnsureInstance();
        Instance.audioSource.PlayOneShot(Instance.playerDeathClip, 0.95f);
    }

    private static AudioClip GeneratePlayerHurtClip()
    {
        int sampleRate = 44100;
        float duration = 0.15f;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];

        float phase = 0f;
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float freq = Mathf.Lerp(400f, 110f, t);
            phase += 2f * Mathf.PI * freq / sampleRate;

            float tone = Mathf.Sin(phase);
            float hitNoise = (Random.value * 2f - 1f) * Mathf.Exp(-t * 12f);
            float envelope = Mathf.Exp(-t * 8f);

            data[i] = (tone * 0.6f + hitNoise * 0.4f) * envelope;
        }

        AudioClip clip = AudioClip.Create("PlayerHurtSFX", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip GeneratePlayerDeathClip()
    {
        int sampleRate = 44100;
        float duration = 0.6f;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];

        float phase = 0f;
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float freq = Mathf.Lerp(600f, 60f, t * t);
            phase += 2f * Mathf.PI * freq / sampleRate;

            float tone = Mathf.Sin(phase);
            float noise = (Random.value * 2f - 1f) * (1f - t);
            float envelope = Mathf.Pow(1f - t, 2f);

            data[i] = Mathf.Clamp((tone * 0.7f + noise * 0.3f) * envelope, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("PlayerDeathSFX", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // ==========================================
    // 12. DIALOG (Typischer 2D/Retro Talk Blip)
    // ==========================================
    public static void PlayDialogBlipSound()
    {
        EnsureInstance();
        if (Instance.audioSource != null && Instance.dialogBlipClip != null)
        {
            Instance.audioSource.pitch = Random.Range(0.94f, 1.14f); // Leichte Variation für organischen 2D-Game-Talk
            Instance.audioSource.PlayOneShot(Instance.dialogBlipClip, 0.4f);
        }
    }

    private static AudioClip GenerateDialogBlipClip()
    {
        int sampleRate = 44100;
        float duration = 0.032f;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];

        float phase = 0f;
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            phase += 2f * Mathf.PI * 750f / sampleRate;

            float tone = Mathf.Sign(Mathf.Sin(phase)); // Retro Soft Square Wave
            float envelope = Mathf.Sin(t * Mathf.PI);

            data[i] = tone * envelope * 0.35f;
        }

        AudioClip clip = AudioClip.Create("DialogBlipSFX", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // ==========================================
    // 13. WATER HAND TRAP (Eigenes Audio)
    // ==========================================
    public static void PlayWaterHandTrapSound()
    {
        EnsureInstance();
        if (Instance.audioSource != null && Instance.waterHandClip != null)
        {
            Instance.audioSource.PlayOneShot(Instance.waterHandClip, 1.0f);
        }
    }

    private static AudioClip LoadResourceAudio(string resourceName)
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

        AudioClip clip = Resources.Load<AudioClip>(resourceName);
        if (clip != null) return clip;

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
}
