#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class BathtubTrussArchBuilder
{
    private const string RootName = "SPSU_START_FINISH_TRUSS";
    private const string OldRootName = "SPSU_START_FINISH_GENERATED";
    private const string Base = "Assets/BathtubRacingArch";
    private const string Materials = Base + "/Materials";
    private const string Textures = Base + "/Textures";

    [MenuItem("Tools/Bathtub Racing/Install Metal Truss Arch + Dynamic Banner V3")]
    public static void Install()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            EditorUtility.DisplayDialog("Bathtub Racing", "Open Racetrack.unity first.", "OK");
            return;
        }

        Transform finish = FindSceneTransform("FinishLine");
        if (finish == null)
        {
            EditorUtility.DisplayDialog("Bathtub Racing", "Could not find FinishLine in the active scene.", "OK");
            return;
        }

        EnsureFolder("Assets", "BathtubRacingArch");
        EnsureFolder(Base, "Materials");
        ConfigureTextures();

        Material metal = MakeColorMaterial("M_TrussMetal", new Color(0.80f,0.84f,0.86f,1f), 0.55f, 0.70f);
        Material dark = MakeColorMaterial("M_TrussDark", new Color(0.08f,0.09f,0.10f,1f), 0.05f, 0.25f);
        Material concrete = MakeColorMaterial("M_TrussConcrete", new Color(0.42f,0.44f,0.45f,1f), 0.0f, 0.15f);
        Material white = MakeColorMaterial("M_WhitePaint", new Color(0.93f,0.94f,0.94f,1f), 0.0f, 0.20f);

        Material pre = MakeBannerMaterial("M_Banner_PreRace", "banner_prerace.png");
        Material racing = MakeBannerMaterial("M_Banner_Racing", "banner_racing.png");
        Material finalLap = MakeBannerMaterial("M_Banner_FinalLap", "banner_final_lap.png");
        Material finishMat = MakeBannerMaterial("M_Banner_Finish", "banner_finish.png");
        Material checker = MakeBannerMaterial("M_CheckerGround", "checker.png");

        Transform source = FindChildIgnoreCase(finish, "checkered line");
        float yaw = source != null ? NormalizeSignedAngle(source.localEulerAngles.y) : -6.7f;
        float roadY = source != null ? source.localPosition.y : -1.20f;
        float centerX = source != null ? source.localPosition.x : 0.5f;
        float centerZ = source != null ? source.localPosition.z : 0f;
        float roadWidth = source != null ? Mathf.Max(9.5f, Mathf.Abs(source.localScale.x)) : 9.5f;

        DestroyChildIfExists(finish, RootName);
        DestroyChildIfExists(finish, OldRootName);
        DisableOldFinishVisuals(finish);

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Install SPSU metal truss arch");
        root.transform.SetParent(finish, false);
        root.transform.localPosition = new Vector3(centerX, 0f, centerZ);
        root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        root.transform.localScale = Vector3.one;

        BuildVisibleFinishLine(root.transform, roadY, roadWidth, checker, white);
        Renderer[] banners = BuildTrussArch(root.transform, roadY, roadWidth, metal, dark, concrete, pre);

        RaceBannerStateController controller = root.AddComponent<RaceBannerStateController>();
        controller.bannerRenderers = banners;
        controller.preRaceMaterial = pre;
        controller.racingMaterial = racing;
        controller.finalLapMaterial = finalLap;
        controller.finishMaterial = finishMat;

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = root;
        EditorUtility.DisplayDialog(
            "Metal Truss Arch + Dynamic Banner V2 Installed",
            "The same silver wired/truss arch was installed with the new dynamic banners.\n\nGET READY stays visible before/during the countdown. RACE! appears when RaceActive turns on. FINAL LAP and FINISHED follow automatically.\n\nYour existing Lap Collider was not changed.\n\nPress Ctrl+S now, then test from Bootstrap.",
            "OK");
    }

    [MenuItem("Tools/Bathtub Racing/Remove Final Metal Truss Arch")]
    public static void Remove()
    {
        Transform finish = FindSceneTransform("FinishLine");
        if (finish == null) return;
        DestroyChildIfExists(finish, RootName);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Bathtub Racing", "Final metal truss arch removed. Lap Collider was not changed.", "OK");
    }

    private static Renderer[] BuildTrussArch(Transform parent, float roadY, float roadWidth, Material metal, Material dark, Material concrete, Material bannerMat)
    {
        Transform arch = NewContainer("MetalTrussBannerArch", parent);
        float opening = roadWidth + 2.6f;
        float towerX = opening * 0.5f;
        float bottomY = roadY + 0.05f;
        float lowerBeamY = roadY + 5.45f;
        float upperBeamY = roadY + 6.35f;
        float towerHalfWidth = 0.38f;
        float tube = 0.11f;
        float depth = 0.36f;

        // Concrete feet
        foreach (float sx in new float[]{-1f,1f})
        {
            CreateCube("Base_Outer_" + (sx < 0 ? "L":"R"), arch,
                new Vector3(sx*(towerX+towerHalfWidth), bottomY+0.18f, 0f), new Vector3(0.65f,0.36f,0.75f), concrete);
            CreateCube("Base_Inner_" + (sx < 0 ? "L":"R"), arch,
                new Vector3(sx*(towerX-towerHalfWidth), bottomY+0.18f, 0f), new Vector3(0.65f,0.36f,0.75f), concrete);
        }

        // Four vertical pipes per side (front/back depth included)
        foreach (float sx in new float[]{-1f,1f})
        foreach (float xoff in new float[]{-towerHalfWidth,towerHalfWidth})
        foreach (float z in new float[]{-depth,depth})
        {
            Vector3 a = new Vector3(sx*towerX + xoff, bottomY+0.34f, z);
            Vector3 b = new Vector3(sx*towerX + xoff, upperBeamY, z);
            CreatePipe("TowerPipe", arch, a, b, tube, metal);
        }

        // Side tower X bracing on front and back faces
        int sideSegments = 4;
        for (int s=0;s<2;s++)
        {
            float sx = s==0 ? -1f : 1f;
            float xl = sx*towerX - towerHalfWidth;
            float xr = sx*towerX + towerHalfWidth;
            for (int i=0;i<sideSegments;i++)
            {
                float y0 = Mathf.Lerp(bottomY+0.45f, upperBeamY-0.2f, i/(float)sideSegments);
                float y1 = Mathf.Lerp(bottomY+0.45f, upperBeamY-0.2f, (i+1)/(float)sideSegments);
                foreach(float z in new float[]{-depth,depth})
                {
                    CreatePipe("TowerDiag", arch, new Vector3(xl,y0,z), new Vector3(xr,y1,z), tube*0.72f, metal);
                    CreatePipe("TowerDiag", arch, new Vector3(xr,y0,z), new Vector3(xl,y1,z), tube*0.72f, metal);
                }
            }
        }

        // Top truss main rails on front/back faces
        foreach(float z in new float[]{-depth,depth})
        {
            CreatePipe("TopLowerRail", arch, new Vector3(-towerX-towerHalfWidth,lowerBeamY,z), new Vector3(towerX+towerHalfWidth,lowerBeamY,z), tube, metal);
            CreatePipe("TopUpperRail", arch, new Vector3(-towerX-towerHalfWidth,upperBeamY,z), new Vector3(towerX+towerHalfWidth,upperBeamY,z), tube, metal);
        }

        // Cross-depth connectors
        int connectors = 9;
        for(int i=0;i<=connectors;i++)
        {
            float x = Mathf.Lerp(-towerX-towerHalfWidth,towerX+towerHalfWidth,i/(float)connectors);
            CreatePipe("DepthConnector", arch, new Vector3(x,lowerBeamY,-depth), new Vector3(x,lowerBeamY,depth), tube*0.85f, metal);
            CreatePipe("DepthConnector", arch, new Vector3(x,upperBeamY,-depth), new Vector3(x,upperBeamY,depth), tube*0.85f, metal);
        }

        // Repeating triangular/X braces across top, matching the generated reference.
        int bays = 9;
        float x0 = -towerX-towerHalfWidth;
        float x1 = towerX+towerHalfWidth;
        for(int i=0;i<bays;i++)
        {
            float xa = Mathf.Lerp(x0,x1,i/(float)bays);
            float xb = Mathf.Lerp(x0,x1,(i+1)/(float)bays);
            foreach(float z in new float[]{-depth,depth})
            {
                CreatePipe("TopDiag", arch, new Vector3(xa,lowerBeamY,z), new Vector3(xb,upperBeamY,z), tube*0.72f, metal);
                CreatePipe("TopDiag", arch, new Vector3(xa,upperBeamY,z), new Vector3(xb,lowerBeamY,z), tube*0.72f, metal);
            }
        }

        // Banner support rail
        CreatePipe("BannerRail", arch, new Vector3(-towerX+0.15f,lowerBeamY-0.22f,-0.43f), new Vector3(towerX-0.15f,lowerBeamY-0.22f,-0.43f), tube*0.8f, dark);
        CreatePipe("BannerRailRear", arch, new Vector3(-towerX+0.15f,lowerBeamY-0.22f,0.43f), new Vector3(towerX-0.15f,lowerBeamY-0.22f,0.43f), tube*0.8f, dark);

        float bannerW = opening - 0.9f;
        float bannerH = 1.55f;
        Renderer front = CreateQuad("BannerFront", arch, new Vector3(0f,lowerBeamY-0.86f,-0.455f), Quaternion.identity, new Vector3(bannerW,bannerH,1f), bannerMat);
        Renderer rear = CreateQuad("BannerRear", arch, new Vector3(0f,lowerBeamY-0.86f,0.455f), Quaternion.Euler(0f,180f,0f), new Vector3(bannerW,bannerH,1f), bannerMat);

        // Visible grommet/tie points make the panel read like fabric instead of a solid sign.
        for(int i=0;i<9;i++)
        {
            float x = Mathf.Lerp(-bannerW*0.46f,bannerW*0.46f,i/8f);
            CreateSphere("BannerTie", arch, new Vector3(x,lowerBeamY-0.10f,-0.49f), 0.055f, dark);
            CreateSphere("BannerTie", arch, new Vector3(x,lowerBeamY-1.62f,-0.49f), 0.045f, dark);
        }

        return new Renderer[]{front,rear};
    }

    private static void BuildVisibleFinishLine(Transform parent, float roadY, float roadWidth, Material checker, Material white)
    {
        GameObject line = CreateCube("FinishLine_Checker_LongDistance", parent,
            new Vector3(0f, roadY + 0.075f, 0f), new Vector3(roadWidth,0.045f,2.2f), checker);
        PrepareRenderer(line.GetComponent<Renderer>());

        GameObject a = CreateCube("FinishLine_Edge_A", parent,
            new Vector3(0f, roadY + 0.078f, -1.18f), new Vector3(roadWidth,0.05f,0.10f), white);
        GameObject b = CreateCube("FinishLine_Edge_B", parent,
            new Vector3(0f, roadY + 0.078f, 1.18f), new Vector3(roadWidth,0.05f,0.10f), white);
        PrepareRenderer(a.GetComponent<Renderer>());
        PrepareRenderer(b.GetComponent<Renderer>());
    }

    private static Material MakeBannerMaterial(string name, string textureFile)
    {
        string path = Materials + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = FindUnlitShader();
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat,path);
        }
        else mat.shader = shader;
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/" + textureFile);
        mat.mainTexture = tex;
        mat.color = Color.white;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap",tex);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor",Color.white);
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull",0f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material MakeColorMaterial(string name, Color color, float metallic, float smoothness)
    {
        string path = Materials + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat,path);
        }
        else mat.shader = shader;
        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor",color);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic",metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness",smoothness);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Shader FindUnlitShader()
    {
        Shader s = Shader.Find("Universal Render Pipeline/Unlit");
        if (s == null) s = Shader.Find("Unlit/Texture");
        if (s == null) s = Shader.Find("Standard");
        return s;
    }

    private static void ConfigureTextures()
    {
        Directory.CreateDirectory(Materials);
        AssetDatabase.Refresh();
        foreach(string f in new[]{"banner_prerace.png","banner_racing.png","banner_final_lap.png","banner_finish.png","checker.png"})
        {
            string path = Textures + "/" + f;
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            ti.textureType = TextureImporterType.Default;
            ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
        }
    }

    private static Renderer CreateQuad(string name, Transform parent, Vector3 pos, Quaternion rot, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Undo.RegisterCreatedObjectUndo(go,"Create "+name);
        go.name = name;
        go.transform.SetParent(parent,false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        go.transform.localScale = scale;
        Collider c = go.GetComponent<Collider>(); if(c!=null) UnityEngine.Object.DestroyImmediate(c);
        Renderer r = go.GetComponent<Renderer>(); r.sharedMaterial = mat; PrepareRenderer(r);
        return r;
    }

    private static GameObject CreateCube(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(go,"Create "+name);
        go.name = name;
        go.transform.SetParent(parent,false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = scale;
        Collider c = go.GetComponent<Collider>(); if(c!=null) UnityEngine.Object.DestroyImmediate(c);
        Renderer r = go.GetComponent<Renderer>(); r.sharedMaterial = mat; PrepareRenderer(r);
        return go;
    }

    private static void CreateSphere(string name, Transform parent, Vector3 pos, float diameter, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Undo.RegisterCreatedObjectUndo(go,"Create "+name);
        go.name=name; go.transform.SetParent(parent,false); go.transform.localPosition=pos; go.transform.localScale=Vector3.one*diameter;
        Collider c=go.GetComponent<Collider>(); if(c!=null) UnityEngine.Object.DestroyImmediate(c);
        Renderer r=go.GetComponent<Renderer>(); r.sharedMaterial=mat; PrepareRenderer(r);
    }

    private static void CreatePipe(string name, Transform parent, Vector3 a, Vector3 b, float radius, Material mat)
    {
        Vector3 dir = b-a;
        float len = dir.magnitude;
        if(len < 0.001f) return;
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Undo.RegisterCreatedObjectUndo(go,"Create "+name);
        go.name=name;
        go.transform.SetParent(parent,false);
        go.transform.localPosition=(a+b)*0.5f;
        go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,dir.normalized);
        go.transform.localScale=new Vector3(radius,len*0.5f,radius);
        Collider c=go.GetComponent<Collider>(); if(c!=null) UnityEngine.Object.DestroyImmediate(c);
        Renderer r=go.GetComponent<Renderer>(); r.sharedMaterial=mat; PrepareRenderer(r);
    }

    private static void PrepareRenderer(Renderer r)
    {
        if(r==null) return;
        r.shadowCastingMode=ShadowCastingMode.On;
        r.receiveShadows=true;
        r.allowOcclusionWhenDynamic=false;
    }

    private static Transform NewContainer(string name, Transform parent)
    {
        GameObject go=new GameObject(name); Undo.RegisterCreatedObjectUndo(go,"Create "+name); go.transform.SetParent(parent,false); return go.transform;
    }

    private static void DisableOldFinishVisuals(Transform finish)
    {
        foreach(Transform t in finish.GetComponentsInChildren<Transform>(true))
        {
            if(t==finish) continue;
            string n=t.name.ToLowerInvariant();
            if(n=="checkered line" || n.Contains("checkered line decal"))
                t.gameObject.SetActive(false);
        }
    }

    private static void DestroyChildIfExists(Transform parent,string name)
    {
        Transform t=parent.Find(name); if(t!=null) Undo.DestroyObjectImmediate(t.gameObject);
    }

    private static Transform FindSceneTransform(string name)
    {
        Scene scene=SceneManager.GetActiveScene();
        foreach(GameObject root in scene.GetRootGameObjects())
        {
            Transform[] all=root.GetComponentsInChildren<Transform>(true);
            foreach(Transform t in all) if(string.Equals(t.name,name,StringComparison.OrdinalIgnoreCase)) return t;
        }
        return null;
    }

    private static Transform FindChildIgnoreCase(Transform parent,string name)
    {
        foreach(Transform t in parent.GetComponentsInChildren<Transform>(true))
            if(t!=parent && string.Equals(t.name,name,StringComparison.OrdinalIgnoreCase)) return t;
        return null;
    }

    private static float NormalizeSignedAngle(float a) { while(a>180f)a-=360f; while(a<-180f)a+=360f; return a; }

    private static void EnsureFolder(string parent,string child)
    {
        string path=parent+"/"+child; if(!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent,child);
    }
}
#endif
