using UnityEngine;
using UnityEditor;
using System.IO;

[InitializeOnLoad]
public class RenameMpegToMp3
{
    static RenameMpegToMp3()
    {
        EditorApplication.delayCall += () => {
            if (!EditorApplication.isPlaying && !EditorApplication.isCompiling)
            {
                ProcessMpegFiles();
            }
        };
    }

    [MenuItem("Tools/Fix All MPEG File Extensions")]
    public static void ProcessMpegFiles()
    {
        string soundsDir = Path.Combine(Application.dataPath, "Sounds");
        string resourcesDir = Path.Combine(Application.dataPath, "Resources");

        bool changed = false;
        if (FixDirectory(soundsDir)) changed = true;
        if (FixDirectory(resourcesDir)) changed = true;

        if (changed)
        {
            AssetDatabase.Refresh();
            Debug.Log("✅ Alle .mpeg Dateien wurden zu .mp3 gefixt & Unity AssetDatabase aktualisiert!");
        }

        // Stelle sicher, dass LightningRod4 als echter AudioClip (gelbe Welle) reimportiert wird
        string lr4Path = "Assets/Sounds/LightningRod4.mp3";
        if (File.Exists(Path.Combine(Application.dataPath, "Sounds/LightningRod4.mp3")))
        {
            AssetDatabase.ImportAsset(lr4Path, ImportAssetOptions.ForceUpdate);
        }
    }

    private static bool FixDirectory(string dirPath)
    {
        if (!Directory.Exists(dirPath)) return false;

        string[] files = Directory.GetFiles(dirPath, "*.mpeg", SearchOption.AllDirectories);
        if (files.Length == 0) return false;

        foreach (string file in files)
        {
            string newFilePath = Path.ChangeExtension(file, ".mp3");
            if (File.Exists(file))
            {
                if (File.Exists(newFilePath)) File.Delete(newFilePath);
                File.Move(file, newFilePath);
                Debug.Log($"[FixMPEG] Umbenannt: {Path.GetFileName(file)} -> {Path.GetFileName(newFilePath)}");
            }

            string metaFile = file + ".meta";
            string newMetaPath = newFilePath + ".meta";
            if (File.Exists(metaFile))
            {
                if (File.Exists(newMetaPath)) File.Delete(newMetaPath);
                File.Move(metaFile, newMetaPath);
            }
        }
        return true;
    }
}
