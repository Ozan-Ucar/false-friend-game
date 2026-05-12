using UnityEngine;

public class BridgeJuiceToggle : MonoBehaviour
{
    private Rigidbody2D[] segments;
    private Vector2[] startPositions;
    private Quaternion[] startRotations;
    private RigidbodyType2D[] originalTypes;

    void Start()
    {
        // Finde alle Rigidbody2D in diesem Objekt und in allen Unter-Objekten (Children)
        segments = GetComponentsInChildren<Rigidbody2D>();
        
        startPositions = new Vector2[segments.Length];
        startRotations = new Quaternion[segments.Length];
        originalTypes = new RigidbodyType2D[segments.Length];

        for (int i = 0; i < segments.Length; i++)
        {
            startPositions[i] = segments[i].position;
            startRotations[i] = segments[i].transform.rotation;
            originalTypes[i] = segments[i].bodyType;
        }
    }

    void Update()
    {
        bool isJuicy = JuiceManager.Instance == null || JuiceManager.Instance.isJuicy;

        for (int i = 0; i < segments.Length; i++)
        {
            // Die äußeren Pfosten der Brücke sind wahrscheinlich eh "Static", die ignorieren wir
            if (originalTypes[i] == RigidbodyType2D.Static) continue;

            if (!isJuicy && segments[i].bodyType == RigidbodyType2D.Dynamic)
            {
                // Mach das Brückenteil fest (Kinematic)
                segments[i].bodyType = RigidbodyType2D.Kinematic;
                segments[i].linearVelocity = Vector2.zero;
                segments[i].angularVelocity = 0f;
                
                // Setze es genau auf die waagerechte Startposition zurück
                segments[i].position = startPositions[i];
                segments[i].transform.rotation = startRotations[i];
            }
            else if (isJuicy && segments[i].bodyType == RigidbodyType2D.Kinematic)
            {
                // Mache es wieder flexibel
                segments[i].bodyType = RigidbodyType2D.Dynamic;
            }
        }
    }
}
