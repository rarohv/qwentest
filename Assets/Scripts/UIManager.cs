using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private GolfManager golfManager;
    private CameraController cameraController;
    private AudioManager audioManager;

    private Text strokeText;
    private Slider sensitivitySlider;
    private Text sensitivityValue;
    private int lastStrokeCount = -1;

    private bool settingsOpen;
    private Slider fovSlider;
    private Text fovValue;
    private Slider volumeSlider;
    private Text volumeValue;
    private Slider sensSettingsSlider;
    private Text sensSettingsValue;

    private Canvas gameCanvas;
    private Canvas settingsCanvas;

    void Start()
    {
        golfManager = FindObjectOfType<GolfManager>();
        cameraController = FindObjectOfType<CameraController>();
        audioManager = FindObjectOfType<AudioManager>();
        CreateUI();
    }

    void Update()
    {
        UpdateStrokeCounter();
        HandleSettingsToggle();
    }

    void HandleSettingsToggle()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            settingsOpen = !settingsOpen;
            settingsCanvas.gameObject.SetActive(settingsOpen);

            if (cameraController != null)
                cameraController.LockCursor(!settingsOpen);

            if (settingsOpen)
            {
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = 1f;
            }
        }
    }

    void CreateUI()
    {
        gameCanvas = CreateCanvas("GameCanvas", 100);
        CreateStrokeCounter(gameCanvas.transform);

        settingsCanvas = CreateCanvas("SettingsCanvas", 200);
        settingsCanvas.gameObject.SetActive(false);
        CreateSettingsPanel(settingsCanvas.transform);
    }

    Canvas CreateCanvas(string name, int sortOrder)
    {
        GameObject canvasObj = new GameObject(name);
        canvasObj.transform.SetParent(transform);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        return canvas;
    }

    void CreateStrokeCounter(Transform parent)
    {
        GameObject panelObj = new GameObject("StrokePanel");
        panelObj.transform.SetParent(parent, false);

        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(20f, -60f);
        panelRect.sizeDelta = new Vector2(280f, 70f);

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject textObj = new GameObject("StrokeText");
        textObj.transform.SetParent(panelRect, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(15f, 10f);
        textRect.offsetMax = new Vector2(-15f, -10f);

        strokeText = textObj.AddComponent<Text>();
        strokeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        strokeText.fontSize = 32;
        strokeText.fontStyle = FontStyle.Bold;
        strokeText.color = Color.white;
        strokeText.alignment = TextAnchor.MiddleLeft;
        strokeText.text = "Strokes: 0";
    }

    void CreateSettingsPanel(Transform parent)
    {
        GameObject bgObj = new GameObject("SettingsBG");
        bgObj.transform.SetParent(parent, false);

        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.7f);

        GameObject panelObj = new GameObject("SettingsPanel");
        panelObj.transform.SetParent(parent, false);

        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(500f, 450f);

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panelRect, false);

        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.85f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(20f, 0f);
        titleRect.offsetMax = new Vector2(-20f, -5f);

        Text titleText = titleObj.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 30;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.text = "SETTINGS";

        fovSlider = CreateSettingRow(panelRect, "FOV", 50f, 110f, PlayerPrefs.GetFloat("CameraFOV", 65f), 0.15f, out fovValue, OnFOVChanged);
        sensSettingsSlider = CreateSettingRow(panelRect, "Mouse Sensitivity", 0.3f, 5f, PlayerPrefs.GetFloat("MouseSensitivity", 1.25f), 0.38f, out sensSettingsValue, OnSensitivityChanged);
        volumeSlider = CreateSettingRow(panelRect, "Volume", 0f, 1f, PlayerPrefs.GetFloat("MasterVolume", 1f), 0.61f, out volumeValue, OnVolumeChanged);

        GameObject closeBtnObj = new GameObject("CloseButton");
        closeBtnObj.transform.SetParent(panelRect, false);

        RectTransform closeBtnRect = closeBtnObj.AddComponent<RectTransform>();
        closeBtnRect.anchorMin = new Vector2(0.2f, 0.04f);
        closeBtnRect.anchorMax = new Vector2(0.8f, 0.14f);
        closeBtnRect.offsetMin = Vector2.zero;
        closeBtnRect.offsetMax = Vector2.zero;

        Image closeBtnImg = closeBtnObj.AddComponent<Image>();
        closeBtnImg.color = new Color(0.2f, 0.5f, 0.9f);

        Button closeBtn = closeBtnObj.AddComponent<Button>();
        closeBtn.targetGraphic = closeBtnImg;
        closeBtn.onClick.AddListener(CloseSettings);

        GameObject closeLabelObj = new GameObject("Label");
        closeLabelObj.transform.SetParent(closeBtnRect, false);

        RectTransform closeLabelRect = closeLabelObj.AddComponent<RectTransform>();
        closeLabelRect.anchorMin = Vector2.zero;
        closeLabelRect.anchorMax = Vector2.one;
        closeLabelRect.offsetMin = Vector2.zero;
        closeLabelRect.offsetMax = Vector2.zero;

        Text closeLabel = closeLabelObj.AddComponent<Text>();
        closeLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        closeLabel.fontSize = 20;
        closeLabel.fontStyle = FontStyle.Bold;
        closeLabel.color = Color.white;
        closeLabel.alignment = TextAnchor.MiddleCenter;
        closeLabel.text = "CLOSE (ESC)";
    }

    Slider CreateSettingRow(Transform parent, string label, float min, float max, float current, float verticalPos, out Text valueText, UnityEngine.Events.UnityAction<float> onChanged)
    {
        GameObject labelObj = new GameObject(label + "Label");
        labelObj.transform.SetParent(parent, false);

        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, verticalPos);
        labelRect.anchorMax = new Vector2(0.4f, verticalPos + 0.1f);
        labelRect.offsetMin = new Vector2(20f, 0f);
        labelRect.offsetMax = new Vector2(-5f, -5f);

        Text labelText = labelObj.AddComponent<Text>();
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 18;
        labelText.fontStyle = FontStyle.Bold;
        labelText.color = new Color(0.7f, 0.8f, 1f);
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.text = label;

        GameObject valObj = new GameObject(label + "Value");
        valObj.transform.SetParent(parent, false);

        RectTransform valRect = valObj.AddComponent<RectTransform>();
        valRect.anchorMin = new Vector2(0.7f, verticalPos);
        valRect.anchorMax = new Vector2(1f, verticalPos + 0.1f);
        valRect.offsetMin = new Vector2(5f, 0f);
        valRect.offsetMax = new Vector2(-20f, -5f);

        valueText = valObj.AddComponent<Text>();
        valueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        valueText.fontSize = 18;
        valueText.fontStyle = FontStyle.Bold;
        valueText.color = Color.white;
        valueText.alignment = TextAnchor.MiddleRight;
        valueText.text = current.ToString("F1");

        GameObject sliderObj = new GameObject(label + "Slider");
        sliderObj.transform.SetParent(parent, false);

        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.4f, verticalPos);
        sliderRect.anchorMax = new Vector2(0.7f, verticalPos + 0.1f);
        sliderRect.offsetMin = new Vector2(5f, 2f);
        sliderRect.offsetMax = new Vector2(-5f, -2f);

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = false;
        slider.value = current;

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderRect, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.25f);

        GameObject fillAreaObj = new GameObject("Fill Area");
        fillAreaObj.transform.SetParent(sliderRect, false);
        RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillAreaRect, false);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = new Color(0.3f, 0.6f, 1f);

        GameObject handleAreaObj = new GameObject("Handle Area");
        handleAreaObj.transform.SetParent(sliderRect, false);
        RectTransform handleAreaRect = handleAreaObj.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = Vector2.zero;
        handleAreaRect.offsetMax = Vector2.zero;

        GameObject handleObj = new GameObject("Handle");
        handleObj.transform.SetParent(handleAreaRect, false);
        RectTransform handleRect = handleObj.AddComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(18f, 18f);
        Image handleImage = handleObj.AddComponent<Image>();
        handleImage.color = Color.white;

        slider.targetGraphic = handleImage;
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.direction = Slider.Direction.LeftToRight;

        slider.onValueChanged.AddListener(onChanged);

        return slider;
    }

    void UpdateStrokeCounter()
    {
        if (golfManager == null || strokeText == null) return;

        int currentStrokes = golfManager.StrokeCount;
        if (currentStrokes != lastStrokeCount)
        {
            lastStrokeCount = currentStrokes;
            strokeText.text = string.Format("Strokes: {0}", currentStrokes);
        }
    }

    void OnFOVChanged(float value)
    {
        if (fovValue != null)
            fovValue.text = value.ToString("F0");

        if (cameraController != null)
            cameraController.SetFOV(value);
    }

    void OnSensitivityChanged(float value)
    {
        PlayerPrefs.SetFloat("MouseSensitivity", value);
        PlayerPrefs.Save();

        if (sensSettingsValue != null)
            sensSettingsValue.text = value.ToString("F2");

        if (cameraController != null)
            cameraController.SetPlayerMultiplier(value);
    }

    void OnVolumeChanged(float value)
    {
        if (volumeValue != null)
            volumeValue.text = value.ToString("F0") + "%";

        if (audioManager != null)
            audioManager.SetMasterVolume(value);
    }

    void CloseSettings()
    {
        settingsOpen = false;
        settingsCanvas.gameObject.SetActive(false);
        Time.timeScale = 1f;

        if (cameraController != null)
            cameraController.LockCursor(true);
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
