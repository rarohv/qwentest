using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.Rendering;

public static class URPSetup
{
    [MenuItem("GolfGame/Setup URP")]
    public static void SetupURP()
    {
        if (!AssetDatabase.IsValidFolder("Assets/URP"))
            AssetDatabase.CreateFolder("Assets", "URP");

        var rendererAsset = ScriptableObject.CreateInstance<UniversalRendererData>();
        rendererAsset.name = "URPRenderer";
        rendererAsset.renderingMode = RenderingMode.Forward;
        AssetDatabase.CreateAsset(rendererAsset, "Assets/URP/URPRenderer.asset");

        var urpAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
        urpAsset.name = "URPAsset";

        var serializedAsset = new SerializedObject(urpAsset);
        var rendererDataProp = serializedAsset.FindProperty("m_RendererDataList");
        rendererDataProp.arraySize = 1;
        rendererDataProp.GetArrayElementAtIndex(0).objectReferenceValue = rendererAsset;
        serializedAsset.ApplyModifiedProperties();

        AssetDatabase.CreateAsset(urpAsset, "Assets/URP/URPAsset.asset");

        GraphicsSettings.defaultRenderPipeline = urpAsset;
        QualitySettings.renderPipeline = urpAsset;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[URPSetup] URP configured successfully!");
    }
}
