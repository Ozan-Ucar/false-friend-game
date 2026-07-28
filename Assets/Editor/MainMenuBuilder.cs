using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One-click builder for the False Friend main menu.
/// Open the MainMenue scene, then run:  False Friend ▸ Build Main Menu
/// Re-running clears the previously built menu and rebuilds it.
/// </summary>
public static class MainMenuBuilder
{
    const string BgPath  = "Assets/Textures/UI/menu_background.png";
    const string RootBg  = "Menu_Background";
    const string RootUi  = "Menu_Canvas";

    // ── palette (wooden / golden look like the reference) ─────────────────────
    static readonly Color BtnText    = Hex("F2D9A0");

    [MenuItem("False Friend/Build Main Menu")]
    static void Build()
    {
        // rebuild only the UI – leave the user's Menu_Background untouched
        DestroyExisting(RootUi);
        DestroyExisting("EventSystem");
        DestroyExisting("Pause_Canvas");                        // a pause overlay never belongs in the menu
        DestroyByNames("Title", "FALSE_word", "FRIEND_word", "EscToMenu");

        var canvas = BuildCanvas();

        var mm = canvas.gameObject.AddComponent<MenuManager>();

        BuildMainButtons(canvas.transform, mm);
        var optionsPanel      = BuildOptionsPanel(canvas.transform, mm);
        var achievementsPanel = BuildOverlayPanel(canvas.transform, mm, "Panel_Achievements",
                                    "ACHIEVEMENTS", "Coming soon …");
        var creditsPanel      = BuildOverlayPanel(canvas.transform, mm, "Panel_Credits",
                                    "CREDITS", "False Friend\n\nMade by the team");

        WireMenuManager(mm, optionsPanel, achievementsPanel, creditsPanel);

        // sub-panels start hidden
        optionsPanel.SetActive(false);
        achievementsPanel.SetActive(false);
        creditsPanel.SetActive(false);

        // make sure a SoundManager exists (keeps an already-set music clip)
        var sm = UnityEngine.Object.FindFirstObjectByType<SoundManager>();
        if (sm == null) sm = new GameObject("SoundManager").AddComponent<SoundManager>();
        AssignMusicIfEmpty(sm);

        // keep the user's background sprite pinned to its chosen transform
        var bg0 = GameObject.Find("background_0");
        if (bg0 != null)
        {
            bg0.transform.position   = new Vector3(-0.02f, -0.27f, 0f);
            bg0.transform.localScale = new Vector3(1.237739f, 1.229287f, 1f);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = canvas.gameObject;
        Debug.Log("<color=#7CFC00>[MainMenuBuilder]</color> Menu built. Save the scene (Ctrl+S).");
    }

    // assigns the menu music to the SoundManager if no clip is set yet
    static void AssignMusicIfEmpty(SoundManager sm)
    {
        const string musicPath = "Assets/Audio/menu_music.mp3";
        var so = new SerializedObject(sm);
        var prop = so.FindProperty("backgroundMusic");
        if (prop != null && prop.objectReferenceValue == null)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(musicPath);
            if (clip != null)
            {
                prop.objectReferenceValue = clip;
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("<color=#7CFC00>[MainMenuBuilder]</color> Background music assigned: " + musicPath);
            }
        }
    }

    // ── Remove everything the builder created ─────────────────────────────────

    [MenuItem("False Friend/Clear Menu")]
    static void ClearMenu()
    {
        DestroyExisting(RootBg);
        DestroyExisting(RootUi);
        DestroyExisting("EventSystem");
        // sweep up any stray title objects from older builds
        DestroyByNames("Title", "FALSE_word", "FRIEND_word");

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("<color=#FFD27C>[MainMenuBuilder]</color> Menu cleared. Save the scene (Ctrl+S).");
    }

