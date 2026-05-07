using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 1;
    public float lifetime = 5f;
    
    private Vector2 direction;

    public void Setup(Vector2 dir)
    {
        direction = dir.normalized;
        
        // Pfeil in Flugrichtung drehen
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // Da der Sprite standardmäßig nach LINKS schaut (180 Grad):
        // Wir korrigieren die Rotation so, dass "links" der Nullpunkt ist
        transform.rotation = Quaternion.Euler(0, 0, angle + 180f);

        // Sprite spiegeln, wenn er nach RECHTS fliegt
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.flipX = (direction.x > 0);
        }
        
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            HealthSystem health = other.GetComponent<HealthSystem>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
        else if (other.CompareTag("Ground") || other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            // Pfeil bleibt stecken oder wird zerstört, wenn er die Wand trifft
            Destroy(gameObject);
        }
    }
}
