using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float defaultSens = 2f;
    [SerializeField] private float distance = 5f;
    [SerializeField] private float headExtra = 0.3f;
    [SerializeField] private float fallbackHeadHeight = 1.7f;
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 60f;
    [SerializeField] private float defaultFOV = 65f;
    [SerializeField] private float golfFOV = 50f;
    [SerializeField] private float fovTransitionSpeed = 8f;
    [SerializeField] private LayerMask collisionMask = ~0;

    [Header("Smooth Damping")]
    [SerializeField] private float positionSmoothTime = 0.15f;
    [SerializeField] private float rotationSmoothTime = 0.1f;
    [SerializeField] private float lookAheadFactor = 0.3f;
    [SerializeField] private float verticalOffsetDamping = 0.15f;
    [SerializeField] private float collisionOffset = 0.3f;

    private float playerMultiplier;
    private Transform player;
    private CharacterController playerController;
    private Camera cam;

    private float yaw;
    private float pitch;

    private Vector3 positionVelocity;
    private float yawVelocity;
    private float pitchVelocity;
    private Vector3 lastPlayerPosition;
    private Vector3 playerVelocityEstimate;

    private float currentFOV;
    private float targetFOV;
    private bool isGolfMode;

    void Awake()
    {
        AutoLinkPlayer();
        LoadSensitivity();
        InitializeAngles();
        LockCursor(true);

        cam = GetComponent<Camera>();
        if (cam == null)
            cam = Camera.main;

        currentFOV = PlayerPrefs.GetFloat("CameraFOV", defaultFOV);
        targetFOV = currentFOV;
        if (cam != null)
            cam.fieldOfView = currentFOV;
    }

    void OnEnable()
    {
        Application.focusChanged += OnAppFocusChanged;
    }

    void OnDisable()
    {
        Application.focusChanged -= OnAppFocusChanged;
    }

    void OnAppFocusChanged(bool hasFocus)
    {
        if (hasFocus && Time.timeScale > 0f)
        {
            LockCursor(true);
        }
    }

    void AutoLinkPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerController = playerObj.GetComponent<CharacterController>();
            lastPlayerPosition = player.position;
        }
        else
        {
            Debug.LogError("[CameraController] No GameObject with 'Player' tag found!");
        }
    }

    void LoadSensitivity()
    {
        playerMultiplier = PlayerPrefs.GetFloat("MouseSensitivity", 1.25f);
    }

    void InitializeAngles()
    {
        yaw = transform.eulerAngles.y;
        pitch = 15f;
    }

    public void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void LateUpdate()
    {
        if (player == null)
        {
            AutoLinkPlayer();
            if (player == null) return;
        }

        EstimatePlayerVelocity();
        HandleMouseInput();
        UpdateCameraPosition();
        UpdateFOV();
    }

    void EstimatePlayerVelocity()
    {
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 currentVelocity = (player.position - lastPlayerPosition) / dt;
        playerVelocityEstimate = Vector3.Lerp(playerVelocityEstimate, currentVelocity, verticalOffsetDamping);
        lastPlayerPosition = player.position;
    }

    void HandleMouseInput()
    {
        if (Time.timeScale <= 0f) return;
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mouseX = Input.GetAxisRaw("Mouse X") * defaultSens * playerMultiplier;
        float mouseY = Input.GetAxisRaw("Mouse Y") * defaultSens * playerMultiplier;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    float ComputeHeadOffset()
    {
        if (playerController != null)
        {
            return playerController.center.y + playerController.height * 0.5f + headExtra;
        }
        return fallbackHeadHeight;
    }

    void UpdateCameraPosition()
    {
        Vector3 lookAheadOffset = playerVelocityEstimate * lookAheadFactor;
        lookAheadOffset.y = 0f;

        Vector3 targetCenter = player.position + Vector3.up * ComputeHeadOffset() + lookAheadOffset;

        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPosition = targetCenter - (targetRotation * Vector3.forward * distance);

        desiredPosition = HandleCollision(targetCenter, desiredPosition);

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, positionSmoothTime);

        Quaternion currentRotation = transform.rotation;
        float targetYaw = Mathf.SmoothDampAngle(currentRotation.eulerAngles.y, yaw, ref yawVelocity, rotationSmoothTime);
        float targetPitch = Mathf.SmoothDampAngle(currentRotation.eulerAngles.x, pitch, ref pitchVelocity, rotationSmoothTime);

        transform.rotation = Quaternion.Euler(targetPitch, targetYaw, 0f);
    }

    Vector3 HandleCollision(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        float dist = dir.magnitude;
        if (dist <= 0.0001f) return to;

        if (Physics.SphereCast(from, collisionOffset, dir.normalized, out RaycastHit hit, dist, collisionMask, QueryTriggerInteraction.Ignore))
        {
            return from + dir.normalized * Mathf.Max(hit.distance - collisionOffset, 0.05f);
        }

        return to;
    }

    void UpdateFOV()
    {
        if (cam == null) return;

        targetFOV = isGolfMode ? golfFOV : PlayerPrefs.GetFloat("CameraFOV", defaultFOV);
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, fovTransitionSpeed * Time.unscaledDeltaTime);
        cam.fieldOfView = currentFOV;
    }

    public float GetYaw() => yaw;
    public float GetPitch() => pitch;
    public float GetFOV() => currentFOV;

    public void SetPlayerMultiplier(float value)
    {
        playerMultiplier = value;
    }

    public void SetFOV(float fov)
    {
        PlayerPrefs.SetFloat("CameraFOV", fov);
        PlayerPrefs.Save();
    }

    public void SetGolfMode(bool enabled)
    {
        isGolfMode = enabled;
    }
}
