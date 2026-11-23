
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ScoreScreenManager : MonoBehaviour
{
    [Header("Score Screen")]
    public GameObject ScoresScreen;

    [Header("Score Panels")]
    public GameObject YourScoresPanel;
    public GameObject GlobalLeaderboardPanel;

    [Header("Score Buttons")]
    public Button YourScoresBtn;
    public Button GlobalLeaderboardBtn;
    public Button ScoresBackBtn;

    [Header("Scene Settings")]
    public string MainMenuSceneName = "MainMenu";

    void Awake()
    {
        if (YourScoresBtn != null)
            YourScoresBtn.onClick.AddListener(OpenYourScores);

        if (GlobalLeaderboardBtn != null)
            GlobalLeaderboardBtn.onClick.AddListener(OpenGlobalLeaderboard);

        if (ScoresBackBtn != null)
            ScoresBackBtn.onClick.AddListener(BackToMainMenu);
    }

    void Start()
    {
        OpenYourScores();
    }

    public void OpenYourScores()
    {
        if (YourScoresPanel != null)
            YourScoresPanel.SetActive(true);

        if (GlobalLeaderboardPanel != null)
            GlobalLeaderboardPanel.SetActive(false);

        Debug.Log("Opened Your Scores");
    }

    public void OpenGlobalLeaderboard()
    {
        if (YourScoresPanel != null)
            YourScoresPanel.SetActive(false);

        if (GlobalLeaderboardPanel != null)
            GlobalLeaderboardPanel.SetActive(true);

        Debug.Log("Opened Global Leaderboard");
    }

    public void BackToMainMenu()
    {
        if (string.IsNullOrEmpty(MainMenuSceneName))
        {
            Debug.LogError("Main Menu scene name is missing.");
            return;
        }

        if (Application.CanStreamedLevelBeLoaded(MainMenuSceneName))
        {
            Debug.Log("Loading Main Menu");
            SceneManager.LoadScene(MainMenuSceneName);
        }
        else
        {
            Debug.LogError(
                "Main Menu scene not found in Build Settings: "
                + MainMenuSceneName
            );
        }
    }

    void OnDestroy()
    {
        if (YourScoresBtn != null)
            YourScoresBtn.onClick.RemoveListener(OpenYourScores);

        if (GlobalLeaderboardBtn != null)
            GlobalLeaderboardBtn.onClick.RemoveListener(OpenGlobalLeaderboard);

        if (ScoresBackBtn != null)
            ScoresBackBtn.onClick.RemoveListener(BackToMainMenu);
    }
}
