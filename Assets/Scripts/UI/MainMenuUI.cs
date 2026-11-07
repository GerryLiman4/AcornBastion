using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject creditPanel;

    public void StartGame()
    {
        GameManager gm = FindFirstObjectByType<GameManager>();
        gm.StartCampaign();
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void OpenCredits()
    {
        creditPanel.SetActive(true);
    }

    public void CloseCredits()
    {
        creditPanel.SetActive(false);
    }
}
