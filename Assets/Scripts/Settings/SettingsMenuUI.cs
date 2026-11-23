using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The Settings menu panel (Assets/Resources/Prefabs/UI/Settings Menu.prefab).
/// The same prefab is used in the Main Menu and in the Racetrack pause menu.
/// All values are read from / written to <see cref="SettingsManager"/>, so both menus always show the same saved values.
///
/// Open it with <see cref="Open"/>. Back (button, Esc, or controller B) closes it and calls the callback passed to Open.
/// </summary>
public class SettingsMenuUI : MonoBehaviour
{
    public static SettingsMenuUI Instance { get; private set; }

    /// <summary>True while a settings menu is showing.</summary>
    public static bool IsOpen => Instance != null && Instance.IsMenuOpen;
    /// <summary>True on the frame the menu was closed. Lets the pause menu ignore the same Esc press.</summary>
    public static bool ClosedThisFrame => lastClosedFrame == Time.frameCount;
    static int lastClosedFrame = -1;

    [Tooltip("The object that is shown/hidden. Keep the root of this prefab active so Esc handling works.")]
    public GameObject Root;

    [Header("Audio")]
    public Slider MasterSlider;
    public Slider MusicSlider;
    public Slider SfxSlider;
    public TMP_Text MasterValueText;
    public TMP_Text MusicValueText;
    public TMP_Text SfxValueText;

    [Header("Graphics")]
    public TMP_Dropdown QualityDropdown;

    [Header("Controls")]
    [Tooltip("Parent the control rows are created under.")]
    public RectTransform ControlsList;
    [Tooltip("Inactive row with child texts named Action, Keyboard and Controller.")]
    public GameObject ControlsRowTemplate;

    [Header("Buttons")]
    public Button ResetBtn;
    public Button BackBtn;

    public bool IsMenuOpen => Root != null && Root.activeSelf;

    Action onClosed;
    bool initialized;

    void Awake()
    {
        Instance = this;
        SettingsManager.EnsureExists();
        Initialize();

        //Start hidden.
        if (Root != null && Root.activeSelf)
            Root.SetActive(false);
    }

    void OnDestroy()
    {
        SettingsManager.OnSettingsChanged -= Refresh;
        if (Instance == this)
            Instance = null;
    }

    void OnDisable()
    {
        SettingsManager.SaveIfDirty();
    }

    void Initialize()
    {
        if (initialized)
            return;
        initialized = true;

        SetupSlider(MasterSlider, SettingsManager.SetMasterVolume);
        SetupSlider(MusicSlider, SettingsManager.SetMusicVolume);
        SetupSlider(SfxSlider, SettingsManager.SetSfxVolume);

        if (QualityDropdown != null)
        {
            QualityDropdown.ClearOptions();
            QualityDropdown.AddOptions(new System.Collections.Generic.List<string>(SettingsManager.QualityOptionNames));
            QualityDropdown.onValueChanged.AddListener(SettingsManager.SetQualityOption);
        }

        if (ResetBtn != null)
            ResetBtn.onClick.AddListener(SettingsManager.ResetToDefaults);
        if (BackBtn != null)
            BackBtn.onClick.AddListener(Close);

        BuildControlsList();

        //Button animators must keep animating while the game is paused (Time.timeScale = 0).
        UseUnscaledAnimators(gameObject);

        SettingsManager.OnSettingsChanged += Refresh;
        Refresh();
    }

    static void SetupSlider(Slider slider, UnityEngine.Events.UnityAction<float> onChanged)
    {
        if (slider == null)
            return;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.onValueChanged.AddListener(onChanged);
    }

    void Update()
    {
        if (!IsMenuOpen || InputManager.InputActions == null)
            return;

        //Esc / controller B goes back.
        if (InputManager.InputActions.UI.Cancel.WasPressedThisFrame())
        {
            if (QualityDropdown != null && QualityDropdown.IsExpanded)
                QualityDropdown.Hide();
            else
                Close();
        }
    }

    /// <summary>Shows the menu with the currently saved values.</summary>
    /// <param name="onClosedCallback">Called when the player presses Back.</param>
    public void Open(Action onClosedCallback = null)
    {
        Initialize();
        onClosed = onClosedCallback;
        Refresh();

        if (Root != null)
            Root.SetActive(true);

        //Select the first control when using a controller so it can navigate.
        if (InputManager.IsUsingController && MasterSlider != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(MasterSlider.gameObject);
    }

    public void Close()
    {
        if (!IsMenuOpen)
            return;

        if (QualityDropdown != null && QualityDropdown.IsExpanded)
            QualityDropdown.Hide();

        Root.SetActive(false);
        SettingsManager.SaveIfDirty();
        lastClosedFrame = Time.frameCount;

        var callback = onClosed;
        onClosed = null;
        callback?.Invoke();
    }

    /// <summary>Updates every control from SettingsManager without triggering change events.</summary>
    public void Refresh()
    {
        SetSlider(MasterSlider, MasterValueText, SettingsManager.MasterVolume);
        SetSlider(MusicSlider, MusicValueText, SettingsManager.MusicVolume);
        SetSlider(SfxSlider, SfxValueText, SettingsManager.SfxVolume);

        if (QualityDropdown != null)
        {
            QualityDropdown.SetValueWithoutNotify(SettingsManager.CurrentQualityOption);
            QualityDropdown.RefreshShownValue();
        }
    }

    static void SetSlider(Slider slider, TMP_Text valueText, float value)
    {
        if (slider != null)
            slider.SetValueWithoutNotify(value);
        if (valueText != null)
            valueText.text = $"{Mathf.RoundToInt(value * 100f)}%";
    }

    void BuildControlsList()
    {
        if (ControlsList == null || ControlsRowTemplate == null)
            return;

        ControlsRowTemplate.SetActive(false);
        for (int i = ControlsList.childCount - 1; i >= 0; i--)
        {
            var child = ControlsList.GetChild(i).gameObject;
            if (child != ControlsRowTemplate)
                Destroy(child);
        }

        int index = 0;
        foreach (var entry in ControlsInfo.GetRaceControls())
        {
            var row = Instantiate(ControlsRowTemplate, ControlsList);
            row.name = entry.Action;
            SetChildText(row.transform, "Action", entry.Action);
            SetChildText(row.transform, "Keyboard", entry.Keyboard);
            SetChildText(row.transform, "Controller", entry.Controller);

            //Alternate row shading for readability.
            var image = row.GetComponent<Image>();
            if (image != null)
                image.enabled = index % 2 == 0;

            row.SetActive(true);
            index++;
        }
    }

    static void SetChildText(Transform row, string childName, string text)
    {
        var child = row.Find(childName);
        if (child != null && child.TryGetComponent(out TMP_Text tmp))
            tmp.text = text;
    }

    /// <summary>Makes all Animators under root (e.g. the shared Button prefab) animate while Time.timeScale is 0.</summary>
    public static void UseUnscaledAnimators(GameObject root)
    {
        if (root == null)
            return;
        foreach (var animator in root.GetComponentsInChildren<Animator>(true))
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
    }
}
