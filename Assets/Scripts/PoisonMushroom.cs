using UnityEngine;
using System.Collections;

public class PoisonMushroom : MonoBehaviour
{
    [Header("Einstellungen")]
    public GameObject poisonCloudPrefab;
    public float shakeTime = 0.3f; // Zeit zum Zittern vor dem Platzen
    public float explosionScale = 1.5f; // Kurz größer werden beim Platzen

    private bool hasExploded = false;
    public bool IsExploded { get { return hasExploded; } }

    // === SEQUENZ STARTEN ===
    private void OnMouseDown()
    {
        if (!hasExploded)
        {
            // Highlight entfernen
            ClickableHighlight highlight = GetComponent<ClickableHighlight>();
            if (highlight != null) highlight.isTriggered = true;

            StartCoroutine(Sequence_MushroomExplosion());
        }
    }

    private IEnumerator Sequence_MushroomExplosion()
    {
        hasExploded = true;

        // 1. ZITTERN (Hintereinander ausführen)
        Vector3 originalPos = transform.position;
        float elapsed = 0;
        while (elapsed < shakeTime)
        {
            transform.position = originalPos + (Vector3)Random.insideUnitCircle * 0.1f;
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPos;

        // 2. PLATZEN (Kurzer visueller Effekt durch Skalierung)
        transform.localScale *= explosionScale;

        // NEU: Kamera wackeln lassen
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.ShakeCustom(0.2f, 0.3f);
        }

        // 3. WOLKE SPAWNEN
        if (poisonCloudPrefab != null)
        {
            Instantiate(poisonCloudPrefab, transform.position, Quaternion.identity);
        }

        // 4. PILZ VERSCHWINDET (Hintereinander abgeschlossen)
        yield return new WaitForSeconds(0.1f);
        gameObject.SetActive(false);
    }
}
