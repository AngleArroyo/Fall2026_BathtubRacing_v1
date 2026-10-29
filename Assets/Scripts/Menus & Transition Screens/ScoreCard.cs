using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;


public class ScoreCard : MonoBehaviour
{
    public RawImage ProfilePicture;
    public TMP_Text ScoreRank;
    public TMP_Text PlayerName;
    public TMP_Text PlayerScore;
    public TMP_Text LapCount;

    public void Layout(int rank, string name, float score, int laps, Sprite picture = null)
    {
        ScoreRank.text = $"#{rank}";
        PlayerName.text = name;
        LapCount.text = $"{laps} Laps";

        int minutes = Mathf.FloorToInt(score / 60);
        int seconds = Mathf.FloorToInt(score % 60);
        int milliseconds = Mathf.FloorToInt((score * 100) % 100);

        PlayerScore.text = $"{minutes:00}:{seconds:00}:{milliseconds:00}";

        if (picture != null)
            ProfilePicture.texture = picture.texture;
    }
}
