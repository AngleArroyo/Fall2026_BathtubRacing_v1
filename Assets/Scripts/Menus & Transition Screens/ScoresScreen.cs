using Steamworks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoresScreen : MonoBehaviour
{
    public Transform BodyBG;
    public GameObject PlayerScoreContainer;

    private void OnEnable()
    {
        ShowScores();
    }

    public void ShowScores()
    {
        ClearScores();
        CreatePlayerCard();
    }

    private void ClearScores()
    {
        foreach (Transform child in BodyBG)
        {
            Destroy(child.gameObject);
        }
    }

    private void CreatePlayerCard()
    {
        int lapCount = TrackSelectionManager.GetSavedLapCount();
        float BestScore = SteamScoresManager.Instance.GetPlayerBestScore(lapCount);

        if (BestScore < 0f)
            return;

        string PlayerName = SteamFriends.GetPersonaName();
        Sprite ProfilePicture = null;

        SteamManager.TryGetAvatar(SteamManager.LocalSteamId, out ProfilePicture);

        GameObject PlayerCard = Instantiate(PlayerScoreContainer, BodyBG);

        ScoreCard card = PlayerCard.GetComponent<ScoreCard>();

        card.Layout(1, PlayerName, BestScore, lapCount, ProfilePicture);
    }
}
