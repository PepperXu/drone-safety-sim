using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VisType : MonoBehaviour
{
    public enum VisualizationType
    {
        MissionOnly,
        SafetyOnly,
        Both,
        None
    }

    [SerializeField] private VisualizationType visType;

    public Transform visRoot;

    public static VisualizationType globalVisType;

    public bool showVisualization = true;

    private VisualizationType originalVisType, hiddenVisType;

    private static bool isRevealing = false;

    [SerializeField]
    private SpriteRenderer[] sprites;
    [SerializeField]
    private Image[] images;
    [SerializeField]
    private TextMeshPro[] texts;
    [SerializeField]
    private TextMeshProUGUI[] textUIs;
    [SerializeField]
    private Renderer[] renderers;

    private List<Color> initialColors = new List<Color>();

    float sigLostAlpha = 0.25f;
    float sigAbnormalAlpha = 0.25f;
    float normalAlpha = 1f;
    bool initialized = false;

    //[SerializeField] private bool allowSwitchToBoth = false;

    // Start is called before the first frame update
    void OnEnable()
    {
        if(initialized)
            return;
        if (!visRoot)
            visRoot = transform.GetChild(0);
        originalVisType = visType;
        hiddenVisType = visType;
        foreach(SpriteRenderer sprite in sprites){
            initialColors.Add(sprite.color);
        }
        foreach(Image image in images){
            initialColors.Add(image.color);
        }
        foreach(TextMeshPro text in texts){
            initialColors.Add(text.color);
        }
        foreach(TextMeshProUGUI textUI in textUIs){
            initialColors.Add(textUI.color);
        }
        foreach(Renderer renderer in renderers){
            initialColors.Add(renderer.material.color);
        }
        initialized = true;
    }

    // Update is called once per frame
    void Update()
    {
        if(globalVisType == VisualizationType.None){
            visRoot.gameObject.SetActive(false);
            return;
        }
        if(isRevealing) visType = hiddenVisType; else visType = originalVisType;
        visRoot.gameObject.SetActive((globalVisType == visType || visType == VisualizationType.Both || globalVisType == VisualizationType.Both) && showVisualization);
    }

    public static void SwitchVisType()
    {
        if (globalVisType == VisualizationType.MissionOnly)
            globalVisType = VisualizationType.SafetyOnly;
        else
            globalVisType = VisualizationType.MissionOnly;
    }

    public void SwitchHiddenVisTypeLocal(bool isBoth){
        hiddenVisType = isBoth?VisualizationType.Both:VisualizationType.SafetyOnly;
    }

    public static void RevealHiddenVisType(bool reveal){
        isRevealing = reveal;
    }

    public void SetTransparency(int level){
        int i = 0;
        foreach(SpriteRenderer sprite in sprites){
            Color c = initialColors[i];
            c.a = initialColors[i].a * (level == 0 ? normalAlpha: (level == 1?sigAbnormalAlpha: sigLostAlpha));
            sprite.color = c;
            i++;
        }
        foreach(Image image in images){
            Color c = initialColors[i];
            c.a = initialColors[i].a * (level == 0 ? normalAlpha: (level == 1?sigAbnormalAlpha: sigLostAlpha));
            image.color = c;
            i++;
        }
        foreach(TextMeshPro text in texts){
            Color c = initialColors[i];
            c.a = initialColors[i].a * (level == 0 ? normalAlpha: (level == 1?sigAbnormalAlpha: sigLostAlpha));
            text.color = c;
            i++;
        }
        foreach(TextMeshProUGUI textUI in textUIs){
            Color c = initialColors[i];
            c.a = initialColors[i].a * (level == 0 ? normalAlpha: (level == 1?sigAbnormalAlpha: sigLostAlpha));
            textUI.color = c;
            i++;
        }
        foreach(Renderer renderer in renderers){
            Color c = initialColors[i];
            c.a = initialColors[i].a * (level == 0 ? normalAlpha: (level == 1?sigAbnormalAlpha: sigLostAlpha));
            renderer.material.color = c;
            i++;
        }
    }
}