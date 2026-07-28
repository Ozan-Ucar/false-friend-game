using UnityEngine;

/// <summary>
/// Keeps a player (or any character) playing its idle animation in a menu/cutscene,
/// without needing the movement script. It just feeds the Animator the
/// "standing still on the ground" parameters every frame.
/// Put this on the player and DISABLE PlayerMovement.
/// </summary>
public class MenuPlayerIdle : MonoBehaviour
{
    [SerializeField] Animator animator;

    void Reset()  => animator = GetComponentInChildren<Animator>();
    void OnEnable()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        Apply();
    }

    void Update() => Apply();   // keep it pinned to idle every frame

    void Apply()
    {
        if (animator == null) return;
        SetBoolIfExists("isGrounded", true);
        SetBoolIfExists("isWalking",  false);
        SetBoolIfExists("isClimbing", false);
        SetFloatIfExists("yVelocity", 0f);
    }

    void SetBoolIfExists(string name, bool v)  { if (Has(name)) animator.SetBool(name, v); }
    void SetFloatIfExists(string name, float v){ if (Has(name)) animator.SetFloat(name, v); }

    bool Has(string name)
    {
        foreach (var p in animator.parameters)
            if (p.name == name) return true;
        return false;
    }
}
