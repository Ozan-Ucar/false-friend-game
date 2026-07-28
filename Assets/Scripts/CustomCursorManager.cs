using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Verwahret und wechselt automatisch den Maus-Cursor zwischen Normal-Zustand 
/// und Hover-Zustand (wenn die Maus über Buttons, Falle, Fallen-Spawners oder klickbare Objekte fährt).
/// 
/// Funktioniert automatisch in ALLEN Szenen!
/// </summary>
public class CustomCursorManager : MonoBehaviour
{
    public static CustomCursorManager Instance { get; private set; }

    [Header("Cursor Textures - Home Levels (House, LivingRoom, Bathroom)")]
    public Texture2D homeNormalCursor;
    public Texture2D homeHoverCursor;

    [Header("Cursor Textures - Alle anderen Szenen (Menü, Boss, etc.)")]
    public Texture2D generalNormalCursor;
    public Texture2D generalHoverCursor;

    [Header("Aktive Cursors (Automatisch zugewiesen)")]
    public Texture2D normalCursor;
    public Texture2D hoverCursor;

    [Header("Einstellungen")]
    [Tooltip("Soll der Klickpunkt automatisch exakt in der Bildmitte liegen? (Empfohlen für Fadenkreuze / Hand-Icons)")]
    public bool centerHotspot = true;

    [Tooltip("Der Klick-Punkt (Hotspot) auf dem Bild (nur aktiv wenn 'centerHotspot' false ist). (0,0) = oben links.")]
    public Vector2 hotspot = Vector2.zero;

    [Tooltip("Soll auch über 2D-Collidern (z.B. Fallen, Trigger) auf den Hover-Cursor gewechselt werden?")]
    public bool check2DColliders = true;

    [Header("Cursor-Größe in Pixeln")]
    [Range(16, 512)]
    [Tooltip("Die genaue Größe des Cursors auf dem Bildschirm in Pixeln")]
    public int targetPixelSize = 220;

    private bool isHovering = false;
    private Camera mainCam;

