using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Mobile-friendly on-screen controls for the Web scene.
/// - Left virtual stick  → Yaw (X) + Throttle (Y)
/// - Right virtual stick → Roll (X) + Pitch (Y)
/// - Take Off / RTH / Autopilot buttons (show/hide with MonitorUI)
/// - Tap on FPV camera area → Mark Defect
///
/// Call MobileControlsUI.SetUIMode(true)  to hide controls (experiment setup screen).
/// Call MobileControlsUI.SetUIMode(false) to show controls (experiment running).
/// </summary>
public class MobileControlsUI : MonoBehaviour
{
    // ── Inspector tunables ─────────────────────────────────────────────────────
    [Header("Stick Settings")]
    [SerializeField] private float stickMaxRadius   = 60f;   // pixels (reference res)
    [SerializeField] private float horizontalSens   = 4f;
    [SerializeField] private float verticalSens     = 4f;
    [SerializeField] private float throttleSens     = 0.03f;

    [Header("Appearance")]
    [SerializeField] private float stickBaseSize    = 130f;
    [SerializeField] private float stickKnobSize    = 60f;
    [SerializeField] private float buttonWidth      = 150f;
    [SerializeField] private float buttonHeight     = 70f;
    [SerializeField] private float buttonFontSize   = 18f;

    // ── Static UI-mode gate (mirrors WebPlayerController) ─────────────────────
    private static bool _uiMode = true;   // true = EXP-UI showing, controls hidden

    public static void SetUIMode(bool uiActive)
    {
        _uiMode = uiActive;
        if (_instance != null)
            _instance._controlRoot.SetActive(!uiActive);
    }

    /// <summary>
    /// True while at least one virtual joystick is being actively dragged.
    /// InputControl checks this to yield control rather than overwriting with zeros.
    /// </summary>

    // ── Runtime state ──────────────────────────────────────────────────────────
    private static MobileControlsUI _instance;

    private Canvas    _canvas;
    private GameObject _controlRoot;

    // Joystick state
    private JoystickTracker _leftStick;
    private JoystickTracker _rightStick;


    // ══════════════════════════════════════════════════════════════════════════
    #region Unity lifecycle

    private void Awake()
    {
        _instance = this;
        BuildUI();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    public static Vector2 GetLeftStickValue() => _instance != null && _instance._leftStick != null ? _instance._leftStick.Value : Vector2.zero;
    public static Vector2 GetRightStickValue() => _instance != null && _instance._rightStick != null ? _instance._rightStick.Value : Vector2.zero;


    #endregion

    // ══════════════════════════════════════════════════════════════════════════
    #region UI construction

    private void BuildUI()
    {
        // ── Canvas ────────────────────────────────────────────────────────────
        var canvasGO = new GameObject("MobileControlsCanvas");
        canvasGO.transform.SetParent(transform, false);

        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 50;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920); // portrait reference
        scaler.screenMatchMode    = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Ensure an EventSystem exists
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        // ── Root container (hidden in UI mode) ────────────────────────────────
        _controlRoot = new GameObject("ControlRoot");
        _controlRoot.transform.SetParent(canvasGO.transform, false);
        var rootRT = _controlRoot.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        _controlRoot.SetActive(!_uiMode);

        // ── Left joystick (bottom-left) ────────────────────────────────────────
        _leftStick  = CreateJoystick(_controlRoot.transform, false);

        // ── Right joystick (bottom-right) ─────────────────────────────────────
        _rightStick = CreateJoystick(_controlRoot.transform, true);

        // ── Action buttons (bottom-centre, above sticks) ──────────────────────
        CreateActionButtons(_controlRoot.transform);

    }

    // ── Joystick ──────────────────────────────────────────────────────────────

