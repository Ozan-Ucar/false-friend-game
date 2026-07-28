using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject optionsPanel;       // fly-out next to the buttons
    [SerializeField] GameObject achievementsPanel;  // overlay
    [SerializeField] GameObject creditsPanel;       // overlay

    [Header("Scene")]
    [SerializeField] string firstLevelScene = "OzanScene";

    void Start()
    {
        CloseAll();
    }

    // ── Main ──────────────────────────────────────────────────────────────

    public void OnPlay()
    {
        SceneManager.LoadScene(firstLevelScene);
    }

    // Options is a fly-out: toggle it without hiding the main buttons.
    public void OnOptions()
    {
        bool show = !optionsPanel.activeSelf;
        CloseAll();
        optionsPanel.SetActive(show);
    }

    public void OnAchievements()
    {
        CloseAll();
        achievementsPanel.SetActive(true);
    }

    public void OnCredits()
    {
        CloseAll();
        creditsPanel.SetActive(true);
    }

    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── Back / close any open sub-panel ────────────────────────────────────

    public void OnBack() => CloseAll();

    void CloseAll()
    {
        if (optionsPanel != null)      optionsPanel.SetActive(false);
        if (achievementsPanel != null) achievementsPanel.SetActive(false);
        if (creditsPanel != null)      creditsPanel.SetActive(false);
    }
}
