using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ControlVisUpdater : MonoBehaviour
{
    private bool visEnabled = false;
    [SerializeField] private LineRenderer dis2groundVis;
    [SerializeField] private LineRenderer futureTrajectory;
    
    [SerializeField] private LayerMask realObstacleLayerMask;

    [SerializeField] private Transform droneParent;

    private float dis2ground;

    public Vector3[] predictedPoints;

    void Update(){
        transform.position = droneParent.position;
        UpdateDistance2Ground();
        UpdateFutureTrajectory();
    }

    void UpdateDistance2Ground(){
        Ray rayDown = new Ray(transform.position, Vector3.down);
        RaycastHit hit;
        if(Physics.Raycast(rayDown, out hit, float.MaxValue, realObstacleLayerMask)){
            dis2ground = hit.distance;
            dis2groundVis.SetPosition(1, transform.InverseTransformPoint(hit.point));
            dis2groundVis.transform.GetChild(0).position = hit.point + (transform.position-hit.point).normalized*0.01f;
            dis2groundVis.transform.GetChild(1).GetChild(0).GetComponent<TextMeshPro>().text = "" + Mathf.Round(dis2ground*10f)/10f + " m";
        }
    }

    void UpdateFutureTrajectory(){
        List<Vector3> trajectory = new List<Vector3>
        {
            Vector3.zero
        };
        foreach(Vector3 predictedPoint in predictedPoints){
            trajectory.Add(predictedPoint);
        }
        futureTrajectory.positionCount = trajectory.Count;
        futureTrajectory.SetPositions(trajectory.ToArray());
    }

}