    private JoystickTracker CreateJoystick(Transform parent, bool rightSide)
    {
        // Outer dead-zone touch area (fills half the screen width, bottom 40%)
        var zoneGO = new GameObject(rightSide ? "RightStickZone" : "LeftStickZone");
        zoneGO.transform.SetParent(parent, false);
        var zoneRT  = zoneGO.AddComponent<RectTransform>();
        zoneRT.anchorMin = rightSide ? new Vector2(0.5f, 0f) : new Vector2(0f, 0f);
        zoneRT.anchorMax = rightSide ? new Vector2(1f,  0.4f): new Vector2(0.5f, 0.4f);
        zoneRT.offsetMin = Vector2.zero;
        zoneRT.offsetMax = Vector2.zero;

        // Invisible touch receiver
        var zoneImg   = zoneGO.AddComponent<Image>();
        zoneImg.color = new Color(0, 0, 0, 0);

        // Base ring (cosmetic, centred in zone)
        var baseGO = new GameObject("StickBase");
        baseGO.transform.SetParent(zoneGO.transform, false);
        var baseRT = baseGO.AddComponent<RectTransform>();
        baseRT.anchorMin = new Vector2(0.5f, 0.5f);
        baseRT.anchorMax = new Vector2(0.5f, 0.5f);
        baseRT.pivot     = new Vector2(0.5f, 0.5f);
        baseRT.sizeDelta = new Vector2(stickBaseSize, stickBaseSize);

        var baseImg  = baseGO.AddComponent<Image>();
        baseImg.color = new Color(1f, 1f, 1f, 0.15f);

        // Knob
        var knobGO = new GameObject("StickKnob");
        knobGO.transform.SetParent(baseGO.transform, false);
        var knobRT = knobGO.AddComponent<RectTransform>();
        knobRT.anchorMin = new Vector2(0.5f, 0.5f);
        knobRT.anchorMax = new Vector2(0.5f, 0.5f);
        knobRT.pivot     = new Vector2(0.5f, 0.5f);
        knobRT.sizeDelta = new Vector2(stickKnobSize, stickKnobSize);

        var knobImg  = knobGO.AddComponent<Image>();
        knobImg.color = new Color(1f, 1f, 1f, 0.55f);

        // Label
        var labelGO = new GameObject("StickLabel");
        labelGO.transform.SetParent(baseGO.transform, false);
        var labelRT  = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0.5f, 0f);
        labelRT.anchorMax = new Vector2(0.5f, 0f);
        labelRT.pivot     = new Vector2(0.5f, 1.5f);
        labelRT.sizeDelta = new Vector2(120f, 24f);

        var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
        labelTMP.text      = rightSide ? "PITCH / ROLL" : "THROTTLE / YAW";
        labelTMP.fontSize  = 11f;
        labelTMP.color     = new Color(1f, 1f, 1f, 0.4f);
        labelTMP.alignment = TextAlignmentOptions.Center;
        labelTMP.raycastTarget = false;

