
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Creates a finished-looking Unity scene similar to the provided forest reference.
// No gameplay mechanics included: only level architecture and visual blockout.
public static class CreateForestLevelLikeReference
{
    const float TILE = 1f;

    [MenuItem("Tools/Level Design/Create Forest Level Like Reference")]
    public static void CreateLevel()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject("LEVEL_01_FOREST_LIKE_REFERENCE");

        Material grass = Mat("Grass_Top", new Color(0.28f, 0.70f, 0.22f));
        Material grassDark = Mat("Grass_Dark", new Color(0.12f, 0.38f, 0.14f));
        Material dirt = Mat("Dirt_Main", new Color(0.39f, 0.20f, 0.11f));
        Material dirtDark = Mat("Dirt_Dark", new Color(0.22f, 0.12f, 0.08f));
        Material stone = Mat("Cave_Stone", new Color(0.20f, 0.18f, 0.20f));
        Material wood = Mat("Wood", new Color(0.62f, 0.37f, 0.15f));
        Material sky = Mat("Sky_Window", new Color(0.28f, 0.68f, 0.90f));
        Material bgTree = Mat("Background_Trees", new Color(0.06f, 0.25f, 0.16f));
        Material vine = Mat("Vines", new Color(0.08f, 0.46f, 0.17f));
        Material danger = Mat("Danger", new Color(0.85f, 0.14f, 0.10f));
        Material purple = Mat("Purple_Gas_Visual", new Color(0.47f, 0.22f, 0.58f, 0.65f));
        Material markerP1 = Mat("Marker_P1", new Color(0.0f, 0.85f, 1.0f));
        Material markerP2 = Mat("Marker_P2", new Color(1.0f, 0.1f, 0.45f));

        CreateCamera();
        CreateLight();

        // Background cave + forest window
        Rect(root, "Dark_Background", 30, 0, 88, 29, Mat("Dark_Background_Mat", new Color(0.04f, 0.06f, 0.08f)), -4);
        Rect(root, "Forest_Window_Sky", 38, 5.5f, 28, 14, sky, -3.8f);
        for (int i = 0; i < 7; i++)
        {
            TreeSilhouette(root, 25 + i * 4.5f, -1.5f, 6 + (i % 3), bgTree);
        }

        // Cave ceiling silhouettes
        TerrainChunk(root, "Ceiling_Left_Cave", -3, 11, 24, 4, stone, dirtDark, false);
        TerrainChunk(root, "Ceiling_Right_Cave", 38, 12, 36, 4, stone, dirtDark, false);
        TerrainChunk(root, "Left_Cave_Wall", -18, -1, 4, 20, stone, dirtDark, false);
        TerrainChunk(root, "Right_Cave_Wall", 77, 0, 5, 19, stone, dirtDark, false);

        // Main route: this follows the image flow.
        TerrainChunk(root, "Start_Big_Ground", -16, -8, 15, 3, grass, dirt, true);
        TerrainChunk(root, "Low_Step_01", -8, -5, 6, 2, grass, dirt, true);
        TerrainChunk(root, "Low_Step_02", -2, -3, 8, 2, grass, dirt, true);
        TerrainChunk(root, "Rising_Step_03", 5, 0, 8, 2, grass, dirt, true);
        TerrainChunk(root, "Spring_Mushroom_Ledge", 11, 4, 8, 2, grass, dirt, true);

        TerrainChunk(root, "High_Route_Left", 22, 4.2f, 12, 2, grass, dirt, true);
        TerrainChunk(root, "High_Route_Main", 33, 4.2f, 17, 2, grass, dirt, true);

        TerrainChunk(root, "Middle_Platform_A", 25, -2.2f, 6, 1.6f, grass, dirt, true);
        TerrainChunk(root, "Middle_Platform_B", 32, -4.8f, 5, 1.6f, grass, dirt, true);

        TerrainChunk(root, "Bridge_Left_Cliff", 39, -8, 7, 3, grass, dirt, true);
        TerrainChunk(root, "Bridge_Right_Cliff", 52, -8, 7, 3, grass, dirt, true);
        Bridge(root, "Wooden_Swing_Bridge_Visual", 45.5f, -6.2f, 8.5f, wood);

        TerrainChunk(root, "Spike_Floor", 58, -8, 13, 3, grass, dirt, true);
        Spikes(root, "Spike_Row", 60, -5.2f, 9, danger);

        TerrainChunk(root, "Gas_Mushroom_Island", 61, -1.7f, 10, 2, grass, dirt, true);
        GasCloud(root, "Gas_Cloud_Visual", 66, 0.0f, purple);

