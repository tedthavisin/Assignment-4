using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Tools > Lake Dock > Build Scene
// Builds a simple sunset lake scene out of Unity primitives: a wooden dock,
// a reflective lake, trees, hills, a lantern and the Meta XR camera rig.
public static class LakeDockBuilder
{
    const string Folder = "Assets/LakeDock";
    const string ScenePath = Folder + "/LakeDock.unity";
    const float DeckHeight = 0.6f;
    const float DockLength = 16f;

    [MenuItem("Tools/Lake Dock/Build Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "LakeDock");
        if (!AssetDatabase.IsValidFolder(Folder + "/Materials")) AssetDatabase.CreateFolder(Folder, "Materials");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---------- materials
        Material water = Mat("Water", new Color(0.08f, 0.17f, 0.24f), 0.95f, 0.3f);
        Material wood = Mat("Wood", new Color(0.45f, 0.33f, 0.22f), 0.15f);
        Material darkWood = Mat("DarkWood", new Color(0.28f, 0.2f, 0.14f), 0.1f);
        Material grass = Mat("Grass", new Color(0.28f, 0.33f, 0.15f), 0.05f);
        Material hills = Mat("Hills", new Color(0.16f, 0.22f, 0.13f), 0.05f);
        Material mountains = Mat("Mountains", new Color(0.33f, 0.32f, 0.4f), 0.05f);
        Material trunk = Mat("Trunk", new Color(0.25f, 0.17f, 0.11f), 0.05f);
        Material[] leaves =
        {
            Mat("LeavesPine", new Color(0.1f, 0.2f, 0.1f), 0.05f),
            Mat("LeavesOrange", new Color(0.85f, 0.4f, 0.1f), 0.05f),
            Mat("LeavesRed", new Color(0.7f, 0.15f, 0.08f), 0.05f),
            Mat("LeavesGold", new Color(0.9f, 0.65f, 0.15f), 0.05f),
        };
        Material lanternGlow = Mat("LanternGlow", new Color(1f, 0.75f, 0.4f), 0.5f);
        lanternGlow.EnableKeyword("_EMISSION");
        lanternGlow.SetColor("_EmissionColor", new Color(1f, 0.6f, 0.25f) * 2f);
        lanternGlow.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;

        // ---------- sky, sun and fog
        var sky = new Material(Shader.Find("Skybox/Procedural"));
        sky.SetFloat("_SunSize", 0.05f);
        sky.SetFloat("_AtmosphereThickness", 2.1f);
        sky.SetColor("_SkyTint", new Color(0.4f, 0.62f, 0.55f));
        sky.SetColor("_GroundColor", new Color(0.35f, 0.28f, 0.27f));
        sky.SetFloat("_Exposure", 1.3f);
        AssetDatabase.DeleteAsset(Folder + "/Materials/SunsetSky.mat");
        AssetDatabase.CreateAsset(sky, Folder + "/Materials/SunsetSky.mat");
        RenderSettings.skybox = sky;

        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.66f, 0.42f);
        sun.intensity = 1.4f;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(6f, 180f, 0f); // low sun straight ahead of the dock
        RenderSettings.sun = sun;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 60f;
        RenderSettings.fogEndDistance = 900f;
        RenderSettings.fogColor = new Color(0.95f, 0.62f, 0.42f);

        // ---------- lake and shore
        Shape(PrimitiveType.Plane, "Lake", new Vector3(0f, 0f, 200f), new Vector3(200f, 1f, 200f), water);
        Shape(PrimitiveType.Sphere, "Shore", new Vector3(0f, -1.6f, -22f), new Vector3(90f, 4f, 44f), grass);

        // far hills in a half ring across the lake, mountains behind them
        for (int i = 0; i < 9; i++)
        {
            float angle = Mathf.Lerp(-80f, 80f, i / 8f);
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            Shape(PrimitiveType.Sphere, "Hill", new Vector3(0f, -6f, 120f) + dir * 280f,
                  new Vector3(160f, 40f + (i % 3) * 15f, 90f), hills).transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }
        float[] mountainAngles = { -70f, -45f, -25f, 30f, 50f, 75f };
        foreach (float angle in mountainAngles)
        {
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            Shape(PrimitiveType.Sphere, "Mountain", new Vector3(0f, -20f, 120f) + dir * 700f,
                  new Vector3(450f, 240f, 300f), mountains);
        }

        // ---------- trees
        Random.InitState(7);
        var forest = new GameObject("Trees").transform;
        for (int i = 0; i < 140; i++)
        {
            float angle = Random.Range(-85f, 85f);
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            Vector3 pos = new Vector3(0f, 0f, 120f) + dir * Random.Range(205f, 230f);
            Tree(pos, Random.Range(1.8f, 2.8f), leaves, trunk, forest);
        }
        // a few trees on the near shore, behind the dock
        Vector3[] nearTrees =
        {
            new Vector3(-9f, 0f, -8f), new Vector3(-15f, 0f, -12f), new Vector3(-6f, 0f, -16f),
            new Vector3(10f, 0f, -9f), new Vector3(16f, 0f, -14f), new Vector3(5f, 0f, -18f),
        };
        foreach (var p in nearTrees) Tree(p, Random.Range(1.0f, 1.4f), leaves, trunk, forest);

        // ---------- dock
        var dock = new GameObject("Dock").transform;
        for (float z = -2f; z < DockLength; z += 0.35f)
        {
            var plank = Shape(PrimitiveType.Cube, "Plank", new Vector3(0f, DeckHeight, z),
                              new Vector3(2.4f, 0.08f, 0.3f), Random.value < 0.3f ? darkWood : wood);
            plank.transform.Rotate(0f, Random.Range(-1f, 1f), 0f);
            plank.transform.SetParent(dock, true);
        }
        for (float z = -1f; z <= DockLength; z += 3f)
        {
            foreach (float x in new[] { -1.25f, 1.25f })
            {
                var post = Shape(PrimitiveType.Cylinder, "Post", new Vector3(x, -0.6f, z), new Vector3(0.22f, 1.4f, 0.22f), darkWood);
                post.transform.SetParent(dock, true);
            }
        }

        // ---------- lantern on the last post
        var lanternPos = new Vector3(1.25f, DeckHeight + 0.95f, DockLength - 1f);
        Shape(PrimitiveType.Cylinder, "LanternPost", new Vector3(1.25f, DeckHeight + 0.4f, DockLength - 1f),
              new Vector3(0.18f, 0.45f, 0.18f), darkWood).transform.SetParent(dock, true);
        Shape(PrimitiveType.Sphere, "Lantern", lanternPos, Vector3.one * 0.22f, lanternGlow).transform.SetParent(dock, true);
        var lamp = new GameObject("LanternLight").AddComponent<Light>();
        lamp.type = LightType.Point;
        lamp.color = new Color(1f, 0.6f, 0.3f);
        lamp.range = 6f;
        lamp.intensity = 2f;
        lamp.transform.position = lanternPos;
        lamp.transform.SetParent(dock, true);

        // ---------- player
        AddPlayer(new Vector3(0f, DeckHeight + 0.04f, DockLength - 2f));

        // ---------- bake the sky lighting so the water reflects the sunset in the build
        var lighting = new LightingSettings { bakedGI = false, realtimeGI = false };
        AssetDatabase.DeleteAsset(Folder + "/LakeLighting.lighting");
        AssetDatabase.CreateAsset(lighting, Folder + "/LakeLighting.lighting");
        Lightmapping.lightingSettings = lighting;
        EditorSceneManager.SaveScene(scene, ScenePath);
        Lightmapping.Bake();
        EditorSceneManager.SaveScene(scene, ScenePath);

        // make it the first scene in the build
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(s => s.path == ScenePath);
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("Lake Dock scene saved to " + ScenePath);
    }

