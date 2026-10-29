using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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
    public GameObject AboutPanel;
    public GameObject BathtubHistoryPanel;
    public GameObject ProjectResourcesPanel;
    public GameObject ProjectCreditsPanel;
    public GameObject ScoresPanel;
    [Space]
    public Button SingleplayerBtn;
    public Button MultiplayerBtn;
    public Button CustomizeBtn;
    public Button ScoresBtn;
    public Button ScoresBackBtn;
    public Button AboutBtn;
    public Button AboutBackBtn;
    public Button BathtubBtn;
    public Button BathtubBackBtn;
    public Button ResourcesBtn;
    public Button ResourcesBackBtn;
    public Button CreditsBtn;
    public Button CreditsBackBtn;
    public Button QuitBtn;
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
    private Stack<GameObject> CurrentButtonInput = new();

    [NonSerialized] public RaycastResult? FirstMouseRaycast = null;
    public List<RaycastResult> MouseRaycasts = new();

    public int CallOrder => 0;

    string originJoinBtnText;
    Coroutine changeJoinBtnCor = null;

    const string websiteUrl = "https://multiplayertubracinggame.com/";

    void Awake()
    {
        SingleplayerBtn.onClick.AddListener(OnSingleplayerClick);
        MultiplayerBtn.onClick.AddListener(OnMultiplayerClick);
        CustomizeBtn.onClick.AddListener(OnCustomizeClick);
        ScoresBtn.onClick.AddListener(OnScoresClick);
        AboutBtn.onClick.AddListener(OnAboutClick);
        BathtubBtn.onClick.AddListener(OnBathtubClick);
        ResourcesBtn.onClick.AddListener(OnResourcesClick);
        CreditsBtn.onClick.AddListener(OnCreditsClick);
        QuitBtn.onClick.AddListener(OnQuitClick);

        HostBtn.onClick.AddListener(OnHostClick);
        JoinBtn.onClick.AddListener(OnJoinClick);
        JoinRandomBtn.onClick.AddListener(OnJoinRandomClick);
        CancelJoinRandomBtn.onClick.AddListener(OnCancelJoinRandomClick);
        CancelMultiplayerBtn.onClick.AddListener(OnCancelMultiplayerClick);
        AboutBackBtn.onClick.AddListener(OnBackClick);

        BathtubBackBtn.onClick.AddListener(OnBackClick);
        ResourcesBackBtn.onClick.AddListener(OnBackClick);
        CreditsBackBtn.onClick.AddListener(OnBackClick);
        ScoresBackBtn.onClick.AddListener(OnBackClick);

        originJoinBtnText = JoinBtnText.text;

        SteamManager.OnLobbyEntered += SteamManager_OnLobbyEntered;

        //Enable to call awake and initialize, then disable.
        LobbyUI.gameObject.SetActive(true);
        LobbyUI.gameObject.SetActive(false);
    }

    public void OrderedAwake()
    {
        Instance = this;

        ShowPanel(MainMenuPanel, true);
        //If we're in a Steam lobby, show lobby UI.
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
        FirstMouseRaycast = MouseRaycasts.Count > 0 ? MouseRaycasts[0] : null;

        if (InputManager.InputActions.UI.Submit.WasPressedThisFrame())
        {
            if (FirstMouseRaycast.HasValue)
                Debug.Log($"First mouse raycast: {FirstMouseRaycast.Value.gameObject.name}", FirstMouseRaycast.Value.gameObject);
        }
    }

    public void ShowPanel(GameObject panel, bool show, GameObject parentPanel = null)
    {
        if (panel == null)
            return;
        ActivePanels.RemoveAll(x => x == null);

        if (show && !ActivePanels.Contains(panel))
        {
            //Disable last.
            if (ActivePanels.Count > 0)
            {
                //Only disable if not the parent of new panel.
                if (ActivePanels[^1] != parentPanel)
                    ActivePanels[^1].SetActiveSafe(false);
            }

            //Add and enable new.
            ActivePanels.Add(panel);
            panel.SetActiveSafe(true);

            //If it's the track selection, disable the map background particle system.
            BackgroundParticleSystem.SetActiveSafe(panel != TrackSelection.gameObject);
        }
        else if (!show && ActivePanels.Contains(panel))
        {
            //Disable and remove.
            ActivePanels.Remove(panel);
            panel.SetActiveSafe(false);

            //Enable next if exists.
            if (ActivePanels.Count > 0)
                ActivePanels[^1].SetActiveSafe(true);
        }
    }

    private void ShowPanelWithCurrentButtonInput(GameObject panel, GameObject currentButton)
    {
        if (EventSystem.current.currentSelectedGameObject != null)
            CurrentButtonInput.Push(EventSystem.current.currentSelectedGameObject);
        
        ShowPanel(panel, true);

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(currentButton);
    }
    public void ShowTrackSelection(bool show, GameObject parentPanel = null)
    {
        ShowPanel(TrackSelection.gameObject, show, parentPanel);
    }

    void OnSingleplayerClick()
    {
        ShowTrackSelection(MainMenuPanel);
        //NetworkManager.Instance.StartGame();
    }
    void OnMultiplayerClick()
    {
        //Show the multiplayer panel as a child of main menu, keeping main menu active.
        ShowPanel(MultiplayerPanel, true, MainMenuPanel);
        //Select a multiplayer panel btn. Allows controller to navigate.
        //EventSystem.current.SetSelectedGameObject(HostBtn.gameObject);
    }
    void OnCustomizeClick()
    {
        SceneLoader.LoadScene(SceneType.KartCustomization);
    }
    void OnScoresClick()
    {
        ShowPanelWithCurrentButtonInput(ScoresPanel, ScoresBackBtn.gameObject);
        //ShowPanel(ScoresPanel, true);
        //EventSystem.current.SetSelectedGameObject(null);
        //EventSystem.current.SetSelectedGameObject(ScoresBackBtn.gameObject);
    }
    void OnAboutClick()
    {
        ShowPanelWithCurrentButtonInput(AboutPanel, BathtubBtn.gameObject);
        //Application.OpenURL(websiteUrl); keeping url will repurpose it on new about page as a "click here to read more" button/link
        //should also be stated that this disables/hides everything in the main menu EXCEPT for that animation of the historical papers flying in the back... might not change it since probably has no impact but.. its also not that hard to show/hide in the transistions... i'll look into it when about is fully implemented... LAA 10/15
        //ShowPanel(AboutPanel, true);
        //EventSystem.current.SetSelectedGameObject(null);
        //EventSystem.current.SetSelectedGameObject(BathtubBtn.gameObject);
    }


    void OnBathtubClick()
    {
        ShowPanelWithCurrentButtonInput(BathtubHistoryPanel, BathtubBackBtn.gameObject);
        //should also be stated that this disables/hides everything in the main menu EXCEPT for that animation of the historical papers flying in the back... might not change it since probably has no impact but.. its also not that hard to show/hide in the transistions... i'll look into it when about is fully implemented... LAA 10/15
        //ShowPanel(BathtubHistoryPanel, true);
        //EventSystem.current.SetSelectedGameObject(null);
       // EventSystem.current.SetSelectedGameObject(BathtubBackBtn.gameObject);
    }

    void OnResourcesClick()
    {
        ShowPanelWithCurrentButtonInput(ProjectResourcesPanel, ResourcesBackBtn.gameObject);
        //should also be stated that this disables/hides everything in the main menu EXCEPT for that animation of the historical papers flying in the back... might not change it since probably has no impact but.. its also not that hard to show/hide in the transistions... i'll look into it when about is fully implemented... LAA 10/15
        //ShowPanel(ProjectResourcesPanel, true);
        //EventSystem.current.SetSelectedGameObject(null);
        //EventSystem.current.SetSelectedGameObject(ResourcesBackBtn.gameObject);
    }

    void OnCreditsClick()
    {
        ShowPanelWithCurrentButtonInput(ProjectCreditsPanel, CreditsBackBtn.gameObject);
        //should also be stated that this disables/hides everything in the main menu EXCEPT for that animation of the historical papers flying in the back... might not change it since probably has no impact but.. its also not that hard to show/hide in the transistions... i'll look into it when about is fully implemented... LAA 10/15
        //ShowPanel(ProjectCreditsPanel, true);
        //EventSystem.current.SetSelectedGameObject(null);
        //EventSystem.current.SetSelectedGameObject(CreditsBackBtn.gameObject);
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
        //Disable multiplayer panel.
        ShowPanel(MultiplayerPanel, false);
    }
    public void ChangeJoinBtnText(bool change, string text = null)
    {
        //Reset cor, don't let more than one run at a time.
        if (changeJoinBtnCor != null)
        {
            StopCoroutine(changeJoinBtnCor);
            changeJoinBtnCor = null;
        }

        if (change)
            changeJoinBtnCor = StartCoroutine(ChangeJoinBtnCor(text));
        else
            JoinBtnText.text = originJoinBtnText;
    }
    IEnumerator ChangeJoinBtnCor(string text)
    {
        //Show error text for a bit, then revert text.
        JoinBtnText.text = text;

        yield return new WaitForSeconds(2.5f);

        JoinBtnText.text = originJoinBtnText;
        changeJoinBtnCor = null;
    }

    void OnJoinRandomClick()
    {
        //Close multiplayer panel.
        OnCancelMultiplayerClick();

        SteamManager.StartJoiningRandomLobby();
    }
    void OnCancelJoinRandomClick()
    {
        SteamManager.StopJoiningRandomLobby();
    }

    void SteamManager_OnLobbyEntered(Steamworks.LobbyEnter_t enterInfo)
    {
        //Disable multiplayer panel. Keeps main menu panel active.
        ShowPanel(MultiplayerPanel, false);
    }

    public void OnCancel(BaseEventData eventData)
    {
        //Called when either esc or controller back (B or circle) is pressed.
        if (MultiplayerPanel.activeSelf)
            OnCancelMultiplayerClick();
        else if (LobbyUI.gameObject.activeSelf)
            LobbyUI.LeaveBtn.onClick.Invoke();
        else if (ActivePanels.Count > 1)
            OnBackClick();
    }

    void OnBackClick()
    {
        if (ActivePanels.Count <= 1)
            return;
        GameObject curPanel = ActivePanels[^1];
        ShowPanel(curPanel, false);

        if (CurrentButtonInput.Count > 0)
        {
            GameObject PreviousSelection = CurrentButtonInput.Pop();

            if (PreviousSelection != null && PreviousSelection.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(PreviousSelection);
            }
        }
    }
}
