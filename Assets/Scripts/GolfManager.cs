using UnityEngine;
using UnityEngine.SceneManagement;

public class GolfManager : MonoBehaviour
{
    public enum GolfState
    {
        Walking,
        Golfing,
        Charging,
        HoleComplete,
        GameOver
    }

    public static GolfManager Instance { get; private set; }

    [SerializeField] private float minPower = 8f;
    [SerializeField] private float maxPower = 28f;
    [SerializeField] private float chargeSpeed = 18f;
    [SerializeField] private float golfPickupRange = 2.0f;

    private GolfState currentState = GolfState.Walking;
    private float currentPower;
    private float chargeDirection = 1f;
    private bool isCharging;
    private int strokeCount;

    private GolfBall golfBall;
    private Transform playerTransform;
    private CameraController cameraController;
    private PlayerMovement playerMovement;
    private PlayerVisuals playerVisuals;
    private AudioManager audioManager;
    private LevelManager levelManager;
    private HoleTarget holeTarget;

    public GolfState CurrentState => currentState;
    public int StrokeCount => strokeCount;
    public float CurrentPower => currentPower;
    public float MinPower => minPower;
    public float MaxPower => maxPower;
    public float ChargeFraction => Mathf.InverseLerp(minPower, maxPower, currentPower);
    public bool IsCharging => isCharging;
    public bool IsLastHoleComplete { get; private set; }

    public bool BallReady => golfBall != null && !golfBall.IsMoving();
    public Vector3 BallPosition => golfBall != null ? golfBall.transform.position : Vector3.zero;
    public Vector3 HolePosition => holeTarget != null ? holeTarget.transform.position : Vector3.zero;
    public Transform BallTransform => golfBall != null ? golfBall.transform : null;
    public Transform HoleTransform => holeTarget != null ? holeTarget.transform : null;
    public Transform PlayerTransform => playerTransform;
    public string HoleName => (levelManager != null && levelManager.CurrentHole != null) ? levelManager.CurrentHole.holeName : "";
    public int HolePar => (levelManager != null && levelManager.CurrentHole != null) ? levelManager.CurrentHole.par : 0;
    public int HoleNumber => (levelManager != null) ? levelManager.CurrentHoleNumber : 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        AutoLinkReferences();
        SetupHole();
    }

    void AutoLinkReferences()
    {
        golfBall = FindObjectOfType<GolfBall>();
        cameraController = FindObjectOfType<CameraController>();
        audioManager = FindObjectOfType<AudioManager>();
        levelManager = FindObjectOfType<LevelManager>();
        holeTarget = FindObjectOfType<HoleTarget>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerMovement = playerObj.GetComponent<PlayerMovement>();
            playerVisuals = playerObj.GetComponent<PlayerVisuals>();
        }
    }

    void SetupHole()
    {
        strokeCount = 0;
        currentState = GolfState.Walking;
        currentPower = minPower;
        IsLastHoleComplete = false;

        if (cameraController != null)
            cameraController.SetGolfMode(false);
        if (playerMovement != null)
            playerMovement.SetMovementEnabled(true);
        if (playerVisuals != null)
            playerVisuals.ShowClub(false);
    }

    void Update()
    {
        switch (currentState)
        {
            case GolfState.Walking:
                CheckPickupBall();
                break;
            case GolfState.Golfing:
                HandleGolfingInput();
                break;
            case GolfState.Charging:
                HandleCharging();
                break;
        }
    }

    void CheckPickupBall()
    {
        if (Input.GetKeyDown(KeyCode.E) && IsPlayerNearBall())
        {
            EnterGolfMode();
        }
    }

    bool IsPlayerNearBall()
    {
        if (golfBall == null || playerTransform == null) return false;
        float distance = Vector3.Distance(playerTransform.position, golfBall.transform.position);
        return distance < golfPickupRange;
    }

    void EnterGolfMode()
    {
        currentState = GolfState.Golfing;
        if (playerMovement != null) playerMovement.SetMovementEnabled(false);
        if (cameraController != null) cameraController.SetGolfMode(true);
        if (playerVisuals != null) playerVisuals.ShowClub(true);
    }

    void HandleGolfingInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitGolfMode();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            BeginCharge();
        }
    }

    void ExitGolfMode()
    {
        currentState = GolfState.Walking;
        if (playerMovement != null) playerMovement.SetMovementEnabled(true);
        if (cameraController != null) cameraController.SetGolfMode(false);
        if (playerVisuals != null) playerVisuals.ShowClub(false);
    }

    void BeginCharge()
    {
        currentState = GolfState.Charging;
        currentPower = minPower;
        chargeDirection = 1f;
        isCharging = true;
        if (playerVisuals != null) playerVisuals.SetCharging(true);
    }

    void HandleCharging()
    {
        currentPower += chargeSpeed * chargeDirection * Time.deltaTime;
        if (currentPower >= maxPower)
        {
            currentPower = maxPower;
            chargeDirection = -1f;
        }
        else if (currentPower <= minPower)
        {
            currentPower = minPower;
            chargeDirection = 1f;
        }

        if (Input.GetMouseButtonUp(0))
        {
            ReleaseShot();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            isCharging = false;
            if (playerVisuals != null) playerVisuals.SetCharging(false);
            currentState = GolfState.Golfing;
        }
    }

    void ReleaseShot()
    {
        isCharging = false;
        if (playerVisuals != null)
        {
            playerVisuals.SetCharging(false);
            playerVisuals.TriggerSwing();
        }

        if (golfBall != null && cameraController != null)
        {
            Vector3 direction = ComputeShotDirection();
            golfBall.HitBall(direction, currentPower);
            strokeCount++;
            if (audioManager != null) audioManager.PlayHitSound();
            if (levelManager != null) levelManager.RecordHoleStrokes(strokeCount);
        }

        ExitGolfMode();
    }

    Vector3 ComputeShotDirection()
    {
        if (cameraController == null || golfBall == null) return Vector3.forward;
        Vector3 cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        float yaw = cameraController.GetYaw();
        Vector3 horizontal = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        return horizontal.normalized;
    }

    public void ShowHoleComplete(bool isLastHole)
    {
        currentState = isLastHole ? GolfState.GameOver : GolfState.HoleComplete;
        IsLastHoleComplete = isLastHole;
    }

    public void LoadNextHole()
    {
        if (levelManager == null) return;
        levelManager.LoadHoleScene(levelManager.CurrentHoleIndex);
    }

    public void RestartGame()
    {
        if (levelManager == null) return;
        levelManager.LoadFirstHole();
    }

    public void ReturnToMainMenu()
    {
        if (levelManager != null)
        {
            levelManager.LoadMainMenu();
        }
        else
        {
            SceneManager.LoadScene(LevelManager.MainMenuSceneName);
        }
    }
}
