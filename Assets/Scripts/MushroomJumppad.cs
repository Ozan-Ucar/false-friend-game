using UnityEngine;

public class MushroomJumppad : MonoBehaviour
{
    [Header("Settings")]
    public float bounceForce = 25f;
    public string bounceParam = "bounce";
    public Animator animator;

    private void Update()
    {
        bool isJuicy = JuiceManager.Instance == null || JuiceManager.Instance.isJuicy;
        if (animator != null) animator.enabled = isJuicy;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // launch player
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceForce);
                
                bool isJuicy = JuiceManager.Instance == null || JuiceManager.Instance.isJuicy;
                // play animation
                if (isJuicy && animator != null)
                {
                    animator.SetTrigger(bounceParam);
                }
            }
        }
    }
}
