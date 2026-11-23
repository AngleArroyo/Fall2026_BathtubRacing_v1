
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AboutScreenManager : MonoBehaviour
{
    [Header("About Screens")]
    public GameObject AboutScreen;
    public GameObject BathtubHistoryScreen;
    public GameObject ProjectResourcesScreen;
    public GameObject ProjectCreditsScreen;

    [Header("About Screen Buttons")]
    public Button BathtubHistoryBtn;
    public Button ProjectResourcesBtn;
    public Button CreditsBtn;
    public Button AboutBackBtn;

    [Header("Bathtub History Buttons")]
    public Button BathtubBackBtn;

    [Header("Project Resources Buttons")]
    public Button ResourcesBackBtn;

    [Header("Project Credits Buttons")]
    public Button CreditsBackBtn;

    [Header("Scene Settings")]
    public string MainMenuSceneName = "MainMenu";

    void Awake()
    {
        if (BathtubHistoryBtn != null)
            BathtubHistoryBtn.onClick.AddListener(OpenHistory);

        if (ProjectResourcesBtn != null)
            ProjectResourcesBtn.onClick.AddListener(OpenResources);

        if (CreditsBtn != null)
            CreditsBtn.onClick.AddListener(OpenCredits);

        if (AboutBackBtn != null)
            AboutBackBtn.onClick.AddListener(BackToMainMenu);

        if (BathtubBackBtn != null)
            BathtubBackBtn.onClick.AddListener(OpenAbout);

        if (ResourcesBackBtn != null)
            ResourcesBackBtn.onClick.AddListener(OpenAbout);

        if (CreditsBackBtn != null)
            CreditsBackBtn.onClick.AddListener(OpenAbout);

        OpenAbout();
    }

    void HideAllScreens()
    {
        if (AboutScreen != null)
            AboutScreen.SetActive(false);

        if (BathtubHistoryScreen != null)
            BathtubHistoryScreen.SetActive(false);

        if (ProjectResourcesScreen != null)
            ProjectResourcesScreen.SetActive(false);

        if (ProjectCreditsScreen != null)
            ProjectCreditsScreen.SetActive(false);
    }

    public void OpenAbout()
    {
        HideAllScreens();

        if (AboutScreen != null)
        {
            AboutScreen.SetActive(true);
            Debug.Log("Opened About Screen");
        }
    }

    public void OpenHistory()
    {
        HideAllScreens();

        if (BathtubHistoryScreen != null)
        {
            BathtubHistoryScreen.SetActive(true);
            Debug.Log("Opened Bathtub History Screen");
        }
    }

    public void OpenResources()
    {
        HideAllScreens();

        if (ProjectResourcesScreen != null)
        {
            ProjectResourcesScreen.SetActive(true);
            Debug.Log("Opened Project Resources Screen");
        }
    }

    public void OpenCredits()
    {
        HideAllScreens();

        if (ProjectCreditsScreen != null)
        {
            ProjectCreditsScreen.SetActive(true);
            Debug.Log("Opened Project Credits Screen");
        }
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
            Debug.Log("Loading Main Menu Scene");
            SceneManager.LoadScene(MainMenuSceneName);
        }
        else
        {
            Debug.LogError(
                "Main Menu scene not found in Build Settings: " +
                MainMenuSceneName
            );
        }
    }

    void OnDestroy()
    {
        if (BathtubHistoryBtn != null)
            BathtubHistoryBtn.onClick.RemoveListener(OpenHistory);

        if (ProjectResourcesBtn != null)
            ProjectResourcesBtn.onClick.RemoveListener(OpenResources);

        if (CreditsBtn != null)
            CreditsBtn.onClick.RemoveListener(OpenCredits);

        if (AboutBackBtn != null)
            AboutBackBtn.onClick.RemoveListener(BackToMainMenu);

        if (BathtubBackBtn != null)
            BathtubBackBtn.onClick.RemoveListener(OpenAbout);

        if (ResourcesBackBtn != null)
            ResourcesBackBtn.onClick.RemoveListener(OpenAbout);

        if (CreditsBackBtn != null)
            CreditsBackBtn.onClick.RemoveListener(OpenAbout);
    }
}
