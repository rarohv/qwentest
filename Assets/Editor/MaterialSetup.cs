using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;

public static class MaterialSetup
{
    [MenuItem("GolfGame/Setup Materials")]
    public static void SetupMaterials()
    {
        ApplyGroundMaterial();
        ApplyBridgeMaterial();
        CreateWaterPlaneAndMaterial();
        SetEveningLighting();
        MarkSceneDirty();
        Debug.Log("[MaterialSetup] Materials and lighting configured successfully!");
    }

    static void ApplyGroundMaterial()
    {
        GameObject ground = GameObject.FindGameObjectWithTag("Ground");
        if (ground == null)
        {
            Debug.LogWarning("[MaterialSetup] No Ground object found!");
            return;
        }

        Renderer renderer = ground.GetComponent<Renderer>();
        if (renderer == null) return;

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.name = "Ground_Green";

        Texture2D grassTex = Resources.Load<Texture2D>("Textures/Grass");
        if (grassTex != null)
        {
            mat.mainTexture = grassTex;
            mat.SetTextureScale("_BaseMap", new Vector2(5f, 5f));
            Debug.Log("[MaterialSetup] Grass texture loaded from Resources/Textures/Grass");
        }
        else
        {
            mat.mainTexture = GenerateGrassTexture();
            mat.color = new Color(0.22f, 0.52f, 0.18f);
            Debug.Log("[MaterialSetup] Using procedural grass texture (no Grass.png found in Resources/Textures/)");
        }

        mat.SetFloat("_Smoothness", 0.1f);
        renderer.sharedMaterial = mat;
    }

    static void ApplyBridgeMaterial()
    {
        GameObject bridge = GameObject.Find("Bridge");
        if (bridge == null)
        {
            Debug.LogWarning("[MaterialSetup] No Bridge object found!");
            return;
        }

        Renderer renderer = bridge.GetComponent<Renderer>();
        if (renderer == null) return;

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.name = "Bridge_Wood";

        Texture2D woodTex = Resources.Load<Texture2D>("Textures/Wood");
        if (woodTex != null)
        {
            mat.mainTexture = woodTex;
            mat.SetTextureScale("_BaseMap", new Vector2(2f, 2f));
            Debug.Log("[MaterialSetup] Wood texture loaded from Resources/Textures/Wood");
        }
        else
        {
            mat.mainTexture = GenerateWoodTexture();
            mat.color = new Color(0.55f, 0.35f, 0.18f);
            Debug.Log("[MaterialSetup] Using procedural wood texture (no Wood.png found in Resources/Textures/)");
        }

        mat.SetFloat("_Smoothness", 0.3f);
        renderer.sharedMaterial = mat;
        ApplyWoodToChildren(bridge, mat);
    }

    static void ApplyWoodToChildren(GameObject parent, Material mat)
    {
        foreach (Transform child in parent.transform)
        {
            Renderer childRenderer = child.GetComponent<Renderer>();
            if (childRenderer != null)
            {
                childRenderer.sharedMaterial = mat;
            }
        }
    }

    static void CreateWaterPlaneAndMaterial()
    {
        Shader waterShader = Shader.Find("Custom/RealisticWater");
        if (waterShader == null)
        {
            Debug.LogError("[MaterialSetup] RealisticWater shader not found! Make sure RealisticWater.shader is in the project.");
            return;
        }

        Material waterMat = new Material(waterShader);
        waterMat.name = "Water_Realistic";

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

        GameObject existingWater = GameObject.Find("StreamWater");
        if (existingWater != null)
        {
            Object.DestroyImmediate(existingWater);
        }

        GameObject waterPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        waterPlane.name = "StreamWater";
        waterPlane.transform.position = new Vector3(0f, 0.15f, 20f);
        waterPlane.transform.localScale = new Vector3(0.8f, 1f, 0.6f);

        Renderer waterRenderer = waterPlane.GetComponent<Renderer>();
        waterRenderer.sharedMaterial = waterMat;

        Collider waterCollider = waterPlane.GetComponent<Collider>();
        if (waterCollider != null)
        {
            Object.DestroyImmediate(waterCollider);
        }

        BoxCollider triggerCollider = waterPlane.AddComponent<BoxCollider>();
        triggerCollider.isTrigger = true;

        WaterTrigger waterTrigger = waterPlane.AddComponent<WaterTrigger>();

        GameObject ditch = GameObject.Find("Ditch");
        if (ditch != null)
        {
            waterPlane.transform.parent = ditch.transform;
            waterPlane.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        }
    }

