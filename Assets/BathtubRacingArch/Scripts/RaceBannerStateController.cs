using System;
using System.Reflection;
using UnityEngine;

public class RaceBannerStateController : MonoBehaviour
{
    public Renderer[] bannerRenderers;
    public Material preRaceMaterial;
    public Material racingMaterial;
    public Material finalLapMaterial;
    public Material finishMaterial;

    [Header("Read-only diagnostics")]
    [SerializeField] private string currentBannerState = "GET READY";
    [SerializeField] private string countdownTextDebug = "";
    [SerializeField] private bool countdownHasBeenSeen = false;
    [SerializeField] private bool raceHasLatched = false;

    private MonoBehaviour raceManager;
    private Component countdownTextComponent;
    private Component lapTextComponent;
    private Material currentMaterial;
    private float nextRefresh;
    private bool raceHasBeenActive;
    private bool finishLatched;
    private float raceLatchedAt;

    private void Awake()
    {
        // Force the first visible state before any race-manager polling can happen.
        Apply(preRaceMaterial, "GET READY");
    }

    private void Start()
    {
        Apply(preRaceMaterial, "GET READY");
        CacheReferences();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.05f;

        if (raceManager == null || countdownTextComponent == null || lapTextComponent == null)
            CacheReferences();

        UpdateBannerState();
    }

    private void CacheReferences()
    {
        // Bind specifically to the component that owns RaceActive. This avoids grabbing
        // an unrelated component on the RaceManager GameObject.
        GameObject rm = GameObject.Find("RaceManager");
        if (rm != null)
        {
            foreach (MonoBehaviour mb in rm.GetComponents<MonoBehaviour>())
            {
                if (mb == null) continue;
                Type t = mb.GetType();
                if (FindBoolMember(t, "RaceActive") != null)
                {
                    raceManager = mb;
                    break;
                }
            }
        }

        // Find the TextMeshPro component that belongs to the existing "Countdown Text" object.
        if (countdownTextComponent == null)
        {
            MonoBehaviour[] all = FindObjectsOfType<MonoBehaviour>(true);
            foreach (MonoBehaviour mb in all)
            {
                if (mb == null || mb.gameObject == null) continue;
                if (!string.Equals(mb.gameObject.name, "Countdown Text", StringComparison.OrdinalIgnoreCase))
                    continue;

                PropertyInfo p = mb.GetType().GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
                if (p != null && p.PropertyType == typeof(string))
                {
                    countdownTextComponent = mb;
                    break;
                }
            }
        }

        // Find the existing HUD text that reads "Lap: X/Y".
        if (lapTextComponent == null)
        {
            MonoBehaviour[] all = FindObjectsOfType<MonoBehaviour>(true);
            foreach (MonoBehaviour mb in all)
            {
                if (mb == null) continue;
                PropertyInfo p = mb.GetType().GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
                if (p == null || p.PropertyType != typeof(string)) continue;

                try
                {
                    string s = p.GetValue(mb) as string;
                    if (!string.IsNullOrEmpty(s) && s.TrimStart().StartsWith("Lap:", StringComparison.OrdinalIgnoreCase))
                    {
                        lapTextComponent = mb;
                        break;
                    }
                }
                catch { }
            }
        }
    }

    private void UpdateBannerState()
    {
        string countdown = ReadText(countdownTextComponent);
        countdownTextDebug = countdown ?? "";
        string normalizedCountdown = Normalize(countdown);

        // -------------------------
        // PRE-RACE / COUNTDOWN
        // -------------------------
        // Do NOT use RaceActive to leave GET READY. In this project RaceActive can become
        // true before the visible countdown has finished. Instead, use the actual existing
        // Countdown Text object so the banner matches what the player sees.
        if (!raceHasLatched)
        {
            if (IsGoText(normalizedCountdown))
            {
                countdownHasBeenSeen = true;
                LatchRace();
            }
            else if (!string.IsNullOrEmpty(normalizedCountdown))
            {
                // 3 / 2 / 1 (or any other countdown message) keeps GET READY visible.
                countdownHasBeenSeen = true;
                Apply(preRaceMaterial, "GET READY");
                return;
            }
            else if (countdownHasBeenSeen)
            {
                // The countdown text was visible and has now cleared/hidden: race has begun.
                LatchRace();
            }
            else
            {
                // Countdown has not begun yet.
                Apply(preRaceMaterial, "GET READY");
                return;
            }
        }

        // -------------------------
        // ACTIVE RACE
        // -------------------------
        bool active = ReadBool("RaceActive", true);

        int currentLap;
        int totalLaps;
        ReadLapText(out currentLap, out totalLaps);

        if (active)
            raceHasBeenActive = true;

        // A lap display beyond the configured total is a strong finish signal.
        if (totalLaps > 0 && currentLap > totalLaps)
            finishLatched = true;

        // Once the banner has genuinely entered RACE!, RaceActive going false later is
        // treated as race completion. The short delay prevents a same-frame transition.
        if (raceHasLatched && raceHasBeenActive && !active && Time.unscaledTime - raceLatchedAt > 0.75f)
            finishLatched = true;

        if (finishLatched)
        {
            Apply(finishMaterial, "FINISHED");
            return;
        }

        if (totalLaps > 0 && currentLap >= totalLaps)
        {
            Apply(finalLapMaterial, "FINAL LAP");
            return;
        }

        Apply(racingMaterial, "RACE!");
    }

