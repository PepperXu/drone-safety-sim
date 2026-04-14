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

    // ── Private State ─────────────────────────────────────────────────────────
    private float _yaw;
    private float _pitch;
    private bool  _cursorLocked;

    // True while EXP-UI is showing — keeps cursor visible for UI interaction.
    // Set by ExperimentServer.StartExperiment() / ResetExperiment().
    private static bool _uiMode = true;

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by ExperimentServer when the experiment starts (uiActive=false)
    /// or resets (uiActive=true). Locks/unlocks cursor accordingly.
    /// </summary>
    public static void SetUIMode(bool uiActive)
    {
        _uiMode = uiActive;
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

    // ── Cursor lock: Escape to release, click to re-lock ─────────────────────
    void HandleCursorLock()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
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
