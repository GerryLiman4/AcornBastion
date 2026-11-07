using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private const string LevelScenePrefix = "Level ";
    // Campaign levels without their own scene, such as the final level, reuse this map.
    private const string FallbackLevelScene = "Level 4";

    public static GameManager Instance { get; private set; }
    public int CampaignLevel { get; private set; } = 1;

    private void Awake()
    {
        // Check if an instance already exists in the scene
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Kill duplicates
            return;
        }

        Instance = this;

        // Optional: Keep this manager alive when loading new scenes
        DontDestroyOnLoad(gameObject);

        // Pressing Play inside a level scene should play that level, not campaign level 1.
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName.StartsWith(LevelScenePrefix)
            && int.TryParse(sceneName.Substring(LevelScenePrefix.Length), out int sceneLevel))
            CampaignLevel = sceneLevel;
    }

    public void LoadLevel()
    {
        string levelScene = LevelScenePrefix + CampaignLevel;
        SceneLoader.Load(Application.CanStreamedLevelBeLoaded(levelScene) ? levelScene : FallbackLevelScene);
    }

    public void StartCampaign()
    {
        CampaignLevel = 1;
        LoadLevel();
    }

    public void AdvanceLevel()
    {
        CampaignLevel = Mathf.Min(CampaignLevel + 1, CampaignConfig.Load().levels.Length);
        LoadLevel();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void LoadMainMenu()
    {
        SceneLoader.Load(0);
    }
}