        TerrainChunk(root, "Final_Lower", 72, -8, 9, 3, grass, dirt, true);
        TerrainChunk(root, "Final_Boxes_Ledge", 76, -3.8f, 6, 2, grass, dirt, true);
        TerrainChunk(root, "Exit_Platform", 82, 4.2f, 9, 2, grass, dirt, true);

        // Vertical cave / walls to make it feel like the Pinterest level
        TerrainChunk(root, "Central_Vertical_Wall", 18.5f, -4, 2.5f, 14, stone, dirtDark, false);
        Waterfall(root, "Waterfall_Decor", 18.5f, 1.5f, 1.2f, 11.0f);
        Ladder(root, "Start_Ladder", -12.5f, -4.2f, 7.5f, wood);
        Ladder(root, "Central_Ladder", 16.8f, -1.2f, 9.0f, wood);
        Ladder(root, "Exit_Ladder", 84.0f, -2.1f, 8.0f, wood);

        // Decorative blocks / crates
        CrateStack(root, 75.5f, -6.0f, wood);
        CrateStack(root, 78.0f, -2.2f, wood);
        Rock(root, "Rolling_Stone_Placeholder", 29.0f, 6.1f, 1.2f, Mat("Rock_Mat", new Color(0.43f, 0.33f, 0.21f)));
        Mushroom(root, "Spring_Mushroom_Placeholder", 12.5f, 6.2f, Mat("Mushroom_Purple", new Color(0.55f, 0.18f, 0.70f)));
        Mushroom(root, "Gas_Mushroom_Placeholder", 64.0f, 0.7f, Mat("Mushroom_Red", new Color(0.85f, 0.10f, 0.08f)));

        // Vines / plants
        for (int i = 0; i < 18; i++)
        {
            float x = -6 + i * 4.7f;
            float y = 4 + (i % 5);
            Vine(root, "Vine_" + i, x, y, 2.2f + (i % 3) * 0.7f, vine);
        }

        // Spawn / exit markers
        Marker(root, "START_P1", -19.5f, -4.4f, markerP1);
        Marker(root, "START_P2", 74.0f, -4.4f, markerP2);
        Flag(root, "EXIT", 85.5f, 7.2f, Mat("Flag_Red", new Color(1.0f, 0.12f, 0.08f)));

