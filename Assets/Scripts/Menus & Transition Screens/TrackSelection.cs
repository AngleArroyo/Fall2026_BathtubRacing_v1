
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TrackSelectionManager : MonoBehaviour
{
    // LAP SETTINGS
    public const string SavedLapCountKey = "LapCount";
    public const int DefaultLapCount = 3;
    public const int MaxLapCount = 50;

    // AI RACER SETTINGS
    public const string SavedAICountKey = "AICount";
    public const int DefaultAICount = 3;

    [Header("Lap and AI Input Fields")]
    public TMP_InputField lapsInput;
    public TMP_InputField opponentsInput;

    [Header("Lap Adjustment Buttons")]
    public Button LapDecreaseBtn;
    public Button LapIncreaseBtn;

    [Header("AI Racer Adjustment Buttons")]
    public Button OpponentDecreaseBtn;
    public Button OpponentIncreaseBtn;

    [Header("Navigation Buttons")]
    public Button BackBtn;
    public Button StartBtn;

    // Maximum number of AI racers allowed.
    public int MaxAICount => SteamManager.InSteamLobby
        ? NetworkManager.MaxPlayerCount -
          SteamManager.GetLobbyPlayerCount()
        : NetworkManager.MaxPlayerCount - 1;

    // Allow zero AI racers when multiple
    // players are in a Steam lobby.
    public int MinAICount =>
        SteamManager.InSteamLobby &&
        SteamManager.GetLobbyPlayerCount() > 1 ? 0 : 1;

    void Awake()
    {
        // Input field listeners.
        lapsInput.onValueChanged.AddListener(
            OnLapsInputChanged);

        opponentsInput.onValueChanged.AddListener(
            OnOpponentsInputChanged);

        // Navigation buttons.
        BackBtn.onClick.AddListener(OnBackClick);
        StartBtn.onClick.AddListener(OnStartClick);

        // Lap adjustment buttons.
        if (LapDecreaseBtn != null)
        {
            LapDecreaseBtn.onClick.AddListener(
                DecreaseLaps);
        }

        if (LapIncreaseBtn != null)
        {
            LapIncreaseBtn.onClick.AddListener(
                IncreaseLaps);
        }

        // AI racer adjustment buttons.
        if (OpponentDecreaseBtn != null)
        {
            OpponentDecreaseBtn.onClick.AddListener(
                DecreaseOpponents);
        }

        if (OpponentIncreaseBtn != null)
        {
            OpponentIncreaseBtn.onClick.AddListener(
                IncreaseOpponents);
        }
    }

    void OnEnable()
    {
        // Load previously saved values.
        int savedLaps = Mathf.Clamp(
            GetSavedLapCount(),
            1,
            MaxLapCount
        );

        int savedAI = Mathf.Clamp(
            GetSavedAICount(),
            MinAICount,
            MaxAICount
        );

        // Update the displayed numbers.
        lapsInput.SetTextWithoutNotify(
            savedLaps.ToString());

        opponentsInput.SetTextWithoutNotify(
            savedAI.ToString());
    }

    // ==================================
    // LAP INCREASE AND DECREASE
    // ==================================

    public void IncreaseLaps()
    {
        ChangeLaps(1);
    }

    public void DecreaseLaps()
    {
        ChangeLaps(-1);
    }

    void ChangeLaps(int amount)
    {
        int currentLaps;

        if (!int.TryParse(
            lapsInput.text, out currentLaps))
        {
            currentLaps = DefaultLapCount;
        }

        int newLapCount = Mathf.Clamp(
            currentLaps + amount,
            1,
            MaxLapCount
        );

        lapsInput.SetTextWithoutNotify(
            newLapCount.ToString());

        Debug.Log("Lap Count: " + newLapCount);
    }

    // ==================================
    // AI RACER INCREASE AND DECREASE
    // ==================================

    public void IncreaseOpponents()
    {
        ChangeOpponents(1);
    }

    public void DecreaseOpponents()
    {
        ChangeOpponents(-1);
    }

    void ChangeOpponents(int amount)
    {
        int currentOpponents;

        if (!int.TryParse(
            opponentsInput.text,
            out currentOpponents))
        {
            currentOpponents = DefaultAICount;
        }

        int newOpponentCount = Mathf.Clamp(
            currentOpponents + amount,
            MinAICount,
            MaxAICount
        );

        opponentsInput.SetTextWithoutNotify(
            newOpponentCount.ToString());

        Debug.Log(
            "AI Racer Count: " + newOpponentCount);
    }

    // ==================================
    // MANUAL LAP INPUT VALIDATION
    // ==================================

    void OnLapsInputChanged(string input)
    {
        if (int.TryParse(
            input, out int parsedLaps))
        {
            parsedLaps = Mathf.Clamp(
                parsedLaps,
                1,
                MaxLapCount
            );

            lapsInput.SetTextWithoutNotify(
                parsedLaps.ToString());
        }
        else
        {
            lapsInput.SetTextWithoutNotify(
                DefaultLapCount.ToString());
        }
    }

    // ==================================
    // MANUAL AI INPUT VALIDATION
    // ==================================

    void OnOpponentsInputChanged(string input)
    {
        if (int.TryParse(
            input, out int parsedAICount))
        {
            parsedAICount = Mathf.Clamp(
                parsedAICount,
                MinAICount,
                MaxAICount
            );

            opponentsInput.SetTextWithoutNotify(
                parsedAICount.ToString());
        }
        else
        {
            int defaultAI = Mathf.Clamp(
                DefaultAICount,
                MinAICount,
                MaxAICount
            );

            opponentsInput.SetTextWithoutNotify(
                defaultAI.ToString());
        }
    }

    // ==================================
    // BACK BUTTON
    // ==================================

    void OnBackClick()
    {
        if (MainMenuManager.Instance != null)
        {
            MainMenuManager.Instance
                .ShowTrackSelection(false);
        }
        else
        {
            Debug.LogError(
                "TrackSelectionManager: " +
                "MainMenuManager.Instance is NULL! " +
                "Cannot return to the main menu."
            );
        }
    }

    // ==================================
    // START RACE BUTTON
    // ==================================

    void OnStartClick()
    {
        // Read the selected lap count.
        if (!int.TryParse(
            lapsInput.text, out int parsedLaps))
        {
            parsedLaps = DefaultLapCount;
        }

        parsedLaps = Mathf.Clamp(
            parsedLaps,
            1,
            MaxLapCount
        );

        // Read the selected AI count.
        if (!int.TryParse(
            opponentsInput.text,
            out int parsedAICount))
        {
            parsedAICount = DefaultAICount;
        }

        parsedAICount = Mathf.Clamp(
            parsedAICount,
            MinAICount,
            MaxAICount
        );

        // Save selections to PlayerPrefs.
        PlayerPrefs.SetInt(
            SavedLapCountKey,
            parsedLaps
        );

        PlayerPrefs.SetInt(
            SavedAICountKey,
            parsedAICount
        );

        PlayerPrefs.Save();

        Debug.Log(
            $"TrackSelectionManager: " +
            $"Set lap count: {parsedLaps}, " +
            $"set opponent count: {parsedAICount}"
        );

        // ==================================
        // NETWORK MANAGER NULL CHECK
        // ==================================

        // Check whether the NetworkManager
        // has been initialized before
        // attempting to start the race.

        if (NetworkManager.Instance != null)
        {
            Debug.Log(
                "TrackSelectionManager: " +
                "NetworkManager found. " +
                "Starting race..."
            );

            NetworkManager.Instance.StartGame();
        }
        else
        {
            Debug.LogError(
                "TrackSelectionManager: " +
                "NetworkManager.Instance is NULL! " +
                "The NetworkManager has not been " +
                "initialized. Cannot start the race. " +
                "Check whether the NetworkManager " +
                "exists in the current scene or " +
                "is initialized through Bootstrap."
            );
        }
    }

    // ==================================
    // SAVED VALUES
    // ==================================

    public static int GetSavedLapCount()
    {
        return PlayerPrefs.GetInt(
            SavedLapCountKey,
            DefaultLapCount
        );
    }

    public static int GetSavedAICount()
    {
        return PlayerPrefs.GetInt(
            SavedAICountKey,
            DefaultAICount
        );
    }
}
