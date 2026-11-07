using UnityEngine;

/// <summary>Pause menu on the LevelManager. Opening it pauses the game; closing it resumes.</summary>
public class PauseMenuUI : MonoBehaviour
{
    [Tooltip("Object shown while paused. Defaults to this GameObject.")]
    [SerializeField] private GameObject menuRoot;
    [Tooltip("Game speed restored on Resume.")]
    [SerializeField, Min(0f)] private float resumeTimeScale = 1f;

    public bool IsPaused { get; private set; }

    private GameObject Root => menuRoot != null ? menuRoot : gameObject;

    // Hooked to SettingsButton on the HUD.
    public void Open()
    {
        if (IsPaused || SceneLoader.IsLoading) return;
        IsPaused = true;
        Time.timeScale = 0f;
        Root.SetActive(true);
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;
        Time.timeScale = resumeTimeScale;
        Root.SetActive(false);
    }

    public void Toggle()
    {
        if (IsPaused) Resume();
        else Open();
    }

    public void BackToMainMenu()
    {
        // Unpause first so the next scene does not start frozen.
        Resume();
        if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu();
        else SceneLoader.Load(0);
    }

    public void Quit()
    {
        Resume();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        if (IsPaused) Time.timeScale = resumeTimeScale;
    }
}
