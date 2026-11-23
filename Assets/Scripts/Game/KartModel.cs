
using System;
using System.Collections.Generic;
using UnityEngine;

public class KartModel : MonoBehaviour
{
    private const string WheelSaveKey = "SelectedWheelStyle";

    [Header("Roll Cage Customization")]
    public List<GameObject> RollCageObjs;

    [Header("Wheel Customization")]
    public List<GameObject> WheelObjs;

    [Header("Extra Details")]
    public List<GameObject> ExtraDetailObjs;

    [Header("Decal Customization")]
    public Transform DecalParent;

    [Tooltip("Should be ordered according to the enum.")]
    public List<Material> DecalMaterialChoices;

    [Header("Kart Materials")]
    public MeshRenderer BodyRenderer;
    public MeshRenderer TrimRenderer;

    [Header("Camera Visibility")]
    [Tooltip("Objects that obstruct the camera's view.")]
    public List<GameObject> ViewBlockingObjs;

    [NonSerialized]
    public CustomKartData KartData = new();

    public void ApplyKartData(CustomKartData data)
    {
        if (data == null)
        {
            Debug.LogWarning(
                "Kart data is null!", gameObject
            );
            return;
        }

        KartData = data;

        SetRollCage(KartData.RollCage);

        int savedWheel = PlayerPrefs.GetInt(
            WheelSaveKey,
            (int)KartData.Wheel
        );

        SetWheels(savedWheel);

        SetExtraDetail(KartData.ExtraDetail);

        SetDecal(KartData.Decal);

        SetMainColor(KartData.MainColor);
        SetTrimColor(KartData.TrimColor);
        SetDecalColor(KartData.DecalColor);

        Debug.Log(
            "Applying kart customization!",
            gameObject
        );
    }

    public void SetRollCage(int typeInt)
    {
        SetRollCage((RollCageType)typeInt);
    }

    public void SetRollCage(RollCageType type)
    {
        int index = (int)type;

        if (RollCageObjs == null ||
            index < 0 ||
            index >= RollCageObjs.Count)
        {
            Debug.LogWarning(
                "Invalid roll cage selection: " + index,
                gameObject
            );
            return;
        }

        GameObject selectedCage = RollCageObjs[index];

        if (selectedCage == null)
        {
            Debug.LogError(
                "Roll cage missing at index: " + index,
                gameObject
            );
            return;
        }

        if (!selectedCage.scene.IsValid())
        {
            Debug.LogError(
                selectedCage.name +
                " is a prefab asset. Assign the scene object from the Hierarchy.",
                gameObject
            );
            return;
        }

        for (int i = 0; i < RollCageObjs.Count; i++)
        {
            if (RollCageObjs[i] != null &&
                RollCageObjs[i].scene.IsValid())
            {
                RollCageObjs[i].SetActive(false);
            }
        }

        selectedCage.SetActive(true);

        Transform[] cageParts =
            selectedCage.GetComponentsInChildren<Transform>(true);

        foreach (Transform part in cageParts)
        {
            if (part != null)
            {
                part.gameObject.SetActive(true);
            }
        }

        Renderer[] cageRenderers =
            selectedCage.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer rend in cageRenderers)
        {
            if (rend != null)
            {
                rend.enabled = true;
            }
        }

        if (KartData == null)
        {
            KartData = new CustomKartData();
        }

        KartData.RollCage = type;

        Debug.Log(
            "Roll cage selected: " +
            index + " | " +
            selectedCage.name +
            " | Renderers: " +
            cageRenderers.Length,
            gameObject
        );

        if (!selectedCage.activeInHierarchy)
        {
            Debug.LogWarning(
                selectedCage.name +
                " has an inactive parent.",
                gameObject
            );
        }

