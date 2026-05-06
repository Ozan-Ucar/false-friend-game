using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class HealthSystem : MonoBehaviour
{
    [Header("UI Referenzen (Sprites)")]
    public Image[] heartImages; // Deine 3 Herz-Bilder
    
    public Sprite fullHeart;    // 100%
    public Sprite threeQuarter; // 75%
    public Sprite halfHeart;    // 50%
    public Sprite quarterHeart; // 25%
    public Sprite emptyHeart;   // 0%

    [Header("Zustand")]
    private int currentHealth;
    private int maxHealth;
    private const int healthPerHeart = 4; // 4 Stufen pro Herz

    void Start()
    {
        maxHealth = heartImages.Length * healthPerHeart; // z.B. 3 * 4 = 12
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    void Update()
    {
        // Test: O Taste zieht jetzt nur 1 Punkt ab (eine Stufe)
        if (Keyboard.current != null && Keyboard.current.oKey.wasPressedThisFrame)
        {
            TakeDamage(1);
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        // Kamera wackeln lassen bei jedem Treffer
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.ShakeHit();
        }

        UpdateHealthUI();
    }

    void UpdateHealthUI()
    {
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] == null) continue;

            // Verhindert das "Zusammenquetschen"
            heartImages[i].preserveAspect = true;

            int heartHealth = currentHealth - (i * healthPerHeart);
            heartHealth = Mathf.Clamp(heartHealth, 0, healthPerHeart);

            switch (heartHealth)
            {
                case 4: heartImages[i].sprite = fullHeart; break;
                case 3: heartImages[i].sprite = threeQuarter; break;
                case 2: heartImages[i].sprite = halfHeart; break;
                case 1: heartImages[i].sprite = quarterHeart; break;
                case 0: heartImages[i].sprite = emptyHeart; break;
            }
        }
    }
}
