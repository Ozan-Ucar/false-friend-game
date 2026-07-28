using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class SetupSceneSounds
{
    static SetupSceneSounds()
    {
        EditorApplication.delayCall += () => {
            if (!EditorApplication.isPlaying && !EditorApplication.isCompiling)
            {
                AssetDatabase.Refresh();
                AutoSetupCurrentSceneSounds();
            }
        };
    }

    [MenuItem("Tools/Setup All Scene Sounds")]
    public static void SetupAllScenes()
    {
        string currentScenePath = SceneManager.GetActiveScene().path;

        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
        foreach (string guid in sceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            SetupSoundForOpenScene();
            EditorSceneManager.SaveScene(scene);
        }

        if (!string.IsNullOrEmpty(currentScenePath))
        {
            EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
        }

        Debug.Log("✅ Alle Szenen wurden erfolgreich mit SceneSoundManager & AudioClips ausgestattet!");
    }

    public static void AutoSetupCurrentSceneSounds()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        SetupSoundForOpenScene();
    }

    private static void SetupSoundForOpenScene()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.isLoaded) return;

        // 1. AudioListener an der Hauptkamera sicherstellen!
        Camera mainCam = Camera.main;
        if (mainCam == null) mainCam = Object.FindFirstObjectByType<Camera>();

        if (mainCam != null)
        {
            AudioListener listener = mainCam.GetComponent<AudioListener>();
            if (listener == null)
            {
                mainCam.gameObject.AddComponent<AudioListener>();
                EditorUtility.SetDirty(mainCam.gameObject);
                Debug.Log($"[SetupSceneSounds] AudioListener zur Kamera in '{activeScene.name}' hinzugefügt.");
            }
        }

        // 2. SceneSoundManager GameObject suchen oder erstellen
        SceneSoundManager ssm = Object.FindFirstObjectByType<SceneSoundManager>();
        if (ssm == null)
        {
            GameObject ssmGo = new GameObject("SceneSoundManager");
            ssm = ssmGo.AddComponent<SceneSoundManager>();
            Undo.RegisterCreatedObjectUndo(ssmGo, "Create SceneSoundManager");
            Debug.Log($"[SetupSceneSounds] SceneSoundManager GameObject in '{activeScene.name}' erstellt.");
        }

        // 3. AudioClips zuweisen
        string name = activeScene.name;
        bool modified = false;

        if (name.Contains("Living") || name.Contains("House") || name.Contains("Bath") || name.Contains("Home"))
        {
            if (ssm.backgroundMusic == null)
            {
                ssm.backgroundMusic = LoadAudioClip("HomeBackgroundMusic");
                modified = true;
            }
        }
        else if (name.Contains("Forest") || name.Contains("Level01"))
        {
            if (ssm.backgroundMusic == null)
            {
                ssm.backgroundMusic = LoadAudioClip("ForestBackgroundmusic");
                modified = true;
            }
            if (ssm.ambienceSound == null)
            {
                ssm.ambienceSound = LoadAudioClip("ForestKulisse");
                modified = true;
            }
        }
        else if (name.Contains("Factory"))
        {
            if (ssm.backgroundMusic == null)
            {
                ssm.backgroundMusic = LoadAudioClip("FactoryBackground");
                modified = true;
            }
            if (ssm.ambienceSound == null)
            {
                ssm.ambienceSound = LoadAudioClip("FactoryKulisse");
                modified = true;
            }
        }
        else if (name.Contains("Dungeon"))
        {
            if (ssm.backgroundMusic == null)
            {
                ssm.backgroundMusic = LoadAudioClip("DungeonBackgroundMusic");
                modified = true;
            }
        }
        else if (name.Contains("Stronghold"))
        {
            if (ssm.backgroundMusic == null)
            {
                ssm.backgroundMusic = LoadAudioClip("StrongholdBackgroundMusic");
                modified = true;
            }
        }
        else if (name.Contains("Desert"))
        {
            if (ssm.ambienceSound == null)
            {
                ssm.ambienceSound = LoadAudioClip("soundreality-sandstorm");
                modified = true;
            }
        }
        else if (name.Contains("Water"))
        {
            if (ssm.ambienceSound == null)
            {
                ssm.ambienceSound = LoadAudioClip("WaterLevelKulisse");
                modified = true;
            }
        }
        else if (name.Contains("Boss"))
        {
            if (ssm.backgroundMusic == null)
            {
                ssm.backgroundMusic = LoadAudioClip("BossRoomBackgroundMusic");
                modified = true;
            }
        }
        else if (name.Contains("Credit"))
        {
            if (ssm.backgroundMusic == null)
            {
                ssm.backgroundMusic = LoadAudioClip("CreditBackgroundmusic");
                modified = true;
            }
        }
        else if (name.Contains("Title") || name.Contains("Main"))
        {
            if (ssm.backgroundMusic == null)
            {
                ssm.backgroundMusic = LoadAudioClip("TittlescreenMusic");
                modified = true;
            }
        }

        if (modified)
        {
            EditorUtility.SetDirty(ssm.gameObject);
            EditorSceneManager.MarkSceneDirty(activeScene);
        }
    }

    private static AudioClip LoadAudioClip(string searchName)
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Sounds", "Assets/Resources" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.ToLower().Contains(searchName.ToLower()))
            {
                return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
        }
        return null;
    }
}
