using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CloudSettings
{
    [Tooltip("Nur für dich zur Übersicht (z.B. 'Kleine Wolke')")]
    public string cloudName = "Wolken Art";
    
    [Tooltip("Das Prefab (oder GameObject) der Wolke! (Einfach reinziehen)")]
    public GameObject cloudPrefab;
    
    [Tooltip("Wie viele Wolken von dieser Sorte sollen gleichzeitig am Himmel sein?")]
    public int amount = 3;

    [Header("Geschwindigkeit")]
    public float minSpeed = 0.5f;
    public float maxSpeed = 1.5f;

    [Header("Flughöhe (Y-Achse)")]
    public float minY = 2f;
    public float maxY = 6f;

    [Header("Juice (Wackeln & Größe)")]
    public float floatAmplitude = 0.3f;
    public float floatFrequency = 0.8f;
    public float minScale = 0.8f;
    public float maxScale = 1.3f;

    [Header("Layer (Sortierung)")]
    [Tooltip("Order in Layer für diese Wolken-Sorte. Höher = weiter vorne.")]
    public int sortingOrder = 10;
}

public class CloudController : MonoBehaviour
{
    [Header("Allgemeine Grenzen (X-Achse)")]
    [Tooltip("Bei welchem X-Wert verschwindet die Wolke ganz links im Bildschirm?")]
    public float despawnX = -20f;
    [Tooltip("Bei welchem X-Wert taucht die Wolke rechts wieder auf?")]
    public float spawnX = 20f;

    [Header("Richtung")]
    [Tooltip("An = Wolken ziehen von links nach rechts. Aus = von rechts nach links (Standard).")]
    public bool moveLeftToRight = false;

    [Header("Globale Höhe")]
    [Tooltip("Verschiebt ALLE Wolken gemeinsam nach oben (+) oder unten (-).")]
    public float heightOffset = 0f;

    private float LeftEdge  => Mathf.Min(spawnX, despawnX);
    private float RightEdge => Mathf.Max(spawnX, despawnX);
    private float EntryX    => moveLeftToRight ? LeftEdge : RightEdge;
    
    [Header("Deine Wolken")]
    [Tooltip("Füge hier deine 3 Wolkenarten hinzu!")]
    public List<CloudSettings> cloudTypes = new List<CloudSettings>();

    // =========================================================
    // Interne Klasse, die sich die Daten jeder Wolke merkt
    // =========================================================
    private class ActiveCloud
    {
        public Transform transform;
        public CloudSettings settings;
        public float currentSpeed;
        public float startY;
        public float randomTimeOffset;
    }

    private List<ActiveCloud> activeClouds = new List<ActiveCloud>();

    private void Start()
    {
        foreach (CloudSettings settings in cloudTypes)
        {
            if (settings.cloudPrefab == null) continue;

            for (int i = 0; i < settings.amount; i++)
            {
                // Spawne die Wolken beim Spielstart zufällig verteilt!
                float startX = Random.Range(despawnX, spawnX);
                float startY = Random.Range(settings.minY, settings.maxY) + heightOffset;
                
                // Wir bauen das GameObject dynamisch aus dem Bild!
                GameObject cloudObj = Instantiate(settings.cloudPrefab, new Vector3(startX, startY, 0), Quaternion.identity, transform);
                cloudObj.name = "Cloud_" + settings.cloudName;

                // apply this sort's layer order to the cloud's renderer(s)
                foreach (var sr in cloudObj.GetComponentsInChildren<SpriteRenderer>())
                    sr.sortingOrder = settings.sortingOrder;
                
                // Wolke in unsere interne Liste aufnehmen
                ActiveCloud ac = new ActiveCloud();
                ac.transform = cloudObj.transform;
                ac.settings = settings;
                
                SetupCloud(ac, false);
                activeClouds.Add(ac);
            }
        }
    }

    private void SetupCloud(ActiveCloud ac, bool isRespawning)
    {
        ac.randomTimeOffset = Random.Range(0f, 100f);
        ac.currentSpeed = Random.Range(ac.settings.minSpeed, ac.settings.maxSpeed);
        
        if (isRespawning)
        {
            ac.startY = Random.Range(ac.settings.minY, ac.settings.maxY) + heightOffset;
            ac.transform.position = new Vector3(EntryX, ac.startY, ac.transform.position.z);
        }
        else
        {
            ac.startY = ac.transform.position.y;
        }

        // Neue, zufällige Größe für Abwechslung
        if (ac.settings.maxScale > 0)
        {
            float randomS = Random.Range(ac.settings.minScale, ac.settings.maxScale);
            float signX = Mathf.Sign(ac.transform.localScale.x); 
            if (signX == 0) signX = 1;
            float randomStretch = Random.Range(0.9f, 1.1f); 
            ac.transform.localScale = new Vector3(signX * randomS * randomStretch, randomS, 1f);
        }
    }

    private void Update()
    {
        // Der Manager bewegt alle Wolken auf einmal (viel performanter!)
        foreach (ActiveCloud ac in activeClouds)
        {
            if (ac.transform == null) continue;

            // 1. In Richtung schieben (links→rechts oder rechts→links)
            Vector3 dir = moveLeftToRight ? Vector3.right : Vector3.left;
            ac.transform.position += dir * ac.currentSpeed * Time.deltaTime;

            // 2. Schweben auf der Y-Achse
            if (ac.settings.floatAmplitude > 0f)
            {
                float time = Time.time + ac.randomTimeOffset;
                float newY = ac.startY + Mathf.Sin(time * ac.settings.floatFrequency) * ac.settings.floatAmplitude;
                ac.transform.position = new Vector3(ac.transform.position.x, newY, ac.transform.position.z);
            }

            // 3. Respawn, wenn sie aus dem Bild fliegen
            bool gone = moveLeftToRight
                ? ac.transform.position.x >= RightEdge
                : ac.transform.position.x <= LeftEdge;
            if (gone)
            {
                SetupCloud(ac, true);
            }
        }
    }
}
