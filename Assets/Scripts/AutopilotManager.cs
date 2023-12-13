using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutopilotManager : MonoBehaviour
{
    static bool isAutopiloting = false;
    //For Autopiloting
    int currentWaypointIndex = 0;

    [SerializeField] FlightPlanning flightPlanning;
    [SerializeField] VelocityControl vc;

    const float waitTime = 0.5f;
    float waitTimer = 0f;

    float autopilot_speed = 4f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(isAutopiloting){
            bool out_of_bound;
            Vector3 target = flightPlanning.GetCurrentWaypoint(currentWaypointIndex, out out_of_bound);
            if(!out_of_bound){
                Vector3 offset = target - vc.transform.position;
                if(offset.magnitude < 0.01f){
                    waitTimer += Time.deltaTime;
                    if(waitTimer >= waitTime){
                        currentWaypointIndex++;
                        waitTimer = 0f;
                    }
                } else {
                    Vector3 localDir = vc.transform.InverseTransformDirection(offset);
                    Vector2 localDirXY = new Vector2(localDir.x, localDir.z);
                    if(localDirXY.magnitude > autopilot_speed){
                        localDirXY = localDirXY.normalized * autopilot_speed;
                    }
                    vc.desired_height = target.y;
                    vc.desired_vx = localDirXY.y;
                    vc.desired_vy = localDirXY.x;
                }
            }
        }
    }

    public static void EnableAutopilot(bool enable){
        isAutopiloting = enable;
    }
}
