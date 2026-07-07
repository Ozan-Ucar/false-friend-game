using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CannonBall : MonoBehaviour
{
    [Header("Einstellungen")]
    public float speed = 15f;
    public int damage = 1;
    public float lifetime = 5f;
    
    private Vector2 moveDirection;
    private bool isFired = false;

    void Start()
    {
        // Automatisch nach 'lifetime' Sekunden zerstören
        Destroy(gameObject, lifetime);
    }

    public void Fire(Vector2 direction)
    {
        moveDirection = direction.normalized;
        isFired = true;
        
        // Rotiere die Kugel in Flugrichtung (falls das Sprite z.B. eine Rakete ist)
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void Update()
    {
        if (isFired)
        {
            transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. Check ob es der Spieler ist
        if (collision.CompareTag("Player"))
        {
            HealthSystem health = collision.GetComponent<HealthSystem>();
            if (health != null)
            {
                health.TakeDamage(damage);
                
                // Zerstöre die Kugel nach dem Treffer
                Destroy(gameObject);
            }
        }
        // 2. Wenn es Wände oder Boden trifft (Layer "Ground")
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
