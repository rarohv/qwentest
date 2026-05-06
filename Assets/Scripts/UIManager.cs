using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private float minSensitivity = 0.25f;
    [SerializeField] private float maxSensitivity = 5f;
    [SerializeField] private float minFOV = 40f;
    [SerializeField] private float maxFOV = 90f;

    private GolfManager golfManager;
    private LevelManager levelManager;
    private CameraController cameraController;
    private AudioManager audioManager;

    private Canvas canvas;

    private GameObject hudRoot;
    private Text holeInfoText;
    private Text strokesText;
    private Text distanceText;
    private Text hintText;
    private Text holeCompleteText;
    private Text holeCompleteSubtext;
    private GameObject holeCompletePanel;
    private GameObject gameOverPanel;
    private Text gameOverText;
    private RectTransform powerBarBackground;
    private RectTransform powerBarFill;
    private RectTransform directionArrow;

    private GameObject settingsPanel;
    private Text fovValueText;
    private Text sensValueText;
    private Text volumeValueText;
    private Text musicVolumeValueText;
    private Text trackNameText;
    private Text resolutionLabelText;
    private Text fullscreenStateText;

    private bool settingsOpen;
    private List<Resolution> uniqueResolutions = new List<Resolution>();
    private int selectedResolutionIndex;

    void Start()
    {
        golfManager = FindObjectOfType<GolfManager>();
        levelManager = FindObjectOfType<LevelManager>();
        cameraController = FindObjectOfType<CameraController>();
        audioManager = FindObjectOfType<AudioManager>();

        EnsureEventSystem();
        BuildUniqueResolutions();
        CreateUI();
        ApplyInitialDisplaySettings();
    }

    void EnsureEventSystem()
    {
        EventSystem es = FindObjectOfType<EventSystem>();
        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }
    }

    void BuildUniqueResolutions()
    {
        Resolution[] all = Screen.resolutions;
        HashSet<long> seen = new HashSet<long>();
        for (int i = 0; i < all.Length; i++)
        {
            long key = ((long)all[i].width << 20) | (long)all[i].height;
            if (seen.Add(key))
            {
                uniqueResolutions.Add(all[i]);
            }
        }
        if (uniqueResolutions.Count == 0)
        {
            uniqueResolutions.Add(new Resolution { width = Screen.width, height = Screen.height, refreshRate = 60 });
        }
        selectedResolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", uniqueResolutions.Count - 1);
        selectedResolutionIndex = Mathf.Clamp(selectedResolutionIndex, 0, uniqueResolutions.Count - 1);
    }

    void ApplyInitialDisplaySettings()
    {
        bool fullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        if (selectedResolutionIndex < uniqueResolutions.Count)
        {
            Resolution r = uniqueResolutions[selectedResolutionIndex];
            Screen.SetResolution(r.width, r.height, fullscreen);
        }
        else
        {
            Screen.fullScreen = fullscreen;
        }
    }

    void CreateUI()
    {
        GameObject canvasObj = new GameObject("UICanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster gr = canvasObj.AddComponent<GraphicRaycaster>();
        gr.ignoreReversedGraphics = true;

        if (golfManager != null)
        {
            CreateHUD();
            CreateHoleCompletePanel();
            CreateGameOverPanel();
        }

        CreateSettingsPanel();
    }

    Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor anchor, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Text t = obj.AddComponent<Text>();
        t.text = content;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        t.fontSize = fontSize;
        t.alignment = anchor;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        Outline outline = obj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.7f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        return t;
    }

    Image CreatePanelImage(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = true;
        return img;
    }

    void CreateHUD()
    {
        hudRoot = new GameObject("HUD");
        hudRoot.transform.SetParent(canvas.transform, false);
        RectTransform hudRT = hudRoot.AddComponent<RectTransform>();
        hudRT.anchorMin = Vector2.zero;
        hudRT.anchorMax = Vector2.one;
        hudRT.offsetMin = Vector2.zero;
        hudRT.offsetMax = Vector2.zero;

        holeInfoText = CreateText(hudRoot.transform, "HoleInfo", "", 36, TextAnchor.UpperCenter, Color.white);
        RectTransform infoRT = holeInfoText.rectTransform;
        infoRT.anchorMin = new Vector2(0.5f, 1f);
        infoRT.anchorMax = new Vector2(0.5f, 1f);
        infoRT.pivot = new Vector2(0.5f, 1f);
        infoRT.anchoredPosition = new Vector2(0f, -20f);
        infoRT.sizeDelta = new Vector2(900f, 80f);

        strokesText = CreateText(hudRoot.transform, "Strokes", "", 28, TextAnchor.UpperCenter, Color.white);
        RectTransform sRT = strokesText.rectTransform;
        sRT.anchorMin = new Vector2(0.5f, 1f);
        sRT.anchorMax = new Vector2(0.5f, 1f);
        sRT.pivot = new Vector2(0.5f, 1f);
        sRT.anchoredPosition = new Vector2(0f, -70f);
        sRT.sizeDelta = new Vector2(600f, 40f);

        distanceText = CreateText(hudRoot.transform, "Distance", "", 26, TextAnchor.LowerLeft, Color.white);
        RectTransform dRT = distanceText.rectTransform;
        dRT.anchorMin = new Vector2(0f, 0f);
        dRT.anchorMax = new Vector2(0f, 0f);
        dRT.pivot = new Vector2(0f, 0f);
        dRT.anchoredPosition = new Vector2(40f, 40f);
        dRT.sizeDelta = new Vector2(500f, 50f);

        hintText = CreateText(hudRoot.transform, "Hint", "", 32, TextAnchor.MiddleCenter, Color.white);
        RectTransform hRT = hintText.rectTransform;
        hRT.anchorMin = new Vector2(0.5f, 0f);
        hRT.anchorMax = new Vector2(0.5f, 0f);
        hRT.pivot = new Vector2(0.5f, 0f);
        hRT.anchoredPosition = new Vector2(0f, 220f);
        hRT.sizeDelta = new Vector2(800f, 60f);

        CreatePowerBar();
        CreateDirectionArrow();
    }

    void CreatePowerBar()
    {
        Image bg = CreatePanelImage(hudRoot.transform, "PowerBarBG", new Color(0f, 0f, 0f, 0.55f));
        powerBarBackground = bg.rectTransform;
        powerBarBackground.anchorMin = new Vector2(0.5f, 0f);
        powerBarBackground.anchorMax = new Vector2(0.5f, 0f);
        powerBarBackground.pivot = new Vector2(0.5f, 0.5f);
        powerBarBackground.anchoredPosition = new Vector2(0f, 130f);
        powerBarBackground.sizeDelta = new Vector2(420f, 28f);
        bg.raycastTarget = false;

        Image fill = CreatePanelImage(powerBarBackground, "PowerBarFill", new Color(0.95f, 0.6f, 0.15f, 0.95f));
        powerBarFill = fill.rectTransform;
        powerBarFill.anchorMin = new Vector2(0f, 0f);
        powerBarFill.anchorMax = new Vector2(0f, 1f);
        powerBarFill.pivot = new Vector2(0f, 0.5f);
        powerBarFill.anchoredPosition = new Vector2(2f, 0f);
        powerBarFill.sizeDelta = new Vector2(0f, -4f);
        fill.raycastTarget = false;

        Text label = CreateText(powerBarBackground, "PowerLabel", "POWER", 18, TextAnchor.MiddleCenter, Color.white);
        RectTransform labelRT = label.rectTransform;
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        powerBarBackground.gameObject.SetActive(false);
    }

    void CreateDirectionArrow()
    {
        GameObject arrowObj = new GameObject("DirectionArrow");
        arrowObj.transform.SetParent(hudRoot.transform, false);
        Image img = arrowObj.AddComponent<Image>();
        img.color = new Color(1f, 0.95f, 0.2f, 0.9f);
        img.raycastTarget = false;
        img.sprite = MakeArrowSprite();
        directionArrow = img.rectTransform;
        directionArrow.anchorMin = new Vector2(0.5f, 0.5f);
        directionArrow.anchorMax = new Vector2(0.5f, 0.5f);
        directionArrow.pivot = new Vector2(0.5f, 0.5f);
        directionArrow.anchoredPosition = new Vector2(0f, 180f);
        directionArrow.sizeDelta = new Vector2(64f, 64f);
        directionArrow.gameObject.SetActive(false);
    }

    Sprite MakeArrowSprite()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color clear = new Color(0f, 0f, 0f, 0f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                tex.SetPixel(x, y, clear);
            }
        }
        for (int y = 0; y < size; y++)
        {
            int width = (int)((y / (float)size) * (size * 0.5f));
            int cx = size / 2;
            for (int x = cx - width; x <= cx + width; x++)
            {
                if (x < 0 || x >= size) continue;
                tex.SetPixel(x, y, Color.white);
            }
        }
        tex.Apply();
        Sprite s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return s;
    }

    void CreateHoleCompletePanel()
    {
        Image bg = CreatePanelImage(canvas.transform, "HoleCompletePanel", new Color(0f, 0f, 0f, 0.6f));
        holeCompletePanel = bg.gameObject;
        RectTransform rt = bg.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(700f, 280f);

        holeCompleteText = CreateText(rt, "Title", "", 56, TextAnchor.UpperCenter, Color.yellow);
        RectTransform titleRT = holeCompleteText.rectTransform;
        titleRT.anchorMin = new Vector2(0.5f, 1f);
        titleRT.anchorMax = new Vector2(0.5f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -20f);
        titleRT.sizeDelta = new Vector2(680f, 80f);

        holeCompleteSubtext = CreateText(rt, "Sub", "", 32, TextAnchor.MiddleCenter, Color.white);
        RectTransform subRT = holeCompleteSubtext.rectTransform;
        subRT.anchorMin = new Vector2(0.5f, 0.5f);
        subRT.anchorMax = new Vector2(0.5f, 0.5f);
        subRT.pivot = new Vector2(0.5f, 0.5f);
        subRT.anchoredPosition = new Vector2(0f, -10f);
        subRT.sizeDelta = new Vector2(680f, 80f);

        holeCompletePanel.SetActive(false);
    }

    void CreateGameOverPanel()
    {
        Image bg = CreatePanelImage(canvas.transform, "GameOverPanel", new Color(0f, 0f, 0f, 0.75f));
        gameOverPanel = bg.gameObject;
        RectTransform rt = bg.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(800f, 460f);

        Text title = CreateText(rt, "Title", "Game Complete!", 56, TextAnchor.UpperCenter, Color.yellow);
        RectTransform titleRT = title.rectTransform;
        titleRT.anchorMin = new Vector2(0.5f, 1f);
        titleRT.anchorMax = new Vector2(0.5f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -25f);
        titleRT.sizeDelta = new Vector2(780f, 80f);

        gameOverText = CreateText(rt, "Summary", "", 30, TextAnchor.MiddleCenter, Color.white);
        RectTransform sumRT = gameOverText.rectTransform;
        sumRT.anchorMin = new Vector2(0.5f, 0.5f);
        sumRT.anchorMax = new Vector2(0.5f, 0.5f);
        sumRT.pivot = new Vector2(0.5f, 0.5f);
        sumRT.anchoredPosition = new Vector2(0f, 30f);
        sumRT.sizeDelta = new Vector2(780f, 120f);

        CreateMenuButton(rt, "PlayAgainBtn", "Play Again", new Vector2(-160f, -160f), () =>
        {
            if (golfManager != null) golfManager.RestartGame();
        });
        CreateMenuButton(rt, "MainMenuBtn", "Main Menu", new Vector2(160f, -160f), () =>
        {
            if (golfManager != null) golfManager.ReturnToMainMenu();
        });

        gameOverPanel.SetActive(false);
    }

    Button CreateMenuButton(Transform parent, string name, string label, Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.4f, 0.25f, 0.95f);
        img.raycastTarget = true;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(0.3f, 0.55f, 0.35f, 1f);
        colors.pressedColor = new Color(0.15f, 0.3f, 0.18f, 1f);
        btn.colors = colors;

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(260f, 70f);

        Text label_ = CreateText(rt, "Label", label, 28, TextAnchor.MiddleCenter, Color.white);
        RectTransform lblRT = label_.rectTransform;
        lblRT.anchorMin = Vector2.zero;
        lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = Vector2.zero;
        lblRT.offsetMax = Vector2.zero;

        btn.onClick.AddListener(onClick);
        return btn;
    }

    void CreateSettingsPanel()
    {
        Image bg = CreatePanelImage(canvas.transform, "SettingsPanel", new Color(0f, 0f, 0f, 0.85f));
        settingsPanel = bg.gameObject;
        RectTransform rt = bg.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(820f, 720f);

        Text title = CreateText(rt, "Title", "Settings", 44, TextAnchor.UpperCenter, Color.white);
        RectTransform titleRT = title.rectTransform;
        titleRT.anchorMin = new Vector2(0.5f, 1f);
        titleRT.anchorMax = new Vector2(0.5f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -20f);
        titleRT.sizeDelta = new Vector2(800f, 60f);

        float row = -100f;
        float rowStep = 75f;

        float currentFOV = PlayerPrefs.GetFloat("CameraFOV", 65f);
        CreateSliderRow(rt, "FOV", minFOV, maxFOV, currentFOV, row, false, out fovValueText, v =>
        {
            if (cameraController != null) cameraController.SetFOV(v);
            fovValueText.text = ((int)v).ToString();
        });
        row -= rowStep;

        float currentSens = PlayerPrefs.GetFloat("MouseSensitivity", 1.25f);
        CreateSliderRow(rt, "Sensitivity", minSensitivity, maxSensitivity, currentSens, row, false, out sensValueText, v =>
        {
            PlayerPrefs.SetFloat("MouseSensitivity", v);
            PlayerPrefs.Save();
            if (cameraController != null) cameraController.SetPlayerMultiplier(v);
            sensValueText.text = v.ToString("F2");
        });
        row -= rowStep;

        float currentVol = PlayerPrefs.GetFloat("MasterVolume", 1f);
        CreateSliderRow(rt, "Master Volume", 0f, 1f, currentVol, row, false, out volumeValueText, v =>
        {
            if (audioManager != null) audioManager.SetMasterVolume(v);
            volumeValueText.text = ((int)(v * 100f)).ToString() + "%";
        });
        row -= rowStep;

        float currentMusic = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        CreateSliderRow(rt, "Music Volume", 0f, 1f, currentMusic, row, false, out musicVolumeValueText, v =>
        {
            if (audioManager != null) audioManager.SetMusicVolume(v);
            musicVolumeValueText.text = ((int)(v * 100f)).ToString() + "%";
        });
        row -= rowStep;

        CreateMusicTrackRow(rt, row);
        row -= rowStep;

        CreateResolutionRow(rt, row);
        row -= rowStep;

        CreateFullscreenRow(rt, row);
        row -= rowStep;

        CreateMenuButton(rt, "CloseBtn", "Close", new Vector2(-130f, -310f), () => CloseSettings());
        if (golfManager == null)
        {
            CreateMenuButton(rt, "BackToMenuBtn", "Back", new Vector2(130f, -310f), () => CloseSettings());
        }
        else
        {
            CreateMenuButton(rt, "MainMenuBtn", "Main Menu", new Vector2(130f, -310f), () =>
            {
                CloseSettings();
                if (golfManager != null) golfManager.ReturnToMainMenu();
            });
        }

        settingsPanel.SetActive(false);
    }

    Slider CreateSliderRow(Transform parent, string label, float min, float max, float current, float verticalPos, bool wholeNumbers, out Text valueText, UnityEngine.Events.UnityAction<float> onChanged)
    {
        GameObject row = new GameObject(label + "Row");
        row.transform.SetParent(parent, false);
        RectTransform rowRT = row.AddComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0.5f, 1f);
        rowRT.anchorMax = new Vector2(0.5f, 1f);
        rowRT.pivot = new Vector2(0.5f, 1f);
        rowRT.anchoredPosition = new Vector2(0f, verticalPos);
        rowRT.sizeDelta = new Vector2(760f, 60f);

        Text labelText = CreateText(row.transform, "Label", label, 24, TextAnchor.MiddleLeft, Color.white);
        RectTransform lblRT = labelText.rectTransform;
        lblRT.anchorMin = new Vector2(0f, 0f);
        lblRT.anchorMax = new Vector2(0f, 1f);
        lblRT.pivot = new Vector2(0f, 0.5f);
        lblRT.anchoredPosition = new Vector2(20f, 0f);
        lblRT.sizeDelta = new Vector2(220f, 0f);

        GameObject sliderObj = new GameObject(label + "Slider");
        sliderObj.transform.SetParent(row.transform, false);
        RectTransform sliderRT = sliderObj.AddComponent<RectTransform>();
        sliderRT.anchorMin = new Vector2(0f, 0.5f);
        sliderRT.anchorMax = new Vector2(0f, 0.5f);
        sliderRT.pivot = new Vector2(0f, 0.5f);
        sliderRT.anchoredPosition = new Vector2(250f, 0f);
        sliderRT.sizeDelta = new Vector2(380f, 30f);

        Image bg = sliderObj.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.18f, 0.18f, 1f);
        bg.raycastTarget = true;

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRT = fillArea.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRT.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRT.offsetMin = new Vector2(10f, 0f);
        fillAreaRT.offsetMax = new Vector2(-10f, 0f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRT = fill.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.4f, 0.7f, 0.45f, 1f);
        fillImg.raycastTarget = true;

        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRT = handleArea.AddComponent<RectTransform>();
        handleAreaRT.anchorMin = Vector2.zero;
        handleAreaRT.anchorMax = Vector2.one;
        handleAreaRT.offsetMin = new Vector2(10f, 0f);
        handleAreaRT.offsetMax = new Vector2(-10f, 0f);

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRT = handle.AddComponent<RectTransform>();
        handleRT.anchorMin = new Vector2(0f, 0f);
        handleRT.anchorMax = new Vector2(0f, 1f);
        handleRT.pivot = new Vector2(0.5f, 0.5f);
        handleRT.sizeDelta = new Vector2(22f, 0f);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        handleImg.raycastTarget = true;

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.targetGraphic = handleImg;
        slider.fillRect = fillRT;
        slider.handleRect = handleRT;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = wholeNumbers;
        slider.value = current;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        slider.onValueChanged.AddListener(onChanged);

        valueText = CreateText(row.transform, "Value", "", 24, TextAnchor.MiddleRight, Color.white);
        RectTransform valRT = valueText.rectTransform;
        valRT.anchorMin = new Vector2(1f, 0f);
        valRT.anchorMax = new Vector2(1f, 1f);
        valRT.pivot = new Vector2(1f, 0.5f);
        valRT.anchoredPosition = new Vector2(-20f, 0f);
        valRT.sizeDelta = new Vector2(140f, 0f);
        valueText.text = wholeNumbers ? ((int)current).ToString() : current.ToString("F2");

        onChanged.Invoke(current);
        return slider;
    }

    void CreateMusicTrackRow(Transform parent, float verticalPos)
    {
        GameObject row = new GameObject("MusicRow");
        row.transform.SetParent(parent, false);
        RectTransform rowRT = row.AddComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0.5f, 1f);
        rowRT.anchorMax = new Vector2(0.5f, 1f);
        rowRT.pivot = new Vector2(0.5f, 1f);
        rowRT.anchoredPosition = new Vector2(0f, verticalPos);
        rowRT.sizeDelta = new Vector2(760f, 60f);

        Text label = CreateText(row.transform, "Label", "Music Track", 24, TextAnchor.MiddleLeft, Color.white);
        RectTransform lblRT = label.rectTransform;
        lblRT.anchorMin = new Vector2(0f, 0f);
        lblRT.anchorMax = new Vector2(0f, 1f);
        lblRT.pivot = new Vector2(0f, 0.5f);
        lblRT.anchoredPosition = new Vector2(20f, 0f);
        lblRT.sizeDelta = new Vector2(220f, 0f);

        CreateMenuButton(row.transform, "PrevTrack", "<", new Vector2(-200f, 0f), () =>
        {
            if (audioManager != null)
            {
                audioManager.PreviousTrack();
                UpdateTrackName();
            }
        }).GetComponent<RectTransform>().sizeDelta = new Vector2(60f, 44f);

        trackNameText = CreateText(row.transform, "TrackName", "", 22, TextAnchor.MiddleCenter, Color.white);
        RectTransform tnRT = trackNameText.rectTransform;
        tnRT.anchorMin = new Vector2(0.5f, 0f);
        tnRT.anchorMax = new Vector2(0.5f, 1f);
        tnRT.pivot = new Vector2(0.5f, 0.5f);
        tnRT.anchoredPosition = new Vector2(70f, 0f);
        tnRT.sizeDelta = new Vector2(280f, 0f);

        CreateMenuButton(row.transform, "NextTrack", ">", new Vector2(290f, 0f), () =>
        {
            if (audioManager != null)
            {
                audioManager.NextTrack();
                UpdateTrackName();
            }
        }).GetComponent<RectTransform>().sizeDelta = new Vector2(60f, 44f);

        UpdateTrackName();
    }

    void UpdateTrackName()
    {
        if (trackNameText == null || audioManager == null) return;
        trackNameText.text = audioManager.GetTrackName(audioManager.SelectedTrackIndex);
    }

    void CreateResolutionRow(Transform parent, float verticalPos)
    {
        GameObject row = new GameObject("ResolutionRow");
        row.transform.SetParent(parent, false);
        RectTransform rowRT = row.AddComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0.5f, 1f);
        rowRT.anchorMax = new Vector2(0.5f, 1f);
        rowRT.pivot = new Vector2(0.5f, 1f);
        rowRT.anchoredPosition = new Vector2(0f, verticalPos);
        rowRT.sizeDelta = new Vector2(760f, 60f);

        Text label = CreateText(row.transform, "Label", "Resolution", 24, TextAnchor.MiddleLeft, Color.white);
        RectTransform lblRT = label.rectTransform;
        lblRT.anchorMin = new Vector2(0f, 0f);
        lblRT.anchorMax = new Vector2(0f, 1f);
        lblRT.pivot = new Vector2(0f, 0.5f);
        lblRT.anchoredPosition = new Vector2(20f, 0f);
        lblRT.sizeDelta = new Vector2(220f, 0f);

        CreateMenuButton(row.transform, "PrevRes", "<", new Vector2(-200f, 0f), () =>
        {
            CycleResolution(-1);
        }).GetComponent<RectTransform>().sizeDelta = new Vector2(60f, 44f);

        resolutionLabelText = CreateText(row.transform, "ResLabel", "", 22, TextAnchor.MiddleCenter, Color.white);
        RectTransform rrRT = resolutionLabelText.rectTransform;
        rrRT.anchorMin = new Vector2(0.5f, 0f);
        rrRT.anchorMax = new Vector2(0.5f, 1f);
        rrRT.pivot = new Vector2(0.5f, 0.5f);
        rrRT.anchoredPosition = new Vector2(70f, 0f);
        rrRT.sizeDelta = new Vector2(280f, 0f);

        CreateMenuButton(row.transform, "NextRes", ">", new Vector2(290f, 0f), () =>
        {
            CycleResolution(1);
        }).GetComponent<RectTransform>().sizeDelta = new Vector2(60f, 44f);

        UpdateResolutionLabel();
    }

    void CycleResolution(int direction)
    {
        if (uniqueResolutions.Count == 0) return;
        selectedResolutionIndex = (selectedResolutionIndex + direction + uniqueResolutions.Count) % uniqueResolutions.Count;
        Resolution r = uniqueResolutions[selectedResolutionIndex];
        bool fullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        Screen.SetResolution(r.width, r.height, fullscreen);
        PlayerPrefs.SetInt("ResolutionIndex", selectedResolutionIndex);
        PlayerPrefs.Save();
        UpdateResolutionLabel();
    }

    void UpdateResolutionLabel()
    {
        if (resolutionLabelText == null) return;
        if (uniqueResolutions.Count == 0)
        {
            resolutionLabelText.text = Screen.width + " x " + Screen.height;
            return;
        }
        Resolution r = uniqueResolutions[selectedResolutionIndex];
        resolutionLabelText.text = r.width + " x " + r.height;
    }

    void CreateFullscreenRow(Transform parent, float verticalPos)
    {
        GameObject row = new GameObject("FullscreenRow");
        row.transform.SetParent(parent, false);
        RectTransform rowRT = row.AddComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0.5f, 1f);
        rowRT.anchorMax = new Vector2(0.5f, 1f);
        rowRT.pivot = new Vector2(0.5f, 1f);
        rowRT.anchoredPosition = new Vector2(0f, verticalPos);
        rowRT.sizeDelta = new Vector2(760f, 60f);

        Text label = CreateText(row.transform, "Label", "Fullscreen", 24, TextAnchor.MiddleLeft, Color.white);
        RectTransform lblRT = label.rectTransform;
        lblRT.anchorMin = new Vector2(0f, 0f);
        lblRT.anchorMax = new Vector2(0f, 1f);
        lblRT.pivot = new Vector2(0f, 0.5f);
        lblRT.anchoredPosition = new Vector2(20f, 0f);
        lblRT.sizeDelta = new Vector2(220f, 0f);

        fullscreenStateText = CreateText(row.transform, "State", "", 22, TextAnchor.MiddleCenter, Color.white);
        RectTransform stateRT = fullscreenStateText.rectTransform;
        stateRT.anchorMin = new Vector2(0.5f, 0f);
        stateRT.anchorMax = new Vector2(0.5f, 1f);
        stateRT.pivot = new Vector2(0.5f, 0.5f);
        stateRT.anchoredPosition = new Vector2(70f, 0f);
        stateRT.sizeDelta = new Vector2(280f, 0f);

        CreateMenuButton(row.transform, "ToggleFullscreen", "Toggle", new Vector2(290f, 0f), () =>
        {
            bool current = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
            bool next = !current;
            PlayerPrefs.SetInt("Fullscreen", next ? 1 : 0);
            PlayerPrefs.Save();
            Screen.fullScreen = next;
            UpdateFullscreenLabel();
        }).GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 44f);

        UpdateFullscreenLabel();
    }

    void UpdateFullscreenLabel()
    {
        if (fullscreenStateText == null) return;
        bool fullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        fullscreenStateText.text = fullscreen ? "ON" : "OFF";
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && CanToggleSettings())
        {
            ToggleSettings();
        }

        UpdateHUD();
    }

    bool CanToggleSettings()
    {
        if (settingsOpen) return true;
        if (golfManager == null) return true;
        GolfManager.GolfState s = golfManager.CurrentState;
        return s != GolfManager.GolfState.Golfing
               && s != GolfManager.GolfState.Charging
               && s != GolfManager.GolfState.GameOver;
    }

    public void ToggleSettings()
    {
        if (settingsPanel == null) return;
        settingsOpen = !settingsOpen;
        settingsPanel.SetActive(settingsOpen);
        if (settingsOpen)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = 1f;
            if (golfManager != null && golfManager.CurrentState != GolfManager.GolfState.GameOver)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    public void CloseSettings()
    {
        if (!settingsOpen) return;
        ToggleSettings();
    }

    void UpdateHUD()
    {
        if (golfManager == null || levelManager == null) return;

        if (holeInfoText != null)
        {
            holeInfoText.text = string.Format("Hole {0}: {1}   |   Par {2}", golfManager.HoleNumber, golfManager.HoleName, golfManager.HolePar);
        }
        if (strokesText != null)
        {
            strokesText.text = "Strokes: " + golfManager.StrokeCount;
        }
        if (distanceText != null)
        {
            float dist = Vector3.Distance(golfManager.BallPosition, golfManager.HolePosition);
            distanceText.text = string.Format("{0:0.0} m to hole", dist);
        }

        UpdateHint();
        UpdatePowerBar();
        UpdateDirectionArrow();
        UpdateHoleCompleteUI();
    }

    void UpdateHint()
    {
        if (hintText == null) return;
        switch (golfManager.CurrentState)
        {
            case GolfManager.GolfState.Walking:
                if (golfManager.BallReady && IsPlayerNearBall())
                    hintText.text = "Press [E] to Golf";
                else
                    hintText.text = "";
                break;
            case GolfManager.GolfState.Golfing:
                hintText.text = "Hold LMB to charge, release to swing  -  ESC to exit";
                break;
            case GolfManager.GolfState.Charging:
                hintText.text = "Release LMB to swing";
                break;
            default:
                hintText.text = "";
                break;
        }
    }

    bool IsPlayerNearBall()
    {
        if (golfManager == null) return false;
        Transform p = golfManager.PlayerTransform;
        Transform b = golfManager.BallTransform;
        if (p == null || b == null) return false;
        return Vector3.Distance(p.position, b.position) < 2.0f;
    }

    void UpdatePowerBar()
    {
        if (powerBarBackground == null) return;
        bool charging = golfManager.CurrentState == GolfManager.GolfState.Charging;
        powerBarBackground.gameObject.SetActive(charging);
        if (!charging) return;

        float bgWidth = powerBarBackground.sizeDelta.x;
        float fill = bgWidth * golfManager.ChargeFraction;
        powerBarFill.sizeDelta = new Vector2(fill - 4f, -4f);
    }

    void UpdateDirectionArrow()
    {
        if (directionArrow == null) return;
        bool show = (golfManager.CurrentState == GolfManager.GolfState.Golfing || golfManager.CurrentState == GolfManager.GolfState.Charging);
        directionArrow.gameObject.SetActive(show);
        if (!show) return;

        if (cameraController == null || Camera.main == null) return;

        Vector3 cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0f;
        if (cameraForward.sqrMagnitude < 0.0001f) return;
        cameraForward.Normalize();

        Vector3 toHole = golfManager.HolePosition - golfManager.BallPosition;
        toHole.y = 0f;
        if (toHole.sqrMagnitude < 0.0001f) return;
        toHole.Normalize();

        float signed = Vector3.SignedAngle(cameraForward, toHole, Vector3.up);
        directionArrow.localEulerAngles = new Vector3(0f, 0f, -signed);
    }

    void UpdateHoleCompleteUI()
    {
        bool isComplete = golfManager.CurrentState == GolfManager.GolfState.HoleComplete;
        bool isGameOver = golfManager.CurrentState == GolfManager.GolfState.GameOver;

        if (holeCompletePanel != null)
        {
            holeCompletePanel.SetActive(isComplete);
            if (isComplete)
            {
                holeCompleteText.text = "Hole " + golfManager.HoleNumber + " Complete!";
                int strokes = golfManager.StrokeCount;
                int diff = strokes - golfManager.HolePar;
                string scoreLabel = (levelManager != null) ? levelManager.GetScoreName(levelManager.CurrentHoleIndex) : "";
                holeCompleteSubtext.text = string.Format("{0} strokes  |  {1}", strokes, string.IsNullOrEmpty(scoreLabel) ? (diff >= 0 ? "+" + diff : diff.ToString()) : scoreLabel);
            }
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(isGameOver);
            if (isGameOver && gameOverText != null && levelManager != null)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                gameOverText.text = levelManager.GetFinalScoreSummary();
            }
        }
    }
}
