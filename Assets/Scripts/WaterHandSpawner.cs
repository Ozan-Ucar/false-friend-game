using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class WaterHandSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [Tooltip("Das Prefab der Hand-Falle (mit dem WaterHandTrap Skript drauf).")]
    public GameObject handTrapPrefab;
    
    [Tooltip("Wie viele Sekunden muss man warten, bis man die nächste Hand spawnen darf?")]
    public float clickCooldown = 1.0f;
    
    [Tooltip("Die genaue Höhe (Y-Achse), auf der die Hand spawnen soll. (z.B. genau auf der Wasserlinie)")]
    public float spawnYPosition = 0f;

    private List<Collider2D> clickColliders = new List<Collider2D>();
    private float lastClickTime = -100f;

    private void Awake()
    {
        // Holt sich alle Collider auf diesem Objekt, damit man das Wasser aus mehreren BoxCollidern bauen kann
        clickColliders.AddRange(GetComponents<Collider2D>());
    }

    private void Update()
    {
        // Sobald die linke Maustaste gedrückt wird
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Cooldown check: Darf der Spieler ("Falscher Freund") schon wieder klicken?
            if (Time.time < lastClickTime + clickCooldown) return;

            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            
            // Haben wir AUF das Wasser (den Spawner-Collider) geklickt?
            bool clickedWater = false;
            foreach (Collider2D col in clickColliders)
            {
                if (col.OverlapPoint(mousePos))
                {
                    clickedWater = true;
                    break;
                }
            }

            if (clickedWater)
            {
                SpawnHandTrap(mousePos.x);
            }
        }
    }

    private void SpawnHandTrap(float spawnX)
    {
        if (handTrapPrefab != null)
        {
            // Wir spawnen das Prefab genau an der X-Position der Maus, aber immer auf der festen Y-Höhe!
            Vector3 spawnPos = new Vector3(spawnX, spawnYPosition, transform.position.z);
            Instantiate(handTrapPrefab, spawnPos, Quaternion.identity);
            
            lastClickTime = Time.time;
        }
        else
        {
            Debug.LogWarning("WaterHandSpawner: Du hast vergessen, das Hand-Prefab im Inspector zuzuweisen!");
        }
    }
}
