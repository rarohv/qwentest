using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class GameSceneBuilder
{
    [MenuItem("GolfGame/Build Level")]
    public static void BuildLevel()
    {
        EnsureTags();
        CreateNewScene();

        LevelManager levelManager = CreateLevelManager();
        HoleData hole = levelManager.CurrentHole;

        GameObject ground = CreateGround(hole);
        CreateWater(hole);
        CreateBridge(hole);
        CreateRamp(hole);
        CreateWall(hole);
        CreateSandBunker(hole);
        CreateHoleTarget(hole);
        CreatePlayer(hole);
        CreateGolfBall(hole);
        CreateGameManager();
        CreateMainCamera();

        MarkSceneDirty();
        Debug.Log("[GameSceneBuilder] Level built successfully!");
    }

    [MenuItem("GolfGame/Build All 10 Holes")]
    public static void BuildAllHoles()
    {
        EnsureTags();

        LevelManager tempLM = CreateTempLevelManager();
        if (tempLM == null)
        {
            Debug.LogError("[GameSceneBuilder] Failed to create LevelManager");
            return;
        }

        for (int i = 0; i < tempLM.TotalHoles; i++)
        {
            HoleData hole = tempLM.GetHole(i);
            CreateNewScene();
            BuildSingleHole(hole, i + 1);
            string scenePath = string.Format("Assets/Scenes/Hole{0:D2}.unity", i + 1);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), scenePath);
            Debug.Log(string.Format("[GameSceneBuilder] Built Hole {0}: {1}", hole.holeNumber, hole.holeName));
        }

        Object.DestroyImmediate(tempLM.gameObject);
        Debug.Log("[GameSceneBuilder] All 10 holes built!");
    }

    static LevelManager CreateTempLevelManager()
    {
        GameObject lmObj = new GameObject("TempLevelManager");
        LevelManager lm = lmObj.AddComponent<LevelManager>();
        return lm;
    }

    static void BuildSingleHole(HoleData hole, int holeNum)
    {
        GameObject ground = CreateGround(hole);
        CreateWater(hole);
        CreateBridge(hole);
        CreateRamp(hole);
        CreateWall(hole);
        CreateSandBunker(hole);
        CreateHoleTarget(hole);
        CreatePlayer(hole);
        CreateGolfBall(hole);
        CreateGameManager();
        CreateMainCamera();
        MarkSceneDirty();
    }

    static void EnsureTags()
    {
        AddTag("Ground");
        AddTag("GolfBall");
        AddTag("GameManager");
        AddTag("Player");
    }

    static void AddTag(string tag)
    {
        UnityEngine.Object[] tagManagerAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tagManagerAssets == null || tagManagerAssets.Length == 0) return;

        SerializedObject tagManager = new SerializedObject(tagManagerAssets[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                return;
        }

        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
        tagManager.Update();
    }

    static void CreateNewScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.35f, 0.28f, 0.45f);
        RenderSettings.skybox = null;
    }

    static GameObject CreateGround(HoleData hole)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.tag = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(hole.groundScaleX, 1f, hole.groundScaleZ);

        Renderer renderer = ground.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.28f, 0.55f, 0.22f);
        renderer.material = mat;

        return ground;
    }

    static void CreateWater(HoleData hole)
    {
        if (!hole.hasWater) return;

        GameObject ditch = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ditch.name = "Ditch";
        ditch.transform.position = hole.waterPosition;
        ditch.transform.localScale = new Vector3(8f, 0.5f, 6f);
        ditch.transform.position = new Vector3(hole.waterPosition.x, hole.waterPosition.y - 0.25f, hole.waterPosition.z);

        Renderer ditchRenderer = ditch.GetComponent<Renderer>();
        Material ditchMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        ditchMat.color = new Color(0.3f, 0.22f, 0.12f);
        ditchRenderer.material = ditchMat;

        Shader waterShader = Shader.Find("Custom/RealisticWater");
        if (waterShader == null)
        {
            GameObject stream = GameObject.CreatePrimitive(PrimitiveType.Plane);
            stream.name = "StreamWater";
            stream.transform.position = hole.waterPosition;
            stream.transform.localScale = hole.waterScale;

            Renderer streamRenderer = stream.GetComponent<Renderer>();
            Material streamMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            streamMat.color = new Color(0.15f, 0.35f, 0.6f);
            streamRenderer.material = streamMat;

            Collider col = stream.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            BoxCollider trigger = stream.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            stream.AddComponent<WaterTrigger>();
        }
        else
        {
            Material waterMat = new Material(waterShader);
            waterMat.name = "Water_Realistic";
            waterMat.SetColor("_BaseColor", new Color(0.08f, 0.32f, 0.58f, 0.65f));
            waterMat.SetColor("_DepthColor", new Color(0.02f, 0.07f, 0.18f, 1f));
            waterMat.SetFloat("_Transparency", 0.55f);
            waterMat.SetFloat("_WaveSpeed", 1.2f);
            waterMat.SetFloat("_WaveScale", 4f);
            waterMat.SetFloat("_WaveAmplitude", 0.06f);

            GameObject waterPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            waterPlane.name = "StreamWater";
            waterPlane.transform.position = new Vector3(hole.waterPosition.x, hole.waterPosition.y + 0.05f, hole.waterPosition.z);
            waterPlane.transform.localScale = hole.waterScale;

            Renderer waterRenderer = waterPlane.GetComponent<Renderer>();
            waterRenderer.sharedMaterial = waterMat;

            Collider col = waterPlane.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            BoxCollider trigger = waterPlane.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            waterPlane.AddComponent<WaterTrigger>();
        }
    }

    static void CreateBridge(HoleData hole)
    {
        if (!hole.hasBridge) return;

        GameObject bridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bridge.name = "Bridge";
        bridge.transform.position = hole.bridgePosition;
        bridge.transform.localScale = hole.bridgeScale;

        Renderer renderer = bridge.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.55f, 0.35f, 0.18f);
        renderer.material = mat;

        float railOffsetX = hole.bridgeScale.x * 0.45f;

        GameObject railLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        railLeft.name = "RailLeft";
        railLeft.transform.parent = bridge.transform;
        railLeft.transform.localPosition = new Vector3(-railOffsetX, 0.6f, 0f);
        railLeft.transform.localScale = new Vector3(0.1f, 1f, 1f);
        railLeft.GetComponent<Renderer>().material = mat;

        GameObject railRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        railRight.name = "RailRight";
        railRight.transform.parent = bridge.transform;
        railRight.transform.localPosition = new Vector3(railOffsetX, 0.6f, 0f);
        railRight.transform.localScale = new Vector3(0.1f, 1f, 1f);
        railRight.GetComponent<Renderer>().material = mat;
    }

    static void CreateRamp(HoleData hole)
    {
        if (!hole.hasRamp) return;

        GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = "Ramp";
        ramp.transform.position = hole.rampPosition;
        ramp.transform.localScale = hole.rampScale;
        ramp.transform.rotation = Quaternion.Euler(hole.rampRotation);

        Renderer renderer = ramp.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.45f, 0.4f, 0.35f);
        renderer.material = mat;
    }

    static void CreateWall(HoleData hole)
    {
        if (!hole.hasWall) return;

        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Wall";
        wall.transform.position = hole.wallPosition;
        wall.transform.localScale = hole.wallScale;

        Renderer renderer = wall.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.5f, 0.48f, 0.45f);
        renderer.material = mat;
    }

    static void CreateSandBunker(HoleData hole)
    {
        if (!hole.hasSandBunker) return;

        GameObject bunker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bunker.name = "SandBunker";
        bunker.transform.position = hole.bunkerPosition;
        bunker.transform.localScale = hole.bunkerScale;

        Renderer renderer = bunker.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.85f, 0.78f, 0.55f);
        renderer.material = mat;
    }

    static void CreateHoleTarget(HoleData hole)
    {
        GameObject holeTarget = new GameObject("HoleTarget");
        holeTarget.transform.position = hole.holePosition;
        holeTarget.AddComponent<HoleTarget>();
    }

    static void CreatePlayer(HoleData hole)
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.tag = "Player";
        player.transform.position = hole.playerStart;
        player.transform.localScale = Vector3.one;

        Renderer renderer = player.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.7f, 0.5f, 0.3f);
        renderer.material = mat;

        player.AddComponent<CharacterController>();
        player.AddComponent<PlayerMovement>();
    }

    static void CreateGolfBall(HoleData hole)
    {
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "GolfBall";
        ball.tag = "GolfBall";
        ball.transform.position = hole.teePosition;
        ball.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);

        Renderer renderer = ball.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = Color.white;
        renderer.material = mat;

        Rigidbody rb = ball.AddComponent<Rigidbody>();
        rb.mass = 0.045f;
        rb.drag = 0.2f;
        rb.angularDrag = 0.5f;

        SphereCollider collider = ball.GetComponent<SphereCollider>();
        collider.material = new PhysicMaterial("GolfBallBounce")
        {
            bounciness = 0.6f,
            dynamicFriction = 0.4f,
            staticFriction = 0.4f,
            frictionCombine = PhysicMaterialCombine.Average,
            bounceCombine = PhysicMaterialCombine.Average
        };

        ball.AddComponent<GolfBall>();
    }

    static LevelManager CreateLevelManager()
    {
        GameObject lmObj = new GameObject("LevelManager");
        LevelManager lm = lmObj.AddComponent<LevelManager>();
        return lm;
    }

    static void CreateGameManager()
    {
        GameObject gm = new GameObject("GameManager");
        gm.tag = "GameManager";
        gm.AddComponent<GolfManager>();
        gm.AddComponent<UIManager>();
        gm.AddComponent<AudioManager>();
    }

    static void CreateMainCamera()
    {
        GameObject cameraObj = new GameObject("Main Camera");
        cameraObj.tag = "MainCamera";
        cameraObj.transform.position = new Vector3(0f, 3f, -6f);
        cameraObj.transform.rotation = Quaternion.Euler(15f, 0f, 0f);

        Camera cam = cameraObj.AddComponent<Camera>();
        cameraObj.AddComponent<AudioListener>();
        cameraObj.AddComponent<CameraController>();

        GameObject dirLight = new GameObject("Directional Light");
        Light light = dirLight.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.85f, 0.65f);
        light.intensity = 0.8f;
        light.shadows = LightShadows.Soft;
        dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    static void MarkSceneDirty()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
    }
}
