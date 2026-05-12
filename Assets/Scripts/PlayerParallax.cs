using UnityEngine;

public class PlayerParallax : MonoBehaviour
{
    [Header("Referenz")]
    public Transform player; // Ziehe hier deinen Spieler rein

    [Header("Einstellungen")]
    [Range(-1f, 1f)]
    public float parallaxFactor = 0.1f; // 0.1 = bewegt sich langsam, 0.5 = schneller
    
    public bool horizontalOnly = true;

    private Vector3 startPos;
    private Vector3 playerStartPos;

    void Start()
    {
        startPos = transform.position;
        if (player != null)
        {
            playerStartPos = player.position;
        }
        else
        {
            // Versuche den Player automatisch zu finden, falls vergessen wurde ihn zuzuweisen
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) 
            {
                player = p.transform;
                playerStartPos = player.position;
            }
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        bool isJuicy = JuiceManager.Instance == null || JuiceManager.Instance.isJuicy;
        if (!isJuicy)
        {
            transform.position = startPos;
            return;
        }

        // Berechne, wie weit sich der Spieler seit dem Start bewegt hat
        Vector3 playerDiff = player.position - playerStartPos;

        // Berechne den Offset (negativer Faktor lässt den Hintergrund "tiefer" wirken)
        float offsetX = playerDiff.x * -parallaxFactor;
        float offsetY = horizontalOnly ? 0 : playerDiff.y * -parallaxFactor;

        // Neue Position setzen
        transform.position = new Vector3(startPos.x + offsetX, startPos.y + offsetY, startPos.z);
    }
}
