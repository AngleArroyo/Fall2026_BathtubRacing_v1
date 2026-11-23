using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// <summary>
/// The single owner of player settings (audio volumes + graphics quality).
///
/// - Created automatically before the first scene loads (no scene setup needed), and kept alive with
///   DontDestroyOnLoad, so there is exactly one instance no matter which scene you press Play in.
/// - Loads saved values from PlayerPrefs on launch and applies them immediately.
/// - Only writes PlayerPrefs when the player actually changes something (never overwrites saved values with defaults).
/// - UI (main menu settings + pause menu settings) reads and writes through this class, so both stay in sync.
///
/// Audio is routed through Assets/Resources/Audio/GameAudioMixer.mixer (Master > Music, SFX).
/// Any AudioSource without an output group is routed automatically: names/clips containing "music" go to Music,
/// everything else goes to SFX. You can also set the Output group on an AudioSource manually in the Inspector.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class SettingsManager : MonoBehaviour
{
    // ---- PlayerPrefs keys ----
    public const string MasterVolumeKey = "Settings_MasterVolume";
    public const string MusicVolumeKey = "Settings_MusicVolume";
    public const string SfxVolumeKey = "Settings_SfxVolume";
    public const string QualityKey = "Settings_GraphicsQuality";

    // ---- Defaults ----
    public const float DefaultMasterVolume = 1f;
    public const float DefaultMusicVolume = 1f;
    public const float DefaultSfxVolume = 1f;
    /// <summary>The project's default quality level for PC (see Project Settings > Quality, Standalone default).</summary>
    public const string DefaultQualityName = "High";

    // ---- Audio mixer ----
    public const string MixerResourcePath = "Audio/GameAudioMixer";
    public const string MasterParam = "MasterVolume";
    public const string MusicParam = "MusicVolume";
    public const string SfxParam = "SFXVolume";
    public const string MusicGroupName = "Music";
    public const string SfxGroupName = "SFX";

    /// <summary>Options shown in the Graphics Quality dropdown. Each is matched by name to a level in Project Settings > Quality.</summary>
    public static readonly string[] QualityOptionNames = { "Low", "Medium", "High" };

    public static SettingsManager Instance { get; private set; }

    /// <summary>Raised whenever any setting changes (slider, dropdown, reset). Open menus listen to stay in sync.</summary>
    public static event Action OnSettingsChanged;

    public static float MasterVolume => masterVolume;
    public static float MusicVolume => musicVolume;
    public static float SfxVolume => sfxVolume;

    public static AudioMixer Mixer { get; private set; }
    public static AudioMixerGroup MusicGroup { get; private set; }
    public static AudioMixerGroup SfxGroup { get; private set; }

    static float masterVolume = DefaultMasterVolume;
    static float musicVolume = DefaultMusicVolume;
    static float sfxVolume = DefaultSfxVolume;
    static int[] qualityOptionLevels;
    static bool dirty;

    float nextRouteTime;

    #region Bootstrapping
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        //Supports "Enter Play Mode Options" with domain reload disabled.
        Instance = null;
        OnSettingsChanged = null;
        Mixer = null;
        MusicGroup = null;
        SfxGroup = null;
        qualityOptionLevels = null;
        dirty = false;
        masterVolume = DefaultMasterVolume;
        musicVolume = DefaultMusicVolume;
        sfxVolume = DefaultSfxVolume;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CreateOnLaunch()
    {
        EnsureExists();
    }

    /// <summary>Returns the one SettingsManager, creating it if needed.</summary>
    public static SettingsManager EnsureExists()
    {
        if (Instance != null)
            return Instance;

        var existing = FindFirstObjectByType<SettingsManager>();
        if (existing != null)
            return existing;

        var go = new GameObject("Settings Manager");
        return go.AddComponent<SettingsManager>();
    }

    void Awake()
    {
        //Prevent duplicates (e.g. if someone also drops this component into a scene).
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadMixer();
        LoadSavedSettings();
        ApplyAll();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        //AudioMixer.SetFloat is ignored during Awake, so re-apply once everything has started.
        ApplyAudio();
        RouteAudioSources();
    }

    void Update()
    {
        //Catch AudioSources spawned at runtime (networked karts, etc.). Unscaled so it still runs while paused.
        if (Time.unscaledTime >= nextRouteTime)
        {
            nextRouteTime = Time.unscaledTime + 1f;
            RouteAudioSources();
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyAudio();
        RouteAudioSources();
    }

    void OnApplicationPause(bool paused)
    {
        if (paused)
            SaveIfDirty();
    }

    void OnApplicationQuit()
    {
        SaveIfDirty();
    }

    void OnDestroy()
    {
        if (Instance != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SaveIfDirty();
        Instance = null;
    }
    #endregion

    #region Load / Save
    static void LoadSavedSettings()
    {
        //GetFloat returns the default when no key exists. We never write defaults back here,
        //so a player's saved values are never overwritten on launch.
        masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, DefaultMasterVolume));
        musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, DefaultMusicVolume));
        sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, DefaultSfxVolume));

        BuildQualityMap();
        if (PlayerPrefs.HasKey(QualityKey))
        {
            int option = Array.FindIndex(QualityOptionNames, n => string.Equals(n, PlayerPrefs.GetString(QualityKey), StringComparison.OrdinalIgnoreCase));
            if (option >= 0)
                QualitySettings.SetQualityLevel(qualityOptionLevels[option], true);
        }
    }

    /// <summary>Writes PlayerPrefs to disk if anything changed since the last save.</summary>
    public static void SaveIfDirty()
    {
        if (!dirty)
            return;
        dirty = false;
        PlayerPrefs.Save();
    }
    #endregion

    #region Audio
    static void LoadMixer()
    {
        Mixer = Resources.Load<AudioMixer>(MixerResourcePath);
        if (Mixer == null)
        {
            Debug.LogWarning($"[SettingsManager] AudioMixer not found at Resources/{MixerResourcePath}. Master volume will use AudioListener.volume; Music/SFX sliders will have no effect.");
            return;
        }

        MusicGroup = FindGroup(MusicGroupName);
        SfxGroup = FindGroup(SfxGroupName);
    }

    static AudioMixerGroup FindGroup(string groupName)
    {
        var groups = Mixer.FindMatchingGroups(groupName);
        foreach (var group in groups)
        {
            if (group.name == groupName)
                return group;
        }
        Debug.LogWarning($"[SettingsManager] Mixer group '{groupName}' not found in {Mixer.name}.");
        return null;
    }

    public static void SetMasterVolume(float value) => SetVolume(ref masterVolume, MasterVolumeKey, value);
    public static void SetMusicVolume(float value) => SetVolume(ref musicVolume, MusicVolumeKey, value);
    public static void SetSfxVolume(float value) => SetVolume(ref sfxVolume, SfxVolumeKey, value);

    static void SetVolume(ref float field, string key, float value)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(field, value))
            return;

        field = value;
        PlayerPrefs.SetFloat(key, value);
        dirty = true;

        ApplyAudio();
        RaiseChanged();
    }

    static void ApplyAudio()
    {
        if (Mixer == null)
        {
            AudioListener.volume = masterVolume;
            return;
        }

        Mixer.SetFloat(MasterParam, LinearToDecibels(masterVolume));
        Mixer.SetFloat(MusicParam, LinearToDecibels(musicVolume));
        Mixer.SetFloat(SfxParam, LinearToDecibels(sfxVolume));
    }

    /// <summary>Slider values are 0-1 (linear). The mixer works in decibels.</summary>
    public static float LinearToDecibels(float linear)
    {
        return linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
    }

    /// <summary>Sends any AudioSource that has no mixer group to the Music or SFX group.</summary>
    public static void RouteAudioSources()
    {
        if (MusicGroup == null || SfxGroup == null)
            return;

        var sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var source in sources)
        {
            if (source.outputAudioMixerGroup == null)
                source.outputAudioMixerGroup = IsMusicSource(source) ? MusicGroup : SfxGroup;
        }
    }

    public static bool IsMusicSource(AudioSource source)
    {
        if (source.name.IndexOf("music", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        return source.clip != null && source.clip.name.IndexOf("music", StringComparison.OrdinalIgnoreCase) >= 0;
    }
    #endregion

    #region Graphics
    /// <summary>Maps each dropdown option (Low/Medium/High) to a quality level index from Project Settings > Quality.</summary>
    static void BuildQualityMap()
    {
        string[] levelNames = QualitySettings.names;
        qualityOptionLevels = new int[QualityOptionNames.Length];
        bool allFound = true;

        for (int i = 0; i < QualityOptionNames.Length; i++)
        {
            int level = Array.FindIndex(levelNames, n => string.Equals(n, QualityOptionNames[i], StringComparison.OrdinalIgnoreCase));
            if (level < 0)
                allFound = false;
            qualityOptionLevels[i] = level;
        }

        if (!allFound)
        {
            //Fallback: spread the options across whatever levels exist (lowest -> highest).
            Debug.LogWarning($"[SettingsManager] Quality levels named {string.Join("/", QualityOptionNames)} were not all found " +
                $"(found: {string.Join(", ", levelNames)}). Mapping options across the available levels instead.");
            int maxLevel = Mathf.Max(0, levelNames.Length - 1);
            for (int i = 0; i < qualityOptionLevels.Length; i++)
                qualityOptionLevels[i] = Mathf.RoundToInt(maxLevel * (i / (float)(QualityOptionNames.Length - 1)));
        }
    }

    /// <summary>The dropdown option that matches the current quality level.</summary>
    public static int CurrentQualityOption
    {
        get
        {
            if (qualityOptionLevels == null)
                BuildQualityMap();

            int current = QualitySettings.GetQualityLevel();
            int best = 0, bestDistance = int.MaxValue;
            for (int i = 0; i < qualityOptionLevels.Length; i++)
            {
                int distance = Mathf.Abs(qualityOptionLevels[i] - current);
                //Prefer the highest option on ties so the dropdown never under-reports quality.
                if (distance <= bestDistance)
                {
                    best = i;
                    bestDistance = distance;
                }
            }
            return best;
        }
    }

    public static int DefaultQualityOption
    {
        get
        {
            int index = Array.FindIndex(QualityOptionNames, n => n == DefaultQualityName);
            return index >= 0 ? index : QualityOptionNames.Length - 1;
        }
    }

    public static void SetQualityOption(int option)
    {
        if (qualityOptionLevels == null)
            BuildQualityMap();

        option = Mathf.Clamp(option, 0, QualityOptionNames.Length - 1);
        QualitySettings.SetQualityLevel(qualityOptionLevels[option], true);

        PlayerPrefs.SetString(QualityKey, QualityOptionNames[option]);
        dirty = true;
        SaveIfDirty();

        RaiseChanged();
    }
    #endregion

    /// <summary>Restores default volumes and quality, applies them, and saves them so they survive a restart.</summary>
    public static void ResetToDefaults()
    {
        masterVolume = DefaultMasterVolume;
        musicVolume = DefaultMusicVolume;
        sfxVolume = DefaultSfxVolume;
        PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        ApplyAudio();

        if (qualityOptionLevels == null)
            BuildQualityMap();
        int defaultOption = DefaultQualityOption;
        QualitySettings.SetQualityLevel(qualityOptionLevels[defaultOption], true);
        PlayerPrefs.SetString(QualityKey, QualityOptionNames[defaultOption]);

        dirty = true;
        SaveIfDirty();

        RaiseChanged();
    }

    static void ApplyAll()
    {
        ApplyAudio();
    }

    static void RaiseChanged()
    {
        try
        {
            OnSettingsChanged?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}
