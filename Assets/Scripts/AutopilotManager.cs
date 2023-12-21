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

    float autopilot_max_speed = 6f;
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

                    if(offsetXZ.magnitude > 0.2f)
                    {
                        Vector3 localDir = vc.transform.InverseTransformDirection(offsetXZ);
                        if (localDir.magnitude > autopilot_max_speed)
                        {
                            localDir = localDir.normalized * autopilot_max_speed;
                        }
                        vc.desired_vx = localDir.z;
                        vc.desired_vy = localDir.x;
                    } else
                    {
                        if(Mathf.Abs(offset.y) > 0.5f)
                        {
                            if(Mathf.Abs(offset.y) > autopilot_max_speed)
                            {
                                vc.desired_height = vc.transform.position.y + Mathf.Sign(offset.y) * autopilot_max_speed;
                            } else {
                                vc.desired_height = currentHomepoint.position.y;
                            }
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
                        float heightTarget = target.y;
                        if(Mathf.Abs(offset.y) > autopilot_max_speed)
                        {
                            heightTarget = autopilot_max_speed * Mathf.Sign(offset.y) + vc.transform.position.y;
                        }
                        Vector2 localDirXY = new Vector2(localDir.x, localDir.z);
                        if (localDirXY.magnitude > autopilot_max_speed)
                        {
                            localDirXY = localDirXY.normalized * autopilot_max_speed;
                        }
                        vc.desired_height = heightTarget;
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
