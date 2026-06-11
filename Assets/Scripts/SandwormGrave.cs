using UnityEngine;
using UnityEngine.InputSystem;

public class SandwormGrave : MonoBehaviour
{
    [Header("Angriff")]
    [Tooltip("Das Prefab mit der echten Wurm-Attacke")]
    public GameObject attackPrefab;
    
    [Tooltip("Verschiebt den gespawnten Angriff (falls die Sprites nicht genau übereinander passen, z.B. X: 0, Y: 0.5)")]
    public Vector2 attackSpawnOffset = Vector2.zero;

    [Header("Grab-Animation")]
    [Tooltip("Soll die Gräber-Animation übersprungen werden und direkt der fertige Angriff spawnen?")]
    public bool skipTransition = false;

    [Tooltip("Wie heißt die Animation für dieses spezielle Grab? (z.B. TransitionSandwormAnim oder TransitionMidSandwormAnim)")]
    public string idleAnimationName = "TransitionSandwormAnim";

    [Tooltip("Wie heißt die Angriffs-Animation, die beim Bauen als Geist-Vorschau gezeigt werden soll?")]
    public string previewAnimationName = "SandwormTrapAnim";
    
    [Header("Wiederverwendbarkeit")]
    [Tooltip("Soll das Grab nach einem Angriff für immer verschwinden oder nach einer Weile wieder auftauchen?")]
    public bool isReusable = true;
    
    [Tooltip("Verschiebt das Grab in dem Moment, in dem es platziert wird (falls es nach dem Klick 'springt')")]
    public Vector2 graveOffset = Vector2.zero;

    [Tooltip("Verschiebt den Geist-Vorschauwurm nach oben/unten (damit kannst du jeden Wurm einzeln anpassen!)")]
    public float ghostYOffset = 0f;

    public bool isPlaced = false;
    private float placeTime = 0f;
    private bool hasPlayedIdle = false;

    void Start()
    {
        PlayIdle();
    }



    public void PlayPreview()
    {
        // Wird NUR vom Manager für den durchsichtigen Geist aufgerufen!
        Animator anim = GetComponent<Animator>();
        if (anim != null && !string.IsNullOrEmpty(previewAnimationName))
        {
            anim.Play(previewAnimationName, 0, 0f);
        }
    }

    void Update()
    {
        // Kein Klick mehr im Grab! Das Grab wartet nur auf das Ende seiner Animation.
    }

    public void PlayIdle()
    {
        if (hasPlayedIdle) return;
        hasPlayedIdle = true;

        placeTime = Time.time; // Speichere die Zeit des Platzierens

        // Verschiebt das Objekt exakt in dem Moment, in dem es vom Attack-Vorschaubild zum Grab wird
        transform.position += (Vector3)graveOffset;

        if (skipTransition)
        {
            // Wenn übersprungen, spawne sofort den fertigen Angriff und lösche den Gräber-Effekt!
            if (attackPrefab != null)
            {
                Vector3 spawnPos = transform.position + (Vector3)attackSpawnOffset;
                Instantiate(attackPrefab, spawnPos, Quaternion.identity);
            }
            Destroy(gameObject);
            return; // Beende die Methode hier
        }

        Animator anim = GetComponent<Animator>();
        if (anim != null && !string.IsNullOrEmpty(idleAnimationName))
        {
            anim.Play(idleAnimationName, 0, 0f);
        }

        // Starte den Timer, um sich nach der Gräber-Animation in den Angriff zu verwandeln!
        StartCoroutine(TransitionRoutine());
    }

    private System.Collections.IEnumerator TransitionRoutine()
    {
        Animator anim = GetComponent<Animator>();
        float animLength = 1.0f; // Standardwert

        // Warte kurz, damit der Animator den State laden kann
        yield return null;

        if (anim != null)
        {
            AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
            animLength = state.length;
        }

        // Warte, bis der Wurm fertig gebuddelt hat
        yield return new WaitForSeconds(animLength);

        // Spawne das Angriffs-Objekt
        if (attackPrefab != null)
        {
            Vector3 spawnPos = transform.position + (Vector3)attackSpawnOffset;
            Instantiate(attackPrefab, spawnPos, Quaternion.identity);
        }

        // Lösche das Grab, seine Arbeit ist getan!
        Destroy(gameObject);
    }
}
