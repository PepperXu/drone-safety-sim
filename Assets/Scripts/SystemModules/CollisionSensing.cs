using System.Collections;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;

public class CollisionSensing : MonoBehaviour
{

    //Vector3[] distances = new Vector3[16];
    [SerializeField] LayerMask obstacleLayer;

    public static float surfaceCautionThreshold = 5.0f, surfaceWarningThreshold = 3.0f;
    //bool collisionSensingEnabled = false;
    const int steps = 16;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        RaySpray();
        CheckTrueDistToGround();
    }

    void RaySpray(){
        int index = 0;
        for(float angle = 360f/(2*steps); angle < 360f; angle += (360f/steps)){
            RaycastHit hit;
            if(Physics.Raycast(Communication.droneRb.transform.position, Quaternion.AngleAxis(angle, Vector3.up) * Communication.droneRb.transform.forward, out hit, 20f, obstacleLayer)){
                Communication.collisionData.distances[index] = hit.point - Communication.droneRb.transform.position;
                if(Communication.collisionData.distances[index].magnitude < surfaceWarningThreshold && DroneManager.currentControlType == DroneManager.ControlType.Autonomous){
                    DroneManager.autopilot_stop_flag = true;
                }
                
            } else {
                Communication.collisionData.distances[index] = Vector3.positiveInfinity;
            }
            index++;
        }
    }


    void CheckTrueDistToGround()
    {
        Ray rayDown = new Ray(Communication.droneRb.transform.position, Vector3.down);
        RaycastHit hit;
        if (Physics.Raycast(rayDown, out hit, float.MaxValue, obstacleLayer))
        {
            
            Communication.collisionData.v2ground = hit.point - Communication.droneRb.transform.position;
        }
        else
        {

            Communication.collisionData.v2ground = Vector3.down * float.PositiveInfinity;
        }
    }

}
