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

    public VisualizationType visType;

    public Transform visRoot;

    public static VisualizationType globalVisType;

    public bool showVisualization = true;

    // Start is called before the first frame update
    void Start()
    {
        if (!visRoot)
            visRoot = transform.GetChild(0);
    }

    // Update is called once per frame
    void Update()
    {
        visRoot.gameObject.SetActive(globalVisType == visType && showVisualization);
    }
}
