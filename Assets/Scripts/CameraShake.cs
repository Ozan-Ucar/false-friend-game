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
    private float originalOrthoSize;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        if (cam != null) originalOrthoSize = cam.orthographicSize;
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
        StopAllCoroutines(); // Alten Shake abbrechen
        StartCoroutine(DoShake(defaultDuration, defaultMagnitude));
    }

    public void ShakeCustom(float duration, float magnitude)
    {
        StartCoroutine(DoShake(duration, magnitude));
    }

    private IEnumerator DoShake(float duration, float magnitude)
    {
        float elapsed = 0.0f;
        
        // Position merken, wo die Kamera war, als der Shake losging!
        Vector3 basePos = transform.localPosition;
        
        // Den Shake an die Kamera-Größe (Zoom) anpassen!
        // 5f ist ein guter Standardwert für 2D-Spiele. 
        // Wenn die Kamera viel größer ist (rausgezoomt), muss der Wackler stärker sein, um gleich auszusehen.
        float zoomMultiplier = 1f;
        if (cam != null && cam.orthographic)
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

            // WICHTIG: Die x und y Offsets müssen ZUR basePos addiert werden!
            transform.localPosition = new Vector3(basePos.x + x, basePos.y + y, basePos.z);
            elapsed += Time.deltaTime;

            yield return null;
        }

        transform.localPosition = basePos;
    }

    public void DoDeathZoom(Transform targetTransform)
    {
        if (cam != null)
        {
            StopAllCoroutines();
            // Wir überschreiben die Position komplett, also brauchen wir originalPos hier nicht mehr zu resetten
            StartCoroutine(DeathZoomCoroutine(targetTransform));
        }
    }

    private IEnumerator DeathZoomCoroutine(Transform target)
    {
        float elapsed = 0f;
        float startSize = cam.orthographicSize;
        Vector3 startPos = transform.position;

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
            // (wie ein CameraFollow) überschrieben werden und die Kamera nicht flackert!
            yield return new WaitForEndOfFrame();
            
            cam.orthographicSize = newSize;
            transform.position = newPos;
        }
        
        // Am Ende exakt setzen
        yield return new WaitForEndOfFrame();
        cam.orthographicSize = deathZoomTarget;
        transform.position = new Vector3(target.position.x, target.position.y, startPos.z);
    }
}
