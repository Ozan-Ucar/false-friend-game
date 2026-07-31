using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Verwendet Punkte (Player vs. Kappa) für die 5 Level:
/// - Forest
/// - Desert
/// - Water
/// - Dungeon
/// - Factory
/// 
/// Regeln pro Level:
/// - Timer: 60 Sekunden (1 Min.)
/// - Tode: Max. 2 Tode erlaubt (bei 3 Tode geht der Punkt an Kappa)
/// - Wenn der Spieler das Level unter 60s & mit weniger als 3 Toden verlässt ➔ Punkt an Player!
/// - Wenn Timer abläuft oder 3 Tode erreicht werden ➔ Punkt an Kappa!
/// 
/// Zeigt die Punkte & den Timer oben rechts an.
/// </summary>
public class LevelScoreManager : MonoBehaviour
{
    public static LevelScoreManager Instance { get; private set; }

    [Header("Punkte-Stand")]
    public int playerPoints = 0;
    public int kappaPoints = 0;

    [Header("Level-Status")]
    public string currentLevelKey = "";
    public float timeRemaining = 60f;
    public int deathsInCurrentLevel = 0;
    public bool isTimerRunning = false;
    public bool currentLevelFinished = false;

    // Speichert, wer den Punkt für welches Level bekommen hat ("Player" oder "Kappa")
    private Dictionary<string, string> levelWinners = new Dictionary<string, string>();
    // Speichert die angesammelten Tode pro Level
    private Dictionary<string, int> levelDeathsMap = new Dictionary<string, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        InitScene(SceneManager.GetActiveScene().name);
    }

    private void Start()
    {
        InitScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInit()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("LevelScoreManager");
            Instance = go.AddComponent<LevelScoreManager>();
            Instance.InitScene(SceneManager.GetActiveScene().name);
        }
    }

    public static bool IsScoredLevel(string sceneName, out string levelKey)
    {
        levelKey = null;
        if (string.IsNullOrEmpty(sceneName)) return false;

        string lower = sceneName.ToLower();

        // Ausschluss von Menü, Home, Boss, Cutscenes, Credits
        if (lower.Contains("main") || lower.Contains("menu") || lower.Contains("house") || 
            lower.Contains("living") || lower.Contains("bath") || lower.Contains("boss") || 
            lower.Contains("credit"))
        {
            return false;
        }

        if (lower.Contains("forest")) { levelKey = "Forest"; return true; }
        if (lower.Contains("desert")) { levelKey = "Desert"; return true; }
        if (lower.Contains("water")) { levelKey = "Water"; return true; }
        if (lower.Contains("dungeon")) { levelKey = "Dungeon"; return true; }
        if (lower.Contains("factory")) { levelKey = "Factory"; return true; }

        return false;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitScene(scene.name);
    }

    [Header("Anleitung")]
    public bool hasShownForestInstruction = false;
    public bool showingForestInstruction = false;

    public bool IsAnyTutorialActiveInScene()
    {
        LevelTrapTutorial ltt = FindAnyObjectByType<LevelTrapTutorial>();
        if (ltt != null && ltt.IsTutorialPendingOrActive()) return true;

        LightningTrapTutorial ltt2 = FindAnyObjectByType<LightningTrapTutorial>();
        if (ltt2 != null && ltt2.IsTutorialPendingOrActive()) return true;

        return false;
    }

    public bool IsCutsceneActive()
    {
        CutscenePlayer cp = FindAnyObjectByType<CutscenePlayer>();
        if (cp != null && cp.IsPlaying) return true;
        return false;
    }

    private void InitScene(string sceneName)
    {
        string levelKey;
        if (IsScoredLevel(sceneName, out levelKey))
        {
            currentLevelKey = levelKey;
            currentLevelFinished = false;

            if (!levelDeathsMap.ContainsKey(levelKey))
            {
                levelDeathsMap[levelKey] = 0;
            }
            deathsInCurrentLevel = levelDeathsMap[levelKey];

            if (!levelWinners.ContainsKey(levelKey))
            {
                timeRemaining = 60f;
                // Warten, bis Tutorial vorbei ist
                isTimerRunning = false;
            }
            else
            {
                isTimerRunning = false;
                timeRemaining = 0f;
            }
        }
        else
        {
            currentLevelKey = "";
            isTimerRunning = false;
            showingForestInstruction = false;
        }
    }

    private void Update()
    {
        if (string.IsNullOrEmpty(currentLevelKey) || currentLevelFinished) return;

        // 1. Solange ein Tutorial oder eine Cutscene läuft ➔ Timer stoppen & HUD ausblenden
        if (IsAnyTutorialActiveInScene() || IsCutsceneActive())
        {
            isTimerRunning = false;
            return;
        }

        // 2. Nur für Forest: Nach dem Tutorial 1x Anleitung einblenden!
        if (currentLevelKey == "Forest" && !hasShownForestInstruction)
        {
            showingForestInstruction = true;
            isTimerRunning = false;

            // Klick oder Leertaste/Enter blendet die Anleitung aus und startet den Timer!
            if ((UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame) ||
                (UnityEngine.InputSystem.Keyboard.current != null && (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame || UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame)))
            {
                showingForestInstruction = false;
                hasShownForestInstruction = true;
                isTimerRunning = true;
            }
            return;
        }

        // 3. Normaler Timer-Start nach dem Tutorial (oder WaterLevel ohne Tutorial)
        if (!levelWinners.ContainsKey(currentLevelKey) && !isTimerRunning)
        {
            isTimerRunning = true;
        }

        if (isTimerRunning)
        {
            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                isTimerRunning = false;
                AwardPointToKappa(currentLevelKey, "Zeit abgelaufen (1 Min)!");
            }
        }
    }

    public void OnPlayerDeath()
    {
        if (string.IsNullOrEmpty(currentLevelKey)) return;

        if (!levelDeathsMap.ContainsKey(currentLevelKey))
            levelDeathsMap[currentLevelKey] = 0;

        levelDeathsMap[currentLevelKey]++;
        deathsInCurrentLevel = levelDeathsMap[currentLevelKey];

        Debug.Log($"[LevelScoreManager] Tod im Level '{currentLevelKey}': {deathsInCurrentLevel}/3");

        // Bei 3 Toden ➔ Punkt an Kappa!
        if (deathsInCurrentLevel >= 3 && !levelWinners.ContainsKey(currentLevelKey))
        {
            AwardPointToKappa(currentLevelKey, "3 Tode erreicht!");
        }
    }

    public void OnLevelCompleted(string completedSceneName)
    {
        string levelKey;
        if (!IsScoredLevel(completedSceneName, out levelKey)) return;

        if (levelWinners.ContainsKey(levelKey)) return; // Schon entschieden

        int deaths = levelDeathsMap.ContainsKey(levelKey) ? levelDeathsMap[levelKey] : 0;

        if (timeRemaining > 0f && deaths < 3)
        {
            AwardPointToPlayer(levelKey);
        }
        else
        {
            AwardPointToKappa(levelKey, "Zu langsam oder zu viele Tode!");
        }
    }

    private void AwardPointToPlayer(string levelKey)
    {
        if (levelWinners.ContainsKey(levelKey)) return;

        levelWinners[levelKey] = "Player";
        playerPoints++;
        isTimerRunning = false;
        currentLevelFinished = true;
        Debug.Log($"[LevelScoreManager] 🎉 PUNKT FÜR PLAYER im Level '{levelKey}'! Stand: Player {playerPoints} | Kappa {kappaPoints}");
    }

    private void AwardPointToKappa(string levelKey, string reason)
    {
        if (levelWinners.ContainsKey(levelKey)) return;

        levelWinners[levelKey] = "Kappa";
        kappaPoints++;
        isTimerRunning = false;
        currentLevelFinished = true;
        Debug.Log($"[LevelScoreManager] 👹 PUNKT FÜR KAPPA im Level '{levelKey}' ({reason})! Stand: Player {playerPoints} | Kappa {kappaPoints}");
    }

    private Texture2D roundedBgTex;
    private Font arcadeFont;

    private Texture2D GetRoundedTexture()
    {
        if (roundedBgTex != null) return roundedBgTex;

        int size = 64;
        float cornerRadius = 18f;
        Color bgColor = new Color(0.04f, 0.04f, 0.09f, 0.92f); // Deep dark retro glass
        Color borderColor = new Color(0.2f, 0.6f, 1.0f, 0.45f); // Neon glowing cyan border

        roundedBgTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(0, Mathf.Max(cornerRadius - x, x - (size - 1 - cornerRadius)));
                float dy = Mathf.Max(0, Mathf.Max(cornerRadius - y, y - (size - 1 - cornerRadius)));
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist > cornerRadius)
                {
                    pixels[y * size + x] = Color.clear;
                }
                else if (dist > cornerRadius - 2f)
                {
                    pixels[y * size + x] = borderColor;
                }
                else
                {
                    pixels[y * size + x] = bgColor;
                }
            }
        }

        roundedBgTex.SetPixels(pixels);
        roundedBgTex.Apply();
        return roundedBgTex;
    }

    private void OnGUI()
    {
        // Nur in den 5 betroffenen Leveln oben rechts anzeigen
        if (string.IsNullOrEmpty(currentLevelKey)) return;

        // Solange ein Tutorial oder eine Cutscene läuft, blenden wir die HUD & Popups aus
        if (IsAnyTutorialActiveInScene() || IsCutsceneActive()) return;

        Texture2D bgTex = GetRoundedTexture();

        if (arcadeFont == null)
        {
            arcadeFont = Resources.Load<Font>("ARCADECLASSIC");
#if UNITY_EDITOR
            if (arcadeFont == null)
            {
                arcadeFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/ARCADECLASSIC.TTF");
            }
#endif
        }

        // Wenn Forest-Anleitung aktiv ist: Mittiges PopUp anzeigen!
        if (showingForestInstruction)
        {
            GUIStyle instrStyle = new GUIStyle(GUI.skin.box);
            instrStyle.normal.background = bgTex;
            instrStyle.border = new RectOffset(18, 18, 18, 18);
            if (arcadeFont != null) instrStyle.font = arcadeFont;
            instrStyle.fontSize = 28;
            instrStyle.alignment = TextAnchor.MiddleCenter;
            instrStyle.padding = new RectOffset(30, 30, 25, 25);
            instrStyle.richText = true;

            float w = 920f;
            float h = 320f;
            Rect centerRect = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);

            string text = "<color=#FFD700><size=34>PUNKTESYSTEM</size></color>\n\n" +
                         "<color=#00FF66>ANNI</color>    Ziel in unter 1 Min  (Max 2 Tode)\n" +
                         "<color=#FF3333>KAPPA</color>   Stoppe Anni  (1 Min oder 3 Tode)\n\n" +
                         "<color=#FFFF00><size=22>[ DRUECKE LEERTASTE ODER KLICKE ZUM STARTEN ]</size></color>";

            GUI.Box(centerRect, text, instrStyle);
            return;
        }

        int mins = Mathf.FloorToInt(timeRemaining / 60f);
        int secs = Mathf.FloorToInt(timeRemaining % 60f);
        string timeStr = string.Format("{0}:{1:00}", mins, secs);

        GUIStyle bgStyle = new GUIStyle(GUI.skin.box);
        bgStyle.normal.background = bgTex;
        bgStyle.border = new RectOffset(18, 18, 18, 18);
        if (arcadeFont != null) bgStyle.font = arcadeFont;
        bgStyle.fontSize = 22;
        bgStyle.alignment = TextAnchor.MiddleLeft;
        bgStyle.padding = new RectOffset(18, 18, 14, 14);
        bgStyle.richText = true;

        float boxWidth = 340f;
        float boxHeight = 115f;
        float posX = Screen.width - boxWidth - 15f;
        float posY = 15f;

        // Im Dungeon-Level exakt mittig oben auf der X-Achse!
        if (currentLevelKey == "Dungeon")
        {
            posX = (Screen.width - boxWidth) / 2f;
        }

        Rect rect = new Rect(posX, posY, boxWidth, boxHeight);

        string winnerStr = "";
        if (levelWinners.ContainsKey(currentLevelKey))
        {
            string winner = levelWinners[currentLevelKey];
            winnerStr = (winner == "Player" || winner == "Anni") 
                ? "\n<color=#00FF66>POINT: ANNI</color>" 
                : "\n<color=#FF3333>POINT: KAPPA</color>";
        }

        string display = $"<color=#00FF66>ANNI: {playerPoints}</color>    <color=#FF3333>KAPPA: {kappaPoints}</color>\n" +
                         $"<color=#FFFFFF>TIME: {timeStr}</color>\n" +
                         $"<color=#AAAAAA>DEATHS: {deathsInCurrentLevel}/3</color>" +
                         winnerStr;

        GUI.Box(rect, display, bgStyle);
    }
}
