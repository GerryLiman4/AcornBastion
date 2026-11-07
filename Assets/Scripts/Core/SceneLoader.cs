using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Loads scenes through the Loading scene, which stays on screen for at least <see cref="MinimumLoadingTime"/> seconds.
/// </summary>
public sealed class SceneLoader : MonoBehaviour
{
    public const string LoadingSceneName = "Loading";
    public const float MinimumLoadingTime = 1.5f;

    private static SceneLoader instance;

    public static bool IsLoading { get; private set; }

    /// <summary>0 to 1. Reaches 1 only when the scene is loaded and the minimum time has passed.</summary>
    public static float Progress { get; private set; }

    public static void Load(string sceneName) => Begin(() => SceneManager.LoadSceneAsync(sceneName));

    public static void Load(int buildIndex) => Begin(() => SceneManager.LoadSceneAsync(buildIndex));

    private static void Begin(Func<AsyncOperation> loadTarget)
    {
        if (IsLoading) return;
        if (instance == null)
        {
            var obj = new GameObject(nameof(SceneLoader));
            DontDestroyOnLoad(obj);
            instance = obj.AddComponent<SceneLoader>();
        }
        instance.StartCoroutine(instance.LoadRoutine(loadTarget));
    }

    private IEnumerator LoadRoutine(Func<AsyncOperation> loadTarget)
    {
        IsLoading = true;
        Progress = 0f;
        yield return SceneManager.LoadSceneAsync(LoadingSceneName);

        float shownAt = Time.unscaledTime;
        AsyncOperation load = loadTarget();
        load.allowSceneActivation = false;
        while (true)
        {
            float loaded = Mathf.Clamp01(load.progress / .9f);
            float waited = Mathf.Clamp01((Time.unscaledTime - shownAt) / MinimumLoadingTime);
            Progress = Mathf.Min(loaded, waited);
            if (loaded >= 1f && waited >= 1f) break;
            yield return null;
        }

        Progress = 1f;
        load.allowSceneActivation = true;
        yield return load;
        IsLoading = false;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            IsLoading = false;
        }
    }
}
