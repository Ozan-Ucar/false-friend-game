using UnityEngine;
using System.Collections;

public class StoneShard : MonoBehaviour
{
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Zufällige Drehung beim Erscheinen
        transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
        
        // Nach einer kurzen Zeit anfangen auszufaden
        StartCoroutine(FadeAndDestroy());
    }

    private IEnumerator FadeAndDestroy()
    {
        // Wartezeit bevor das Ausfaden beginnt
        yield return new WaitForSeconds(1.2f);
        
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            float elapsed = 0f;
            float duration = 0.8f;
            Color startColor = sr.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1, 0, elapsed / duration);
                sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
        }

        Destroy(gameObject);
    }
}
