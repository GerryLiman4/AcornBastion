using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent scene loader. Put the SceneLoader prefab in every scene; the first one survives scene loads
/// and later copies remove themselves, like GameManager.
/// Loads go through the Loading scene with a pattern transition before and after it.
/// </summary>
public sealed class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Tooltip("Scene shown while the next scene loads. Must be in Build Settings.")]
    [SerializeField] private string loadingSceneName = "Loading";
    [Tooltip("The Loading scene stays on screen for at least this many seconds.")]
    [SerializeField, Min(0f)] private float minimumLoadingTime = 1.5f;
    [SerializeField] private SceneTransition transition;

    private bool isLoading;
    private float progress;

    public static bool IsLoading => Instance != null && Instance.isLoading;

    /// <summary>0 to 1. Reaches 1 only when the scene is loaded and the minimum time has passed.</summary>
    public static float Progress => Instance != null ? Instance.progress : 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void Load(string sceneName)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[SceneLoader] No SceneLoader in this scene; loading without the Loading scene.");
            SceneManager.LoadScene(sceneName);
            return;
        }
        Instance.Begin(() => SceneManager.LoadSceneAsync(sceneName));
    }

    public static void Load(int buildIndex)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[SceneLoader] No SceneLoader in this scene; loading without the Loading scene.");
            SceneManager.LoadScene(buildIndex);
            return;
        }
        Instance.Begin(() => SceneManager.LoadSceneAsync(buildIndex));
    }

    private void Begin(Func<AsyncOperation> loadTarget)
    {
        if (isLoading) return;
        StartCoroutine(LoadRoutine(loadTarget));
    }

    private IEnumerator LoadRoutine(Func<AsyncOperation> loadTarget)
    {
        isLoading = true;
        progress = 0f;
        if (transition != null) yield return transition.Cover();
        yield return SceneManager.LoadSceneAsync(loadingSceneName);

        float shownAt = Time.unscaledTime;
        AsyncOperation load = loadTarget();
        load.allowSceneActivation = false;
        if (transition != null) yield return transition.Uncover();
        while (true)
        {
            float loaded = Mathf.Clamp01(load.progress / .9f);
            float waited = minimumLoadingTime > 0f ? Mathf.Clamp01((Time.unscaledTime - shownAt) / minimumLoadingTime) : 1f;
            progress = Mathf.Min(loaded, waited);
            if (loaded >= 1f && waited >= 1f) break;
            yield return null;
        }

        progress = 1f;
        if (transition != null) yield return transition.Cover();
        load.allowSceneActivation = true;
        yield return load;
        yield return null; // Let the new scene run its first frame behind the pattern.
        if (transition != null) yield return transition.Uncover();
        isLoading = false;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
