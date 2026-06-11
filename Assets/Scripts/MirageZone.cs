using UnityEngine;

/// <summary>
/// Ziehe dieses Skript einfach auf IRGENDEIN Objekt in der Wüsten-Szene (z.B. auf die Kamera oder ein leeres Objekt).
/// Sobald die Szene geladen wird, schaltet das Skript den Hitzefilter global ein.
/// Wenn du die Szene verlässt, schaltet es ihn automatisch wieder aus!
/// </summary>
public class MirageZone : MonoBehaviour
{
    void OnEnable()
    {
        // 1 = Hitzeflimmern ist AN
        Shader.SetGlobalFloat("_GlobalMirageActive", 1f);
    }

    void OnDisable()
    {
        // 0 = Hitzeflimmern ist AUS (die Verzerrung wird mit 0 multipliziert)
        Shader.SetGlobalFloat("_GlobalMirageActive", 0f);
    }
}
