using UnityEngine;
using System.Collections;

public class GolfManager : MonoBehaviour
{
    [SerializeField] private float maxPower = 25f;
    [SerializeField] private float chargeSpeed = 15f;
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private float golfCameraDistance = 4f;
    [SerializeField] private float golfCameraHeight = 2f;

    public enum GolfState { Walking, Golfing, Charging, HoleComplete, GameOver }
    public GolfState CurrentState { get; private set; } = GolfState.Walking;

    private Transform player;
    private CameraController cameraController;
    private GolfBall golfBall;
    private PlayerMovement playerMovement;
    private Rigidbody ballRb;
    private AudioManager audioManager;
    private LevelManager levelManager;

    private float currentCharge;
    private bool isCharging;
    private int strokeCount;

    private bool showingHoleComplete;
    private bool showingGameOver;

    public int StrokeCount => strokeCount;

    void Start()
    {
        AutoLinkReferences();
        SetupHole();
    }

    void AutoLinkReferences()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerMovement = playerObj.GetComponent<PlayerMovement>();
        }

        GameObject cameraObj = GameObject.FindGameObjectWithTag("MainCamera");
        if (cameraObj != null)
            cameraController = cameraObj.GetComponent<CameraController>();

        levelManager = FindObjectOfType<LevelManager>();
        audioManager = FindObjectOfType<AudioManager>();
    }

    void SetupHole()
    {
        if (levelManager == null) return;

        HoleData hole = levelManager.CurrentHole;
        if (hole == null) return;

        GameObject ballObj = GameObject.FindGameObjectWithTag("GolfBall");
        if (ballObj != null)
        {
            golfBall = ballObj.GetComponent<GolfBall>();
            ballRb = ballObj.GetComponent<Rigidbody>();
            ballObj.transform.position = hole.teePosition;
        }

        if (player != null)
            player.position = hole.playerStart;

        strokeCount = 0;
        CurrentState = GolfState.Walking;
    }

    void Update()
    {
        switch (CurrentState)
        {
            case GolfState.Walking:
                HandleWalkingState();
                break;
            case GolfState.Golfing:
                HandleGolfingState();
                break;
            case GolfState.Charging:
                HandleChargingState();
                break;
            case GolfState.HoleComplete:
                break;
            case GolfState.GameOver:
                break;
        }
    }

    void HandleWalkingState()
    {
        if (Input.GetKeyDown(KeyCode.E) && IsPlayerNearBall())
        {
            EnterGolfingMode();
        }
    }

    void HandleGolfingState()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            ExitGolfingMode();
            return;
        }

        if (Input.GetMouseButtonDown(0) && !golfBall.IsMoving())
        {
            EnterChargingMode();
        }

        PositionCameraOnBall();
    }

    void HandleChargingState()
    {
        currentCharge += chargeSpeed * Time.deltaTime;
        currentCharge = Mathf.Clamp(currentCharge, 0f, maxPower);

        if (Input.GetMouseButtonUp(0))
        {
            HitBall();
            ExitChargingMode();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            currentCharge = 0f;
            isCharging = false;
            ExitGolfingMode();
        }

        PositionCameraOnBall();
    }

    bool IsPlayerNearBall()
    {
        if (player == null || golfBall == null) return false;
        return Vector3.Distance(player.position, golfBall.transform.position) <= interactDistance;
    }

    void EnterGolfingMode()
    {
        CurrentState = GolfState.Golfing;
        if (playerMovement != null)
            playerMovement.enabled = false;
        if (cameraController != null)
            cameraController.SetGolfMode(true);
    }

    void ExitGolfingMode()
    {
        CurrentState = GolfState.Walking;
        if (playerMovement != null)
            playerMovement.enabled = true;
        if (cameraController != null)
            cameraController.SetGolfMode(false);
    }

    void EnterChargingMode()
    {
        CurrentState = GolfState.Charging;
        isCharging = true;
        currentCharge = 0f;
    }

    void ExitChargingMode()
    {
        CurrentState = GolfState.Golfing;
        isCharging = false;
    }

    void HitBall()
    {
        if (cameraController == null || golfBall == null) return;

        Vector3 camForward = cameraController.transform.forward;
        Vector3 hitDirection = new Vector3(camForward.x, camForward.z > 0 ? Mathf.Max(camForward.y, 0.05f) : camForward.y, camForward.z).normalized;

        golfBall.HitBall(hitDirection, currentCharge);
        strokeCount++;
        currentCharge = 0f;

        if (audioManager != null)
            audioManager.PlayHitSound();
    }

    void PositionCameraOnBall()
    {
        if (golfBall == null || cameraController == null) return;

        Transform camTransform = cameraController.transform;
        float yaw = cameraController.GetYaw();

        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
        Vector3 targetPosition = golfBall.transform.position + Vector3.up * golfCameraHeight - (rotation * Vector3.forward * golfCameraDistance);

        camTransform.position = Vector3.Lerp(camTransform.position, targetPosition, 10f * Time.deltaTime);

        Vector3 lookTarget = golfBall.transform.position + Vector3.up * 0.5f;
        camTransform.rotation = Quaternion.Lerp(camTransform.rotation, Quaternion.LookRotation(lookTarget - camTransform.position), 10f * Time.deltaTime);
    }

    public void ShowHoleComplete(bool isLastHole)
    {
        if (isLastHole)
        {
            CurrentState = GolfState.GameOver;
            showingGameOver = true;
        }
        else
        {
            CurrentState = GolfState.HoleComplete;
            showingHoleComplete = true;
        }
    }

    public void LoadNextHole()
    {
        showingHoleComplete = false;

        if (levelManager == null) return;

        HoleData hole = levelManager.CurrentHole;
        if (hole == null) return;

        GameObject ballObj = GameObject.FindGameObjectWithTag("GolfBall");
        if (ballObj != null)
        {
            ballObj.transform.position = hole.teePosition;
            Rigidbody rb = ballObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            GolfBall gb = ballObj.GetComponent<GolfBall>();
            if (gb != null) gb.ResetToNewPosition(hole.teePosition);
        }

        if (player != null)
            player.position = hole.playerStart;

        strokeCount = 0;
        CurrentState = GolfState.Walking;

        if (playerMovement != null)
            playerMovement.enabled = true;
        if (cameraController != null)
            cameraController.SetGolfMode(false);
    }

    public void RestartGame()
    {
        showingGameOver = false;
        if (levelManager != null)
            levelManager.ResetProgress();

        LoadNextHole();
    }

    void OnGUI()
    {
        if (CurrentState == GolfState.Walking && IsPlayerNearBall())
        {
            DrawCenterLabel("Press [E] to Golf", 24, Color.white);
        }

        if (CurrentState == GolfState.Golfing)
        {
            DrawCenterLabel("GOLF MODE  |  [LMB] Charge  |  [E] Exit", 20, Color.yellow);
        }

        if (CurrentState == GolfState.Charging)
        {
            float pct = currentCharge / maxPower;
            DrawPowerBar(pct);
            DrawCenterLabel(string.Format("POWER: {0:F0}%", pct * 100f), 28, Color.red);
        }

        DrawHoleInfo();

        if (showingHoleComplete)
        {
            DrawHoleCompletePanel();
        }

        if (showingGameOver)
        {
            DrawGameOverPanel();
        }
    }

    void DrawHoleInfo()
    {
        if (levelManager == null) return;
        HoleData hole = levelManager.CurrentHole;
        if (hole == null) return;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperCenter
        };
        style.normal.textColor = Color.white;

        string info = string.Format("Hole {0}: {1}  |  Par {2}  |  Strokes: {3}",
            hole.holeNumber, hole.holeName, hole.par, strokeCount);

        float w = 600f;
        float h = 35f;
        Rect rect = new Rect((Screen.width - w) * 0.5f, 5f, w, h);

        GUI.color = new Color(0f, 0f, 0f, 0.5f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(rect, info, style);
    }

    void DrawHoleCompletePanel()
    {
        if (levelManager == null) return;
        int holeIdx = levelManager.CurrentHoleNumber - 1;

        float w = 400f;
        float h = 180f;
        float x = (Screen.width - w) * 0.5f;
        float y = (Screen.height - h) * 0.5f;

        GUI.color = new Color(0f, 0f, 0f, 0.8f);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = Color.yellow;

        GUIStyle bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            alignment = TextAnchor.MiddleCenter
        };
        bodyStyle.normal.textColor = Color.white;

        GUI.Label(new Rect(x, y + 15f, w, 40f), "HOLE COMPLETE!", titleStyle);

        int strokes = levelManager.GetHoleStrokes(holeIdx);
        string scoreName = levelManager.GetScoreName(holeIdx);
        GUI.Label(new Rect(x, y + 60f, w, 30f), string.Format("Strokes: {0}  ({1})", strokes, scoreName), bodyStyle);

        GUI.Label(new Rect(x, y + 100f, w, 30f), "Loading next hole...", bodyStyle);

        GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        hintStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
        GUI.Label(new Rect(x, y + 140f, w, 25f), "Press [ESC] for Settings", hintStyle);
    }

    void DrawGameOverPanel()
    {
        float w = 500f;
        float h = 260f;
        float x = (Screen.width - w) * 0.5f;
        float y = (Screen.height - h) * 0.5f;

        GUI.color = new Color(0f, 0f, 0f, 0.85f);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 32,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = Color.yellow;

        GUIStyle bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            alignment = TextAnchor.MiddleCenter
        };
        bodyStyle.normal.textColor = Color.white;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        GUI.Label(new Rect(x, y + 15f, w, 40f), "GAME COMPLETE!", titleStyle);
        GUI.Label(new Rect(x, y + 55f, w, 30f), levelManager.GetFinalScoreSummary(), bodyStyle);

        string detailLine = "";
        for (int i = 0; i < levelManager.TotalHoles; i++)
        {
            int s = levelManager.GetHoleStrokes(i);
            string sn = levelManager.GetScoreName(i);
            detailLine += string.Format("H{0}:{1}({2})  ", i + 1, s, sn);
        }

        GUIStyle detailStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        detailStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
        GUI.Label(new Rect(x + 20f, y + 90f, w - 40f, 60f), detailLine, detailStyle);

        if (GUI.Button(new Rect(x + 50f, y + 170f, w - 100f, 40f), "Play Again", buttonStyle))
        {
            RestartGame();
        }

        if (GUI.Button(new Rect(x + 50f, y + 215f, w - 100f, 30f), "Quit", buttonStyle))
        {
            Application.Quit();
        }
    }

    void DrawCenterLabel(string text, int size, Color color)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = size,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        style.normal.textColor = color;

        float w = 500f;
        float h = 40f;
        Rect rect = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.1f, w, h);
        GUI.Label(rect, text, style);
    }

    void DrawPowerBar(float pct)
    {
        float barWidth = 300f;
        float barHeight = 20f;
        float x = (Screen.width - barWidth) * 0.5f;
        float y = Screen.height * 0.85f;

        GUI.color = Color.gray;
        GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), Texture2D.whiteTexture);

        Color barColor = Color.Lerp(Color.green, Color.red, pct);
        GUI.color = barColor;
        GUI.DrawTexture(new Rect(x, y, barWidth * pct, barHeight), Texture2D.whiteTexture);

        GUI.color = Color.white;
    }
}