        // Notes in hierarchy
        Note(root, "Architecture only. Replace placeholder blocks with your pixel-art tilemap.");
        Note(root, "Path: start -> rising platforms -> spring ledge -> high route -> bridge -> gas island -> exit.");
        Note(root, "First level: tutorial-friendly, readable, not too hard.");

        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Level01_Forest_LikeReference.unity");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Created scene: Assets/Scenes/Level01_Forest_LikeReference.unity");
    }

    static void CreateCamera()
    {
        GameObject camObj = new GameObject("Main Camera");
        Camera cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 13f;
        camObj.transform.position = new Vector3(32, -1, -10);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.06f, 0.09f);
        camObj.tag = "MainCamera";
    }

    static void CreateLight()
    {
        GameObject lightObj = new GameObject("Directional Light");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.85f;
        lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
    }

    static Material Mat(string name, Color color)
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (mat.shader == null) mat = new Material(Shader.Find("Standard"));
        mat.name = name;
        mat.color = color;
        return mat;
    }

    static void Rect(GameObject parent, string name, float x, float y, float w, float h, Material mat, float z = 0)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.parent = parent.transform;
        obj.transform.position = new Vector3(x, y, z);
        obj.transform.localScale = new Vector3(w, h, 1);
        Renderer r = obj.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = mat;
    }

    static void TerrainChunk(GameObject parent, string name, float x, float y, float w, float h, Material grass, Material dirt, bool grassy)
    {
        GameObject group = new GameObject(name);
        group.transform.parent = parent.transform;

        Rect(group, "Body", x, y - h * 0.15f, w, h * 0.85f, dirt, 0);
        if (grassy) Rect(group, "Grass_Top", x, y + h * 0.38f, w, h * 0.24f, grass, 0.02f);

        // small pixel details on the dirt
        Material dark = Mat(name + "_Small_Dark_Details", new Color(0.16f, 0.09f, 0.06f));
        int count = Mathf.RoundToInt(w * 1.4f);
        for (int i = 0; i < count; i++)
        {
            float px = x - w/2 + 0.5f + (i * 0.73f) % Mathf.Max(1, w-1);
            float py = y - h/2 + 0.4f + (i % 3) * 0.45f;
            Rect(group, "Dirt_Detail_" + i, px, py, 0.28f, 0.18f, dark, 0.04f);
        }
    }

    static void TreeSilhouette(GameObject parent, float x, float y, float height, Material mat)
    {
        Rect(parent, "Tree_Trunk_BG", x, y, 0.7f, height, mat, -3.6f);
        Rect(parent, "Tree_Crown_BG", x, y + height * 0.45f, 4f, 2.7f, mat, -3.55f);
    }

    static void Vine(GameObject parent, string name, float x, float y, float h, Material mat)
    {
        Rect(parent, name, x, y, 0.15f, h, mat, 0.08f);
        Rect(parent, name + "_Leaf_1", x - 0.18f, y - 0.3f, 0.45f, 0.18f, mat, 0.09f);
        Rect(parent, name + "_Leaf_2", x + 0.18f, y + 0.5f, 0.45f, 0.18f, mat, 0.09f);
    }

    static void Waterfall(GameObject parent, string name, float x, float y, float w, float h)
    {
        Material mat = Mat("Waterfall_Blue", new Color(0.18f, 0.70f, 0.92f, 0.65f));
        Rect(parent, name, x, y, w, h, mat, 0.06f);
    }

    static void Bridge(GameObject parent, string name, float x, float y, float w, Material mat)
    {
        GameObject group = new GameObject(name);
        group.transform.parent = parent.transform;
        Rect(group, "Left_Post", x - w/2, y + 0.4f, 0.25f, 2.2f, mat, 0.07f);
        Rect(group, "Right_Post", x + w/2, y + 0.4f, 0.25f, 2.2f, mat, 0.07f);
        for (int i = 0; i < 9; i++)
        {
            float px = x - w/2 + 0.5f + i * (w-1)/8f;
            float sag = -Mathf.Abs(i - 4) * 0.08f;
            Rect(group, "Plank_" + i, px, y + sag, 0.7f, 0.18f, mat, 0.08f);
        }
        Rect(group, "Rope_Top", x, y + 0.9f, w, 0.08f, mat, 0.09f);
    }

    static void Ladder(GameObject parent, string name, float x, float y, float h, Material mat)
    {
        GameObject group = new GameObject(name);
        group.transform.parent = parent.transform;
        Rect(group, "Left", x - 0.28f, y, 0.12f, h, mat, 0.1f);
        Rect(group, "Right", x + 0.28f, y, 0.12f, h, mat, 0.1f);
        int steps = Mathf.RoundToInt(h);
        for (int i = 0; i < steps; i++)
            Rect(group, "Step_" + i, x, y - h/2 + 0.45f + i, 0.75f, 0.09f, mat, 0.11f);
    }

    static void Spikes(GameObject parent, string name, float x, float y, int count, Material mat)
    {
        GameObject group = new GameObject(name);
        group.transform.parent = parent.transform;
        for (int i = 0; i < count; i++)
        {
            Rect(group, "Spike_" + i, x + i * 0.75f, y, 0.35f, 0.9f, mat, 0.12f);
        }
    }

    static void GasCloud(GameObject parent, string name, float x, float y, Material mat)
    {
        for (int i = 0; i < 7; i++)
        {
            Rect(parent, name + "_" + i, x + i * 0.9f, y + Mathf.Sin(i)*0.25f, 1.4f, 0.8f, mat, 0.1f);
        }
    }

    static void CrateStack(GameObject parent, float x, float y, Material mat)
    {
        Rect(parent, "Crate_1", x, y, 1, 1, mat, 0.12f);
        Rect(parent, "Crate_2", x + 1, y, 1, 1, mat, 0.12f);
        Rect(parent, "Crate_3", x + 0.5f, y + 1, 1, 1, mat, 0.12f);
    }

    static void Rock(GameObject parent, string name, float x, float y, float size, Material mat)
    {
        Rect(parent, name, x, y, size, size, mat, 0.13f);
    }

    static void Mushroom(GameObject parent, string name, float x, float y, Material capMat)
    {
        Material stem = Mat(name + "_Stem", new Color(0.78f, 0.62f, 0.42f));
        Rect(parent, name + "_Stem", x, y - 0.45f, 0.5f, 0.9f, stem, 0.13f);
        Rect(parent, name + "_Cap", x, y + 0.15f, 1.6f, 0.65f, capMat, 0.14f);
    }

    static void Marker(GameObject parent, string name, float x, float y, Material mat)
    {
        Rect(parent, name, x, y, 0.9f, 1.6f, mat, 0.2f);
    }

    static void Flag(GameObject parent, string name, float x, float y, Material mat)
    {
        Material pole = Mat("Flag_Pole", new Color(0.52f, 0.31f, 0.14f));
        Rect(parent, name + "_Pole", x, y - 0.8f, 0.15f, 2.7f, pole, 0.2f);
        Rect(parent, name + "_Flag", x + 0.65f, y + 0.25f, 1.3f, 0.7f, mat, 0.21f);
    }

    static void Note(GameObject parent, string text)
    {
        GameObject note = new GameObject("NOTE - " + text);
        note.transform.parent = parent.transform;
    }
}
