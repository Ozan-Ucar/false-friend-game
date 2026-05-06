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

    private Vector3 originalPos;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        originalPos = transform.localPosition;
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

        while (elapsed < duration)
        {
            float currentMagnitude = magnitude;
            
            if (useDecay)
            {
                currentMagnitude = Mathf.Lerp(magnitude, 0, elapsed / duration);
            }

            float x = Random.Range(-1f, 1f) * currentMagnitude;
            float y = Random.Range(-1f, 1f) * currentMagnitude;

            transform.localPosition = new Vector3(x, y, originalPos.z);
            elapsed += Time.deltaTime;

            yield return null;
        }

        transform.localPosition = originalPos;
    }
}
