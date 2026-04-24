using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Draws a semi-transparent button-mapping hint panel anchored to the top-right
/// corner of the screen. Press Tab to toggle visibility.
/// </summary>
public class ButtonHintsUI : MonoBehaviour
{
    [Tooltip("Background panel opacity (0–1).")]
    [SerializeField] [Range(0f, 1f)] private float panelAlpha = 0.75f;

    [Tooltip("Font size for hint text. Adjust for screen resolution.")]
    [SerializeField] private float fontSize = 14f;

    [Tooltip("Key used to toggle the panel.")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    // --- internal state ---
    private Canvas _canvas;
    private GameObject _panel;
    private bool _visible = true;

    // Button-mapping data: (key label, action description)
    private static readonly List<(string key, string action)> Bindings = new List<(string, string)>
    {
        ("T",      "Take Off"),
        ("H",      "Return to Home (RTH)"),
        ("R",      "Toggle Autopilot"),
        ("M",      "Mark Defect"),
        ("X",      "Reset Experiment"),
        ("",       ""),               // spacer
        ("W / S",  "Ascend / Descend"),
        ("A / D",  "Yaw Left / Right"),
        ("I / K",  "Forward / Backward"),
        ("J / L",  "Strafe Left / Right"),
        ("",       ""),               // spacer
        ("Click",  "Lock Mouse Look"),
        ("Alt",    "Release Cursor"),
    };

    private void Awake()
    {
        BuildUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            SetVisible(!_visible);
    }

    // -------------------------------------------------------------------------

    private void BuildUI()
    {
        // --- Canvas (Screen Space – Overlay, sorted above everything else) ---
        var canvasGO = new GameObject("ButtonHintsCanvas");
        canvasGO.transform.SetParent(transform, false);
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // --- Background panel ---
        _panel = new GameObject("HintsPanel");
        _panel.transform.SetParent(canvasGO.transform, false);

        var bg = _panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.05f, panelAlpha);

        var rt = _panel.GetComponent<RectTransform>();
        // Anchor to top-right corner
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-12f, -12f);   // 12 px margin from edge
        rt.sizeDelta = new Vector2(430f, 0f);             // width fixed; height auto below

        // --- Vertical layout to stack rows ---
        var layout = _panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 8, 8);
        layout.spacing = 2f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth  = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth  = true;
        layout.childForceExpandHeight = false;

        var fitter = _panel.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // --- Header ---
        AddHeader(_panel.transform, $"Keyboard Controls  <size={fontSize * 0.75f}>[Tab] toggle</size>");

        // Thin divider
        AddDivider(_panel.transform);

        // --- Binding rows ---
        foreach (var (key, action) in Bindings)
        {
            if (string.IsNullOrEmpty(key))
            {
                AddDivider(_panel.transform);
                continue;
            }
            AddRow(_panel.transform, key, action);
        }
    }

    private void AddHeader(Transform parent, string text)
    {
        var go = new GameObject("Header");
        go.transform.SetParent(parent, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize + 1f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.raycastTarget = false;

        var le = go.AddComponent<LayoutElement>();
        le.minHeight = fontSize * 1.6f;
    }

    private void AddDivider(Transform parent)
    {
        var go = new GameObject("Divider");
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.15f);
        img.raycastTarget = false;

        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 1f;
        le.preferredHeight = 1f;
    }

    private void AddRow(Transform parent, string key, string action)
    {
        // Row container
        var row = new GameObject($"Row_{key}");
        row.transform.SetParent(parent, false);

        var hLayout = row.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 6f;
        hLayout.childAlignment = TextAnchor.MiddleLeft;
        hLayout.childControlWidth  = false;
        hLayout.childControlHeight = true;
        hLayout.childForceExpandWidth  = false;
        hLayout.childForceExpandHeight = false;

        var rowLE = row.AddComponent<LayoutElement>();
        rowLE.minHeight = fontSize * 1.5f;

        // Key badge
        var badgeGO = new GameObject("Key");
        badgeGO.transform.SetParent(row.transform, false);

        var badgeBg = badgeGO.AddComponent<Image>();
        badgeBg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        badgeBg.raycastTarget = false;

        var badgeLE = badgeGO.AddComponent<LayoutElement>();
        badgeLE.minWidth  = 58f;
        badgeLE.minHeight = fontSize * 1.5f;

        var keyTextGO = new GameObject("KeyText");
        keyTextGO.transform.SetParent(badgeGO.transform, false);

        var keyText = keyTextGO.AddComponent<TextMeshProUGUI>();
        keyText.text = key;
        keyText.fontSize = fontSize - 1f;
        keyText.fontStyle = FontStyles.Bold;
        keyText.color = new Color(0.9f, 0.85f, 0.3f, 1f);   // gold/yellow key label
        keyText.alignment = TextAlignmentOptions.Center;
        keyText.raycastTarget = false;

        var keyRT = keyTextGO.GetComponent<RectTransform>();
        keyRT.anchorMin = Vector2.zero;
        keyRT.anchorMax = Vector2.one;
        keyRT.offsetMin = Vector2.zero;
        keyRT.offsetMax = Vector2.zero;

        // Action label
        var actionGO = new GameObject("Action");
        actionGO.transform.SetParent(row.transform, false);

        var actionText = actionGO.AddComponent<TextMeshProUGUI>();
        actionText.text = action;
        actionText.fontSize = fontSize;
        actionText.color = new Color(0.88f, 0.88f, 0.88f, 1f);
        actionText.alignment = TextAlignmentOptions.Left;
        actionText.raycastTarget = false;

        var actionLE = actionGO.AddComponent<LayoutElement>();
        actionLE.flexibleWidth = 1f;
        actionLE.minHeight = fontSize * 1.5f;
    }

    private void SetVisible(bool visible)
    {
        _visible = visible;
        if (_panel != null)
            _panel.SetActive(visible);
    }
}
