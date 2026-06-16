using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlaySpecificAnimation : MonoBehaviour
{
    [Header("Welche Animation soll gespielt werden?")]
    [Tooltip("Schreibe hier exakt den Namen der Animation rein, wie sie im Animator heißt (z.B. 'Idle', 'Spin', etc.).")]
    public string animationName;

    void Start()
    {
        if (!string.IsNullOrEmpty(animationName))
        {
            Animator anim = GetComponent<Animator>();
            
            // Spielt die angegebene Animation sofort ab
            // Die Nullen bedeuten: Layer 0, Start bei Sekunde 0.
            anim.Play(animationName, 0, 0f);
        }
        else
        {
            Debug.LogWarning("Auf dem Objekt '" + gameObject.name + "' fehlt der Name der Animation im Skript 'PlaySpecificAnimation'!");
        }
    }
}
