using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// In-scene pause overlay that looks like the main menu. ESC freezes the game
/// (Time.timeScale = 0) and shows the overlay; CONTINUE resumes EXACTLY where
/// you paused (the scene is never unloaded). No scene switching.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [SerializeField] GameObject pausePanel;     // whole overlay (background + buttons)
    [SerializeField] GameObject optionsPanel;   // sound settings fly-out

    public bool IsPaused { get; private set; }

    void Start()
    {
        if (pausePanel   != null) pausePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // if the options fly-out is open, ESC closes it first
            if (IsPaused && optionsPanel != null && optionsPanel.activeSelf)
                optionsPanel.SetActive(false);
            else
                Toggle();
        }
    }

    public void Toggle() { if (IsPaused) Resume(); else Pause(); }

    public void Pause()
    {
        IsPaused = true;
        if (pausePanel   != null) pausePanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        IsPaused = false;
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (pausePanel   != null) pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void OpenOptions()  { if (optionsPanel != null) optionsPanel.SetActive(true); }
    public void CloseOptions() { if (optionsPanel != null) optionsPanel.SetActive(false); }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnDisable() => Time.timeScale = 1f;   // never leave the game frozen
}
