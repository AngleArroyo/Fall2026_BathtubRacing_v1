using System;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// One-click setup for the Settings menu.
///
/// Tools > Bathtub Racing > Install Settings Menu
///   1. Builds Assets/Resources/Prefabs/UI/Settings Menu.prefab (Audio, Graphics, Controls, Reset, Back) using the
///      project's Button prefab, Franklin Gothic font and Main Menu colors.
///   2. MainMenu scene: adds a "Settings" button to the main menu button grid and a Settings Menu instance,
///      and wires both into MainMenuManager.
///   3. Racetrack scene: adds a "Settings" button to the pause menu (Resume / Settings / Main Menu),
///      adds a Settings Menu instance, wires the Pause component, and routes scene AudioSources to the mixer.
///   4. Routes kart prefab AudioSources (engine sounds) to the SFX mixer group.
/// Safe to run again: existing buttons/instances are reused instead of duplicated.
///
/// Tools > Bathtub Racing > Validate Settings Setup
///   Checks the mixer, quality levels, prefab references and both scenes, and logs anything missing.
/// </summary>
public static class SettingsMenuInstaller
{
    const string Title = "Bathtub Racing Settings";
    const string PrefabPath = "Assets/Resources/Prefabs/UI/Settings Menu.prefab";
    const string ButtonPrefabPath = "Assets/Resources/Prefabs/UI/Button.prefab";
    const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/Franklin Gothic SDF.asset";
    const string MixerPath = "Assets/Resources/Audio/GameAudioMixer.mixer";
    const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    const string RacetrackScenePath = "Assets/Scenes/Racetrack.unity";
    const string KartPrefabFolder = "Assets/Resources/Prefabs/Karts";
    const string SettingsMenuName = "Settings Menu";
    const int UILayer = 5;

    //Theme: matches the Main Menu (Button.prefab green, grey panels, Franklin Gothic, white text).
    static readonly Color Green = new Color(0.427f, 0.718f, 0.259f, 1f);
    static readonly Color DarkGreen = new Color(0.30f, 0.52f, 0.18f, 1f);
    static readonly Color PanelColor = new Color(0.20f, 0.20f, 0.20f, 1f);
    static readonly Color DimColor = new Color(0f, 0f, 0f, 0.7f);
    static readonly Color FieldColor = new Color(0.12f, 0.12f, 0.12f, 1f);
    static readonly Color ListColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    static readonly Color DividerColor = new Color(1f, 1f, 1f, 0.15f);
    static readonly Color SubtleText = new Color(0.78f, 0.78f, 0.78f, 1f);
    static readonly Color RowShade = new Color(1f, 1f, 1f, 0.06f);

    static TMP_FontAsset font;
    static GameObject buttonPrefab;
    static AudioMixerGroup musicGroup, sfxGroup;
    static Sprite uiSprite, bgSprite, fieldSprite, knobSprite, checkSprite, arrowSprite, maskSprite;

    #region Install
    [MenuItem("Tools/Bathtub Racing/Install Settings Menu")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(Title, "Exit Play Mode before installing the Settings menu.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        if (!LoadAssets(out string error))
        {
            EditorUtility.DisplayDialog(Title, error, "OK");
            return;
        }

        bool rebuildPrefab = true;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            int choice = EditorUtility.DisplayDialogComplex(Title,
                "A Settings Menu prefab already exists.\n\nRebuild it from scratch (overwrites any manual layout edits), or keep it and only re-wire the scenes?",
                "Rebuild Prefab", "Cancel", "Keep Existing Prefab");
            if (choice == 1)
                return;
            rebuildPrefab = choice == 0;
        }

        string originalScenePath = SceneManager.GetActiveScene().path;
        var log = new StringBuilder();

        try
        {
            RunInstall(rebuildPrefab, log);
        }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogException(e);
            EditorUtility.DisplayDialog(Title, $"Install stopped: {e.Message}\n\nSee the Console for details. No further scenes were changed.", "OK");
            return;
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        if (!string.IsNullOrEmpty(originalScenePath) && originalScenePath != SceneManager.GetActiveScene().path)
            EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);

