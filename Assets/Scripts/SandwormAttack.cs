using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SandwormAttack : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 1;
    [Tooltip("Der Collider, der den Spieler tötet (sollte Is Trigger = true haben)")]
    public Collider2D damageCollider;
    
    [Tooltip("Zeit in Sekunden, bis der Schaden anfängt (Wann schnappt der Wurm zu?)")]
    public float damageStartDelay = 0.2f;
    
    [Tooltip("Wie lange bleibt die Hitbox tödlich?")]
    public float damageDuration = 1.0f;
    
    [Header("Animation")]
    [Tooltip("Trage hier den Namen der Angriffs-Animation ein (z.B. SandwormTrapAnim oder MidSandwormTrapAnim)")]
    public string attackAnimationName = "SandwormTrapAnim";

    [Header("Cooldown")]
    [Tooltip("Zeit in Sekunden, bis der Wurm wieder anklickbar ist (falls nicht gelöscht)")]
    public float cooldown = 3.0f;

    [Tooltip("Soll der Wurm nach der Attacke für immer gelöscht werden?")]
    public bool destroyAfterAttack = false;

    private bool isReady = true;

    void Start()
    {
        if (damageCollider != null)
        {
            damageCollider.enabled = false;
        }
        
        // Zwinge ihn in die richtige Animation (damit der Mid-Wurm nicht wie der normale aussieht), 
        // aber pausiere die Animation sofort auf dem ersten Frame, bis er angeklickt wird!
        Animator anim = GetComponent<Animator>();
        if (anim != null && !string.IsNullOrEmpty(attackAnimationName))
        {
            anim.Play(attackAnimationName, 0, 0f);
            anim.Update(0f);
            anim.speed = 0f;
        }
    }

    void Update()
    {
        if (!isReady) return;

        if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
            Collider2D col = GetComponent<Collider2D>(); // Braucht einen BoxCollider2D zum Anklicken!

            if (col != null && col.OverlapPoint(mousePos))
            {
                TriggerAttack();
            }
        }

        // Schaden austeilen (wenn Damage aktiv ist)
        if (damageCollider != null && damageCollider.enabled)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.NoFilter();
            List<Collider2D> results = new List<Collider2D>();
            damageCollider.Overlap(filter, results);

            foreach (Collider2D col in results)
            {
                if (col.CompareTag("Player"))
                {
                    HealthSystem health = col.GetComponent<HealthSystem>();
                    if (health != null)
                    {
                        health.TakeDamage(damage);
                    }
                }
            }
        }
    }

    public void TriggerAttack()
    {
        if (!isReady) return;
        isReady = false;

        // Highlight aus
        ClickableHighlight highlight = GetComponent<ClickableHighlight>();
        if (highlight != null) highlight.isTriggered = true;
        
        Animator anim = GetComponent<Animator>();
        if (anim != null && !string.IsNullOrEmpty(attackAnimationName))
        {
            anim.speed = 1f; // Lass ihn zubeißen!
            anim.Play(attackAnimationName, 0, 0f);
        }
        
        StartCoroutine(AttackSequence());
    }

    private System.Collections.IEnumerator AttackSequence()
    {
        yield return new WaitForSeconds(damageStartDelay);

        if (damageCollider != null) damageCollider.enabled = true;

        yield return new WaitForSeconds(damageDuration);

        if (damageCollider != null) damageCollider.enabled = false;

        if (destroyAfterAttack)
        {
            Destroy(gameObject);
        }
        else
        {
            // Cooldown abwarten
            yield return new WaitForSeconds(cooldown);
            
            // Wieder bereit!
            isReady = true;
            
            // Setze ihn wieder als versteckten Erdhaufen zurück!
            Animator anim = GetComponent<Animator>();
            if (anim != null && !string.IsNullOrEmpty(attackAnimationName))
            {
                anim.Play(attackAnimationName, 0, 0f);
                anim.Update(0f);
                anim.speed = 0f;
            }
            
            // Highlight wieder an
            ClickableHighlight highlight = GetComponent<ClickableHighlight>();
            if (highlight != null) highlight.isTriggered = false;
        }
    }

    private void OnDrawGizmos()
    {
        if (damageCollider != null)
        {
            Gizmos.color = (Application.isPlaying && damageCollider.enabled) ? new Color(1f, 0f, 0f, 0.7f) : new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawCube(damageCollider.bounds.center, damageCollider.bounds.size);
        }
    }
}
