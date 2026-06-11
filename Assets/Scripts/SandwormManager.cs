using UnityEngine;
using UnityEngine.InputSystem;

public class SandwormManager : MonoBehaviour
{
    [Header("Placement Settings")]
    [Tooltip("Die Gräber-Prefabs in der Reihenfolge, wie sie platziert werden sollen (z.B. Normal, Mid, High)")]
    public GameObject[] wormPrefabs;
    
    [Tooltip("Mindestabstand zu anderen Würmern, damit sie nicht ineinander spawnen")]
    public float minDistanceBetweenWorms = 1.0f;
    
    [Tooltip("Welcher Layer ist der Boden? (Damit der Raycast weiß, wo er einrasten soll)")]
    public LayerMask groundLayer;

    private int placedCount = 0;
    private PlayerMovement playerMovement;
    
    // Ghost (Vorschau)
    private GameObject ghostWorm;
    private SpriteRenderer[] ghostRenderers;

    void Start()
    {
        if (groundLayer.value == 0)
        {
            Debug.LogWarning("ACHTUNG: Ground Layer im SandwormManager ist auf 'Nothing' gestellt! Der Raycast wird nichts treffen.");
        }

        // 1. Finde den Spieler und friere ihn ein
        playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        // 2. Erstelle den ersten "Geist"
        CreateGhost();
    }

