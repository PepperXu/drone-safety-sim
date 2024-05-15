using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Utilities.Tweenables.Primitives;

public class InteractiveCamera : MonoBehaviour
{
    [SerializeField] private Camera fpvCam;
    [SerializeField] private LayerMask buildingCollision;
    [SerializeField] private LayerMask correctMarkTrigger;

    public static float raycastLengthThreshold = 20f;
    //[SerializeField] private WorldVisUpdater worldVisUpdater;
    //[SerializeField] private UIUpdater uIUpdater;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void MarkDefectFromCamera(RaycastHit hit){
        Vector3 localHitPoint = hit.transform.InverseTransformPoint(hit.point);
        //Debug.Log(localHitPoint);
        Vector2 viewportPoint = (Vector2)localHitPoint + Vector2.one * 0.5f;
        Ray ray = fpvCam.ViewportPointToRay(viewportPoint);

        RaycastHit buildingHit;
        if(Physics.Raycast(ray, out buildingHit, raycastLengthThreshold, buildingCollision)){
            DroneManager.mark_defect_flag = true;
            Communication.markDefectHit = buildingHit;
        }
        
        RaycastHit correctMarkHit;
        if(Physics.Raycast(ray, out correctMarkHit, raycastLengthThreshold, correctMarkTrigger)){
            ExperimentServer.RecordEventData("Defect marked at", correctMarkHit.transform.gameObject.name + "|distance:" + (Communication.realPose.WorldPosition - (Communication.positionData.virtualPosition + Communication.positionData.v2surf)).magnitude , "correct mark?true");
        } else {
            ExperimentServer.RecordEventData("Defect marked at", buildingHit.point.x + "|" + buildingHit.point.y + "|" + buildingHit.point.z, "correct mark?false");
        }
    }
}
