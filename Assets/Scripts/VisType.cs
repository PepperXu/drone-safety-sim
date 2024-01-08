using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisType : MonoBehaviour
{
    public enum VisualizationType
    {
        MissionOnly,
        SafetyOnly,
        Both
    }

    [SerializeField] private VisualizationType visType;

    public Transform visRoot;

    public static VisualizationType globalVisType;

    public bool showVisualization = true;

    private VisualizationType originalVisType, hiddenVisType;

    private static bool isRevealing = false;

    //[SerializeField] private bool allowSwitchToBoth = false;

    // Start is called before the first frame update
    void Start()
    {
        if (!visRoot)
            visRoot = transform.GetChild(0);
        originalVisType = visType;
        hiddenVisType = visType;
    }

    // Update is called once per frame
    void Update()
    {
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
}
