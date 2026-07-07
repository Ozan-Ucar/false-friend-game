using UnityEngine;
using UnityEngine.InputSystem; // Für Keyboard Abfrage
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    [Header("Standard Einstellungen (Hit)")]
    public float defaultDuration = 0.2f;
    public float defaultMagnitude = 0.1f;

    [Header("Feineinstellung")]
    [Tooltip("Soll der Shake zum Ende hin schwächer werden?")]
    public bool useDecay = true;

    [Header("Death Zoom")]
    [Tooltip("Wie lange der Zoom in echten Sekunden dauert")]
    public float deathZoomDuration = 1.5f;
    [Tooltip("Ziel-Größe der Kamera (kleinerer Wert = näher dran)")]
    public float deathZoomTarget = 3f;
    [Tooltip("Slow-Motion Stärke (1 = normal, 0.2 = sehr langsam)")]
    [Range(0.01f, 1f)]
    public float deathSlowMoScale = 0.2f;

    private Camera cam;
    private bool isShaking = false;
    private Vector3 originalLocalPos;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        // Test-Funktion: I Taste zum Wackeln
        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
        {
            ShakeHit();
        }
    }

    public void ShakeHit()
    {
        if (isTutorialZooming) return; // Keine Shakes während Cutscenes!
        if (!isShaking && cam != null) originalLocalPos = cam.transform.localPosition;
        StopAllCoroutines(); 
        StartCoroutine(DoShake(defaultDuration, defaultMagnitude));
    }

    public void ShakeCustom(float duration, float magnitude)
    {
        if (isTutorialZooming) return; // Keine Shakes während Cutscenes!
        if (!isShaking && cam != null) originalLocalPos = cam.transform.localPosition;
        StopAllCoroutines();
        StartCoroutine(DoShake(duration, magnitude));
    }

    private IEnumerator DoShake(float duration, float magnitude)
    {
        if (cam == null) yield break;
        isShaking = true;

        float elapsed = 0.0f;
        
        // WICHTIG: Wir nutzen die safe original position, damit sich das Bild nie permanent verschiebt!
        Vector3 basePos = originalLocalPos;
        
        float zoomMultiplier = 1f;
        if (cam.orthographic)
        {
            zoomMultiplier = cam.orthographicSize / 5f;
        }

        while (elapsed < duration)
        {
            float currentMagnitude = magnitude * zoomMultiplier;
            
            if (useDecay)
            {
                currentMagnitude = Mathf.Lerp(magnitude * zoomMultiplier, 0, elapsed / duration);
            }

            float x = Random.Range(-1f, 1f) * currentMagnitude;
            float y = Random.Range(-1f, 1f) * currentMagnitude;

            cam.transform.localPosition = new Vector3(basePos.x + x, basePos.y + y, basePos.z);
            
            // unscaledDeltaTime ist GANZ WICHTIG, da der HealthSystem den HitStop 
            // (Time.timeScale = 0) nutzt! Sonst pausiert der Wackler.
            elapsed += Time.unscaledDeltaTime; 
            yield return null;
        }

        cam.transform.localPosition = basePos;
        isShaking = false;
    }

    public void DoDeathZoom(Transform targetTransform)
    {
        if (cam != null)
        {
            StopAllCoroutines();
            StartCoroutine(DeathZoomCoroutine(targetTransform));
        }
    }

    private IEnumerator DeathZoomCoroutine(Transform target)
    {
        if (cam == null) yield break;

        float elapsed = 0f;
        float startSize = cam.orthographicSize;
        Vector3 startPos = cam.transform.position;

        // Einstellbare Slow-Motion aktivieren
        Time.timeScale = deathSlowMoScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        while (elapsed < deathZoomDuration)
        {
            // unscaledDeltaTime sorgt dafür, dass der Zoom in echter Zeit passiert, 
            // auch wenn das Spiel in Slow-Motion läuft
            elapsed += Time.unscaledDeltaTime; 
            float t = Mathf.Clamp01(elapsed / deathZoomDuration);
            
            // Clean Cubic Ease-Out
            float curveT = 1f - Mathf.Pow(1f - t, 3f); 
            
            // Berechne neue Werte
            float newSize = Mathf.Lerp(startSize, deathZoomTarget, curveT);
            
            // Z-Position (Tiefe) beibehalten, X und Y zum Player interpolieren
            Vector3 targetPos = new Vector3(target.position.x, target.position.y, startPos.z);
            Vector3 newPos = Vector3.Lerp(startPos, targetPos, curveT);
            
            // Wir warten bis ans Ende des Frames. So stellen wir sicher, dass andere Kamera-Skripte 
            // überschrieben werden und die Kamera nicht flackert!
            yield return new WaitForEndOfFrame();
            
            cam.orthographicSize = newSize;
            cam.transform.position = newPos;
        }
        
        // Am Ende exakt setzen
        yield return new WaitForEndOfFrame();
        cam.orthographicSize = deathZoomTarget;
        cam.transform.position = new Vector3(target.position.x, target.position.y, startPos.z);
    }

    // ==========================================
    // TUTORIAL ZOOM (KEIN SLOW-MO, KEIN TOD)
    // ==========================================
    private float preTutorialSize;
    private Vector3 preTutorialLocalPos;
    private bool isTutorialZooming = false;

    public void DoTutorialZoom(Transform targetTransform, float targetSize, float duration)
    {
        if (cam != null)
        {
            preTutorialSize = cam.orthographicSize;
            preTutorialLocalPos = cam.transform.localPosition;
            
            isTutorialZooming = true;
            StopAllCoroutines();
            StartCoroutine(TutorialZoomCoroutine(targetTransform, targetSize, duration));
        }
    }

    public void ResetTutorialZoom(float duration)
    {
        if (cam != null)
        {
            StopAllCoroutines();
            // Nutze exakt die Größe, die die Kamera VOR dem Tutorial hatte!
            StartCoroutine(TutorialZoomCoroutine(cam.transform, preTutorialSize, duration, true)); 
        }
    }

    private IEnumerator TutorialZoomCoroutine(Transform target, float targetSize, float duration, bool resetToOrigin = false)
    {
        if (cam == null) yield break;

        float elapsed = 0f;
        float startSize = cam.orthographicSize;
        Vector3 startPos = cam.transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; 
            float t = Mathf.Clamp01(elapsed / duration);
            
            // Clean Ease-In-Out (SmoothStep)
            float curveT = t * t * (3f - 2f * t);
            
            float newSize = Mathf.Lerp(startSize, targetSize, curveT);
            
            Vector3 targetPos;
            if (resetToOrigin)
            {
                // Wenn wir resetten, gehen wir exakt zur Position VOR dem Tutorial zurück
                targetPos = new Vector3(preTutorialLocalPos.x, preTutorialLocalPos.y, startPos.z);
            }
            else
            {
                targetPos = new Vector3(target.position.x, target.position.y, startPos.z);
            }
            
            Vector3 newPos = Vector3.Lerp(startPos, targetPos, curveT);
            
            yield return new WaitForEndOfFrame();
            
            cam.orthographicSize = newSize;
            if (resetToOrigin)
            {
                cam.transform.localPosition = newPos;
            }
            else
            {
                cam.transform.position = newPos;
            }
        }
        
        yield return new WaitForEndOfFrame();
        cam.orthographicSize = targetSize;
        
        if (resetToOrigin)
        {
            cam.transform.localPosition = new Vector3(preTutorialLocalPos.x, preTutorialLocalPos.y, startPos.z);
        }
        else
        {
            cam.transform.position = new Vector3(target.position.x, target.position.y, startPos.z);
        }
        
        isTutorialZooming = false;
    }
}
