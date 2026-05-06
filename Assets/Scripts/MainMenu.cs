using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private float cameraOrbitSpeed = 6f;
    [SerializeField] private float cameraOrbitRadius = 12f;
    [SerializeField] private float cameraOrbitHeight = 4f;

    private LevelManager levelManager;
    private AudioManager audioManager;
    private UIManager uiManager;
    private Canvas canvas;
    private GameObject mainPanel;
    private GameObject selectPanel;
    private Camera orbitCamera;
    private Vector3 orbitCenter;

    void Start()
    {
        levelManager = FindObjectOfType<LevelManager>();
        audioManager = FindObjectOfType<AudioManager>();
        uiManager = FindObjectOfType<UIManager>();
        orbitCamera = Camera.main;

        EnsureEventSystem();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;
        BuildMenuCanvas();
    }

    void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }
    }

    void Update()
    {
        if (orbitCamera == null) return;
        float angle = Time.time * cameraOrbitSpeed * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Sin(angle) * cameraOrbitRadius, cameraOrbitHeight, Mathf.Cos(angle) * cameraOrbitRadius);
        orbitCamera.transform.position = orbitCenter + offset;
        orbitCamera.transform.LookAt(orbitCenter + Vector3.up * 1.5f);
    }

    public void SetOrbitCenter(Vector3 center)
    {
        orbitCenter = center;
    }

    void BuildMenuCanvas()
    {
        GameObject canvasObj = new GameObject("MainMenuCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>().ignoreReversedGraphics = true;

        BuildMainPanel();
        BuildSelectPanel();
        ShowMain();
    }

    void BuildMainPanel()
    {
        mainPanel = new GameObject("MainPanel");
        mainPanel.transform.SetParent(canvas.transform, false);
        RectTransform rt = mainPanel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Text title = MakeText(mainPanel.transform, "Title", "GOLF", 160, TextAnchor.UpperCenter, Color.white);
        RectTransform titleRT = title.rectTransform;
        titleRT.anchorMin = new Vector2(0.5f, 1f);
        titleRT.anchorMax = new Vector2(0.5f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -100f);
        titleRT.sizeDelta = new Vector2(800f, 200f);
        title.color = new Color(1f, 0.95f, 0.6f);

        Text subtitle = MakeText(mainPanel.transform, "Subtitle", "10 Holes  |  Procedural Course", 30, TextAnchor.UpperCenter, Color.white);
        RectTransform subRT = subtitle.rectTransform;
        subRT.anchorMin = new Vector2(0.5f, 1f);
        subRT.anchorMax = new Vector2(0.5f, 1f);
        subRT.pivot = new Vector2(0.5f, 1f);
        subRT.anchoredPosition = new Vector2(0f, -300f);
        subRT.sizeDelta = new Vector2(800f, 60f);

        float btnY = -460f;
        float step = 90f;
        MakeButton(mainPanel.transform, "PlayBtn", "Play", new Vector2(0f, btnY), () => OnPlay());
        btnY -= step;
        MakeButton(mainPanel.transform, "SelectBtn", "Select Hole", new Vector2(0f, btnY), () => ShowSelect());
        btnY -= step;
        MakeButton(mainPanel.transform, "SettingsBtn", "Settings", new Vector2(0f, btnY), () =>
        {
            if (uiManager != null) uiManager.ToggleSettings();
        });
        btnY -= step;
        MakeButton(mainPanel.transform, "QuitBtn", "Quit", new Vector2(0f, btnY), () => OnQuit());
    }

    void BuildSelectPanel()
    {
        selectPanel = new GameObject("SelectPanel");
        selectPanel.transform.SetParent(canvas.transform, false);
        RectTransform rt = selectPanel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = selectPanel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.55f);
        bg.raycastTarget = true;

        Text title = MakeText(selectPanel.transform, "Title", "Select Hole", 60, TextAnchor.UpperCenter, Color.white);
        RectTransform titleRT = title.rectTransform;
        titleRT.anchorMin = new Vector2(0.5f, 1f);
        titleRT.anchorMax = new Vector2(0.5f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -80f);
        titleRT.sizeDelta = new Vector2(800f, 80f);

        int holeCount = (levelManager != null) ? levelManager.TotalHoles : 10;
        int columns = 5;
        float buttonW = 180f;
        float buttonH = 100f;
        float spacing = 30f;
        float gridW = columns * buttonW + (columns - 1) * spacing;
        float startX = -gridW * 0.5f + buttonW * 0.5f;
        float startY = -250f;

        for (int i = 0; i < holeCount; i++)
        {
            int row = i / columns;
            int col = i % columns;
            float x = startX + col * (buttonW + spacing);
            float y = startY - row * (buttonH + spacing);
            int idx = i;
            HoleData h = (levelManager != null) ? levelManager.GetHole(idx) : null;
            string label = h != null ? string.Format("{0}\n{1} (P{2})", idx + 1, h.holeName, h.par) : (idx + 1).ToString();
            Button btn = MakeButton(selectPanel.transform, "Hole" + (idx + 1), label, new Vector2(x, y), () =>
            {
                if (levelManager != null)
                {
                    levelManager.ResetProgress();
                    levelManager.LoadHoleScene(idx);
                }
            });
            RectTransform brt = btn.GetComponent<RectTransform>();
            brt.sizeDelta = new Vector2(buttonW, buttonH);
            Text txt = btn.GetComponentInChildren<Text>();
            if (txt != null) txt.fontSize = 22;
        }

        MakeButton(selectPanel.transform, "BackBtn", "Back", new Vector2(0f, -880f), () => ShowMain()).GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 70f);
    }

    void ShowMain()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (selectPanel != null) selectPanel.SetActive(false);
    }

    void ShowSelect()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (selectPanel != null) selectPanel.SetActive(true);
    }

    void OnPlay()
    {
        if (levelManager != null) levelManager.LoadFirstHole();
    }

    void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    Text MakeText(Transform parent, string name, string content, int fontSize, TextAnchor anchor, Color color)
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
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(2f, -2f);
        return t;
    }

    Button MakeButton(Transform parent, string name, string label, Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick)
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
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(360f, 70f);

        Text labelText = MakeText(rt, "Label", label, 28, TextAnchor.MiddleCenter, Color.white);
        RectTransform lblRT = labelText.rectTransform;
        lblRT.anchorMin = Vector2.zero;
        lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = Vector2.zero;
        lblRT.offsetMax = Vector2.zero;

        btn.onClick.AddListener(onClick);
        return btn;
    }
}