    static void Tree(Vector3 pos, float size, Material[] leaves, Material trunk, Transform parent)
    {
        var tree = new GameObject("Tree").transform;
        tree.position = pos;
        tree.SetParent(parent, true);
        Shape(PrimitiveType.Cylinder, "Trunk", pos + Vector3.up * 2f * size, new Vector3(0.4f, 2f, 0.4f) * size, trunk)
            .transform.SetParent(tree, true);

        Material m = leaves[Random.Range(0, leaves.Length)];
        bool pine = m == leaves[0];
        Vector3 crown = pine ? new Vector3(2.5f, 7f, 2.5f) : new Vector3(4.5f, 4f, 4.5f);
        Shape(PrimitiveType.Sphere, "Leaves", pos + Vector3.up * (pine ? 5.5f : 5f) * size, crown * size, m)
            .transform.SetParent(tree, true);
    }

    static void AddPlayer(Vector3 position)
    {
        var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab");
        if (rigPrefab == null)
        {
            Debug.LogError("OVRCameraRig not found - is the Meta XR SDK installed?");
            return;
        }
        var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
        rig.transform.position = position;
        foreach (var cam in rig.GetComponentsInChildren<Camera>(true)) cam.farClipPlane = 1500f;
        // Put the camera at standing height for the editor preview (the headset takes over when you play)
        foreach (var t in rig.GetComponentsInChildren<Transform>(true))
            if (t.name == "CenterEyeAnchor") t.localPosition = new Vector3(0f, 1.65f, 0f);

        // show the Touch controllers in each hand
        var controller = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.meta.xr.sdk.core/Prefabs/OVRControllerPrefab.prefab");
        if (controller == null) return;
        AddController(rig.transform, "LeftControllerAnchor", controller, 1);  // OVRInput.Controller.LTouch
        AddController(rig.transform, "RightControllerAnchor", controller, 2); // OVRInput.Controller.RTouch
    }

    static void AddController(Transform rig, string anchorName, GameObject prefab, int controllerType)
    {
        foreach (var t in rig.GetComponentsInChildren<Transform>(true))
        {
            if (t.name != anchorName) continue;
            var c = (GameObject)PrefabUtility.InstantiatePrefab(prefab, t);
            var helper = c.GetComponent("OVRControllerHelper");
            if (helper != null)
            {
                var so = new SerializedObject(helper);
                so.FindProperty("m_controller").intValue = controllerType;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            return;
        }
    }

    static GameObject Shape(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static Material Mat(string name, Color color, float smoothness, float metallic = 0f)
    {
        string path = Folder + "/Materials/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", smoothness);
        m.SetFloat("_Metallic", metallic);
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }
}
