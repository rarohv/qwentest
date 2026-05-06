using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameSceneBuilder
{
    private const string SceneFolder = "Assets/Scenes";
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

    [MenuItem("GolfGame/Build All 10 Holes")]
    public static void BuildAll()
    {
        EnsureSceneFolder();
        BuildMainMenuScene();

        int total = 10;
        for (int i = 0; i < total; i++)
        {
            BuildHoleSceneByIndex(i);
        }

        AddAllScenesToBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[GameSceneBuilder] All 10 holes + MainMenu built and added to Build Settings.");
    }

    [MenuItem("GolfGame/Build Current Hole In Open Scene")]
    public static void BuildCurrentSceneAsHole1()
    {
        ConfigureHoleSceneContents(0);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[GameSceneBuilder] Current scene populated as Hole 1.");
    }

    [MenuItem("GolfGame/Build MainMenu Scene")]
    public static void BuildMainMenuOnly()
    {
        EnsureSceneFolder();
        BuildMainMenuScene();
        AddSceneToBuildSettings(MainMenuScenePath, true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("GolfGame/Configure Build Settings")]
    public static void ConfigureBuildSettings()
    {
        AddAllScenesToBuildSettings();
        ApplyPlayerSettings();
        Debug.Log("[GameSceneBuilder] Build Settings & Player Settings configured.");
    }

    private static void EnsureSceneFolder()
    {
        if (!AssetDatabase.IsValidFolder(SceneFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }
    }

    private static void BuildHoleSceneByIndex(int holeIndex)
    {
        string sceneName = "Hole" + (holeIndex + 1).ToString("D2");
        string scenePath = SceneFolder + "/" + sceneName + ".unity";

        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        ConfigureHoleSceneContents(holeIndex);
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("[GameSceneBuilder] Saved " + scenePath);
    }

    private static void BuildMainMenuScene()
    {
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        ConfigureMainMenuSceneContents();
        EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        Debug.Log("[GameSceneBuilder] Saved " + MainMenuScenePath);
    }

    private static void AddAllScenesToBuildSettings()
    {
        EnsureSceneFolder();

        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();

        if (File.Exists(MainMenuScenePath))
            scenes.Add(new EditorBuildSettingsScene(MainMenuScenePath, true));

        for (int i = 0; i < 10; i++)
        {
            string scenePath = SceneFolder + "/Hole" + (i + 1).ToString("D2") + ".unity";
            if (File.Exists(scenePath))
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void AddSceneToBuildSettings(string path, bool enabled)
    {
        if (!File.Exists(path)) return;
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == path)
            {
                scenes[i].enabled = enabled;
                EditorBuildSettings.scenes = scenes.ToArray();
                return;
            }
        }
        scenes.Add(new EditorBuildSettingsScene(path, enabled));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void ApplyPlayerSettings()
    {
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.runInBackground = false;
        PlayerSettings.resizableWindow = true;

        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetArchitecture(BuildTargetGroup.Standalone, 1);
        PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Standalone, ApiCompatibilityLevel.NET_Standard);
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Standalone, ManagedStrippingLevel.Medium);

        QualitySettings.vSyncCount = 1;
    }

    private static void ConfigureMainMenuSceneContents()
    {
        EnsureTags();

        GameObject lightObj = new GameObject("Sun");
        Light sun = lightObj.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.85f, 0.7f);
        sun.intensity = 1.1f;
        sun.shadows = LightShadows.Soft;
        lightObj.transform.rotation = Quaternion.Euler(35f, 30f, 0f);

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.tag = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(8f, 1f, 8f);
        ApplyGroundMaterial(ground);

        AddDecorationsToMainMenu();

        GameObject cameraObj = new GameObject("Main Camera");
        cameraObj.tag = "MainCamera";
        Camera cam = cameraObj.AddComponent<Camera>();
        cam.fieldOfView = 60f;
        cam.transform.position = new Vector3(0f, 4f, 12f);
        cam.transform.LookAt(Vector3.up * 1.5f);
        cameraObj.AddComponent<AudioListener>();

        GameObject coreObj = new GameObject("Core");
        coreObj.AddComponent<DisplaySettings>();
        coreObj.AddComponent<LevelManager>();
        coreObj.AddComponent<AudioManager>();
        coreObj.AddComponent<UIManager>();

        GameObject menuObj = new GameObject("MainMenu");
        MainMenu menu = menuObj.AddComponent<MainMenu>();
        menu.SetOrbitCenter(Vector3.zero);

        ApplyEveningSky();
    }

    private static void AddDecorationsToMainMenu()
    {
        GameObject root = new GameObject("MenuDecorations");
        Random.InitState(2024);
        for (int i = 0; i < 18; i++)
        {
            float angle = i * (Mathf.PI * 2f / 18f);
            float r = 7f + Random.value * 2.5f;
            Vector3 pos = new Vector3(Mathf.Sin(angle) * r, 0f, Mathf.Cos(angle) * r);
            CreateTree(pos, root.transform, 1f + Random.value * 0.6f);
        }
    }

    private static void ConfigureHoleSceneContents(int holeIndex)
    {
        EnsureTags();

        HoleData data = GetSampleHoleData(holeIndex);

        // Sun
        GameObject lightObj = new GameObject("Sun");
        Light sun = lightObj.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.7f, 0.45f);
        sun.intensity = 0.85f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.85f;
        lightObj.transform.rotation = Quaternion.Euler(20f, -45f, 0f);

        ApplyEveningSky();

        // Ground (rough base)
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.tag = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(data.groundScaleX, 1f, data.groundScaleZ);
        ApplyRoughMaterial(ground);

        // Fairway, green, rough patches
        CreateFairway(data);
        CreateGreen(data);
        CreateRoughDetails(data);

        // Obstacles
        if (data.hasWater) CreateWater(data);
        if (data.hasBridge) CreateBridge(data);
        if (data.hasRamp) CreateRamp(data);
        if (data.hasWall) CreateWall(data);
        if (data.hasSandBunker) CreateBunker(data);

        // Decorations
        CreateDecorations(data);

        // Tee box marker
        CreateTeeBox(data.teePosition);

        // Hole target
        GameObject holeObj = new GameObject("Hole");
        holeObj.transform.position = data.holePosition;
        holeObj.AddComponent<HoleTarget>();

        // Golf ball
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "GolfBall";
        ball.tag = "GolfBall";
        ball.transform.position = data.teePosition + Vector3.up * 0.3f;
        ball.transform.localScale = Vector3.one * 0.12f;
        Renderer ballRenderer = ball.GetComponent<Renderer>();
        if (ballRenderer != null)
        {
            Material ballMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            ballMat.color = Color.white;
            ballMat.SetFloat("_Smoothness", 0.85f);
            ballRenderer.sharedMaterial = ballMat;
        }
        Object.DestroyImmediate(ball.GetComponent<SphereCollider>());
        ball.AddComponent<SphereCollider>();
        ball.AddComponent<Rigidbody>();
        ball.AddComponent<GolfBall>();

        // Player
        GameObject player = CreatePlayer(data.playerStart);

        // Camera (with offset, controller will reposition each frame)
        GameObject cameraObj = new GameObject("Main Camera");
        cameraObj.tag = "MainCamera";
        Camera cam = cameraObj.AddComponent<Camera>();
        cam.fieldOfView = PlayerPrefs.GetFloat("CameraFOV", 65f);
        cam.transform.position = data.playerStart + new Vector3(0f, 2.5f, -4f);
        cam.transform.LookAt(data.playerStart + Vector3.up * 1.2f);
        cameraObj.AddComponent<AudioListener>();

        cameraObj.AddComponent<CameraController>();
        // CameraController auto-links to GameObject tagged "Player" in Awake().

        // Core managers
        GameObject coreObj = new GameObject("Core");
        coreObj.AddComponent<DisplaySettings>();
        coreObj.AddComponent<LevelManager>();
        coreObj.AddComponent<AudioManager>();
        coreObj.AddComponent<GolfManager>();
        coreObj.AddComponent<UIManager>();
    }

    private static void CreateFairway(HoleData data)
    {
        Vector3 startXZ = new Vector3(data.teePosition.x, 0.01f, data.teePosition.z);
        Vector3 endXZ = new Vector3(data.holePosition.x, 0.01f, data.holePosition.z);
        Vector3 mid = (startXZ + endXZ) * 0.5f;
        Vector3 dir = endXZ - startXZ;
        float length = new Vector2(dir.x, dir.z).magnitude;

        GameObject fairway = GameObject.CreatePrimitive(PrimitiveType.Plane);
        fairway.name = "Fairway";
        fairway.transform.position = mid + Vector3.up * 0.01f;
        fairway.transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z), Vector3.up);
        fairway.transform.localScale = new Vector3(0.4f, 1f, length / 10f + 0.4f);
        ApplyFairwayMaterial(fairway);
        Object.DestroyImmediate(fairway.GetComponent<MeshCollider>());
    }

    private static void CreateGreen(HoleData data)
    {
        GameObject green = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        green.name = "Green";
        green.transform.position = new Vector3(data.holePosition.x, data.holePosition.y - 0.02f, data.holePosition.z);
        green.transform.localScale = new Vector3(5f, 0.04f, 5f);
        ApplyGreenMaterial(green);
        Object.DestroyImmediate(green.GetComponent<CapsuleCollider>());
    }

    private static void CreateRoughDetails(HoleData data)
    {
        GameObject root = new GameObject("Rough");
        Random.InitState(data.decorationSeed * 7 + 41);
        int count = 30;
        Material rough = MakeRoughBlade();
        for (int i = 0; i < count; i++)
        {
            float side = (i % 2 == 0) ? -1f : 1f;
            float t = Random.value;
            float halfW = data.groundScaleX * 5f - 1.5f;
            float x = Mathf.Lerp(-halfW, halfW, t) + Random.Range(-1.5f, 1.5f);
            if (Mathf.Abs(x) < 1.8f) x = side * (1.8f + Random.Range(0f, 1f));
            float z = Mathf.Lerp(0f, data.groundScaleZ * 5f, Random.value);
            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.transform.parent = root.transform;
            blade.transform.position = new Vector3(x, 0.15f + Random.value * 0.05f, z);
            blade.transform.localScale = new Vector3(0.06f, 0.3f + Random.value * 0.25f, 0.06f);
            Object.DestroyImmediate(blade.GetComponent<BoxCollider>());
            Renderer r = blade.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = rough;
        }
    }

    private static void CreateWater(HoleData data)
    {
        Shader waterShader = Shader.Find("Custom/RealisticWater");
        Material waterMat;
        if (waterShader != null)
        {
            waterMat = new Material(waterShader);
            waterMat.SetColor("_BaseColor", new Color(0.08f, 0.32f, 0.58f, 0.65f));
            waterMat.SetColor("_DepthColor", new Color(0.02f, 0.07f, 0.18f, 1f));
            waterMat.SetFloat("_Transparency", 0.55f);
            waterMat.SetFloat("_RefractionStrength", 0.015f);
            waterMat.SetFloat("_WaveSpeed", 1.2f);
            waterMat.SetFloat("_WaveScale", 4f);
            waterMat.SetFloat("_WaveAmplitude", 0.06f);
            waterMat.SetColor("_SpecularColor", new Color(1f, 0.7f, 0.35f, 1f));
            waterMat.SetFloat("_SpecularPower", 128f);
            waterMat.SetFloat("_SpecularIntensity", 1.5f);
            waterMat.SetFloat("_FresnelPower", 4f);
            waterMat.SetColor("_FoamColor", new Color(0.85f, 0.9f, 0.95f, 1f));
            waterMat.SetFloat("_FoamThreshold", 0.5f);
            waterMat.SetFloat("_FoamIntensity", 0.3f);
        }
        else
        {
            waterMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            waterMat.color = new Color(0.1f, 0.4f, 0.7f, 0.8f);
        }

        GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
        water.name = "StreamWater";
        water.transform.position = data.waterPosition;
        water.transform.localScale = data.waterScale;

        Renderer r = water.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = waterMat;
        Object.DestroyImmediate(water.GetComponent<MeshCollider>());

        BoxCollider bc = water.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        water.AddComponent<WaterTrigger>();
    }

    private static void CreateBridge(HoleData data)
    {
        GameObject bridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bridge.name = "Bridge";
        bridge.transform.position = data.bridgePosition;
        bridge.transform.localScale = data.bridgeScale;
        ApplyBridgeMaterial(bridge);
    }

    private static void CreateRamp(HoleData data)
    {
        GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = "Ramp";
        ramp.transform.position = data.rampPosition;
        ramp.transform.rotation = Quaternion.Euler(data.rampRotation);
        ramp.transform.localScale = data.rampScale;

        Renderer r = ramp.GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.45f, 0.32f, 0.18f);
            r.sharedMaterial = mat;
        }
    }

    private static void CreateWall(HoleData data)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Wall";
        wall.transform.position = data.wallPosition;
        wall.transform.localScale = data.wallScale;

        Renderer r = wall.GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.55f, 0.5f, 0.45f);
            r.sharedMaterial = mat;
        }
    }

    private static void CreateBunker(HoleData data)
    {
        GameObject bunker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bunker.name = "SandBunker";
        bunker.transform.position = data.bunkerPosition;
        bunker.transform.localScale = data.bunkerScale;

        Renderer r = bunker.GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.95f, 0.85f, 0.6f);
            r.sharedMaterial = mat;
        }
    }

    private static void CreateTeeBox(Vector3 teePos)
    {
        GameObject teeBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        teeBox.name = "TeeBox";
        teeBox.transform.position = new Vector3(teePos.x, teePos.y, teePos.z);
        teeBox.transform.localScale = new Vector3(1.5f, 0.15f, 1f);

        Renderer r = teeBox.GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.85f, 0.85f, 0.85f);
            r.sharedMaterial = mat;
        }

        GameObject teePeg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        teePeg.name = "TeePeg";
        teePeg.transform.parent = teeBox.transform;
        teePeg.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        teePeg.transform.localScale = new Vector3(0.04f, 0.4f, 0.04f);
        Object.DestroyImmediate(teePeg.GetComponent<CapsuleCollider>());
        Renderer pegR = teePeg.GetComponent<Renderer>();
        if (pegR != null)
        {
            Material pmat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            pmat.color = Color.white;
            pegR.sharedMaterial = pmat;
        }
    }

    private static void CreateDecorations(HoleData data)
    {
        GameObject root = new GameObject("Decorations");
        Random.InitState(data.decorationSeed > 0 ? data.decorationSeed : 1234);

        Vector3 tee = data.teePosition;
        Vector3 hole = data.holePosition;
        Vector3 fairwayDir = (new Vector3(hole.x, 0f, hole.z) - new Vector3(tee.x, 0f, tee.z)).normalized;
        Vector3 fairwayPerp = new Vector3(-fairwayDir.z, 0f, fairwayDir.x);
        float halfW = data.groundScaleX * 5f - 1f;
        float halfL = data.groundScaleZ * 5f - 1f;

        // Trees
        int treeCount = Random.Range(8, 14);
        for (int i = 0; i < treeCount; i++)
        {
            Vector3 pos = RandomEdgePoint(halfW, halfL, tee, hole, fairwayPerp, 4f);
            CreateTree(pos, root.transform, 0.85f + Random.value * 0.7f);
        }

        // Bushes
        int bushCount = Random.Range(8, 14);
        for (int i = 0; i < bushCount; i++)
        {
            Vector3 pos = RandomEdgePoint(halfW, halfL, tee, hole, fairwayPerp, 2.5f);
            CreateBush(pos, root.transform);
        }

        // Rocks
        int rockCount = Random.Range(5, 10);
        for (int i = 0; i < rockCount; i++)
        {
            Vector3 pos = RandomEdgePoint(halfW, halfL, tee, hole, fairwayPerp, 2f);
            CreateRock(pos, root.transform);
        }

        // Flowers near tee and green
        for (int i = 0; i < 12; i++)
        {
            Vector3 baseAt = (i < 6) ? tee : hole;
            float angle = Random.value * Mathf.PI * 2f;
            float dist = 1.5f + Random.value * 1.5f;
            Vector3 pos = baseAt + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
            CreateFlower(pos, root.transform);
        }
    }

    private static Vector3 RandomEdgePoint(float halfW, float halfL, Vector3 tee, Vector3 hole, Vector3 fairwayPerp, float minOffset)
    {
        for (int attempt = 0; attempt < 8; attempt++)
        {
            float x = Random.Range(-halfW, halfW);
            float z = Random.Range(-3f, halfL);
            Vector3 p = new Vector3(x, 0f, z);

            float along = Mathf.InverseLerp(tee.z, hole.z, z);
            float fairwayCenterX = Mathf.Lerp(tee.x, hole.x, along);
            if (Mathf.Abs(x - fairwayCenterX) < minOffset) continue;

            return p;
        }
        return new Vector3(halfW * 0.9f * (Random.value < 0.5f ? -1f : 1f), 0f, Random.Range(0f, halfL));
    }

    private static void CreateTree(Vector3 position, Transform parent, float scale)
    {
        GameObject tree = new GameObject("Tree");
        tree.transform.parent = parent;
        tree.transform.position = position;

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.parent = tree.transform;
        trunk.transform.localPosition = new Vector3(0f, 1.5f * scale, 0f);
        trunk.transform.localScale = new Vector3(0.3f * scale, 1.5f * scale, 0.3f * scale);
        Object.DestroyImmediate(trunk.GetComponent<CapsuleCollider>());
        Renderer trunkR = trunk.GetComponent<Renderer>();
        if (trunkR != null)
        {
            Material trunkMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            trunkMat.color = new Color(0.36f, 0.22f, 0.12f);
            trunkR.sharedMaterial = trunkMat;
        }

        GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        canopy.name = "Canopy";
        canopy.transform.parent = tree.transform;
        canopy.transform.localPosition = new Vector3(0f, 3.4f * scale, 0f);
        canopy.transform.localScale = new Vector3(2.0f * scale, 2.0f * scale, 2.0f * scale);
        Object.DestroyImmediate(canopy.GetComponent<SphereCollider>());
        Renderer canR = canopy.GetComponent<Renderer>();
        if (canR != null)
        {
            Material canMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            canMat.color = new Color(0.16f + Random.value * 0.1f, 0.4f + Random.value * 0.18f, 0.18f + Random.value * 0.08f);
            canR.sharedMaterial = canMat;
        }
    }

    private static void CreateBush(Vector3 position, Transform parent)
    {
        GameObject bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bush.name = "Bush";
        bush.transform.parent = parent;
        bush.transform.position = new Vector3(position.x, 0.25f, position.z);
        bush.transform.localScale = new Vector3(0.6f + Random.value * 0.4f, 0.45f + Random.value * 0.25f, 0.6f + Random.value * 0.4f);
        Object.DestroyImmediate(bush.GetComponent<SphereCollider>());

        Renderer r = bush.GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.18f, 0.42f, 0.18f);
            r.sharedMaterial = mat;
        }
    }

    private static void CreateRock(Vector3 position, Transform parent)
    {
        GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rock.name = "Rock";
        rock.transform.parent = parent;
        rock.transform.position = new Vector3(position.x, 0.2f, position.z);
        float s = 0.4f + Random.value * 0.7f;
        rock.transform.localScale = new Vector3(s, s * (0.5f + Random.value * 0.5f), s);
        rock.transform.rotation = Quaternion.Euler(Random.value * 30f, Random.value * 360f, Random.value * 30f);

        Renderer r = rock.GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            float gray = 0.42f + Random.value * 0.18f;
            mat.color = new Color(gray, gray, gray * 0.95f);
            r.sharedMaterial = mat;
        }
    }

    private static void CreateFlower(Vector3 position, Transform parent)
    {
        GameObject flower = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flower.name = "Flower";
        flower.transform.parent = parent;
        flower.transform.position = new Vector3(position.x, 0.15f, position.z);
        flower.transform.localScale = new Vector3(0.06f, 0.3f, 0.06f);
        Object.DestroyImmediate(flower.GetComponent<BoxCollider>());

        Renderer r = flower.GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            float pick = Random.value;
            if (pick < 0.33f) mat.color = new Color(1f, 0.85f, 0.25f);
            else if (pick < 0.66f) mat.color = new Color(0.95f, 0.4f, 0.6f);
            else mat.color = new Color(0.5f, 0.55f, 0.95f);
            r.sharedMaterial = mat;
        }
    }

    private static GameObject CreatePlayer(Vector3 spawn)
    {
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = spawn;

        CharacterController cc = player.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 1f, 0f);
        cc.height = 1.9f;
        cc.radius = 0.3f;
        cc.skinWidth = 0.04f;
        cc.minMoveDistance = 0.001f;

        player.AddComponent<PlayerMovement>();
        player.AddComponent<PlayerVisuals>();
        return player;
    }

    private static void ApplyGroundMaterial(GameObject obj)
    {
        Renderer r = obj.GetComponent<Renderer>();
        if (r == null) return;
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.name = "Ground_Green";
        Texture2D tex = Resources.Load<Texture2D>("Textures/Grass");
        if (tex != null)
        {
            mat.mainTexture = tex;
            mat.SetTextureScale("_BaseMap", new Vector2(5f, 5f));
        }
        else
        {
            mat.color = new Color(0.22f, 0.52f, 0.18f);
        }
        mat.SetFloat("_Smoothness", 0.1f);
        r.sharedMaterial = mat;
    }

    private static void ApplyRoughMaterial(GameObject obj)
    {
        Renderer r = obj.GetComponent<Renderer>();
        if (r == null) return;
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.name = "Rough_Green";
        Texture2D tex = Resources.Load<Texture2D>("Textures/Grass");
        if (tex != null)
        {
            mat.mainTexture = tex;
            mat.SetTextureScale("_BaseMap", new Vector2(7f, 7f));
        }
        mat.color = new Color(0.16f, 0.38f, 0.14f);
        mat.SetFloat("_Smoothness", 0.05f);
        r.sharedMaterial = mat;
    }

    private static void ApplyFairwayMaterial(GameObject obj)
    {
        Renderer r = obj.GetComponent<Renderer>();
        if (r == null) return;
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.name = "Fairway_Green";
        Texture2D tex = Resources.Load<Texture2D>("Textures/Grass");
        if (tex != null)
        {
            mat.mainTexture = tex;
            mat.SetTextureScale("_BaseMap", new Vector2(3f, 3f));
        }
        mat.color = new Color(0.27f, 0.6f, 0.22f);
        mat.SetFloat("_Smoothness", 0.1f);
        r.sharedMaterial = mat;
    }

    private static void ApplyGreenMaterial(GameObject obj)
    {
        Renderer r = obj.GetComponent<Renderer>();
        if (r == null) return;
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.name = "Putting_Green";
        mat.color = new Color(0.36f, 0.7f, 0.27f);
        mat.SetFloat("_Smoothness", 0.15f);
        r.sharedMaterial = mat;
    }

    private static void ApplyBridgeMaterial(GameObject obj)
    {
        Renderer r = obj.GetComponent<Renderer>();
        if (r == null) return;
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.name = "Bridge_Wood";
        Texture2D woodTex = Resources.Load<Texture2D>("Textures/Wood");
        if (woodTex != null)
        {
            mat.mainTexture = woodTex;
            mat.SetTextureScale("_BaseMap", new Vector2(2f, 2f));
        }
        else
        {
            mat.color = new Color(0.55f, 0.35f, 0.18f);
        }
        mat.SetFloat("_Smoothness", 0.3f);
        r.sharedMaterial = mat;
    }

    private static Material MakeRoughBlade()
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.name = "RoughBlade";
        mat.color = new Color(0.18f, 0.42f, 0.16f);
        return mat;
    }

    private static void ApplyEveningSky()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.38f, 0.22f, 0.35f);

        Texture2D skyboxTex = Resources.Load<Texture2D>("Textures/Skybox");
        Material skybox = null;
        if (skyboxTex != null)
        {
            Shader s = Shader.Find("Skybox/Panoramic");
            if (s != null)
            {
                skybox = new Material(s);
                if (skybox.HasProperty("_MainTex")) skybox.SetTexture("_MainTex", skyboxTex);
                if (skybox.HasProperty("_Tint")) skybox.SetColor("_Tint", Color.white);
                if (skybox.HasProperty("_Exposure")) skybox.SetFloat("_Exposure", 1.0f);
                if (skybox.HasProperty("_Mapping")) skybox.SetFloat("_Mapping", 0f);
            }
        }
        if (skybox == null)
        {
            Shader s = Shader.Find("Skybox/Procedural");
            if (s != null)
            {
                skybox = new Material(s);
                if (skybox.HasProperty("_SunTint")) skybox.SetColor("_SunTint", new Color(1f, 0.5f, 0.2f));
                if (skybox.HasProperty("_SkyTint")) skybox.SetColor("_SkyTint", new Color(0.6f, 0.3f, 0.5f));
                if (skybox.HasProperty("_GroundColor")) skybox.SetColor("_GroundColor", new Color(0.2f, 0.12f, 0.15f));
                if (skybox.HasProperty("_AtmosphereThickness")) skybox.SetFloat("_AtmosphereThickness", 1.5f);
                if (skybox.HasProperty("_Exposure")) skybox.SetFloat("_Exposure", 0.9f);
            }
        }
        if (skybox != null) RenderSettings.skybox = skybox;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.012f;
        RenderSettings.fogColor = new Color(0.55f, 0.3f, 0.4f);
    }

    private static void EnsureTags()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        if (tagsProp == null) return;

        string[] requiredTags = new[] { "Player", "GolfBall", "Ground", "GameManager" };
        foreach (string tag in requiredTags)
        {
            bool found = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                tagsProp.arraySize++;
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            }
        }
        tagManager.ApplyModifiedProperties();
    }

    private static HoleData GetSampleHoleData(int holeIndex)
    {
        // Mirrors LevelManager.BuildHoleData definitions.
        switch (holeIndex)
        {
            case 0:
                return new HoleData
                {
                    holeNumber = 1, holeName = "First Swing", sceneName = "Hole01", par = 2,
                    teePosition = new Vector3(0f, 0.15f, 2f),
                    holePosition = new Vector3(0f, 0.08f, 35f),
                    playerStart = new Vector3(0f, 1f, 0f),
                    groundScaleX = 5f, groundScaleZ = 8f,
                    decorationSeed = 1001
                };
            case 1:
                return new HoleData
                {
                    holeNumber = 2, holeName = "Over the Stream", sceneName = "Hole02", par = 3,
                    teePosition = new Vector3(0f, 0.15f, 2f),
                    holePosition = new Vector3(0f, 0.08f, 38f),
                    playerStart = new Vector3(0f, 1f, 0f),
                    groundScaleX = 6f, groundScaleZ = 8f,
                    decorationSeed = 1002,
                    hasWater = true, waterPosition = new Vector3(0f, 0.15f, 20f), waterScale = new Vector3(0.8f, 1f, 0.6f),
                    hasBridge = true, bridgePosition = new Vector3(0f, 0.35f, 20f), bridgeScale = new Vector3(4f, 0.2f, 6f)
                };
            case 2:
                return new HoleData
                {
                    holeNumber = 3, holeName = "The Bunker", sceneName = "Hole03", par = 3,
                    teePosition = new Vector3(0f, 0.15f, 2f),
                    holePosition = new Vector3(0f, 0.08f, 40f),
                    playerStart = new Vector3(0f, 1f, 0f),
                    groundScaleX = 6f, groundScaleZ = 9f,
                    decorationSeed = 1003,
                    hasSandBunker = true, bunkerPosition = new Vector3(2f, -0.1f, 25f), bunkerScale = new Vector3(3f, 0.3f, 3f)
                };
            case 3:
                return new HoleData
                {
                    holeNumber = 4, holeName = "Ramp Shot", sceneName = "Hole04", par = 3,
                    teePosition = new Vector3(0f, 0.15f, 2f),
                    holePosition = new Vector3(0f, 1.2f, 38f),
                    playerStart = new Vector3(0f, 1f, 0f),
                    groundScaleX = 6f, groundScaleZ = 8f,
                    decorationSeed = 1004,
                    hasRamp = true, rampPosition = new Vector3(0f, 0.3f, 20f), rampScale = new Vector3(4f, 0.3f, 5f), rampRotation = new Vector3(-15f, 0f, 0f)
                };
            case 4:
                return new HoleData
                {
                    holeNumber = 5, holeName = "Island Green", sceneName = "Hole05", par = 3,
                    teePosition = new Vector3(0f, 0.15f, 2f),
                    holePosition = new Vector3(0f, 0.15f, 40f),
                    playerStart = new Vector3(0f, 1f, 0f),
                    groundScaleX = 7f, groundScaleZ = 9f,
                    decorationSeed = 1005,
                    hasWater = true, waterPosition = new Vector3(0f, 0.15f, 22f), waterScale = new Vector3(1.5f, 1f, 1.2f),
                    hasBridge = true, bridgePosition = new Vector3(0f, 0.35f, 22f), bridgeScale = new Vector3(2.5f, 0.2f, 6f)
                };
            case 5:
                return new HoleData
                {
                    holeNumber = 6, holeName = "The Wall", sceneName = "Hole06", par = 4,
                    teePosition = new Vector3(0f, 0.15f, 2f),
                    holePosition = new Vector3(5f, 0.08f, 35f),
                    playerStart = new Vector3(0f, 1f, 0f),
                    groundScaleX = 8f, groundScaleZ = 8f,
                    decorationSeed = 1006,
                    hasWall = true, wallPosition = new Vector3(2f, 1f, 18f), wallScale = new Vector3(0.5f, 3f, 10f),
                    hasSandBunker = true, bunkerPosition = new Vector3(5f, -0.1f, 28f), bunkerScale = new Vector3(3f, 0.3f, 3f)
                };
            case 6:
                return new HoleData
                {
                    holeNumber = 7, holeName = "Ramp & River", sceneName = "Hole07", par = 4,
                    teePosition = new Vector3(0f, 0.15f, 2f),
                    holePosition = new Vector3(0f, 1.5f, 42f),
                    playerStart = new Vector3(0f, 1f, 0f),
                    groundScaleX = 6f, groundScaleZ = 9f,
                    decorationSeed = 1007,
                    hasWater = true, waterPosition = new Vector3(0f, 0.15f, 25f), waterScale = new Vector3(1f, 1f, 0.8f),
                    hasBridge = true, bridgePosition = new Vector3(0f, 0.35f, 25f), bridgeScale = new Vector3(3f, 0.2f, 6f),
                    hasRamp = true, rampPosition = new Vector3(0f, 0.3f, 15f), rampScale = new Vector3(4f, 0.3f, 4f), rampRotation = new Vector3(-12f, 0f, 0f)
                };
            case 7:
                return new HoleData
                {
                    holeNumber = 8, holeName = "Narrow Path", sceneName = "Hole08", par = 3,
                    teePosition = new Vector3(0f, 0.15f, 2f),
                    holePosition = new Vector3(0f, 0.08f, 40f),
                    playerStart = new Vector3(0f, 1f, 0f),
                    groundScaleX = 4f, groundScaleZ = 9f,
                    decorationSeed = 1008,
                    hasWater = true, waterPosition = new Vector3(0f, 0.15f, 20f), waterScale = new Vector3(1.2f, 1f, 1f)
                };
            case 8:
                return new HoleData
                {
                    holeNumber = 9, holeName = "The Gauntlet", sceneName = "Hole09", par = 5,
                    teePosition = new Vector3(0f, 0.15f, 2f),
                    holePosition = new Vector3(0f, 1.8f, 45f),
                    playerStart = new Vector3(0f, 1f, 0f),
                    groundScaleX = 8f, groundScaleZ = 10f,
                    decorationSeed = 1009,
                    hasWater = true, waterPosition = new Vector3(0f, 0.15f, 15f), waterScale = new Vector3(1f, 1f, 0.8f),
                    hasBridge = true, bridgePosition = new Vector3(0f, 0.35f, 15f), bridgeScale = new Vector3(2.5f, 0.2f, 5f),
                    hasRamp = true, rampPosition = new Vector3(0f, 0.3f, 30f), rampScale = new Vector3(5f, 0.3f, 5f), rampRotation = new Vector3(-10f, 0f, 0f),
                    hasWall = true, wallPosition = new Vector3(3f, 1f, 22f), wallScale = new Vector3(0.5f, 2.5f, 6f),
                    hasSandBunker = true, bunkerPosition = new Vector3(-2f, -0.1f, 35f), bunkerScale = new Vector3(3f, 0.3f, 4f)
                };
            default:
                return new HoleData
                {
                    holeNumber = 10, holeName = "Grand Finale", sceneName = "Hole10", par = 5,
                    teePosition = new Vector3(0f, 0.15f, 2f),
                    holePosition = new Vector3(0f, 2f, 50f),
                    playerStart = new Vector3(0f, 1f, 0f),
                    groundScaleX = 10f, groundScaleZ = 12f,
                    decorationSeed = 1010,
                    hasWater = true, waterPosition = new Vector3(0f, 0.15f, 20f), waterScale = new Vector3(2f, 1f, 1.5f),
                    hasBridge = true, bridgePosition = new Vector3(-2f, 0.35f, 20f), bridgeScale = new Vector3(2f, 0.2f, 5f),
                    hasRamp = true, rampPosition = new Vector3(0f, 0.4f, 35f), rampScale = new Vector3(5f, 0.4f, 6f), rampRotation = new Vector3(-18f, 0f, 0f),
                    hasWall = true, wallPosition = new Vector3(4f, 1.5f, 28f), wallScale = new Vector3(0.5f, 3f, 8f),
                    hasSandBunker = true, bunkerPosition = new Vector3(-3f, -0.1f, 42f), bunkerScale = new Vector3(4f, 0.3f, 4f)
                };
        }
    }
}