    static void SetEveningLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.38f, 0.22f, 0.35f);

        Material customSkybox = TryLoadCustomSkybox();
        if (customSkybox != null)
        {
            RenderSettings.skybox = customSkybox;
            Debug.Log("[MaterialSetup] Custom skybox loaded from Resources/Textures/Skybox");
        }
        else
        {
            RenderSettings.skybox = CreateSunsetSkybox();
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.012f;
        RenderSettings.fogColor = new Color(0.55f, 0.3f, 0.4f);

        Light sunLight = Object.FindObjectOfType<Light>();
        if (sunLight != null && sunLight.type == LightType.Directional)
        {
            sunLight.color = new Color(1f, 0.7f, 0.4f);
            sunLight.intensity = 0.75f;
            sunLight.shadows = LightShadows.Soft;
            sunLight.shadowStrength = 0.8f;
            sunLight.transform.rotation = Quaternion.Euler(15f, -45f, 0f);
        }
    }

    static Material TryLoadCustomSkybox()
    {
        Texture2D skyboxTex = Resources.Load<Texture2D>("Textures/Skybox");
        if (skyboxTex == null) return null;

        Shader panoramaShader = Shader.Find("Skybox/Panoramic");
        if (panoramaShader == null)
        {
            Debug.LogWarning("[MaterialSetup] Skybox/Panoramic shader not found. Falling back to procedural skybox.");
            return null;
        }

        Material skybox = new Material(panoramaShader);
        skybox.name = "CustomPanoramaSkybox";

        if (skybox.HasProperty("_MainTex"))
            skybox.SetTexture("_MainTex", skyboxTex);
        if (skybox.HasProperty("_Tint"))
            skybox.SetColor("_Tint", Color.white);
        if (skybox.HasProperty("_Exposure"))
            skybox.SetFloat("_Exposure", 1.0f);
        if (skybox.HasProperty("_Rotation"))
            skybox.SetFloat("_Rotation", 0f);
        if (skybox.HasProperty("_Mapping"))
            skybox.SetFloat("_Mapping", 0f);

        return skybox;
    }

    static Material CreateSunsetSkybox()
    {
        Shader skyboxShader = Shader.Find("Skybox/Procedural");
        if (skyboxShader == null)
        {
            skyboxShader = Shader.Find("RenderPipeline/Universal Render Pipeline/Skybox/Procedural");
        }

        if (skyboxShader == null)
        {
            return null;
        }

        Material skybox = new Material(skyboxShader);
        skybox.name = "SunsetSkybox";

        if (skybox.HasProperty("_SunTint"))
            skybox.SetColor("_SunTint", new Color(1f, 0.5f, 0.2f));
        if (skybox.HasProperty("_SkyTint"))
            skybox.SetColor("_SkyTint", new Color(0.6f, 0.3f, 0.5f));
        if (skybox.HasProperty("_GroundColor"))
            skybox.SetColor("_GroundColor", new Color(0.2f, 0.12f, 0.15f));
        if (skybox.HasProperty("_AtmosphereThickness"))
            skybox.SetFloat("_AtmosphereThickness", 1.5f);
        if (skybox.HasProperty("_Exposure"))
            skybox.SetFloat("_Exposure", 0.9f);

        return skybox;
    }

    static Texture2D GenerateGrassTexture()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, false);
        tex.name = "GrassProcedural";

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float noise = Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 0.15f;
                float r = 0.22f + noise * 0.5f;
                float g = 0.52f + noise;
                float b = 0.18f + noise * 0.3f;
                tex.SetPixel(x, y, new Color(r, g, b));
            }
        }

        tex.Apply();
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }

    static Texture2D GenerateWoodTexture()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, false);
        tex.name = "WoodProcedural";

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float grain = Mathf.PerlinNoise(x * 0.05f, y * 0.4f) * 0.12f;
                float stripe = Mathf.Sin(y * 0.8f) * 0.04f;
                float r = 0.55f + grain + stripe;
                float g = 0.35f + grain * 0.8f + stripe;
                float b = 0.18f + grain * 0.5f + stripe;
                tex.SetPixel(x, y, new Color(r, g, b));
            }
        }

        tex.Apply();
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }

    static void MarkSceneDirty()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
    }
}
