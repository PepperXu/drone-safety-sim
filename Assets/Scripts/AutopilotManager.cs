using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Utilities.Tweenables.Primitives;

public class AutopilotManager : MonoBehaviour
{
    bool isAutopiloting = false;
    bool isRTH = false;
    //For Autopiloting
    int currentWaypointIndex = 0;

    [SerializeField] FlightPlanning flightPlanning;
    [SerializeField] VelocityControl vc;
    [SerializeField] UIUpdater uiUpdater;
    [SerializeField] WorldVisUpdater wordVis;

    [SerializeField] Transform[] homePoints;

    Transform currentHomepoint;
    float ground_offset = 0.2f;


    const float waitTime = 0.5f;
    float waitTimer = 0f;

    float autopilot_max_speed = 3.0f;
    float autopilot_slowing_start_dist = 5.0f;
    public Vector3 vectorToBuildingSurface;

    bool photoTaken = false;
    public Vector3 positionOffset;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void ResetAutopilot(){
        isAutopiloting = false;
        isRTH = false;
        currentWaypointIndex = 0;
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.LogWarning("AUTOPILOT: " + isAutopiloting);
        if(isAutopiloting){
            if (isRTH)
            {
                if(DroneManager.currentFlightState == DroneManager.FlightState.Navigating || DroneManager.currentFlightState == DroneManager.FlightState.Hovering)
                {
                    Vector3 offset = currentHomepoint.position - vc.transform.position + Vector3.up * ground_offset;
                    Vector3 offsetXZ = new Vector3(offset.x, 0f, offset.z);

                    if(offsetXZ.magnitude > 0.5f)
                    {
                        Vector3 localDir = vc.transform.InverseTransformDirection(offsetXZ);
                        if (localDir.magnitude > autopilot_slowing_start_dist)
                        {
                            localDir = localDir.normalized * autopilot_max_speed;
                        } else {
                            localDir = localDir.normalized * localDir.magnitude/autopilot_slowing_start_dist*autopilot_max_speed;
                        }
                        vc.desired_vx = localDir.z;
                        vc.desired_vy = localDir.x;
                    } else
                    {
                        if(Mathf.Abs(offset.y) > 0.2f)
                        {
                            if(Mathf.Abs(offset.y) > autopilot_slowing_start_dist)
                            {
                                vc.desired_height = vc.transform.position.y + Mathf.Sign(offset.y) * autopilot_max_speed;
                            } else {
                                vc.desired_height = vc.transform.position.y + Mathf.Sign(offset.y) * autopilot_max_speed * (Mathf.Abs(offset.y)/autopilot_slowing_start_dist) ;
                            }
                        } 
                    }
                } else {
                    isAutopiloting = false;
                    isRTH = false;
                }
            }
            else
            {
                bool out_of_bound;
                Vector3 target = flightPlanning.GetCurrentWaypoint(currentWaypointIndex, out out_of_bound);
                if (!out_of_bound)
                {
                    Vector3 sensedPosition = vc.transform.position + positionOffset;
                    //Debug.LogWarning("Moving to waypoint " + currentWaypointIndex);
                    Vector3 offset = target - sensedPosition;
                    if (offset.magnitude < 0.5f)
                    {
                        waitTimer += Time.deltaTime;
                        if(waitTimer >= waitTime/2f & !photoTaken){
                            DroneManager.take_photo_flag = true;
                            photoTaken = true;
                        }
                        if (waitTimer >= waitTime)
                        {
                            currentWaypointIndex++;
                            uiUpdater.missionProgress = GetMissionProgress();
                            wordVis.missionProgress = GetMissionProgress();
                            wordVis.currentWaypointIndex = this.currentWaypointIndex;
                            waitTimer = 0f;
                            photoTaken = false;
                        }
                    }
                    else
                    {
                        Vector3 localDir = vc.transform.InverseTransformDirection(offset);
                        float heightTarget = target.y;
                        if(Mathf.Abs(offset.y) > autopilot_slowing_start_dist)
                        {
                            heightTarget = autopilot_max_speed * Mathf.Sign(offset.y) + sensedPosition.y;
                        } else {
                            heightTarget = autopilot_max_speed * (Mathf.Abs(offset.y)/autopilot_slowing_start_dist) * Mathf.Sign(offset.y) + sensedPosition.y;
                        }
                        Vector2 localDirXY = new Vector2(localDir.x, localDir.z);
                        if (localDirXY.magnitude > autopilot_slowing_start_dist)
                        {
                            localDirXY = localDirXY.normalized * autopilot_max_speed;
                        } else {
                            localDirXY = localDirXY.normalized * localDirXY.magnitude/autopilot_slowing_start_dist*autopilot_max_speed;
                        }
                        vc.desired_height = heightTarget;
                        vc.desired_vx = localDirXY.y;
                        vc.desired_vy = localDirXY.x;
                        if(vectorToBuildingSurface.magnitude < 10f){
                            Vector3 localVector = vc.transform.InverseTransformDirection(vectorToBuildingSurface).normalized;
                            Vector2 localVectorXY = new Vector2(localVector.x, localVector.z);
                            float angleOffset = Vector2.SignedAngle(-Vector2.up, localVectorXY);
                            while(angleOffset > 180f){
                                angleOffset -= 360f;
                            }
                            while(angleOffset <= -180f){
                                angleOffset += 360f;
                            }
                            if(angleOffset > 5f){
                                angleOffset = 5f;
                            }
                            if(angleOffset < -5f){
                                angleOffset = -5f;
                            }
                            vc.desired_yaw = angleOffset;
                        }
                    }
                }
            }
        }
    }

    public void EnableAutopilot(bool enable){
        isAutopiloting = enable;
        wordVis.currentWaypointIndex = this.currentWaypointIndex;
        isRTH = false;
    }

    public void EnableAutopilot(bool enable, bool rth)
    {
        isAutopiloting = enable;
        isRTH = rth;
        if (rth)
        {
            SetCurrentHomepoint();
        } else {
            wordVis.currentWaypointIndex = this.currentWaypointIndex;
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

    float GetMissionProgress()
    {
        return (currentWaypointIndex + 1f) / flightPlanning.GetTotalWaypointCount();
    }
}
