
using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuManager : MonoBehaviour, IOrderedScript, ICancelHandler
{
    public static MainMenuManager Instance { get; private set; }

    public Canvas MainMenuCanvas;
    public GraphicRaycaster MainMenuRaycaster;

    [Space]
    public GameObject BackgroundParticleSystem;
    public GameObject MainMenuPanel;
    public GameObject JoiningRandomPanel;
    public LobbyUI LobbyUI;
    public TrackSelectionManager TrackSelection;

    [Space]
    [Header("Main Menu Buttons")]
    public Button SingleplayerBtn;
    public Button MultiplayerBtn;
    public Button CustomizeBtn;
    public Button ScoresBtn;
    public Button AboutBtn;
    public Button TutorialBtn;
    public Button QuitBtn;

    [Space]
    [Header("Menu Scenes")]
    public string AboutSceneName = "About";
    public string ScoresSceneName = "score";
    public string TutorialSceneName = "TutorialPanel";

    [Space]
    [Header("Settings")]
    public Button SettingsBtn;
    public SettingsMenuUI SettingsMenu;

    [Space]
    public GameObject MultiplayerPanel;
    public Button HostBtn;
    public TMP_InputField JoinCodeField;
    public Button JoinBtn;
    public TextMeshProUGUI JoinBtnText;
    public Button JoinRandomBtn;
    public Button CancelMultiplayerBtn;

    [Space]
    public Button CancelJoinRandomBtn;

    [Space]
    [Header("Auto Set")]
    public List<GameObject> ActivePanels = new();

    [NonSerialized]
    public RaycastResult? FirstMouseRaycast = null;

    public List<RaycastResult> MouseRaycasts = new();

    public int CallOrder => 0;

    string originJoinBtnText;
    Coroutine changeJoinBtnCor = null;

    void Awake()
    {
        SingleplayerBtn.onClick.AddListener(OnSingleplayerClick);
        MultiplayerBtn.onClick.AddListener(OnMultiplayerClick);
        CustomizeBtn.onClick.AddListener(OnCustomizeClick);

        if (ScoresBtn != null)
            ScoresBtn.onClick.AddListener(OnScoresClick);

        if (AboutBtn != null)
            AboutBtn.onClick.AddListener(OnAboutClick);

        if (TutorialBtn != null)
            TutorialBtn.onClick.AddListener(OnTutorialClick);
        else
            Debug.LogWarning("MainMenuManager: TutorialBtn is not assigned!");

        QuitBtn.onClick.AddListener(OnQuitClick);

        if (SettingsBtn != null)
            SettingsBtn.onClick.AddListener(OnSettingsClick);

        HostBtn.onClick.AddListener(OnHostClick);
        JoinBtn.onClick.AddListener(OnJoinClick);
        JoinRandomBtn.onClick.AddListener(OnJoinRandomClick);
        CancelJoinRandomBtn.onClick.AddListener(OnCancelJoinRandomClick);
        CancelMultiplayerBtn.onClick.AddListener(OnCancelMultiplayerClick);

        originJoinBtnText = JoinBtnText.text;

        SteamManager.OnLobbyEntered += SteamManager_OnLobbyEntered;

        LobbyUI.gameObject.SetActive(true);
        LobbyUI.gameObject.SetActive(false);
    }

    public void OrderedAwake()
    {
        Instance = this;

        ShowPanel(MainMenuPanel, true);

        if (SteamManager.InSteamLobby)
            LobbyUI.ShowCurrentLobby();
    }

    public void OrderedStart()
    {
    }

    void Update()
    {
        JoiningRandomPanel.SetActiveSafe(SteamManager.IsJoiningRandom);
        GetMouseRaycasts();
    }

    void GetMouseRaycasts()
    {
        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = InputManager.MousePosition,
        };

        MouseRaycasts.Clear();

        MainMenuRaycaster.Raycast(pointerData, MouseRaycasts);

        FirstMouseRaycast = MouseRaycasts.Count > 0
            ? MouseRaycasts[0]
            : null;

        if (InputManager.InputActions.UI.Submit.WasPressedThisFrame())
        {
            if (FirstMouseRaycast.HasValue)
            {
                Debug.Log(
                    $"First mouse raycast: {FirstMouseRaycast.Value.gameObject.name}",
                    FirstMouseRaycast.Value.gameObject
                );
            }
        }
    }

    public void ShowPanel(GameObject panel, bool show, GameObject parentPanel = null)
    {
        if (panel == null)
            return;

        ActivePanels.RemoveAll(x => x == null);

        if (show && !ActivePanels.Contains(panel))
        {
            if (ActivePanels.Count > 0)
            {
                if (ActivePanels[^1] != parentPanel)
                    ActivePanels[^1].SetActiveSafe(false);
            }

            ActivePanels.Add(panel);
            panel.SetActiveSafe(true);

            BackgroundParticleSystem.SetActiveSafe(
                panel != TrackSelection.gameObject
            );
        }
        else if (!show && ActivePanels.Contains(panel))
        {
            ActivePanels.Remove(panel);
            panel.SetActiveSafe(false);

            if (ActivePanels.Count > 0)
                ActivePanels[^1].SetActiveSafe(true);
        }
    }

    public void ShowTrackSelection(bool show, GameObject parentPanel = null)
    {
        ShowPanel(TrackSelection.gameObject, show, parentPanel);
    }

    void OnSingleplayerClick()
    {
        ShowTrackSelection(MainMenuPanel);
    }

    void OnMultiplayerClick()
    {
        ShowPanel(MultiplayerPanel, true, MainMenuPanel);
    }

    void OnCustomizeClick()
    {
        SceneLoader.LoadScene(SceneType.KartCustomization);
    }

    void OnScoresClick()
    {
        LoadMenuScene(ScoresSceneName);
    }

    void OnAboutClick()
    {
        LoadMenuScene(AboutSceneName);
    }

    void OnTutorialClick()
    {
        string sceneName = string.IsNullOrWhiteSpace(TutorialSceneName)
            ? "TutorialPanel"
            : TutorialSceneName;

        Debug.Log("Opening Tutorial Scene: " + sceneName);

        LoadMenuScene(sceneName);
    }

    void LoadMenuScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError(
                "MainMenuManager: No scene name has been assigned!"
            );
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                "MainMenuManager: Scene '" + sceneName +
                "' was not found in Build Settings!"
            );
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    void OnSettingsClick()
    {
        var settings = SettingsMenu != null
            ? SettingsMenu
            : SettingsMenuUI.Instance;

        if (settings == null)
        {
            Debug.LogWarning(
                "MainMenuManager: no SettingsMenuUI found in the scene. Run Tools > Bathtub Racing > Install Settings Menu."
            );
            return;
        }

        settings.Open(() =>
        {
            if (InputManager.IsUsingController && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(
                    SettingsBtn.gameObject
                );
            }
        });
    }

    void OnQuitClick()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }

    void OnHostClick()
    {
        SteamManager.CreateLobby();
    }

    void OnJoinClick()
    {
        var joinCodeStr = JoinCodeField.text;

        if (ulong.TryParse(joinCodeStr, out ulong lobbyId))
        {
            SteamManager.JoinLobby(new Steamworks.CSteamID(lobbyId));
            ChangeJoinBtnText(true, "Joining...");
        }
        else
        {
            Debug.Log("Invalid join code!");
            ChangeJoinBtnText(true, "Invalid code!");
        }
    }

    void OnCancelMultiplayerClick()
    {
        ShowPanel(MultiplayerPanel, false);
    }

    public void ChangeJoinBtnText(bool change, string text = null)
    {
        if (changeJoinBtnCor != null)
        {
            StopCoroutine(changeJoinBtnCor);
            changeJoinBtnCor = null;
        }

        if (change)
        {
            changeJoinBtnCor = StartCoroutine(ChangeJoinBtnCor(text));
        }
        else
        {
            JoinBtnText.text = originJoinBtnText;
        }
    }

    IEnumerator ChangeJoinBtnCor(string text)
    {
        JoinBtnText.text = text;

        yield return new WaitForSeconds(2.5f);

        JoinBtnText.text = originJoinBtnText;
        changeJoinBtnCor = null;
    }

    void OnJoinRandomClick()
    {
        OnCancelMultiplayerClick();
        SteamManager.StartJoiningRandomLobby();
    }

    void OnCancelJoinRandomClick()
    {
        SteamManager.StopJoiningRandomLobby();
    }

    void SteamManager_OnLobbyEntered(Steamworks.LobbyEnter_t enterInfo)
    {
        ShowPanel(MultiplayerPanel, false);
    }

    public void OnCancel(BaseEventData eventData)
    {
        if (MultiplayerPanel.activeSelf)
        {
            OnCancelMultiplayerClick();
        }
        else if (LobbyUI.gameObject.activeSelf)
        {
            LobbyUI.LeaveBtn.onClick.Invoke();
        }
    }

    void OnDestroy()
    {
        SteamManager.OnLobbyEntered -= SteamManager_OnLobbyEntered;
    }
}
