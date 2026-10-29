//based on my research as of 10/24/2026, there is no way to make a global
//leaderboard since we don't have access to the actual steamworks account that
//lets us define and create leaderboards... the best we can do for now is just
//make and save best times for local players through GONet. However, I tried
//my best to comment out somewhat of a starting point for when we or a future
//team do have access to the steamworks account. Additionally, in case the
//Fall 2026 team isn't the one to work on the global leaderboard, be aware that,
//since players are allowed to set the lap amounts, we have to also keep track
//of the lap counts associated with lap time... if not, then players who've done
//one lap races will always and only be displayed on the leaderboard I was
//thinking of maybe making the leaderboard only keep track of best times of
//racers who completed 50 laps (the max) since the original tradition was a long
//race (at least based on some quick and not so inciteful researching) multiple
//offical sources have varying lap counts or don't even acknoweledge lap
//counts but like overall total distance - LAA
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;

public class SteamScoresManager : MonoBehaviour//, IOrderedScript
{

    //Testing Stuff
    [ContextMenu("Set Scores Here")]
    private void SetScore()
    {
        SetPlayerBestScore(1, 90.00f);
    }
    public static SteamScoresManager Instance { get; private set; }
    /*(public int CallOrder => 0;

    private SteamLeaderboard_t raceScores;

    private CallResult<LeaderboardFindResult_t> scoresFind;
    private CallResult<LeaderboardScoreUploaded_t> scoresUploaded;
    private CallResult<LeaderboardScoresDownloaded_t> scoresDownloaded;

    public void OrderedAwake()
    {
        Instance = this;

        scoresFind = CallResult<LeaderboardFindResult_t>.Create(OnScoresFound);
        scoresUploaded = CallResult<LeaderboardScoreUploaded_t>.Create(OnScoresUploaded);
        scoresDownloaded = CallResult<LeaderboardScoresDownloaded_t>.Create(OnScoresDownloaded);
    }

    public void OrderedStart()
    {

    }

    public void OnDestroy()
    {
        scoresFind?.Dispose();
        scoresUploaded?.Dispose();
        scoresDownloaded?.Dispose();
    } */

    //everything below works alongside the RacerInfo, when a race is completed, it will then submit the time and check to see if its better
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("SteamScoresManager made");
    }

    public bool UploadPlayerBestScore (float raceTime, int lapCount)
    {
        string key = GetPlayerBestKey(lapCount);

        if (!PlayerPrefs.HasKey(key)) //first check to see if player even has a score
        {
            SavePlayerBestScore(key, raceTime);
            return true;
        }

        float CurrentPlayerBestScore = PlayerPrefs.GetFloat(key);

        if (raceTime < CurrentPlayerBestScore)
        {
            SavePlayerBestScore(key, raceTime);
            return true;
        }

        return false;
    }

    private void SavePlayerBestScore(string key, float raceTime)
    {
        PlayerPrefs.SetFloat(key, raceTime);
        PlayerPrefs.Save();

        Debug.Log($"New Best Score recorded: {raceTime}");
    }

    public float GetPlayerBestScore(int lapCount)
    {
        string key = GetPlayerBestKey(lapCount);

        if (!PlayerPrefs.HasKey(key))
            return -1f;
        return PlayerPrefs.GetFloat(key);
    }

    private string GetPlayerBestKey(int lapCount)
    {
        return $"Player_Track1_{lapCount}Laps";
    }

    public void SetPlayerBestScore(int lapCount, float raceTime)
    {
        string key = GetPlayerBestKey(lapCount);

        PlayerPrefs.SetFloat(key, raceTime);
        PlayerPrefs.Save();

        Debug.Log($"TESTING FUNCTION: Set score to : {raceTime} seconds - {lapCount} laps");
    }
}