    private void LatchRace()
    {
        raceHasLatched = true;
        raceHasBeenActive = true;
        raceLatchedAt = Time.unscaledTime;
        Apply(racingMaterial, "RACE!");
    }

    private string ReadText(Component component)
    {
        if (component == null) return "";
        try
        {
            PropertyInfo p = component.GetType().GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
            if (p != null && p.PropertyType == typeof(string))
                return p.GetValue(component) as string ?? "";
        }
        catch { }
        return "";
    }

    private string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        return value.Trim().Replace(" ", "").Replace("\n", "").Replace("\r", "").ToUpperInvariant();
    }

    private bool IsGoText(string normalized)
    {
        if (string.IsNullOrEmpty(normalized)) return false;
        return normalized == "GO" || normalized == "GO!" || normalized.Contains("GO!");
    }

    private bool ReadBool(string name, bool fallback)
    {
        if (raceManager == null) return fallback;
        Type t = raceManager.GetType();
        BindingFlags f = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        FieldInfo field = t.GetField(name, f);
        if (field != null && field.FieldType == typeof(bool))
        {
            try { return (bool)field.GetValue(raceManager); } catch { }
        }

        PropertyInfo prop = t.GetProperty(name, f);
        if (prop != null && prop.PropertyType == typeof(bool) && prop.CanRead)
        {
            try { return (bool)prop.GetValue(raceManager); } catch { }
        }
        return fallback;
    }

    private MemberInfo FindBoolMember(Type t, string name)
    {
        BindingFlags f = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        FieldInfo field = t.GetField(name, f);
        if (field != null && field.FieldType == typeof(bool)) return field;
        PropertyInfo prop = t.GetProperty(name, f);
        if (prop != null && prop.PropertyType == typeof(bool)) return prop;
        return null;
    }

    private void ReadLapText(out int current, out int total)
    {
        current = 0;
        total = 0;
        string s = ReadText(lapTextComponent);
        if (string.IsNullOrEmpty(s)) return;

        try
        {
            int colon = s.IndexOf(':');
            int slash = s.IndexOf('/');
            if (colon < 0 || slash < 0 || slash <= colon) return;

            int.TryParse(s.Substring(colon + 1, slash - colon - 1).Trim(), out current);
            int end = slash + 1;
            while (end < s.Length && char.IsWhiteSpace(s[end])) end++;
            int start = end;
            while (end < s.Length && char.IsDigit(s[end])) end++;
            if (end > start)
                int.TryParse(s.Substring(start, end - start), out total);
        }
        catch { }
    }

    private void Apply(Material material, string state)
    {
        currentBannerState = state;
        if (material == null || currentMaterial == material) return;

        currentMaterial = material;
        if (bannerRenderers == null) return;

        foreach (Renderer r in bannerRenderers)
        {
            if (r != null) r.sharedMaterial = material;
        }
    }

    // Optional manual test methods.
    public void ShowGetReady()
    {
        finishLatched = false;
        raceHasBeenActive = false;
        raceHasLatched = false;
        countdownHasBeenSeen = false;
        Apply(preRaceMaterial, "GET READY");
    }

    public void ShowRacing() { LatchRace(); }
    public void ShowFinalLap() { raceHasLatched = true; raceHasBeenActive = true; Apply(finalLapMaterial, "FINAL LAP"); }
    public void ShowFinished() { finishLatched = true; Apply(finishMaterial, "FINISHED"); }
}
