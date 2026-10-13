using UnityEngine;
using TMPro;
using UnityEngine.Rendering;
using System.Collections;
using Unity.VisualScripting;

public class RaceTimer : MonoBehaviour
{
    public TMP_Text timerText;
    public bool raceActive = false;
    public float raceTime = 0f; 
    public bool lapCompleted = false;
    public Coroutine CurrentLapTimeFlash;

    void Update()
    {
        if (!raceActive)
            return;
        raceTime += Time.deltaTime;

        if (!lapCompleted)
            UpdateTimerDisplay();

       // Debug.Log($"RaceTimer: I started now at {Time.time}");
        //UpdateTimerDisplay();
    }

    void UpdateTimerDisplay()
    {
        if (timerText == null)
            return;

        int minutes = Mathf.FloorToInt(raceTime / 60);
        int seconds = Mathf.FloorToInt(raceTime % 60);
        int milliseconds = Mathf.FloorToInt((raceTime * 100) % 100);

        timerText.text = $"{minutes:00}:{seconds:00}:{milliseconds:00}";
    }

    public void StartRace() => raceActive = true;
    public void StopRace() => raceActive = false;

    public void TimerFlash (float CurrentLapTime)
    {
        if (CurrentLapTimeFlash != null)
            StopCoroutine(CurrentLapTimeFlash);
        CurrentLapTimeFlash = StartCoroutine(TimerFlashCoroutine(CurrentLapTime));
    }

    private IEnumerator TimerFlashCoroutine(float CurrentLapTime)
    {
        if (timerText == null)
            yield break;
        lapCompleted = true;

        int minutes = Mathf.FloorToInt(CurrentLapTime / 60);
        int seconds = Mathf.FloorToInt(CurrentLapTime % 60);
        int milliseconds = Mathf.FloorToInt((CurrentLapTime * 100) % 100);

        timerText.text = $"{minutes:00}:{seconds:00}:{milliseconds:00}";

        for (int i = 0; i < 6; i++)
        {
            timerText.enabled = true;
            yield return new WaitForSeconds(0.25f);
            timerText.enabled = false;
            yield return new WaitForSeconds(0.25f);
        }

        timerText.enabled = true;
        lapCompleted = false;
        UpdateTimerDisplay();
        CurrentLapTimeFlash = null;
    }
}