    private Texture2D scaledNormal;
    private Texture2D scaledHover;
    private int lastTargetPixelSize = -1;
    private Texture2D lastNormalSrc;
    private Texture2D lastHoverSrc;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        AutoLoadTextures();
        ApplyCursorForScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInit()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("CustomCursorManager");
            Instance = go.AddComponent<CustomCursorManager>();
        }
    }

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        mainCam = Camera.main;
        AutoLoadTextures();
        ApplyCursorForScene(scene.name);
    }

    private void ApplyCursorForScene(string sceneName)
    {
        string sceneLower = sceneName.ToLower();
        bool isHomeLevel = sceneLower.Contains("house") || sceneLower.Contains("living") || sceneLower.Contains("bath") || sceneLower.Contains("home");

        if (isHomeLevel)
        {
            normalCursor = homeNormalCursor != null ? homeNormalCursor : generalNormalCursor;
            hoverCursor = homeHoverCursor != null ? homeHoverCursor : generalHoverCursor;
        }
        else
        {
            normalCursor = generalNormalCursor != null ? generalNormalCursor : homeNormalCursor;
            hoverCursor = generalHoverCursor != null ? generalHoverCursor : homeHoverCursor;
        }

        SetNormalCursor();
    }

    private void AutoLoadTextures()
    {
        // 1. In Standalone-Builds (.exe) & Editor über Resources laden
        if (homeNormalCursor == null) homeNormalCursor = Resources.Load<Texture2D>("PixelCursorNormal");
        if (homeHoverCursor == null) homeHoverCursor = Resources.Load<Texture2D>("PixelCursorHover");

        if (generalNormalCursor == null) generalNormalCursor = Resources.Load<Texture2D>("CursorNormal3");
        if (generalHoverCursor == null) generalHoverCursor = Resources.Load<Texture2D>("CursorHover3");

#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Texture2D");

        foreach (string g in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
            string lowerPath = path.ToLower();

            // Home-Level Cursors (PixelCursorNormal / PixelCursorHover)
            if (homeNormalCursor == null && lowerPath.Contains("pixelcursornormal"))
            {
                homeNormalCursor = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            else if (homeHoverCursor == null && lowerPath.Contains("pixelcursorhover"))
            {
                homeHoverCursor = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            // General Cursors (CursorNormal3 / CursorHover3 - bevorzugt)
            else if (generalNormalCursor == null && (lowerPath.Contains("cursornormal3") || lowerPath.Contains("cursor_normal3") || lowerPath.Contains("cursor_normal_3")))
            {
                generalNormalCursor = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            else if (generalHoverCursor == null && (lowerPath.Contains("cursorhover3") || lowerPath.Contains("cursor_hover3") || lowerPath.Contains("cursor_hover_3")))
            {
                generalHoverCursor = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            else if (generalNormalCursor == null && (lowerPath.Contains("cursornormal2") || lowerPath.Contains("cursor_normal2") || lowerPath.Contains("cursor_normal_2")))
            {
                generalNormalCursor = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            else if (generalHoverCursor == null && (lowerPath.Contains("cursorhover2") || lowerPath.Contains("cursor_hover2") || lowerPath.Contains("cursor_hover_2")))
            {
                generalHoverCursor = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
        }
#endif
    }

    void Update()
    {
        bool shouldHover = CheckIsHoveringInteractiveElement();

        if (shouldHover != isHovering)
        {
            isHovering = shouldHover;
            if (isHovering)
            {
                SetHoverCursor();
            }
            else
            {
                SetNormalCursor();
            }
        }
    }

    private Vector3 GetMousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            Vector2 pos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            return new Vector3(pos.x, pos.y, 0f);
        }
#endif
        return Input.mousePosition;
    }

    private Texture2D GetScaledTexture(Texture2D src, ref Texture2D cache, ref Texture2D lastSrc)
    {
        if (src == null) return null;

        if (cache != null && lastTargetPixelSize == targetPixelSize && lastSrc == src)
        {
            return cache;
        }

        int maxDim = Mathf.Max(src.width, src.height);
        float scale = (float)targetPixelSize / maxDim;

        int w = Mathf.Max(1, Mathf.RoundToInt(src.width * scale));
        int h = Mathf.Max(1, Mathf.RoundToInt(src.height * scale));

        RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
        rt.filterMode = FilterMode.Point;
        RenderTexture activeBackup = RenderTexture.active;
        RenderTexture.active = rt;

        Graphics.Blit(src, rt);

        Texture2D result = new Texture2D(w, h, TextureFormat.RGBA32, false);
        result.filterMode = FilterMode.Point;
        result.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        result.Apply();

        RenderTexture.active = activeBackup;
        RenderTexture.ReleaseTemporary(rt);

        cache = result;
        lastSrc = src;
        lastTargetPixelSize = targetPixelSize;
        return cache;
    }

    public void SetNormalCursor()
    {
        if (normalCursor != null)
        {
            Texture2D tex = GetScaledTexture(normalCursor, ref scaledNormal, ref lastNormalSrc);
            Vector2 hs = centerHotspot ? new Vector2(tex.width / 2f, tex.height / 2f) : hotspot;
            Cursor.SetCursor(tex, hs, CursorMode.ForceSoftware);
        }
        else
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }

    public void SetHoverCursor()
    {
        Texture2D src = hoverCursor != null ? hoverCursor : normalCursor;
        if (src != null)
        {
            Texture2D tex = GetScaledTexture(src, ref scaledHover, ref lastHoverSrc);
            Vector2 hs = centerHotspot ? new Vector2(tex.width / 2f, tex.height / 2f) : hotspot;
            Cursor.SetCursor(tex, hs, CursorMode.ForceSoftware);
        }
        else
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }

    private bool CheckIsHoveringInteractiveElement()
    {
        Vector3 mPos = GetMousePosition();

        // 1. Überprüfung von UI-Elementen (Buttons, Slider etc.)
        if (EventSystem.current != null)
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = mPos
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (RaycastResult r in results)
            {
                if (r.gameObject != null)
                {
                    // Buttons, Toggle, Slider, Dropdowns oder Clickable-Komponenten
                    if (r.gameObject.GetComponentInParent<UnityEngine.UI.Selectable>() != null ||
                        r.gameObject.GetComponentInParent<IPointerClickHandler>() != null)
                    {
                        return true;
                    }
                }
            }
        }

        // 2. Überprüfung von 2D-Objekten im Spiel (z.B. Falle, ClickableHighlight, Spawner)
        if (check2DColliders)
        {
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam != null)
            {
                Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(mPos);
                Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos);
                if (hit != null)
                {
                    // Objekt hat ein Klick-Skript oder Trigger-Verhalten
                    if (hit.GetComponentInParent<ClickableHighlight>() != null ||
                        hit.CompareTag("Trap") ||
                        hit.CompareTag("Clickable") ||
                        hit.GetComponent<MonoBehaviour>() is IPointerClickHandler)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
