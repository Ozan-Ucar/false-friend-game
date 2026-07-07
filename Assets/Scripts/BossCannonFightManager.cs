using UnityEngine;
using TMPro;
using System.Collections;

public class BossCannonFightManager : MonoBehaviour
{
    public static BossCannonFightManager Instance;

    [Header("UI & Ankündigungen")]
    public TextMeshProUGUI phaseText;

    [Header("Spawn Punkte (für Collectible Spawn)")]
    [Tooltip("Ziehe hier leere Objekte rein, die auf deinen Plattformen liegen.")]
    public Transform[] spawnPoints;

    [Header("Waffen & Gegner")]
    public MouseCannon rightCannon;
    public MouseCannon leftCannon;
    public LaserCannon laserCannon;
    
    [Header("Collectibles")]
    public GameObject collectiblePrefab;
    [Tooltip("Wie viele Collectibles müssen pro Phase gesammelt werden?")]
    public int collectiblesPerPhase = 3;

    [Header("Sieg (Cutscene)")]
    public CutsceneData endCutscene;
    public string nextSceneName;

    [Header("Debug (Zum Testen)")]
    [Tooltip("Mit welcher Phase soll das Spiel starten?")]
    [Range(1, 3)]
    public int debugStartPhase = 1;
    [Tooltip("Mache hier im Play-Mode ein Häkchen, um sofort in die nächste Phase zu springen.")]
    public bool forceNextPhase = false;

    private int currentPhase = 0;
    private int collectedInCurrentPhase = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Sicherstellen, dass zu Beginn alles aus ist
        if (rightCannon != null) rightCannon.SetActive(false);
        if (leftCannon != null) leftCannon.SetActive(false);
        if (laserCannon != null) laserCannon.SetActive(false);
        
        if (phaseText != null)
        {
            phaseText.gameObject.SetActive(false);
        }

        // Kampf starten mit der eingestellten Start-Phase
        StartCoroutine(StartPhase(debugStartPhase));
    }

    void Update()
    {
        if (forceNextPhase)
        {
            forceNextPhase = false;
            // Wir tun einfach so, als hätten wir alle Items bis auf das letzte gesammelt
            // und rufen dann die Einsammel-Funktion auf, damit die Phase wechselt.
            collectedInCurrentPhase = collectiblesPerPhase - 1;
            OnCollectiblePickedUp();
        }
    }

    public void OnCollectiblePickedUp()
    {
        collectedInCurrentPhase++;

        if (collectedInCurrentPhase >= collectiblesPerPhase)
        {
            // Phase geschafft!
            int nextPhase = currentPhase + 1;
            
            if (nextPhase > 3)
            {
                StartCoroutine(BossDefeatedRoutine());
            }
            else
            {
                StartCoroutine(StartPhase(nextPhase));
            }
        }
        else
        {
            // Nächstes Collectible für diese Phase spawnen
            SpawnCollectible();
        }
    }

    private IEnumerator StartPhase(int phase)
    {
        currentPhase = phase;
        collectedInCurrentPhase = 0;

        // Kurze Pause vor der neuen Phase
        yield return new WaitForSeconds(1f);

        // Ankündigung
        if (phaseText != null)
        {
            phaseText.text = "PHASE " + phase + "!";
            phaseText.gameObject.SetActive(true);
            
            // Wackeln für Impact
            if (CameraShake.Instance != null)
            {
                CameraShake.Instance.ShakeCustom(0.5f, 0.2f);
            }

            // Sound
            if (SceneSoundManager.Instance != null && SceneSoundManager.Instance.stoneHitSound != null)
            {
                SceneSoundManager.Instance.PlaySFX(SceneSoundManager.Instance.stoneHitSound);
            }

            yield return new WaitForSeconds(2f);
            phaseText.gameObject.SetActive(false);
        }

        // Waffen aktivieren je nach Phase
        if (phase == 1)
        {
            if (rightCannon != null) rightCannon.SetActive(true);
        }
        else if (phase == 2)
        {
            // Nur zur Sicherheit nochmal rechts an (falls deaktiviert war)
            if (rightCannon != null) rightCannon.SetActive(true);
            if (leftCannon != null) leftCannon.SetActive(true);
        }
        else if (phase == 3)
        {
            if (rightCannon != null) rightCannon.SetActive(true);
            if (leftCannon != null) leftCannon.SetActive(true);
            if (laserCannon != null) laserCannon.SetActive(true);
        }

        // Erstes Collectible der Phase spawnen
        SpawnCollectible();
    }

    private void SpawnCollectible()
    {
        if (collectiblePrefab == null || spawnPoints == null || spawnPoints.Length == 0) return;

        // Wähle einen zufälligen Spawn-Punkt aus der Liste aus
        Transform randomSpawn = spawnPoints[Random.Range(0, spawnPoints.Length)];
        
        Instantiate(collectiblePrefab, randomSpawn.position, Quaternion.identity);
    }

    private IEnumerator BossDefeatedRoutine()
    {
        // Alles deaktivieren (Frieden!)
        if (rightCannon != null) rightCannon.SetActive(false);
        if (leftCannon != null) leftCannon.SetActive(false);
        if (laserCannon != null) laserCannon.SetActive(false);

        if (phaseText != null)
        {
            phaseText.text = "GEWONNEN!";
            phaseText.gameObject.SetActive(true);
        }

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.ShakeCustom(2f, 0.3f);
        }

        // Längeres Beben/Auflösen
        yield return new WaitForSeconds(3f);

        // Cutscene starten
        if (endCutscene != null)
        {
            CutscenePlayer.pendingCutscene = endCutscene;
            CutscenePlayer.pendingTargetScene = nextSceneName;
            CutscenePlayer.Play();
        }
        else
        {
            // Fallback: Einfach nächste Szene laden
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
            }
        }
    }
}