    // Destroys every object with any of the given names, wherever it sits.
    static void DestroyByNames(params string[] names)
    {
        var all = UnityEngine.Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var go in all)
            if (go != null && Array.IndexOf(names, go.name) >= 0)
                UnityEngine.Object.DestroyImmediate(go);
    }

    // ── Pause overlay (ESC) — run while a GAMEPLAY scene is open (e.g. OzanScene) ──

    [MenuItem("False Friend/Add Pause Menu (to open scene)")]
    static void AddPauseMenu()
    {
        if (SceneManager.GetActiveScene().name == "MainMenue")
        {
            EditorUtility.DisplayDialog("Falsche Szene",
                "Öffne zuerst eine GAMEPLAY-Szene (z.B. OzanScene) und führe den Befehl dort aus.\n\n" +
                "Das Pause-Menü gehört NICHT in die Menü-Szene.", "OK");
            return;
        }

        DestroyExisting("Pause_Canvas");
        DestroyExisting("EscToMenu");   // remove the old scene-switch object if present

        // make sure the sliders have a SoundManager (the menu's one persists too)
        var smObj = UnityEngine.Object.FindFirstObjectByType<SoundManager>();
        if (smObj == null) smObj = new GameObject("SoundManager").AddComponent<SoundManager>();
        AssignMusicIfEmpty(smObj);

        // EventSystem for UI clicks (only if the scene lacks one)
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
            var moduleType = Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (moduleType != null) es.AddComponent(moduleType);
            else                    es.AddComponent<StandaloneInputModule>();
        }

        var go = new GameObject("Pause_Canvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;                  // above any in-game HUD
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        var pm = go.AddComponent<PauseMenu>();

        // overlay root (this whole panel toggles on ESC)
        var panel = NewRect("Pause_Panel", go.transform);
        Stretch(panel);

        // full-screen menu background (same look as the main menu)
        var bgImg = panel.gameObject.AddComponent<Image>();
        var bgSprite = ConfigureAndLoad("Assets/Textures/UI/background_noclouds.png");
        if (bgSprite != null) { bgImg.sprite = bgSprite; bgImg.color = Color.white; }
        else                  { bgImg.color = new Color(0.05f, 0.07f, 0.12f, 0.96f); }

        // buttons: CONTINUE / OPTION / QUIT
        var resume  = MakeButton(panel, "Btn_Resume",  "CONTINUE", "continue", new Vector2(0,  40));
        var options = MakeButton(panel, "Btn_Options", "OPTION",   "option",   new Vector2(0, -60));
        var quit    = MakeButton(panel, "Btn_Quit",    "QUIT",     "quit",     new Vector2(0, -160));
        Wire(resume.onClick,  pm.Resume);
        Wire(options.onClick, pm.OpenOptions);
        Wire(quit.onClick,    pm.Quit);

        // sound settings fly-out (centered, wired Back → close)
        var soundPanel = BuildSoundPanel(panel, pm.CloseOptions);
        soundPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -20);

        // wire PauseMenu references; hide everything at start
        var so = new SerializedObject(pm);
        var pp = so.FindProperty("pausePanel");   if (pp != null) pp.objectReferenceValue = panel.gameObject;
        var op = so.FindProperty("optionsPanel"); if (op != null) op.objectReferenceValue = soundPanel;
        so.ApplyModifiedPropertiesWithoutUndo();
        soundPanel.SetActive(false);
        panel.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = go;
        Debug.Log("<color=#7CFC00>[MainMenuBuilder]</color> Pause menu added to '" +
                  SceneManager.GetActiveScene().name + "'. In Play press ESC. Save (Ctrl+S).");
    }

    // ── Add moving clouds (reuses the WaterLevelScene CloudController) ─────────

    [MenuItem("False Friend/Add Moving Clouds")]
    static void AddClouds()
    {
        DestroyExisting("Clouds");

        var go = new GameObject("Clouds");
        go.transform.position = Vector3.zero;
        var cc = go.AddComponent<CloudController>();

        // span a little wider than the camera so clouds enter/leave off-screen
        var cam = Camera.main;
        float halfW = (cam != null) ? cam.orthographicSize * cam.aspect : 9f;
        cc.spawnX   =  halfW + 2.5f;
        cc.despawnX = -(halfW + 2.5f);
        cc.moveLeftToRight = true;          // drift left → right

        // three depth layers (back = small/slow/low order, front = big/fast/high order)
        cc.cloudTypes = new List<CloudSettings>
        {
            //        prefab                                          amount  speed        scale       order  Y-band
            CloudType("Assets/Prefabs/Clouds/GrassLand_Cloud_1.prefab",     3, 0.12f,0.20f, 1.2f,1.8f,  4,  1.6f,2.8f), // back
            CloudType("Assets/Prefabs/Clouds/GrassLand_Cloud_2.prefab",     2, 0.22f,0.32f, 2.2f,2.8f,  9,  1.2f,2.4f), // mid
            CloudType("Assets/Prefabs/Clouds/GrassLand_Cloud_3 (1).prefab", 2, 0.34f,0.48f, 3.0f,4.0f, 14,  0.8f,2.0f), // front
        };

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = go;
        Debug.Log("<color=#7CFC00>[MainMenuBuilder]</color> Clouds added (3 depth layers). Press Play to see them move. Save (Ctrl+S).");
    }

    static CloudSettings CloudType(string prefabPath, int amount, float minSpeed, float maxSpeed,
                                   float minScale, float maxScale, int sortingOrder, float minY, float maxY)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
            Debug.LogWarning($"[MainMenuBuilder] Cloud prefab not found: {prefabPath}");
        return new CloudSettings
        {
            cloudName      = System.IO.Path.GetFileNameWithoutExtension(prefabPath),
            cloudPrefab    = prefab,
            amount         = amount,
            minSpeed       = minSpeed,
            maxSpeed       = maxSpeed,
            minY           = minY,
            maxY           = maxY,
            floatAmplitude = 0.15f,
            floatFrequency = 0.2f,
            minScale       = minScale,
            maxScale       = maxScale,
            sortingOrder   = sortingOrder,
        };
    }

    // ── Set ONLY the background (does not touch buttons) ──────────────────────
    // Select your image in the Project window, then run this. If nothing is
    // selected it falls back to Assets/Textures/UI/menu_background.png.

    [MenuItem("False Friend/Set Background Only")]
    static void SetBackgroundOnly()
    {
        string path = null;
        if (Selection.activeObject != null)
        {
            var sel = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!string.IsNullOrEmpty(sel) &&
                (sel.EndsWith(".png") || sel.EndsWith(".jpg") || sel.EndsWith(".jpeg")))
                path = sel;
        }
        if (path == null) path = BgPath;

        if (!System.IO.File.Exists(path))
        {
            EditorUtility.DisplayDialog("Background not found",
                $"No image at:\n{path}\n\nDrag your background image into the Project, " +
                "select it, then run this again.", "OK");
            return;
        }

        DestroyExisting(RootBg);
        var sprite = ConfigureAndLoad(path);
        PlaceBackground(sprite);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"<color=#7CFC00>[MainMenuBuilder]</color> Background set from {path}. Save the scene (Ctrl+S).");
    }

    // ── Background (world-space sprite, behind everything) ────────────────────

    static void PlaceBackground(Sprite sprite)
    {
        var go = new GameObject(RootBg);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -100;                // player / heart / decals go in front
        go.transform.position = Vector3.zero;

        // scale so it fills the orthographic camera view exactly (+ small margin)
        var cam = Camera.main;
        if (cam != null && sprite != null)
        {
            float worldH = cam.orthographicSize * 2f;
            float worldW = worldH * cam.aspect;
            var sz = sprite.bounds.size;
            go.transform.localScale = new Vector3(
                worldW / sz.x * 1.02f,
                worldH / sz.y * 1.02f, 1f);
        }
    }

    static Sprite ConfigureAndLoad(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType         = TextureImporterType.Sprite;
            importer.spriteImportMode    = SpriteImportMode.Single;
            importer.filterMode          = FilterMode.Point;   // crisp pixel art
            importer.spritePixelsPerUnit = 108f;
            importer.mipmapEnabled       = false;
            importer.SaveAndReimport();
        }
        else
        {
            Debug.LogWarning($"[MainMenuBuilder] No texture importer at {path}.");
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ── Canvas + EventSystem ──────────────────────────────────────────────────

    static Canvas BuildCanvas()
    {
        var go = new GameObject(RootUi, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        // EventSystem (input-system aware)
        var es = new GameObject("EventSystem", typeof(EventSystem));
        var moduleType = Type.GetType(
            "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (moduleType != null) es.AddComponent(moduleType);
        else                    es.AddComponent<StandaloneInputModule>();

        return canvas;
    }

    // ── Main button column ────────────────────────────────────────────────────

    static void BuildMainButtons(Transform parent, MenuManager mm)
    {
        // Full-canvas container; NO layout group so each button is free to drag.
        var panel = NewRect("Panel_Main", parent);
        // fixed RectTransform (matches the position you set up): Left 2 / Right -2 / Top 99 / Bottom -99
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.one;
        panel.offsetMin = new Vector2(2f, -99f);    // (Left, Bottom)
        panel.offsetMax = new Vector2(2f, -99f);    // (-Right, -Top)

        // manual stacking – change these two numbers (or each button's Pos Y) freely
        const float startY = 100f;   // y of the first button (canvas-centre = 0)
        const float step   = 94f;    // vertical distance between buttons

        var play        = MakeButton(panel, "Btn_Play",        "PLAY",         "play",         new Vector2(0, startY - 0 * step));
        var options     = MakeButton(panel, "Btn_Options",     "OPTION",       "option",       new Vector2(0, startY - 1 * step));
        var achievements= MakeButton(panel, "Btn_Achievements","ACHIEVEMENTS", "achievements", new Vector2(0, startY - 2 * step));
        var quit        = MakeButton(panel, "Btn_Quit",        "QUIT",         "quit",         new Vector2(0, startY - 3 * step));
        var credits     = MakeButton(panel, "Btn_Credits",     "CREDITS",      "credits",      new Vector2(0, startY - 4 * step));

        Wire(play.onClick,         mm.OnPlay);
        Wire(options.onClick,      mm.OnOptions);
        Wire(achievements.onClick, mm.OnAchievements);
        Wire(quit.onClick,         mm.OnQuit);
        Wire(credits.onClick,      mm.OnCredits);
    }

    static Button MakeButton(RectTransform parent, string name, string label, string iconName, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        Anchor(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(560, 84);

        var img = go.GetComponent<Image>();
        img.color  = Color.white;              // gradient supplies the real colour
        img.sprite = RoundedSprite;            // built-in rounded-corner UI sprite
        img.type   = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 0.32f;   // lower = rounder corners

        // golden gradient (bright top → amber bottom) like the reference
        var grad = go.AddComponent<UIGradient>();
        grad.top    = Hex("FFD24A");
        grad.bottom = Hex("E89A1C");

        // darker amber border
        var outline = go.AddComponent<Outline>();
        outline.effectColor    = Hex("8A5212");
        outline.effectDistance = new Vector2(3, -3);

        // soft drop shadow → buttons float off the background
        var shadow = go.AddComponent<Shadow>();
        shadow.effectColor    = new Color(0f, 0f, 0f, 0.4f);
        shadow.effectDistance = new Vector2(0, -6);

        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor      = Color.white;
        colors.highlightedColor = Hex("FFF0C0");      // brighten on hover
        colors.pressedColor     = Hex("E0C070");
        colors.selectedColor    = Color.white;
        colors.fadeDuration     = 0.1f;
        btn.colors = colors;
        btn.targetGraphic = img;

        // icon + label as ONE centered group (auto-sized → always balanced)
        var content = new GameObject("Content", typeof(RectTransform));
        var crt = content.GetComponent<RectTransform>();
        crt.SetParent(rt, false);
        Anchor(crt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        crt.anchoredPosition = Vector2.zero;

        var hlg = content.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment        = TextAnchor.MiddleCenter;
        hlg.spacing               = 22;
        hlg.childControlWidth     = true;  hlg.childControlHeight     = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        var icon = LoadIcon(iconName);
        if (icon != null)
        {
            var ig = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            ig.transform.SetParent(content.transform, false);
            var iimg = ig.GetComponent<Image>();
            iimg.sprite = icon;
            iimg.color  = Hex("5A3A12");      // dark brown icon on gold
            iimg.preserveAspect = true;
            var le = ig.GetComponent<LayoutElement>();
            le.preferredWidth = 50; le.preferredHeight = 50;
        }

        // label (dark brown, arcade font, even/monospaced letter spacing)
        NewText("Label", content.transform, Mono(label), 42, Hex("4A2E12"), FontStyles.Bold,
                TextAlignmentOptions.Center, ArcadeFont);

        // make the auto-layout settle immediately (not only at runtime)
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(crt);

        // hover scale animation
        go.AddComponent<ButtonHoverAnimation>();
        return btn;
    }

    // ── Options fly-out (sound settings) ──────────────────────────────────────

    static GameObject BuildOptionsPanel(Transform parent, MenuManager mm)
    {
        var panel = BuildSoundPanel(parent, mm.OnBack);
        panel.GetComponent<RectTransform>().anchoredPosition = new Vector2(660, -40);
        return panel;
    }

    // Reusable sound-settings panel (used by both the main menu and the pause overlay).
    static GameObject BuildSoundPanel(Transform parent, UnityAction onBack)
    {
        var panel = NewRect("Panel_Options", parent);
        Anchor(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(520, 400);

        StylePanel(panel.gameObject);

        var header = NewText("Header", panel, "SOUND", 40, Hex("FFD24A"), FontStyles.Bold,
                             TextAlignmentOptions.TopLeft, ArcadeFont);
        Anchor(header.GetComponent<RectTransform>(), new Vector2(0,1), new Vector2(0,1), new Vector2(0,1));
        header.GetComponent<RectTransform>().anchoredPosition = new Vector2(34, -24);
        header.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 60);

        // golden divider under the header
        var divider = NewRect("Divider", panel);
        Anchor(divider, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
        divider.anchoredPosition = new Vector2(34, -78);
        divider.sizeDelta = new Vector2(452, 4);
        var dimg = divider.gameObject.AddComponent<Image>();
        dimg.color = Hex("C8881E");

        MakeSoundRow(panel, "Fallen Sound",          MenuVolumeSlider.Channel.Fallen,     -110);
        MakeSoundRow(panel, "Hintergrundmusik",      MenuVolumeSlider.Channel.Music,      -180);
        MakeSoundRow(panel, "Player Animation Sound",MenuVolumeSlider.Channel.PlayerAnim, -250);

        var back = MakeSmallButton(panel, "Btn_Back_Options", "BACK",
                                   new Vector2(0.5f, 0f), new Vector2(0, 34));
        Wire(back.onClick, onBack);
        return panel.gameObject;
    }

    static void MakeSoundRow(RectTransform parent, string label, MenuVolumeSlider.Channel channel, float y)
    {
        var lbl = NewText("Lbl_" + label, parent, label, 24, BtnText, FontStyles.Normal,
                          TextAlignmentOptions.Left);
        var lrt = lbl.GetComponent<RectTransform>();
        Anchor(lrt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
        lrt.anchoredPosition = new Vector2(28, y);
        lrt.sizeDelta = new Vector2(290, 44);

        // slider (bound to the SoundManager further below)
        var sliderGo = DefaultControls.CreateSlider(GetResources());
        sliderGo.name = "Slider_" + label;
        var srt = sliderGo.GetComponent<RectTransform>();
        srt.SetParent(parent, false);
        Anchor(srt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
        srt.anchoredPosition = new Vector2(330, y - 14);
        srt.sizeDelta = new Vector2(180, 22);
        var slider = sliderGo.GetComponent<Slider>();
        slider.value = 0.75f;

        // colourful styling: dark track, green fill, cream rounded handle
        var round = RoundedSprite;
        var bg = sliderGo.transform.Find("Background")?.GetComponent<Image>();
        if (bg != null) { bg.color = Hex("2A1B0E"); bg.sprite = round; bg.type = Image.Type.Sliced; bg.pixelsPerUnitMultiplier = 0.5f; }

        if (slider.fillRect != null)
        {
            var fill = slider.fillRect.GetComponent<Image>();
            fill.color = Hex("6FCF4A"); fill.sprite = round; fill.type = Image.Type.Sliced; fill.pixelsPerUnitMultiplier = 0.5f;
        }
        if (slider.handleRect != null)
        {
            var handle = slider.handleRect.GetComponent<Image>();
            handle.color = Hex("FFF0C0");
            slider.handleRect.sizeDelta = new Vector2(24, 24);
        }

        // bind to the SoundManager channel
        sliderGo.AddComponent<MenuVolumeSlider>().channel = channel;
    }

    // ── Generic overlay panel (Achievements / Credits) ────────────────────────

    static GameObject BuildOverlayPanel(Transform parent, MenuManager mm, string name,
                                        string title, string body)
    {
        var panel = NewRect(name, parent);
        Stretch(panel);
        var bg = panel.gameObject.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.03f, 0.02f, 0.85f);

        var card = NewRect("Card", panel);
        Anchor(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        card.sizeDelta = new Vector2(820, 460);
        StylePanel(card.gameObject);

        var t = NewText("Title", card, title, 56, Hex("FFD24A"), FontStyles.Bold,
                        TextAlignmentOptions.Center, ArcadeFont);
        var trt = t.GetComponent<RectTransform>();
        Anchor(trt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        trt.anchoredPosition = new Vector2(0, -50);
        trt.sizeDelta = new Vector2(760, 80);

        var b = NewText("Body", card, body, 30, BtnText, FontStyles.Normal,
                        TextAlignmentOptions.Center);
        var brt = b.GetComponent<RectTransform>();
        Anchor(brt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        brt.anchoredPosition = new Vector2(0, 20);
        brt.sizeDelta = new Vector2(720, 200);

        var back = MakeSmallButton(card, "Btn_Back_" + name, "BACK",
                                   new Vector2(0.5f, 0f), new Vector2(0, 50));
        Wire(back.onClick, mm.OnBack);
        return panel.gameObject;
    }

    static Button MakeSmallButton(RectTransform parent, string name, string label,
                                  Vector2 anchor, Vector2 anchoredPos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        Anchor(rt, anchor, anchor, anchor);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(240, 64);

        var img = go.GetComponent<Image>();
        img.color  = Color.white;
        img.sprite = RoundedSprite;
        img.type   = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 0.32f;

        var grad = go.AddComponent<UIGradient>();
        grad.top    = Hex("FFD24A");
        grad.bottom = Hex("E89A1C");

        var outline = go.AddComponent<Outline>();
        outline.effectColor    = Hex("8A5212");
        outline.effectDistance = new Vector2(2, -2);

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        var c = btn.colors;
        c.highlightedColor = Hex("FFF0C0");
        c.fadeDuration = 0.1f;
        btn.colors = c;

        var txt = NewText("Label", rt, Mono(label), 30, Hex("4A2E12"), FontStyles.Bold,
                          TextAlignmentOptions.Center, ArcadeFont);
        Stretch(txt.GetComponent<RectTransform>());

        go.AddComponent<ButtonHoverAnimation>();
        return btn;
    }

    // ── MenuManager wiring via SerializedObject ───────────────────────────────

    static void WireMenuManager(MenuManager mm, GameObject options, GameObject achievements,
                                GameObject credits)
    {
        var so = new SerializedObject(mm);
        SetRef(so, "optionsPanel",      options);
        SetRef(so, "achievementsPanel", achievements);
        SetRef(so, "creditsPanel",      credits);
        var scene = so.FindProperty("firstLevelScene");
        if (scene != null) scene.stringValue = "OzanScene";   // Play target
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetRef(SerializedObject so, string prop, UnityEngine.Object value)
    {
        var p = so.FindProperty(prop);
        if (p != null) p.objectReferenceValue = value;
        else Debug.LogWarning($"[MainMenuBuilder] MenuManager has no field '{prop}'.");
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    // Unity's built-in rounded-corner UI sprite (the default button background).
    static Sprite RoundedSprite =>
        AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

    // Project's ARCADECLASSIC TMP font (falls back to default if missing).
    static TMP_FontAsset ArcadeFont =>
        AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ARCADECLASSIC SDF.asset");

    // Loads a menu icon and makes sure it's imported as a smooth sprite.
    static Sprite LoadIcon(string iconName)
    {
        string path = $"Assets/Textures/UI/Icons/{iconName}.png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType    = TextureImporterType.Sprite;
            importer.filterMode     = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void Wire(Button.ButtonClickedEvent evt, UnityAction call)
        => UnityEventTools.AddPersistentListener(evt, call);

    // Forces uniform per-letter spacing (ARCADECLASSIC isn't truly monospaced).
    // Tweak the em value if letters look too tight/wide.
    static string Mono(string s) => $"<mspace=0.7em>{s}</mspace>";

    // Gives a panel the polished wooden look: rounded, wood gradient, gold border, shadow.
    static void StylePanel(GameObject go)
    {
        var img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.color  = Color.white;
        img.sprite = RoundedSprite;
        img.type   = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 0.28f;

        var grad = go.GetComponent<UIGradient>();
        if (grad == null) grad = go.AddComponent<UIGradient>();
        grad.top    = Hex("5A3A1C");   // warm wood, opaque
        grad.bottom = Hex("2E1D0D");

        var outline = go.GetComponent<Outline>();
        if (outline == null) outline = go.AddComponent<Outline>();
        outline.effectColor    = Hex("C8881E");   // golden frame
        outline.effectDistance = new Vector2(3, -3);
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }

    static TextMeshProUGUI NewText(string name, Transform parent, string text, float size,
                                   Color color, FontStyles style, TextAlignmentOptions align,
                                   TMP_FontAsset font = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.fontStyle = style;
        t.alignment = align;
        if (font != null) t.font = font;
        return t;
    }

    static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot)
    {
        rt.anchorMin = min; rt.anchorMax = max; rt.pivot = pivot;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    // Destroys ALL root objects with this name (active or inactive), repeatedly,
    // so stray leftovers from earlier builds get cleaned up too.
    static void DestroyExisting(string name)
    {
        var all = UnityEngine.Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var go in all)
            if (go != null && go.name == name && go.transform.parent == null)
                UnityEngine.Object.DestroyImmediate(go);
    }

    static DefaultControls.Resources GetResources() => new DefaultControls.Resources();

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }
}
