using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutopilotManager : MonoBehaviour
{
    bool isAutopiloting = false;
    bool isRTH = false;
    //For Autopiloting
    int currentWaypointIndex = 0;

    [SerializeField] FlightPlanning flightPlanning;
    [SerializeField] VelocityControl vc;

    [SerializeField] Transform[] homePoints;

    Transform currentHomepoint;


    const float waitTime = 0.15f;
    float waitTimer = 0f;

    float autopilot_speed = 4f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Debug.LogWarning("AUTOPILOT: " + isAutopiloting);
        if(isAutopiloting){
            if (isRTH)
            {
                if(DroneManager.currentFlightState == DroneManager.FlightState.Navigating || DroneManager.currentFlightState == DroneManager.FlightState.Hovering)
                {
                    Vector3 offset = currentHomepoint.position - vc.transform.position;
                    Vector3 offsetXZ = new Vector3(offset.x, 0f, offset.z);

                    if(offsetXZ.magnitude > 0.01f)
                    {
                        Vector3 localDir = vc.transform.InverseTransformDirection(offsetXZ);
                        if (localDir.magnitude > autopilot_speed)
                        {
                            localDir = localDir.normalized * autopilot_speed;
                        }
                        vc.desired_vx = localDir.z;
                        vc.desired_vy = localDir.x;
                    } else
                    {
                        if(offset.y > 0.5f)
                        {
                            vc.desired_height = currentHomepoint.position.y;
                        }
                    }
                }
            }
            else
            {
                bool out_of_bound;
                Vector3 target = flightPlanning.GetCurrentWaypoint(currentWaypointIndex, out out_of_bound);
                if (!out_of_bound)
                {
                    Debug.LogWarning("Moving to waypoint " + currentWaypointIndex);
                    Vector3 offset = target - vc.transform.position;
                    if (offset.magnitude < 0.01f)
                    {
                        waitTimer += Time.deltaTime;
                        if (waitTimer >= waitTime)
                        {
                            currentWaypointIndex++;
                            waitTimer = 0f;
                        }
                    }
                    else
                    {
                        Vector3 localDir = vc.transform.InverseTransformDirection(offset);
                        Vector2 localDirXY = new Vector2(localDir.x, localDir.z);
                        if (localDirXY.magnitude > autopilot_speed)
                        {
                            localDirXY = localDirXY.normalized * autopilot_speed;
                        }
                        vc.desired_height = target.y;
                        vc.desired_vx = localDirXY.y;
                        vc.desired_vy = localDirXY.x;
                    }
                }
            }
        }
    }

    public void EnableAutopilot(bool enable){
        isAutopiloting = enable;
        isRTH = false;
    }

    public void EnableAutopilot(bool enable, bool rth)
    {
        isAutopiloting = enable;
        isRTH = rth;
        if (rth)
        {
            SetCurrentHomepoint();
        }
    }

    void SetCurrentHomepoint()
    {
        float shortestDistance = float.MaxValue;
        foreach(Transform homepoint in homePoints)
        {
            Vector3 distance = homepoint.position - vc.transform.position;
            if(distance.magnitude < shortestDistance)
            {
                shortestDistance = distance.magnitude;
                currentHomepoint = homepoint;
            }
        }
    }
}
