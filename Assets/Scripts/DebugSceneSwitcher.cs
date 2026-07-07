using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Ein praktisches Skript, um mit festgelegten Tasten direkt in andere Level zu springen.
/// Unterstützt das NEUE Unity Input System!
/// </summary>
public class DebugSceneSwitcher : MonoBehaviour
{
    // MAGIE: Lädt das Skript automatisch in JEDER Szene, ohne dass es in der Szene liegen muss!
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoSpawn()
    {
        // Wir suchen im Projekt nach einem Prefab mit dem Namen "AutoSceneSwitcher" im "Resources" Ordner
        GameObject prefab = Resources.Load<GameObject>("AutoSceneSwitcher");
        if (prefab != null)
        {
            Instantiate(prefab);
        }
    }

    [System.Serializable]
    public class SceneHotkey
    {
        [Tooltip("Welche Taste soll gedrückt werden? (z.B. Digit1 für die Zahl 1, F1, etc.)")]
        public Key key;
        
        [Tooltip("Wie heißt die Szene exakt? (Muss in den Build Settings hinzugefügt sein!)")]
        public string sceneName;
    }

    [Header("Hotkeys (Tasten zum Szenen-Laden)")]
    [Tooltip("Füge hier deine Tasten und die dazugehörigen Szenen-Namen ein.")]
    public List<SceneHotkey> hotkeys = new List<SceneHotkey>();

    private static DebugSceneSwitcher instance;

    void Awake()
    {
        // Singleton-Pattern
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject); 
        }
    }

    void Update()
    {
        // Prüfen ob überhaupt eine Tastatur angeschlossen ist
        if (Keyboard.current == null) return;

        // Feste Taste: R zum Neustarten der aktuellen Szene
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            Debug.Log("DebugSceneSwitcher: Szene wird neu gestartet!");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        // Prüfe alle konfigurierten Tasten
        foreach (var hotkey in hotkeys)
        {
            // Schutz-Check: Verhindert Abstürze, falls Unity noch alte Tastencodes gespeichert hat!
            if (!System.Enum.IsDefined(typeof(Key), hotkey.key) || hotkey.key == Key.None)
            {
                continue;
            }

            // Abfrage für das NEUE Input System
            if (Keyboard.current[hotkey.key].wasPressedThisFrame)
            {
                if (!string.IsNullOrEmpty(hotkey.sceneName))
                {
                    Debug.Log("DebugSceneSwitcher: Lade Szene -> " + hotkey.sceneName);
                    SceneManager.LoadScene(hotkey.sceneName);
                }
            }
        }
    }
}