    void CreateGhost()
    {
        if (wormPrefabs == null || wormPrefabs.Length == 0 || placedCount >= wormPrefabs.Length) return;

        // Instanziere das Prefab passend zur aktuellen Runde (0 = erstes, 1 = zweites, etc.)
        ghostWorm = Instantiate(wormPrefabs[placedCount]);
        
        // Deaktiviere das Skript, damit man es noch nicht anklicken kann
        SandwormGrave trapScript = ghostWorm.GetComponent<SandwormGrave>();
        if (trapScript != null)
        {
            // Zwinge Unity, die Animation BEREITS IM ERSTEN FRAME korrekt zu laden!
            Animator anim = ghostWorm.GetComponent<Animator>();
            if (anim != null && !string.IsNullOrEmpty(trapScript.previewAnimationName))
            {
                anim.Play(trapScript.previewAnimationName, 0, 0f);
                anim.Update(0f); // Der magische Befehl: Überspringt Unitys Ladeverzögerung!
            }

            trapScript.enabled = false; // Wird erst nach dem Platzieren aktiviert
            trapScript.isPlaced = false;
        }

        // Mache ihn halb-transparent (Geist) und schalte seinen Collider aus (damit der Raycast nicht aus Versehen den Geist trifft!)
        Collider2D col = ghostWorm.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        ghostRenderers = ghostWorm.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in ghostRenderers)
        {
            Color c = sr.color;
            c.a = 0.5f; // 50% durchsichtig
            sr.color = c;
        }
    }

    void Update()
    {
        if (wormPrefabs == null || placedCount >= wormPrefabs.Length) return; // Bau-Phase ist vorbei!

        if (ghostWorm != null && Mouse.current != null)
        {
            // MANUELLER LOOP FÜR DEN GEIST
            Animator anim = ghostWorm.GetComponent<Animator>();
            SandwormGrave trapScript = ghostWorm.GetComponent<SandwormGrave>();
            if (anim != null && trapScript != null && !string.IsNullOrEmpty(trapScript.previewAnimationName))
            {
                AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
                
                // Wenn die Animation fertig ist, starte sie neu (Loop-Ersatz)
                if (state.normalizedTime >= 1.0f)
                {
                    anim.Play(trapScript.previewAnimationName, 0, 0f);
                    anim.Update(0f);
                }
            }

            // 1. Maus-Position in der Welt finden
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            // 2. Wir suchen den Beden! Schieße von weit oben nach ganz unten durch ALLES hindurch.
            RaycastHit2D[] hits = Physics2D.RaycastAll(mousePos + Vector2.up * 20f, Vector2.down, 200f);
            
            RaycastHit2D validHit = new RaycastHit2D();
            bool foundGround = false;

            foreach (RaycastHit2D h in hits)
            {
                // Wir ignorieren den Spieler und wir ignorieren reine Trigger-Zonen.
                // Alles andere (wie die Tilemap) ist unser Boden!
                if (h.collider != null && !h.collider.CompareTag("Player") && !h.collider.isTrigger)
                {
                    validHit = h;
                    foundGround = true;
                    break;
                }
            }

            if (foundGround)
            {
                // PERFEKT! Wir setzen den Geist auf den gefundenen Boden + das individuelle Offset des jeweiligen Wurms
                Vector2 finalPos = validHit.point;
                
                SandwormGrave currentTrap = ghostWorm.GetComponent<SandwormGrave>();
                if (currentTrap != null)
                {
                    finalPos.y += currentTrap.ghostYOffset;
                }

                ghostWorm.transform.position = finalPos;

                // Prüfen, ob der Platz frei ist (ohne Collider vorauszusetzen, über pure Distanz!)
                bool canPlace = true;
                
                SandwormGrave[] allGraves = FindObjectsOfType<SandwormGrave>();
                foreach (SandwormGrave g in allGraves)
                {
                    if (g.gameObject != ghostWorm && Vector2.Distance(finalPos, g.transform.position) < minDistanceBetweenWorms)
                    {
                        canPlace = false;
                        break;
                    }
                }

                if (canPlace)
                {
                    SandwormAttack[] allAttacks = FindObjectsOfType<SandwormAttack>();
                    foreach (SandwormAttack a in allAttacks)
                    {
                        if (a.gameObject != ghostWorm && Vector2.Distance(finalPos, a.transform.position) < minDistanceBetweenWorms)
                        {
                            canPlace = false;
                            break;
                        }
                    }
                }

                if (canPlace)
                {
                    foreach (SpriteRenderer sr in ghostRenderers)
                    {
                        Color c = sr.color;
                        c.r = 1f; c.g = 1f; c.b = 1f; c.a = 0.5f; // Weiß transparent
                        sr.color = c;
                    }

                    if (UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                    {
                        PlaceWorm();
                    }
                }
                else
                {
                    foreach (SpriteRenderer sr in ghostRenderers)
                    {
                        Color c = sr.color;
                        c.r = 1f; c.g = 0f; c.b = 0f; c.a = 0.5f; // Rot transparent (blockiert)
                        sr.color = c;
                    }
                }
            }
            else
            {
                // Falls du die Maus ganz aus der Map rausbewegst, wo wirklich GAR KEIN Boden ist:
                ghostWorm.transform.position = mousePos;
                foreach (SpriteRenderer sr in ghostRenderers)
                {
                    sr.enabled = true;
                    sr.color = new Color(1f, 0f, 0f, 0.6f); // Rot
                }
            }
        }
    }

    void PlaceWorm()
    {
        // Geist wird wieder zu 100% sichtbar und sein Collider wird angeschaltet!
        Collider2D col = ghostWorm.GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        foreach (SpriteRenderer sr in ghostRenderers)
        {
            Color c = sr.color;
            c.a = 1f;
            sr.color = c;
        }

        // Skript aktivieren, damit Spieler 2 es später anklicken kann
        SandwormGrave trapScript = ghostWorm.GetComponent<SandwormGrave>();
        if (trapScript != null)
        {
            // JETZT wird er zum Grab!
            trapScript.PlayIdle();
            trapScript.enabled = true;
            trapScript.isPlaced = true;
        }

        ghostWorm = null;
        placedCount++;

        // Nächsten Geist spawnen oder Action-Phase starten
        if (placedCount < wormPrefabs.Length)
        {
            CreateGhost();
        }
        else
        {
            EndPlacementPhase();
        }
    }

    void EndPlacementPhase()
    {
        Debug.Log("Alle Würmer platziert! Action-Phase beginnt!");
        
        // Spieler auftauen
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }
    }
}