        var tracker = new JoystickTracker(zoneGO, baseGO, knobGO, stickMaxRadius);
        return tracker;
    }

    // ── Action buttons ────────────────────────────────────────────────────────

    private void CreateActionButtons(Transform parent)
    {
        // Container: top-left corner, fixed width, height auto-fits children
        var barGO = new GameObject("ActionButtons");
        barGO.transform.SetParent(parent, false);
        var barRT = barGO.AddComponent<RectTransform>();
        barRT.anchorMin        = new Vector2(0f, 1f);
        barRT.anchorMax        = new Vector2(0f, 1f);
        barRT.pivot            = new Vector2(0f, 1f);
        barRT.anchoredPosition = new Vector2(12f, -12f);   // 12 px margin from edge
        barRT.sizeDelta        = new Vector2(buttonWidth, 0f);

        var vLayout = barGO.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing              = 8f;
        vLayout.padding              = new RectOffset(0, 0, 0, 0);
        vLayout.childAlignment       = TextAnchor.UpperLeft;
        vLayout.childControlWidth    = true;
        vLayout.childControlHeight   = true;
        vLayout.childForceExpandWidth  = true;
        vLayout.childForceExpandHeight = false;

        var fitter = barGO.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateButton(barGO.transform, "TAKE OFF",    new Color(0.18f, 0.62f, 0.18f, 0.90f), OnTakeOff);
        CreateButton(barGO.transform, "RTH",         new Color(0.75f, 0.30f, 0.10f, 0.90f), OnRTH);
        CreateButton(barGO.transform, "AUTOPILOT",   new Color(0.15f, 0.35f, 0.75f, 0.90f), OnAutopilot);
        CreateButton(barGO.transform, "MARK DEFECT", new Color(0.65f, 0.20f, 0.65f, 0.90f), OnMarkDefect);
        CreateButton(barGO.transform, "RESET",       new Color(0.35f, 0.35f, 0.35f, 0.90f), OnReset);
    }

    private Button CreateButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label);
        go.transform.SetParent(parent, false);

        var le = go.AddComponent<LayoutElement>();
        le.minHeight       = buttonHeight;
        le.preferredHeight = buttonHeight;

        var img = go.AddComponent<Image>();
        img.color = color;

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(
            Mathf.Min(color.r + 0.15f, 1f),
            Mathf.Min(color.g + 0.15f, 1f),
            Mathf.Min(color.b + 0.15f, 1f),
            color.a);
        colors.pressedColor = new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f, color.a);
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        var textGO  = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        var textRT  = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = buttonFontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return btn;
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════════
    #region Button callbacks

    private void OnTakeOff()
    {
        DroneManager.take_off_flag = true;
    }

    private void OnRTH()
    {
        DroneManager.rth_flag = true;
    }

    private void OnAutopilot()
    {
        bool isAuto = DroneManager.currentControlType == DroneManager.ControlType.Autonomous;
        if (isAuto)
            DroneManager.autopilot_stop_flag = true;
        else
            DroneManager.autopilot_flag = true;
    }

    private void OnMarkDefect()
    {
        DroneManager.mark_defect_flag = true;
    }

    private void OnReset()
    {
        var server = FindFirstObjectByType<ExperimentServer>();
        if (server != null)
            server.ResetExperiment();
    }



    #endregion

    // ══════════════════════════════════════════════════════════════════════════
    #region JoystickTracker (inner class)

    /// <summary>
    /// Handles multi-touch drag for one virtual joystick.
    /// Uses EventTrigger on the zone image; no PointerEventData needed.
    /// </summary>
    private class JoystickTracker
    {
        public Vector2 Value { get; private set; }   // normalised −1…+1 on each axis

        private readonly RectTransform _zoneRT;
        private readonly RectTransform _baseRT;
        private readonly RectTransform _knobRT;
        private readonly float         _maxRadius;

        private int   _touchId  = -1;
        private bool  _dragging = false;
        private Vector2 _originScreen;   // screen-space position where touch began

        public JoystickTracker(GameObject zone, GameObject baseGO, GameObject knob, float maxRadius)
        {
            _zoneRT    = zone.GetComponent<RectTransform>();
            _baseRT    = baseGO.GetComponent<RectTransform>();
            _knobRT    = knob.GetComponent<RectTransform>();
            _maxRadius = maxRadius;

            var trigger = zone.AddComponent<EventTrigger>();
            AddEntry(trigger, EventTriggerType.PointerDown,  OnPointerDown);
            AddEntry(trigger, EventTriggerType.Drag,         OnDrag);
            AddEntry(trigger, EventTriggerType.PointerUp,    OnPointerUp);
        }

        private void AddEntry(EventTrigger trigger, EventTriggerType type,
                              UnityEngine.Events.UnityAction<BaseEventData> action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(action);
            trigger.triggers.Add(entry);
        }

        private void OnPointerDown(BaseEventData data)
        {
            if (_dragging) return;
            var pdata = (PointerEventData)data;
            _touchId      = pdata.pointerId;
            _dragging     = true;
            _originScreen = pdata.position;

            // Snap base to touch origin
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _zoneRT, pdata.position, pdata.pressEventCamera, out var local);
            _baseRT.anchoredPosition = local;
            _knobRT.anchoredPosition = Vector2.zero;
            Value = Vector2.zero;
        }

        private void OnDrag(BaseEventData data)
        {
            if (!_dragging) return;
            var pdata = (PointerEventData)data;
            if (pdata.pointerId != _touchId) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _zoneRT, pdata.position, pdata.pressEventCamera, out var local);

            // Delta from base centre (which is at touch origin in zone-local space)
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _zoneRT, _originScreen, pdata.pressEventCamera, out var originLocal);

            Vector2 delta  = local - originLocal;
            float   dist   = delta.magnitude;
            Vector2 clamped = dist > _maxRadius ? delta / dist * _maxRadius : delta;

            _knobRT.anchoredPosition = clamped;
            Value = clamped / _maxRadius;   // −1…+1
        }

        private void OnPointerUp(BaseEventData data)
        {
            var pdata = (PointerEventData)data;
            if (pdata.pointerId != _touchId) return;
            _dragging = false;
            _touchId  = -1;
            _knobRT.anchoredPosition = Vector2.zero;
            Value = Vector2.zero;
        }
    }

    #endregion
}
