using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Pause : MonoBehaviour
{
    /// <summary>True while the local player has the pause menu open.</summary>
    public static bool IsPaused { get; private set; }
    static Pause current;

    private bool _isPaused;
    //True only when this script set Time.timeScale to 0 (singleplayer), so we only restore what we changed.
    private bool _frozeTime;

    public GameObject PauseMenu;

    [Header("Settings")]
    public Button SettingsBtn;
    public SettingsMenuUI SettingsMenu;

    /// <summary>Time is only frozen when playing alone. In a Steam lobby other players keep racing.
    /// (Singleplayer also runs a local GONet host, so NetworkManager.IsConnectedGONet is true in both modes.)</summary>
    static bool IsSingleplayer => !SteamManager.InSteamLobby;

    void Awake()
    {
        current = this;

        if (SettingsBtn != null)
            SettingsBtn.onClick.AddListener(OpenSettings);

        //Keep button animations working while time is frozen.
        SettingsMenuUI.UseUnscaledAnimators(PauseMenu);
    }

    void Start()
    {
        _isPaused = false;
        IsPaused = false;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (InputManager.InputActions == null || !InputManager.InputActions.UI.Menu.WasPressedThisFrame())
            return;

        //While Settings is open, Esc/Start means "Back" and is handled by the settings menu.
        if (SettingsMenuUI.IsOpen || SettingsMenuUI.ClosedThisFrame)
            return;

        if (IsRaceEndScreenShowing())
            return;

        TogglePause();
    }

    static bool IsRaceEndScreenShowing()
    {
        var raceManager = RaceManager.Instance;
        return raceManager != null && raceManager.GameUI != null && raceManager.GameUI.RaceEndPanel != null
            && raceManager.GameUI.RaceEndPanel.gameObject.activeInHierarchy;
    }

    /// <summary>Called by the Resume button (persistent OnClick on the Game Menu prefab) and the pause input.</summary>
    public void TogglePause()
    {
        SetPaused(!_isPaused);
    }

    public void SetPaused(bool paused)
    {
        _isPaused = paused;
        IsPaused = paused;

        if (paused)
        {
            //Only affect time scale in singleplayer.
            if (IsSingleplayer)
            {
                Time.timeScale = 0;
                _frozeTime = true;
            }
            SetDrivingInputEnabled(false);

            PauseMenu.SetActive(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            var settings = GetSettingsMenu();
            if (settings != null && settings.IsMenuOpen)
                settings.Close();

            RestoreTime();
            SetDrivingInputEnabled(true);

            PauseMenu.SetActive(false);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    void OpenSettings()
    {
        var settings = GetSettingsMenu();
        if (settings == null)
        {
            Debug.LogWarning("Pause: no SettingsMenuUI found in the scene. Run Tools > Bathtub Racing > Install Settings Menu.");
            return;
        }

        //Hide the pause buttons while settings are open; the game stays paused.
        PauseMenu.SetActive(false);
        settings.Open(OnSettingsClosed);
    }

    void OnSettingsClosed()
    {
        if (!_isPaused)
            return;

        //Back from settings returns to the pause menu.
        PauseMenu.SetActive(true);
        if (InputManager.IsUsingController && SettingsBtn != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(SettingsBtn.gameObject);
    }

    SettingsMenuUI GetSettingsMenu()
    {
        return SettingsMenu != null ? SettingsMenu : SettingsMenuUI.Instance;
    }

    void RestoreTime()
    {
        if (_frozeTime)
        {
            Time.timeScale = 1;
            _frozeTime = false;
        }
    }

    /// <summary>Stops the kart from reading drive/boost/drift input while the menus are open. UI input keeps working.</summary>
    static void SetDrivingInputEnabled(bool enabled)
    {
        var actions = InputManager.InputActions;
        if (actions == null)
            return;

        if (enabled)
            actions.Player.Enable();
        else
            actions.Player.Disable();
    }

    /// <summary>
    /// Call before leaving the race (Main Menu / Quit). Restores normal game time and input
    /// so the main menu and the next race are not stuck paused.
    /// </summary>
    public static void RestoreForSceneExit()
    {
        if (current != null)
        {
            current._isPaused = false;
            current.RestoreTime();
        }
        IsPaused = false;
        Time.timeScale = 1;
        SetDrivingInputEnabled(true);
    }

    void OnDestroy()
    {
        if (current != this)
            return;

        //Safety net if the scene unloads while paused.
        RestoreForSceneExit();
        current = null;
    }
}
