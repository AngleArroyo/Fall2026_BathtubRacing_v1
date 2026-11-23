
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class SelectCustomizations : MonoBehaviour
{
    public KartModel KartModel;

    [Header("Customization Dropdowns")]
    public TMP_Dropdown rollCageInput;
    public TMP_Dropdown wheelInput;
    public TMP_Dropdown extraDetailInput;
    public TMP_Dropdown decalInput;

    private const string ExtraDetailSaveKey = "KartExtraDetailIndex";

    private bool initialized;

    private void Awake()
    {
        initialized = CheckReferences();

        if (initialized)
        {
            RepairSceneReferences();
            SetDropdowns();
        }
    }

    private void OnEnable()
    {
        if (initialized)
        {
            RepairSceneReferences();
            LoadData();
        }
    }

    private void OnDisable()
    {
        if (initialized)
        {
            SaveData();
        }
    }

    private bool CheckReferences()
    {
        if (KartModel == null ||
            rollCageInput == null ||
            wheelInput == null ||
            extraDetailInput == null ||
            decalInput == null)
        {
            Debug.LogError(
                "SelectCustomizations: Assign KartModel and all four dropdowns.",
                this
            );

            return false;
        }

        if (!KartModel.gameObject.scene.IsValid())
        {
            Debug.LogError(
                "KartModel references a prefab asset. Assign the scene KartModel.",
                this
            );

            return false;
        }

        return true;
    }

    private void RepairSceneReferences()
    {
        RepairObjectList(
            KartModel.RollCageObjs,
            new string[]
            {
                "Roll Cages/RollCage_Round",
                "Roll Cages/RollCage_Box",
                "Roll Cages/RollCage_Slim",
                "Roll Cages/MetalBoxCage"
            }
        );

        RepairObjectList(
            KartModel.ExtraDetailObjs,
            new string[]
            {
                "Extra Details/ExtraDetail_None",
                "Extra Details/ExtraDetail_FrontWing",
                "Extra Details/BigSpoiler",
                "Extra Details/LilSpoiler"
            }
        );
    }

    private void RepairObjectList(
        List<GameObject> objects,
        string[] paths)
    {
        if (objects == null)
        {
            Debug.LogWarning(
                "Customization object list is missing.",
                this
            );
            return;
        }

        for (int i = 0; i < paths.Length; i++)
        {
            while (objects.Count <= i)
            {
                objects.Add(null);
            }

            Transform found = KartModel.transform.Find(paths[i]);

            if (found == null ||
                !found.gameObject.scene.IsValid())
            {
                Debug.LogWarning(
                    "Could not find scene object: " +
                    paths[i],
                    this
                );
                continue;
            }

            if (objects[i] != found.gameObject)
            {
                objects[i] = found.gameObject;

                Debug.Log(
                    "Connected scene object: " +
                    found.name,
                    this
                );
            }
        }
    }

    private void SetDropdowns()
    {
        SetRollCageDropdown();
        SetWheelDropdown();
        SetExtraDetailDropdown();

        SetDropdown(
            decalInput,
            typeof(DecalType),
            KartModel.SetDecal
        );
    }

    private void SetRollCageDropdown()
    {
        if (rollCageInput == null || KartModel == null)
        {
            Debug.LogError(
                "Roll cage dropdown or KartModel is missing!",
                this
            );
            return;
        }

        rollCageInput.ClearOptions();

        List<string> rollCageOptions = new List<string>
        {
            "Round",
            "Box",
            "Slim",
            "Metal Box"
        };

        rollCageInput.AddOptions(rollCageOptions);

        rollCageInput.onValueChanged.RemoveAllListeners();

        rollCageInput.onValueChanged.AddListener(index =>
        {
            Debug.Log(
                "Roll cage dropdown changed to: " + index,
                this
            );

            RepairSceneReferences();

            KartModel.SetRollCage(index);

            if (KartModel.RollCageObjs != null &&
                index >= 0 &&
                index < KartModel.RollCageObjs.Count)
            {
                GameObject selectedCage =
                    KartModel.RollCageObjs[index];

                if (selectedCage != null)
                {
                    Debug.Log(
                        "Roll cage: " + selectedCage.name +
                        " | Active: " +
                        selectedCage.activeInHierarchy +
                        " | Scene: " +
                        selectedCage.scene.name,
                        this
                    );

                    Renderer[] renderers =
                        selectedCage.GetComponentsInChildren<Renderer>(true);

                    Debug.Log(
                        "Roll cage renderers found: " +
                        renderers.Length,
                        this
                    );

                    foreach (Renderer rend in renderers)
                    {
                        Debug.Log(
                            "Renderer: " + rend.name +
                            " | Enabled: " + rend.enabled +
                            " | Active: " +
                            rend.gameObject.activeInHierarchy,
                            this
                        );
                    }

                    if (!selectedCage.activeInHierarchy)
                    {
                        Debug.LogWarning(
                            "Selected roll cage is not active in the Hierarchy!",
                            this
                        );
                    }

                    if (renderers.Length == 0)
                    {
                        Debug.LogWarning(
                            "No renderers found on selected roll cage!",
                            this
                        );
                    }
                }
            }

            DebugCustomizationReferences();
        });

        rollCageInput.RefreshShownValue();

        Debug.Log(
            "Roll cage dropdown initialized!",
            this
        );
    }

    private void SetWheelDropdown()
    {
        wheelInput.ClearOptions();

        List<string> wheelOptions = new List<string>
        {
            "Small",
            "Large",
            "Combo",
            "Tire Wheel"
        };

        wheelInput.AddOptions(wheelOptions);

        wheelInput.onValueChanged.RemoveAllListeners();

        wheelInput.onValueChanged.AddListener(index =>
        {
            KartModel.SetWheels(index);

            Debug.Log(
                "Wheel selected: " + index,
                this
            );
        });

        wheelInput.RefreshShownValue();
    }

    private void SetExtraDetailDropdown()
    {
        extraDetailInput.ClearOptions();

        List<string> extraDetailOptions = new List<string>
        {
            "None",
            "Front Wing",
            "Big Spoiler",
            "Lil Spoiler"
        };

        extraDetailInput.AddOptions(extraDetailOptions);

        extraDetailInput.onValueChanged.RemoveAllListeners();

        extraDetailInput.onValueChanged.AddListener(index =>
        {
            RepairSceneReferences();

            KartModel.SetExtraDetail(index);

            PlayerPrefs.SetInt(
                ExtraDetailSaveKey,
                index
            );

            PlayerPrefs.Save();

            Debug.Log(
                "Extra detail selected: " + index,
                this
            );
        });

        extraDetailInput.RefreshShownValue();
    }

    public void SetDropdown(
        TMP_Dropdown dropdown,
        Type enumType,
        UnityAction<int> onValueChanged)
    {
        if (dropdown == null)
            return;

        dropdown.ClearOptions();

        var optionData =
            new List<TMP_Dropdown.OptionData>();

        foreach (var type in Enum.GetValues(enumType))
        {
            optionData.Add(
                new TMP_Dropdown.OptionData(
                    type.ToString().SpaceUppercases()
                )
            );
        }

        dropdown.AddOptions(optionData);

        dropdown.onValueChanged.RemoveAllListeners();

        dropdown.onValueChanged.AddListener(
            onValueChanged
        );

        dropdown.RefreshShownValue();
    }

    private void LoadDropdowns(CustomKartData data)
    {
        rollCageInput.SetValueWithoutNotify(
            Mathf.Clamp(
                (int)data.RollCage,
                0,
                rollCageInput.options.Count - 1
            )
        );

        int savedWheel =
            KartModel.GetSavedWheelIndex(
                (int)data.Wheel
            );

        wheelInput.SetValueWithoutNotify(
            Mathf.Clamp(
                savedWheel,
                0,
                wheelInput.options.Count - 1
            )
        );

        int savedExtraDetail = PlayerPrefs.GetInt(
            ExtraDetailSaveKey,
            (int)data.ExtraDetail
        );

        extraDetailInput.SetValueWithoutNotify(
            Mathf.Clamp(
                savedExtraDetail,
                0,
                extraDetailInput.options.Count - 1
            )
        );

        decalInput.SetValueWithoutNotify(
            Mathf.Clamp(
                (int)data.Decal,
                0,
                decalInput.options.Count - 1
            )
        );

        rollCageInput.RefreshShownValue();
        wheelInput.RefreshShownValue();
        extraDetailInput.RefreshShownValue();
        decalInput.RefreshShownValue();
    }

    public void SelectMainColor(string colorStr)
    {
        Color col = ParseInputColor(colorStr);
        KartModel.SetMainColor(col);
    }

    public void SelectTrimColor(string colorStr)
    {
        Color col = ParseInputColor(colorStr);
        KartModel.SetTrimColor(col);
    }

    public void SelectDecalColor(string colorStr)
    {
        Color col = ParseInputColor(colorStr);
        KartModel.SetDecalColor(col);
    }

    public void LoadData()
    {
        if (!CheckReferences())
            return;

        RepairSceneReferences();

        CustomKartData loadedData =
            KartSaveManager.LoadKartData();

        if (loadedData == null)
        {
            Debug.LogWarning(
                "No saved kart data found. Using defaults."
            );

            loadedData = new CustomKartData();
        }

        KartModel.ApplyKartData(loadedData);

        if (KartModel.ExtraDetailObjs != null &&
            KartModel.ExtraDetailObjs.Count > 0)
        {
            int savedExtraDetail = PlayerPrefs.GetInt(
                ExtraDetailSaveKey,
                (int)loadedData.ExtraDetail
            );

            savedExtraDetail = Mathf.Clamp(
                savedExtraDetail,
                0,
                KartModel.ExtraDetailObjs.Count - 1
            );

            KartModel.SetExtraDetail(savedExtraDetail);

            Debug.Log(
                "Restored extra detail: " +
                savedExtraDetail,
                this
            );
        }

        LoadDropdowns(loadedData);

        DebugCustomizationReferences();

        Debug.Log(
            "Kart customization loaded!",
            this
        );
    }

    public void SaveData()
    {
        if (KartModel == null ||
            KartModel.KartData == null)
            return;

        int selectedExtraDetail =
            (int)KartModel.KartData.ExtraDetail;

        PlayerPrefs.SetInt(
            ExtraDetailSaveKey,
            selectedExtraDetail
        );

        KartModel.PrepareKartDataForSave();

        try
        {
            if (!Enum.IsDefined(
                typeof(ExtraDetailType),
                KartModel.KartData.ExtraDetail))
            {
                KartModel.KartData.ExtraDetail =
                    (ExtraDetailType)0;
            }

            KartSaveManager.SaveKartData(
                KartModel.KartData
            );
        }
        finally
        {
            KartModel.KartData.ExtraDetail =
                (ExtraDetailType)selectedExtraDetail;

            PlayerPrefs.Save();
        }

        Debug.Log(
            "Kart customization saved!",
            this
        );
    }

    public void DebugCustomizationReferences()
    {
        if (KartModel == null)
        {
            Debug.LogError(
                "KartModel is missing!",
                this
            );

            return;
        }

        Debug.Log(
            "Controlling KartModel: " +
            KartModel.name +
            " | Scene: " +
            KartModel.gameObject.scene.name,
            this
        );

        DebugObjectList(
            "Roll Cage",
            KartModel.RollCageObjs
        );

        DebugObjectList(
            "Extra Detail",
            KartModel.ExtraDetailObjs
        );
    }

    private void DebugObjectList(
        string category,
        List<GameObject> objects)
    {
        if (objects == null)
        {
            Debug.LogWarning(
                category + " list is missing.",
                this
            );

            return;
        }

        for (int i = 0; i < objects.Count; i++)
        {
            GameObject obj = objects[i];

            if (obj == null)
            {
                Debug.LogError(
                    category + " " + i +
                    " is missing!",
                    this
                );

                continue;
            }

            Renderer[] renderers =
                obj.GetComponentsInChildren<Renderer>(true);

            Debug.Log(
                category + " " + i +
                " | " + obj.name +
                " | Scene Object: " +
                obj.scene.IsValid() +
                " | Active Self: " +
                obj.activeSelf +
                " | Active In Hierarchy: " +
                obj.activeInHierarchy +
                " | Renderers: " +
                renderers.Length,
                this
            );
        }
    }

    public static Color ParseInputColor(string colorStr)
    {
        ColorUtility.TryParseHtmlString(
            "#" + colorStr,
            out Color newColor
        );

        return newColor;
    }
}
