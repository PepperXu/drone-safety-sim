using System.Collections;
using System.Collections.Generic;
using Unity.Properties;
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
    //public Vector3 positionOffset;

    bool autopilot_initialized = false;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(GetCurrentHomepointCoroutine());
    }

    public void ResetAutopilot(){
        autopilot_initialized = false;
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
                    Vector3 sensedPosition = PositionalSensorSimulator.dronePositionVirtual;
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
                            //uiUpdater.missionProgress = GetMissionProgress();
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


    public void EnableAutopilot(bool enable, bool rth)
    {
        //vc.SetMaxPitchRoll(enable?0.175f:0.3f);

        isAutopiloting = enable;
        isRTH = rth;
        if (rth)
        {
            if(enable){
                int idx = GetCurrentHomepoint();
                ExperimentServer.RecordData("Start Returning To Homepoint", idx + "", "");
            } else {
                ExperimentServer.RecordData("Stop Returning To Homepoint", "", "");
            }

        } else {
            if(enable){
                if(!autopilot_initialized){
                    autopilot_initialized = true;
                    ExperimentServer.RecordData("Start Inspection From Waypoint", "0", "");
                } else {
                    int i = this.currentWaypointIndex;
                    bool out_of_bound;
                    Vector3 target = flightPlanning.GetCurrentWaypoint(i, out out_of_bound);
                    float shortestDistance = (PositionalSensorSimulator.dronePositionVirtual - target).magnitude;
                    while(!out_of_bound){
                        i++;
                        target = flightPlanning.GetCurrentWaypoint(i, out out_of_bound);
                        if((PositionalSensorSimulator.dronePositionVirtual - target).magnitude > shortestDistance || out_of_bound){
                            this.currentWaypointIndex = i-1;
                            break;
                        } else {
                            shortestDistance = (PositionalSensorSimulator.dronePositionVirtual - target).magnitude;
                            i++;
                        }
                    }
                }

                wordVis.currentWaypointIndex = this.currentWaypointIndex;
                ExperimentServer.RecordData("Start Inspection From Waypoint", this.currentWaypointIndex +"", "");
            } else {
                ExperimentServer.RecordData("Stop Inspection", "", "");
            }
        }
    }

    IEnumerator GetCurrentHomepointCoroutine()
    {
        while(true){
            GetCurrentHomepoint();
            yield return new WaitForSeconds(1f);
        }
    }

    int GetCurrentHomepoint(){
        float shortestDistance = float.MaxValue;
        int shortestDistIndex = 0;
        for(int i = 0; i < homePoints.Length; i++)
        {
            Transform homepoint = homePoints[i];
            Vector3 distance = homepoint.position - vc.transform.position;
            if(distance.magnitude < shortestDistance)
            {
                shortestDistance = distance.magnitude;
                currentHomepoint = homepoint;
                wordVis.currentHomepoint = currentHomepoint;
                shortestDistIndex = i;
            }
        }
        return shortestDistIndex;
    }

    //float GetMissionProgress()
    //{
    //    return (currentWaypointIndex + 1f) / flightPlanning.GetTotalWaypointCount();
    //}
}
