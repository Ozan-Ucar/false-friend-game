using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AspectRatioEnforcer : MonoBehaviour
{
    [Header("Ziel-Format (z.B. 16:9)")]
    public float targetAspectX = 16f;
    public float targetAspectY = 9f;

    private Camera cam;
    private float lastAspect = 0f;
    private Camera bgCamera;

    void Start()
    {
        cam = GetComponent<Camera>();
        
        // Erstelle vollautomatisch eine zweite Kamera ganz im Hintergrund,
        // die nur dafür da ist, den Bereich der schwarzen Balken schwarz zu malen.
        GameObject bgCamObj = new GameObject("BlackBarsBackgroundCamera");
        bgCamera = bgCamObj.AddComponent<Camera>();
        
        // Die Hintergrund-Kamera muss hinter unserer Main Camera rendern
        bgCamera.depth = cam.depth - 1; 
        bgCamera.clearFlags = CameraClearFlags.SolidColor;
        bgCamera.backgroundColor = Color.black;
        bgCamera.cullingMask = 0; // Rendert keine Objekte aus dem Spiel, sondern nur das pure Schwarz
        
        // Sicherheitshalber den AudioListener löschen (Unity warnt sonst, wenn es zwei gibt)
        AudioListener al = bgCamObj.GetComponent<AudioListener>();
        if (al != null) Destroy(al);
        
        // Setze das Format, sodass es sich nicht mehr verziehen kann
        EnforceAspectRatio();
    }

    void Update()
    {
        // Überprüfe jeden Frame, ob der Spieler die Fenstergröße zieht/ändert
        float currentAspect = (float)Screen.width / (float)Screen.height;
        if (Mathf.Abs(currentAspect - lastAspect) > 0.001f)
        {
            EnforceAspectRatio();
        }
    }

    void EnforceAspectRatio()
    {
        if (cam == null) return;

        float targetAspect = targetAspectX / targetAspectY;
        float windowAspect = (float)Screen.width / (float)Screen.height;
        
        // Berechne das Verhältnis von Bildschirm zu unserem Zielformat
        float scaleHeight = windowAspect / targetAspect;
        
        lastAspect = windowAspect;

        if (scaleHeight < 1.0f)
        {
            // Bildschirm ist "höher" als 16:9 (z.B. 16:10 Laptop). 
            // -> Letterbox: Schwarze Balken Oben und Unten!
            Rect rect = cam.rect;

            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;
            
            cam.rect = rect;
        }
        else
        {
            // Bildschirm ist "breiter" als 16:9 (z.B. Ultrawide). 
            // -> Pillarbox: Schwarze Balken Links und Rechts!
            float scaleWidth = 1.0f / scaleHeight;

            Rect rect = cam.rect;

            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;

            cam.rect = rect;
        }
    }
}
