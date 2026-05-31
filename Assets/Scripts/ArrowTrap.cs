using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class ArrowTrap : MonoBehaviour
{
    [Header("Animation")]
    public string animationName = "arrowTrapAnim";

    [Header("Arrow Spawning")]
    public GameObject arrowPrefab;
    public Vector2 spawnOffset; // Präzise Einstellung ohne Child-Objekt
    
    [Header("Timing")]
    public float preFireDelay = 0f;
    public float spawnDelay = 0.2f; 
    public float cooldown = 1.0f;
    
    [Header("Movement")]
    public Vector2 shootDirection = Vector2.right;
    public float arrowSpeed = 12f;
    
    [Header("Damage (Optional)")]
    public int damage = 1;

    private Animator anim;
    private bool isFiring = false;
    private Collider2D myCollider;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        myCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        if (anim != null)
        {
            anim.speed = 0;
            anim.Play(animationName, 0, 0f);
        }
    }

    private void Update()
    {
        if (!isFiring && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            if (myCollider != null && myCollider.OverlapPoint(mousePos))
            {
                StartCoroutine(FireSequence());
            }
        }
    }

    private IEnumerator FireSequence()
    {
        isFiring = true;

        // 0. Kurze Pause nach dem Klick
        yield return new WaitForSeconds(preFireDelay);

        ClickableHighlight highlight = GetComponent<ClickableHighlight>();
        if (highlight != null) highlight.isTriggered = true;

        if (anim != null)
        {
            anim.speed = 1;
            anim.Play(animationName, 0, 0f);
        }

        yield return new WaitForSeconds(spawnDelay);
        
        if (arrowPrefab != null)
        {
            // Spawn an Position + Offset
            Vector3 spawnPos = transform.position + (Vector3)spawnOffset;
            GameObject arrow = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);
            
            ArrowProjectile proj = arrow.GetComponent<ArrowProjectile>();
            if (proj != null)
            {
                proj.speed = arrowSpeed;
                proj.damage = damage;
                proj.Setup(shootDirection);
            }
        }

        yield return new WaitForEndOfFrame();
        float duration = anim.GetCurrentAnimatorStateInfo(0).length;
        float remainingTime = Mathf.Max(0, duration - spawnDelay);
        
        yield return new WaitForSeconds(remainingTime);

        if (anim != null)
        {
            anim.speed = 0;
            anim.Play(animationName, 0, 0f);
        }

        // Cooldown abwarten, bevor die Falle wieder passiv (klickbar und gehighlightet) wird
        if (cooldown > 0) yield return new WaitForSeconds(cooldown);

        isFiring = false;
        if (highlight != null) highlight.isTriggered = false;
    }

    // Zeigt den Spawnpoint im Editor an
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + (Vector3)spawnOffset, 0.1f);
    }
}