        Debug.Log($"[Settings Installer] Done.\n{log}");
        EditorUtility.DisplayDialog(Title, "Settings menu installed in MainMenu and Racetrack.\n\nDetails are in the Console. Commit the changed scenes, prefabs and the new Settings files.", "OK");
    }

    /// <summary>Does the actual work for Install. Throws on failure.</summary>
    static void RunInstall(bool rebuildPrefab, StringBuilder log)
    {
        EditorUtility.DisplayProgressBar(Title, "Opening MainMenu scene...", 0.1f);
        var mainMenuScene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

        GameObject prefab = rebuildPrefab ? BuildPrefab(log) : AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        InstallMainMenu(mainMenuScene, prefab, log);
        EditorSceneManager.SaveScene(mainMenuScene);

        EditorUtility.DisplayProgressBar(Title, "Routing kart prefab audio...", 0.35f);
        RouteKartPrefabAudio(log);

        EditorUtility.DisplayProgressBar(Title, "Opening Racetrack scene (large scene, this can take a moment)...", 0.5f);
        var racetrackScene = EditorSceneManager.OpenScene(RacetrackScenePath, OpenSceneMode.Single);
        EditorUtility.DisplayProgressBar(Title, "Installing pause menu settings...", 0.8f);
        InstallRacetrack(racetrackScene, prefab, log);
        EditorSceneManager.SaveScene(racetrackScene);

        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Command-line version with no dialogs, for running Unity in batch mode:
    /// Unity.exe -batchmode -projectPath &lt;project&gt; -executeMethod SettingsMenuInstaller.InstallFromCommandLine -logFile &lt;log&gt;
    /// Always rebuilds the prefab, then runs Validate and exits (exit code 0 = success).
    /// </summary>
    public static void InstallFromCommandLine()
    {
        var log = new StringBuilder();
        int exitCode = 0;
        try
        {
            if (!LoadAssets(out string error))
                throw new Exception(error);

            RunInstall(rebuildPrefab: true, log);
            Debug.Log($"[Settings Installer] Done.\n{log}");
            Validate();
        }
        catch (Exception e)
        {
            Debug.LogError($"[Settings Installer] FAILED: {e}\nProgress before failure:\n{log}");
            exitCode = 1;
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
        EditorApplication.Exit(exitCode);
    }

    static bool LoadAssets(out string error)
    {
        error = null;

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
            font = TMP_Settings.defaultFontAsset;
        if (font == null)
        {
            error = $"Could not load the UI font at {FontPath}.";
            return false;
        }

        buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPrefabPath);
        if (buttonPrefab == null)
        {
            error = $"Could not load the shared button prefab at {ButtonPrefabPath}.";
            return false;
        }

        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        if (mixer == null)
        {
            error = $"Could not load the audio mixer at {MixerPath}. Reimport it (right-click > Reimport) and try again.";
            return false;
        }
        musicGroup = mixer.FindMatchingGroups(SettingsManager.MusicGroupName).FirstOrDefault(g => g.name == SettingsManager.MusicGroupName);
        sfxGroup = mixer.FindMatchingGroups(SettingsManager.SfxGroupName).FirstOrDefault(g => g.name == SettingsManager.SfxGroupName);
        if (musicGroup == null || sfxGroup == null)
        {
            error = $"{MixerPath} is missing its Music or SFX group.";
            return false;
        }

        uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        bgSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        fieldSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");
        knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        checkSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
        arrowSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd");
        maskSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd");
        return true;
    }
    #endregion

    #region Prefab
    static GameObject BuildPrefab(StringBuilder log)
    {
        //Own overlay canvas + scaler, so the menu looks identical in every scene and at every resolution.
        var root = new GameObject(SettingsMenuName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        try
        {
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var ui = root.AddComponent<SettingsMenuUI>();

            //Root (shown/hidden) > Dim (blocks clicks to the menu behind) + Panel.
            var content = CreateRect("Root", root.transform);
            Stretch(content);
            ui.Root = content.gameObject;

            var dim = CreateImage("Dim", content, DimColor, null, raycastTarget: true);
            Stretch(dim.rectTransform);

            var panel = CreateImage("Panel", content, PanelColor, uiSprite, raycastTarget: true);
            Center(panel.rectTransform, 1300, 820);

            //Header bar
            var header = CreateImage("Header", panel.transform, Green, uiSprite);
            var headerRt = header.rectTransform;
            headerRt.anchorMin = new Vector2(0, 1);
            headerRt.anchorMax = new Vector2(1, 1);
            headerRt.pivot = new Vector2(0.5f, 1);
            headerRt.anchoredPosition = Vector2.zero;
            headerRt.sizeDelta = new Vector2(0, 110);
            var title = CreateText("Title", header.transform, "SETTINGS", 64, TextAlignmentOptions.Center, Color.white);
            title.fontStyle = FontStyles.Bold;
            Stretch(title.rectTransform);

            //Columns
            var left = CreateRect("Audio & Graphics", panel.transform);
            Place(left, 50, 140, 580, 560);
            var divider = CreateImage("Divider", panel.transform, DividerColor);
            Place(divider.rectTransform, 659, 150, 2, 540);
            var right = CreateRect("Controls", panel.transform);
            Place(right, 690, 140, 560, 560);

            //---- Audio ----
            SectionHeader(left, "AUDIO", 0, 580);
            ui.MasterSlider = VolumeRow(left, "Master", 70, out ui.MasterValueText);
            ui.MusicSlider = VolumeRow(left, "Music", 140, out ui.MusicValueText);
            ui.SfxSlider = VolumeRow(left, "Sound Effects", 210, out ui.SfxValueText);

            //---- Graphics ----
            SectionHeader(left, "GRAPHICS", 300, 580);
            var qualityLabel = CreateText("Quality Label", left, "Quality", 30, TextAlignmentOptions.MidlineLeft, Color.white);
            Place(qualityLabel.rectTransform, 0, 370, 190, 50);
            ui.QualityDropdown = CreateDropdown(left);
            Place((RectTransform)ui.QualityDropdown.transform, 200, 370, 380, 50);

            //---- Controls ----
            SectionHeader(right, "CONTROLS", 0, 560);
            var columns = CreateRect("Column Headers", right);
            Place(columns, 0, 62, 560, 30);
            ControlColumns(columns, "ACTION", "KEYBOARD", "CONTROLLER", 20, SubtleText, false);

            var list = CreateRect("Controls List", right);
            Place(list, 0, 100, 560, 7 * 54);
            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 2;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ui.ControlsList = list;

            var rowImage = CreateImage("Row Template", list, RowShade);
            rowImage.gameObject.AddComponent<LayoutElement>().preferredHeight = 52;
            ControlColumns(rowImage.rectTransform, "Action", "Keyboard", "Controller", 24, Color.white, true);
            rowImage.gameObject.SetActive(false);
            ui.ControlsRowTemplate = rowImage.gameObject;

            var note = CreateText("Note", right, "Controls are read from the game's input settings.", 18, TextAlignmentOptions.MidlineLeft, SubtleText);
            note.fontStyle = FontStyles.Italic;
            Place(note.rectTransform, 0, 500, 560, 30);

            //---- Footer buttons (shared Button prefab, same look as the Main Menu) ----
            ui.ResetBtn = CreateMenuButton(panel.transform, "Reset Btn", "Reset to Defaults", new Vector2(-180, 40));
            ui.BackBtn = CreateMenuButton(panel.transform, "Back Btn", "Back", new Vector2(180, 40));

            //Explicit controller navigation for the bottom row.
            LinkVertical(ui.MasterSlider, ui.MusicSlider);
            LinkVertical(ui.MusicSlider, ui.SfxSlider);
            LinkVertical(ui.SfxSlider, ui.QualityDropdown);
            LinkVertical(ui.QualityDropdown, ui.ResetBtn);
            var resetNav = ui.ResetBtn.navigation;
            resetNav.mode = Navigation.Mode.Explicit;
            resetNav.selectOnUp = ui.QualityDropdown;
            resetNav.selectOnRight = ui.BackBtn;
            ui.ResetBtn.navigation = resetNav;
            var backNav = ui.BackBtn.navigation;
            backNav.mode = Navigation.Mode.Explicit;
            backNav.selectOnUp = ui.QualityDropdown;
            backNav.selectOnLeft = ui.ResetBtn;
            ui.BackBtn.navigation = backNav;

            content.gameObject.SetActive(false);
            SetLayerRecursively(root, UILayer);

            var saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
            if (!success || saved == null)
                throw new Exception($"Could not save {PrefabPath}.");

            log.AppendLine($"- Built prefab {PrefabPath}");
            return saved;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static void SectionHeader(RectTransform parent, string text, float y, float width)
    {
        var label = CreateText(text + " Header", parent, text, 36, TextAlignmentOptions.BottomLeft, Green);
        label.fontStyle = FontStyles.Bold;
        Place(label.rectTransform, 0, y, width, 46);
        var line = CreateImage(text + " Underline", parent, Green);
        Place(line.rectTransform, 0, y + 50, width, 3);
    }

    static Slider VolumeRow(RectTransform parent, string label, float y, out TMP_Text valueText)
    {
        var text = CreateText(label + " Label", parent, label, 30, TextAlignmentOptions.MidlineLeft, Color.white);
        text.enableAutoSizing = true;
        text.fontSizeMin = 20;
        text.fontSizeMax = 30;
        Place(text.rectTransform, 0, y, 190, 50);

        var sliderGo = DefaultControls.CreateSlider(UiResources());
        sliderGo.name = label + " Slider";
        sliderGo.transform.SetParent(parent, false);
        Place((RectTransform)sliderGo.transform, 205, y + 13, 285, 24);

        var slider = sliderGo.GetComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = 1;
        var colors = slider.colors;
        colors.highlightedColor = new Color(0.85f, 1f, 0.75f);
        colors.selectedColor = new Color(0.85f, 1f, 0.75f);
        colors.pressedColor = Green;
        slider.colors = colors;

        SetImageColor(sliderGo.transform, "Background", FieldColor);
        SetImageColor(sliderGo.transform, "Fill Area/Fill", Green);
        SetImageColor(sliderGo.transform, "Handle Slide Area/Handle", Color.white);
        var handle = sliderGo.transform.Find("Handle Slide Area/Handle") as RectTransform;
        if (handle != null)
            handle.sizeDelta = new Vector2(30, 12);

        valueText = CreateText(label + " Value", parent, "100%", 28, TextAlignmentOptions.MidlineRight, SubtleText);
        Place(valueText.rectTransform, 495, y, 85, 50);
        return slider;
    }

    static TMP_Dropdown CreateDropdown(RectTransform parent)
    {
        var res = new TMP_DefaultControls.Resources
        {
            standard = uiSprite,
            background = bgSprite,
            inputField = fieldSprite,
            knob = knobSprite,
            checkmark = checkSprite,
            dropdown = arrowSprite,
            mask = maskSprite,
        };
        var go = TMP_DefaultControls.CreateDropdown(res);
        go.name = "Quality Dropdown";
        go.transform.SetParent(parent, false);

        var dropdown = go.GetComponent<TMP_Dropdown>();
        dropdown.ClearOptions();
        dropdown.AddOptions(SettingsManager.QualityOptionNames.ToList());
        dropdown.value = SettingsManager.QualityOptionNames.Length - 1;

        //White image + tinted color block = exact colors.
        var image = go.GetComponent<Image>();
        image.color = Color.white;
        dropdown.colors = MakeColors(FieldColor, DarkGreen, Green);

        foreach (var text in go.GetComponentsInChildren<TMP_Text>(true))
        {
            text.font = font;
            text.color = Color.white;
            text.fontSize = 28;
        }
        if (dropdown.captionText != null)
        {
            dropdown.captionText.alignment = TextAlignmentOptions.MidlineLeft;
            dropdown.captionText.rectTransform.offsetMin = new Vector2(16, 4);
            dropdown.captionText.rectTransform.offsetMax = new Vector2(-40, -4);
        }
        SetImageColor(go.transform, "Arrow", Color.white);

        //Drop-down list: 3 items of 48px.
        var template = dropdown.template;
        if (template != null)
        {
            template.sizeDelta = new Vector2(template.sizeDelta.x, 3 * 48 + 8);
            SetImageColor(template, "", ListColor);

            var content = template.Find("Viewport/Content") as RectTransform;
            if (content != null)
                content.sizeDelta = new Vector2(content.sizeDelta.x, 52);

            var item = template.Find("Viewport/Content/Item") as RectTransform;
            if (item != null)
            {
                item.sizeDelta = new Vector2(item.sizeDelta.x, 48);
                var toggle = item.GetComponent<Toggle>();
                if (toggle != null)
                    toggle.colors = MakeColors(ListColor, DarkGreen, Green);
                SetImageColor(item, "Item Background", Color.white);
                SetImageColor(item, "Item Checkmark", Color.white);
                var itemLabel = item.Find("Item Label");
                if (itemLabel != null && itemLabel.TryGetComponent(out TMP_Text itemText))
                    itemText.fontSize = 26;
            }
        }
        return dropdown;
    }

    static ColorBlock MakeColors(Color normal, Color highlighted, Color pressed)
    {
        var colors = ColorBlock.defaultColorBlock;
        colors.normalColor = normal;
        colors.highlightedColor = highlighted;
        colors.selectedColor = highlighted;
        colors.pressedColor = pressed;
        colors.disabledColor = new Color(normal.r, normal.g, normal.b, 0.5f);
        colors.colorMultiplier = 1;
        colors.fadeDuration = 0.1f;
        return colors;
    }

    static void ControlColumns(RectTransform row, string action, string keyboard, string controller, float size, Color color, bool autoSize)
    {
        Column(row, "Action", action, 10, 165, size, color, autoSize);
        Column(row, "Keyboard", keyboard, 180, 200, size - (autoSize ? 2 : 0), color, autoSize);
        Column(row, "Controller", controller, 385, 170, size - (autoSize ? 2 : 0), color, autoSize);
    }

    static void Column(RectTransform row, string name, string text, float x, float width, float size, Color color, bool autoSize)
    {
        var tmp = CreateText(name, row, text, size, TextAlignmentOptions.MidlineLeft, color);
        var rt = tmp.rectTransform;
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0);
        rt.sizeDelta = new Vector2(width, 0);
        if (autoSize)
        {
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 14;
            tmp.fontSizeMax = size;
            tmp.textWrappingMode = TextWrappingModes.Normal;
        }
    }

    static Button CreateMenuButton(Transform parent, string name, string label, Vector2 position)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, parent);
        go.name = name;
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0);
        rt.anchorMax = new Vector2(0.5f, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(300, 60);
        SetButtonLabel(go, label);
        return go.GetComponent<Button>();
    }

    static void LinkVertical(Selectable upper, Selectable lower)
    {
        var upNav = upper.navigation;
        upNav.mode = Navigation.Mode.Explicit;
        upNav.selectOnDown = lower;
        upper.navigation = upNav;

        var downNav = lower.navigation;
        downNav.mode = Navigation.Mode.Explicit;
        downNav.selectOnUp = upper;
        lower.navigation = downNav;
    }
    #endregion

    #region Scenes
    static void InstallMainMenu(Scene scene, GameObject prefab, StringBuilder log)
    {
        var manager = UnityEngine.Object.FindObjectsByType<MainMenuManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        if (manager == null)
            throw new Exception("MainMenuManager not found in the MainMenu scene.");
        if (manager.AboutBtn == null)
            throw new Exception("MainMenuManager.AboutBtn is not assigned, so the main menu button grid could not be found.");

        var settingsUI = GetOrCreateSceneInstance(scene, prefab, log).GetComponent<SettingsMenuUI>();

        //Settings button goes in the main menu button grid, right after About (fills the empty 6th grid cell).
        Transform grid = manager.AboutBtn.transform.parent;
        Button settingsBtn = manager.SettingsBtn;
        if (settingsBtn == null)
        {
            var existing = grid.Find("Settings");
            if (existing != null)
                settingsBtn = existing.GetComponent<Button>();
        }
        if (settingsBtn == null)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, grid);
            go.name = "Settings";
            go.transform.SetSiblingIndex(manager.AboutBtn.transform.GetSiblingIndex() + 1);
            SetButtonLabel(go, "Settings");
            Undo.RegisterCreatedObjectUndo(go, "Add Settings Button");
            settingsBtn = go.GetComponent<Button>();
            log.AppendLine("- MainMenu: added Settings button to the main menu");
        }

        Undo.RecordObject(manager, "Wire Settings Menu");
        manager.SettingsBtn = settingsBtn;
        manager.SettingsMenu = settingsUI;
        EditorUtility.SetDirty(manager);
        log.AppendLine("- MainMenu: MainMenuManager.SettingsBtn / SettingsMenu assigned");

        WarnIfNoEventSystem(scene, log);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    static void InstallRacetrack(Scene scene, GameObject prefab, StringBuilder log)
    {
        var pause = UnityEngine.Object.FindObjectsByType<Pause>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        if (pause == null)
            throw new Exception("Pause component not found in the Racetrack scene.");
        if (pause.PauseMenu == null)
            throw new Exception("Pause.PauseMenu is not assigned in the Racetrack scene.");

        var settingsUI = GetOrCreateSceneInstance(scene, prefab, log).GetComponent<SettingsMenuUI>();

        Transform buttons = pause.PauseMenu.transform.Find("buttons");
        Transform resume = buttons != null ? buttons.Find("resume") : null;
        Transform quit = buttons != null ? buttons.Find("quit") : null;
        if (buttons == null || resume == null)
            throw new Exception("Could not find buttons/resume under the pause menu (Game Menu prefab).");

        //Settings button: a copy of Resume so it matches the pause menu style exactly.
        Button settingsBtn = pause.SettingsBtn;
        if (settingsBtn == null)
        {
            var existing = buttons.Find("settings");
            if (existing != null)
                settingsBtn = existing.GetComponent<Button>();
        }
        if (settingsBtn == null)
        {
            var go = UnityEngine.Object.Instantiate(resume.gameObject, buttons);
            go.name = "settings";
            go.transform.SetSiblingIndex(resume.GetSiblingIndex() + 1);
            settingsBtn = go.GetComponent<Button>();
            settingsBtn.onClick = new Button.ButtonClickedEvent(); //Remove the copied Resume (TogglePause) call.
            SetButtonLabel(go, "Settings", autoSize: true);
            Undo.RegisterCreatedObjectUndo(go, "Add Pause Settings Button");
            log.AppendLine("- Racetrack: added Settings button to the pause menu");
        }

        //Make room for three buttons in the pause menu grid.
        var grid = buttons.GetComponent<GridLayoutGroup>();
        var buttonsRt = (RectTransform)buttons;
        if (grid != null)
        {
            int count = buttons.Cast<Transform>().Count(t => t.gameObject.activeSelf);
            float needed = count * grid.cellSize.y + (count - 1) * grid.spacing.y;
            if (buttonsRt.sizeDelta.y < needed)
            {
                Undo.RecordObject(buttonsRt, "Resize Pause Buttons");
                buttonsRt.sizeDelta = new Vector2(buttonsRt.sizeDelta.x, needed);
                RecordInstanceChange(buttonsRt);
            }
        }

        //The pause "Quit" button returns to the main menu (GameUI.QuitBtn), so label it that way.
        if (quit != null)
            SetButtonLabel(quit.gameObject, "Main Menu", autoSize: true);

        Undo.RecordObject(pause, "Wire Pause Settings");
        pause.SettingsBtn = settingsBtn;
        pause.SettingsMenu = settingsUI;
        EditorUtility.SetDirty(pause);
        RecordInstanceChange(pause);
        log.AppendLine("- Racetrack: Pause.SettingsBtn / SettingsMenu assigned; pause buttons are Resume / Settings / Main Menu");

        RouteSceneAudio(scene, log);
        WarnIfNoEventSystem(scene, log);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    static GameObject GetOrCreateSceneInstance(Scene scene, GameObject prefab, StringBuilder log)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (PrefabUtility.GetCorrespondingObjectFromSource(root) == prefab)
                return root;

            //Remove an old, unlinked copy so there is only ever one settings menu per scene.
            if (root.name == SettingsMenuName && root.GetComponent<SettingsMenuUI>() != null)
            {
                Undo.DestroyObjectImmediate(root);
                log.AppendLine($"- {scene.name}: removed an unlinked '{SettingsMenuName}' object");
            }
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = SettingsMenuName;
        Undo.RegisterCreatedObjectUndo(instance, "Add Settings Menu");
        log.AppendLine($"- {scene.name}: added '{SettingsMenuName}' prefab instance");
        return instance;
    }

    static void WarnIfNoEventSystem(Scene scene, StringBuilder log)
    {
        bool hasEventSystem = UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Any(e => e.gameObject.scene == scene);
        if (!hasEventSystem)
            log.AppendLine($"- WARNING {scene.name}: no EventSystem found; menu buttons will not respond to clicks.");
    }
    #endregion

    #region Audio routing
    static void RouteSceneAudio(Scene scene, StringBuilder log)
    {
        int music = 0, sfx = 0;
        foreach (var source in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (source.gameObject.scene != scene || source.outputAudioMixerGroup != null)
                continue;

            Undo.RecordObject(source, "Route Audio");
            bool isMusic = SettingsManager.IsMusicSource(source);
            source.outputAudioMixerGroup = isMusic ? musicGroup : sfxGroup;
            RecordInstanceChange(source);
            if (isMusic) music++; else sfx++;
        }
        log.AppendLine($"- {scene.name}: routed {music} AudioSource(s) to Music and {sfx} to SFX");
    }

    static void RouteKartPrefabAudio(StringBuilder log)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { KartPrefabFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                int routed = 0;
                foreach (var source in contents.GetComponentsInChildren<AudioSource>(true))
                {
                    if (source.outputAudioMixerGroup != null)
                        continue;
                    source.outputAudioMixerGroup = SettingsManager.IsMusicSource(source) ? musicGroup : sfxGroup;
                    routed++;
                }

                if (routed > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                    log.AppendLine($"- {path}: routed {routed} AudioSource(s) to the mixer");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }
    }
    #endregion

    #region Validate
    [MenuItem("Tools/Bathtub Racing/Validate Settings Setup")]
    public static void Validate()
    {
        var problems = new StringBuilder();
        var ok = new StringBuilder();

        //Mixer
        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        if (mixer == null)
            problems.AppendLine($"Audio mixer missing at {MixerPath}");
        else
        {
            var exposed = new SerializedObject(mixer).FindProperty("m_ExposedParameters");
            foreach (var param in new[] { SettingsManager.MasterParam, SettingsManager.MusicParam, SettingsManager.SfxParam })
            {
                bool found = false;
                for (int i = 0; exposed != null && i < exposed.arraySize; i++)
                {
                    var nameProp = exposed.GetArrayElementAtIndex(i).FindPropertyRelative("name");
                    if (nameProp != null && nameProp.stringValue == param)
                        found = true;
                }
                if (!found)
                    problems.AppendLine($"Mixer parameter '{param}' is not exposed");
            }
            foreach (var group in new[] { SettingsManager.MusicGroupName, SettingsManager.SfxGroupName })
            {
                if (!mixer.FindMatchingGroups(group).Any(g => g.name == group))
                    problems.AppendLine($"Mixer group '{group}' missing");
            }
            ok.AppendLine("Audio mixer checked");
        }

        //Quality levels
        foreach (var option in SettingsManager.QualityOptionNames)
        {
            if (!QualitySettings.names.Contains(option))
                problems.AppendLine($"Quality level '{option}' not found for the current build target (found: {string.Join(", ", QualitySettings.names)})");
        }
        ok.AppendLine($"Quality levels: {string.Join(", ", QualitySettings.names)}");

        //Prefab references
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var ui = prefab != null ? prefab.GetComponent<SettingsMenuUI>() : null;
        if (ui == null)
            problems.AppendLine($"Settings prefab missing at {PrefabPath} (run Install Settings Menu)");
        else
            CheckFields(ui, "Settings Menu prefab", problems);

        //Scenes (only checks scenes that are currently open, to avoid reloading the large Racetrack scene).
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.path == MainMenuScenePath)
            {
                var manager = UnityEngine.Object.FindObjectsByType<MainMenuManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
                if (manager == null || manager.SettingsBtn == null || manager.SettingsMenu == null)
                    problems.AppendLine("MainMenu: MainMenuManager.SettingsBtn or SettingsMenu not assigned");
                else
                    ok.AppendLine("MainMenu scene wired");
            }
            if (scene.path == RacetrackScenePath)
            {
                var pause = UnityEngine.Object.FindObjectsByType<Pause>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
                if (pause == null || pause.SettingsBtn == null || pause.SettingsMenu == null)
                    problems.AppendLine("Racetrack: Pause.SettingsBtn or SettingsMenu not assigned");
                else
                    ok.AppendLine("Racetrack scene wired");
            }
        }

        if (problems.Length == 0)
            Debug.Log($"[Settings Validate] All checks passed.\n{ok}");
        else
            Debug.LogError($"[Settings Validate] Problems found:\n{problems}\nPassed:\n{ok}");
    }

    static void CheckFields(SettingsMenuUI ui, string context, StringBuilder problems)
    {
        void Check(UnityEngine.Object value, string name)
        {
            if (value == null)
                problems.AppendLine($"{context}: {name} is not assigned");
        }
        Check(ui.Root, nameof(ui.Root));
        Check(ui.MasterSlider, nameof(ui.MasterSlider));
        Check(ui.MusicSlider, nameof(ui.MusicSlider));
        Check(ui.SfxSlider, nameof(ui.SfxSlider));
        Check(ui.QualityDropdown, nameof(ui.QualityDropdown));
        Check(ui.ControlsList, nameof(ui.ControlsList));
        Check(ui.ControlsRowTemplate, nameof(ui.ControlsRowTemplate));
        Check(ui.ResetBtn, nameof(ui.ResetBtn));
        Check(ui.BackBtn, nameof(ui.BackBtn));
    }
    #endregion

    #region Helpers
    static DefaultControls.Resources UiResources()
    {
        return new DefaultControls.Resources
        {
            standard = uiSprite,
            background = bgSprite,
            inputField = fieldSprite,
            knob = knobSprite,
            checkmark = checkSprite,
            dropdown = arrowSprite,
            mask = maskSprite,
        };
    }

    static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static Image CreateImage(string name, Transform parent, Color color, Sprite sprite = null, bool raycastTarget = false)
    {
        var rt = CreateRect(name, parent);
        var image = rt.gameObject.AddComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        if (sprite != null)
            image.type = Image.Type.Sliced;
        image.raycastTarget = raycastTarget;
        return image;
    }

    static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, TextAlignmentOptions alignment, Color color)
    {
        var rt = CreateRect(name, parent);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;
        return tmp;
    }

    static void SetButtonLabel(GameObject button, string label, bool autoSize = false)
    {
        var tmp = button.GetComponentInChildren<TMP_Text>(true);
        if (tmp == null)
            return;
        Undo.RecordObject(tmp, "Set Button Label");
        tmp.text = label;
        if (autoSize)
        {
            //Shrink to fit the narrower pause menu buttons instead of overflowing.
            float max = tmp.fontSize;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = Mathf.Min(18, max);
            tmp.fontSizeMax = max;
        }
        RecordInstanceChange(tmp);
    }

    static void SetImageColor(Transform parent, string path, Color color)
    {
        var target = string.IsNullOrEmpty(path) ? parent : parent.Find(path);
        if (target != null && target.TryGetComponent(out Image image))
            image.color = color;
    }

    static void RecordInstanceChange(UnityEngine.Object obj)
    {
        if (PrefabUtility.IsPartOfPrefabInstance(obj))
            PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
    }

    /// <summary>Top-left anchored placement inside the parent (x right, y down).</summary>
    static void Place(RectTransform rt, float x, float y, float width, float height)
    {
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(width, height);
    }

    static void Center(RectTransform rt, float width, float height)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(width, height);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
    #endregion
}
