using UnityEngine;
using UnityEngine.InputSystem;

public class JuiceManager : MonoBehaviour
{
    public static JuiceManager Instance { get; private set; }

    public bool isJuicy = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Optional: DontDestroyOnLoad(gameObject); falls es szenenübergreifend sein soll
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.zKey.wasPressedThisFrame)
        {
            isJuicy = !isJuicy;
            Debug.Log("Juice mode toggled. IsJuicy: " + isJuicy);
        }
    }
}
