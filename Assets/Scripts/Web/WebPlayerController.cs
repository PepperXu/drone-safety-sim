using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Web-only player controller. Attach to the Camera GameObject in the Web scene.
/// Replaces InputControl + CustomRayController for the non-XR / WebGL build.
/// Does NOT modify DroneSim.unity or any VR-facing scripts.
/// </summary>
public class WebPlayerController : MonoBehaviour
{
    // ── Mouse Look ───────────────────────────────────────────────────────────
    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2.5f;
    [SerializeField] private float pitchMin = -80f;
    [SerializeField] private float pitchMax = 80f;

    // ── Drone Control ─────────────────────────────────────────────────────────
    [Header("Drone Control")]
    [SerializeField] private float horizontalSensitivity = 4f;
    [SerializeField] private float verticalSensitivity   = 0.03f;
    [SerializeField] private float turningSensitivity    = 4f;

    // ── Mark Defect ───────────────────────────────────────────────────────────
    [Header("Mark Defect")]
    [SerializeField] private LayerMask fpvCamLayer;
    [SerializeField] private float markDefectRayLength = 50f;

    [Header("Reset")]
    [SerializeField] private ExperimentServer experimentServer;

    // ── Touch Look (Mobile) ───────────────────────────────────────────────────
    [Header("Touch Look (Mobile)")]
    [SerializeField] private float touchSensitivity = 0.15f;

    // ── Private State ─────────────────────────────────────────────────────────
    private float _yaw;
    private float _pitch;
    private bool  _cursorLocked;

    // True while EXP-UI is showing — keeps cursor visible for UI interaction.
    // Set by ExperimentServer.StartExperiment() / ResetExperiment().
    private static bool _uiMode = true;

    // Singleton — used by MobileControlsUI viewport zone to drive look on mobile.
    private static WebPlayerController _instance;
    public  static WebPlayerController Instance => _instance;

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by ExperimentServer when the experiment starts (uiActive=false)
    /// or resets (uiActive=true). Locks/unlocks cursor accordingly.
    /// </summary>
    public static void SetUIMode(bool uiActive)
    {
        _uiMode = uiActive;
    }

    /// <summary>
    /// Called by MobileControlsUI's viewport zone when a mouse button is pressed
    /// on the viewport area. Locks the cursor so desktop mouse-look can start.
    /// (The standard HandleCursorLock path is bypassed because the viewport zone
    /// Image — a full-screen raycast target — always makes IsPointerOverGameObject
    /// return true, which would otherwise prevent locking.)
    /// </summary>
    public void RequestCursorLock()
    {
        if (!_uiMode) LockCursor(true);
    }

    /// <summary>
    /// Applies a screen-space touch drag delta to camera yaw/pitch.
    /// Called by MobileControlsUI's viewport zone on mobile; ignored on desktop
    /// since cursor-lock mouse look handles that path.
    /// </summary>
    public void ApplyLookDelta(float screenDeltaX, float screenDeltaY)
    {
        if (_uiMode) return;
        _yaw   += screenDeltaX * touchSensitivity;
        _pitch -= screenDeltaY * touchSensitivity;
        _pitch  = Mathf.Clamp(_pitch, pitchMin, pitchMax);
        transform.eulerAngles = new Vector3(_pitch, _yaw, 0f);
    }

    void Awake()
    {
        _instance = this;
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    void Start()
    {
        _yaw   = transform.eulerAngles.y;
        _pitch = transform.eulerAngles.x;
        // Start with cursor visible so the user can interact with EXP-UI.
        LockCursor(false);
    }

    void Update()
    {
        // While EXP-UI is active: keep cursor free for UI clicks.
        // Mouse look + drone controls are suspended.
        if (_uiMode)
        {
            if (_cursorLocked) LockCursor(false);
            return;
        }

        HandleCursorLock();
        if (_cursorLocked)
        {
            HandleMouseLook();
        }
    }

    // ── Cursor lock: Alt to release, click to re-lock ────────────────────────
    void HandleCursorLock()
    {
        if (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt))
            LockCursor(false);
        else if (Input.GetMouseButtonDown(0) && !_cursorLocked)
        {
            // Don't lock when the click lands on a UI element (buttons, joysticks, etc.)
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (!overUI)
                LockCursor(true);
        }
    }

    void LockCursor(bool locked)
    {
        _cursorLocked    = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !locked;
    }

    // ── FPS mouse look ────────────────────────────────────────────────────────
    void HandleMouseLook()
    {
        _yaw   += Input.GetAxis("Mouse X") * mouseSensitivity;
        _pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;   // subtract: up = tilt up
        _pitch  = Mathf.Clamp(_pitch, pitchMin, pitchMax);
        transform.eulerAngles = new Vector3(_pitch, _yaw, 0f);
    }
}