        if (cageRenderers.Length == 0)
        {
            Debug.LogWarning(
                selectedCage.name +
                " has no renderers. Check its child meshes.",
                gameObject
            );
        }
    }

    public void SetWheels(int index)
    {
        if (WheelObjs == null ||
            index < 0 ||
            index >= WheelObjs.Count)
        {
            Debug.LogWarning(
                "Invalid wheel selection: " + index,
                gameObject
            );
            return;
        }

        if (WheelObjs[index] == null)
        {
            Debug.LogWarning(
                "Wheel object missing at index: " + index,
                gameObject
            );
            return;
        }

        for (int i = 0; i < WheelObjs.Count; i++)
        {
            if (WheelObjs[i] != null)
            {
                WheelObjs[i].SetActive(i == index);
            }
        }

        if (index == 3)
        {
            GameObject tireGroup = WheelObjs[3];

            tireGroup.SetActive(true);

            Transform[] tireParts =
                tireGroup.GetComponentsInChildren<Transform>(true);

            foreach (Transform tire in tireParts)
            {
                tire.gameObject.SetActive(true);
            }

            Renderer[] tireRenderers =
                tireGroup.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer tireRenderer in tireRenderers)
            {
                tireRenderer.enabled = true;
            }

            Debug.Log(
                "Tire Wheel activated! Renderers found: " +
                tireRenderers.Length
            );
        }

        if (index <= 2)
        {
            KartData.Wheel = (WheelType)index;
        }
        else
        {
            int originalWheel = (int)KartData.Wheel;

            if (originalWheel < 0 || originalWheel > 2)
            {
                KartData.Wheel = (WheelType)0;
            }
        }

        PlayerPrefs.SetInt(WheelSaveKey, index);
        PlayerPrefs.Save();

        Debug.Log(
            "Selected wheel index: " + index +
            " | Object: " + WheelObjs[index].name
        );
    }

    public void SetWheels(WheelType type)
    {
        SetWheels((int)type);
    }

    public int GetSavedWheelIndex(int defaultIndex = 0)
    {
        return PlayerPrefs.GetInt(
            WheelSaveKey,
            defaultIndex
        );
    }

    public void PrepareKartDataForSave()
    {
        if (KartData == null)
        {
            KartData = new CustomKartData();
        }

        int wheelIndex = (int)KartData.Wheel;

        if (wheelIndex < 0 || wheelIndex > 2)
        {
            KartData.Wheel = (WheelType)0;
        }
    }

    public void SetExtraDetail(int index)
    {
        if (ExtraDetailObjs == null ||
            index < 0 ||
            index >= ExtraDetailObjs.Count)
        {
            Debug.LogWarning(
                "Invalid extra detail selection: " + index,
                gameObject
            );
            return;
        }

        if (ExtraDetailObjs[index] == null)
        {
            Debug.LogWarning(
                "Extra detail object missing at index: " + index,
                gameObject
            );
            return;
        }

        for (int i = 0; i < ExtraDetailObjs.Count; i++)
        {
            if (ExtraDetailObjs[i] != null)
            {
                ExtraDetailObjs[i].SetActive(i == index);
            }
        }

        KartData.ExtraDetail = (ExtraDetailType)index;

        Debug.Log(
            "Selected extra detail index: " + index +
            " | Object: " + ExtraDetailObjs[index].name
        );
    }

    public void SetExtraDetail(ExtraDetailType type)
    {
        SetExtraDetail((int)type);
    }

    public void SetDecal(int typeInt)
    {
        SetDecal((DecalType)typeInt);
    }

    public void SetDecal(DecalType type)
    {
        int index = (int)type;

        if (DecalMaterialChoices == null ||
            index < 0 ||
            index >= DecalMaterialChoices.Count)
        {
            Debug.LogWarning(
                "Invalid decal selection: " + index,
                gameObject
            );
            return;
        }

        if (DecalParent == null)
        {
            Debug.LogWarning(
                "Decal Parent is not assigned!",
                gameObject
            );
            return;
        }

        var decalMat = DecalMaterialChoices[index];

        foreach (var renderer in
            DecalParent.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.material = decalMat;
            renderer.material.color = KartData.DecalColor;
        }

        KartData.Decal = type;
    }

    public void SetDecalColor(Color color)
    {
        if (DecalParent != null)
        {
            foreach (var renderer in
                DecalParent.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.material.color = color;
            }
        }

        KartData.DecalColor = color;
    }

    public void SetMainColor(Color color)
    {
        if (BodyRenderer != null)
        {
            BodyRenderer.material.color = color;
        }

        KartData.MainColor = color;
    }

    public void SetTrimColor(Color color)
    {
        if (TrimRenderer != null)
        {
            if (TrimRenderer.gameObject.scene.IsValid())
            {
                Material trimMaterial = TrimRenderer.material;

                if (trimMaterial != null)
                {
                    trimMaterial.color = color;
                }
            }
            else
            {
                Debug.LogWarning(
                    "Trim Renderer references a prefab asset " +
                    "instead of a scene object.",
                    gameObject
                );
            }
        }

        if (ExtraDetailObjs != null)
        {
            foreach (GameObject obj in ExtraDetailObjs)
            {
                if (obj == null)
                    continue;

                if (!obj.scene.IsValid())
                {
                    Debug.LogWarning(
                        "Extra detail references a prefab asset: " +
                        obj.name,
                        gameObject
                    );
                    continue;
                }

                MeshRenderer[] renderers =
                    obj.GetComponentsInChildren<MeshRenderer>(true);

                foreach (MeshRenderer rend in renderers)
                {
                    if (rend == null)
                        continue;

                    if (!rend.gameObject.scene.IsValid())
                        continue;

                    Material detailMaterial = rend.material;

                    if (detailMaterial != null)
                    {
                        detailMaterial.color = color;
                    }
                }
            }
        }

        KartData.TrimColor = color;
    }

    public void EnableViewBlockingObjs(bool enable)
    {
        if (ViewBlockingObjs == null)
            return;

        foreach (var obj in ViewBlockingObjs)
        {
            if (obj != null)
            {
                obj.SetActive(enable);
            }
        }
    }
}
