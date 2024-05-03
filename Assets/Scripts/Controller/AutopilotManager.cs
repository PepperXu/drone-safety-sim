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

    //[SerializeField] FlightPlanning flightPlanning;
    //[SerializeField] VelocityControl vc;
    //[SerializeField] UIUpdater uiUpdater;
    //[SerializeField] WorldVisUpdater wordVis;

    //[SerializeField] Transform[] homePoints;

    [SerializeField] Transform Homepoint;
    float ground_offset = 0.2f;


    const float waitTime = 0.5f;
    float waitTimer = 0f;

    float autopilot_max_speed = 3.0f;
    float autopilot_slowing_start_dist = 5.0f;
    //public Vector3 vectorToBuildingSurface;

    bool photoTaken = false;
    //public Vector3 positionOffset;

    bool autopilot_initialized = false;

    // Start is called before the first frame update


    void OnEnable(){
        DroneManager.resetAllEvent.AddListener(ResetAutopilot);
        DroneManager.autopilotEvent.AddListener(EnableAutopilot);
        DroneManager.autopilotStopEvent.AddListener(StopAutopilot);
        DroneManager.returnToHomeEvent.AddListener(EnableRTH);
    }

    void OnDisable(){
        DroneManager.resetAllEvent.RemoveListener(ResetAutopilot);
        DroneManager.autopilotEvent.RemoveListener(EnableAutopilot);
        DroneManager.autopilotStopEvent.RemoveListener(StopAutopilot);
        DroneManager.returnToHomeEvent.RemoveListener(EnableRTH);
    }
    void Start()
    {
        //StartCoroutine(GetCurrentHomepointCoroutine());
    }

    void ResetAutopilot(){
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
                if(VelocityControl.currentFlightState == VelocityControl.FlightState.Navigating || VelocityControl.currentFlightState == VelocityControl.FlightState.Hovering)
                {
                    Vector3 offset = Homepoint.position - Communication.realPose.WorldPosition + Vector3.up * ground_offset;
                    Vector3 offsetXZ = new Vector3(offset.x, 0f, offset.z);

                    if(offsetXZ.magnitude > 0.5f)
                    {
                        Vector3 localDir = Communication.droneRb.transform.InverseTransformDirection(offsetXZ);
                        if (localDir.magnitude > autopilot_slowing_start_dist)
                        {
                            localDir = localDir.normalized * autopilot_max_speed;
                        } else {
                            localDir = localDir.normalized * localDir.magnitude/autopilot_slowing_start_dist*autopilot_max_speed;
                        }
                        DroneManager.desired_vy = localDir.z;
                        DroneManager.desired_vx = localDir.x;
                    } else
                    {
                        if(Mathf.Abs(offset.y) > 0.2f)
                        {
                            if(Mathf.Abs(offset.y) > autopilot_slowing_start_dist)
                            {
                                DroneManager.desired_height = Communication.realPose.WorldPosition.y + Mathf.Sign(offset.y) * autopilot_max_speed;
                            } else {
                                DroneManager.desired_height = Communication.realPose.WorldPosition.y + Mathf.Sign(offset.y) * autopilot_max_speed * (Mathf.Abs(offset.y)/autopilot_slowing_start_dist) ;
                            }
                        } 
                    }
                    DroneManager.desired_yaw = 0f;
                } else {
                    isAutopiloting = false;
                    isRTH = false;
                }
            }
            else
            {
                Vector3 target;

                if(currentWaypointIndex < Communication.flightTrajectory.Length){
                    target= Communication.flightTrajectory[currentWaypointIndex];

                    
                    Vector3 sensedPosition = Communication.positionData.virtualPosition;
                    //Debug.LogWarning("Moving to waypoint " + currentWaypointIndex);
                    Vector3 offset = target - sensedPosition;

                    //Debug.Log("current target offset" + offset);
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
                            //wordVis.currentWaypointIndex = this.currentWaypointIndex;
                            Communication.currentWaypointIndex = this.currentWaypointIndex;
                            waitTimer = 0f;
                            photoTaken = false;
                        }
                    }
                    else
                    {
                        Vector3 localDir = Communication.droneRb.transform.InverseTransformDirection(offset);
                        float heightTarget;
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
                        DroneManager.desired_height = heightTarget;
                        DroneManager.desired_vy = localDirXY.y;
                        DroneManager.desired_vx = localDirXY.x;
                        if(Communication.positionData.v2surf.magnitude < 10f){
                            Vector3 localVector = Communication.droneRb.transform.InverseTransformDirection(Communication.positionData.v2surf).normalized;
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
                            DroneManager.desired_yaw = angleOffset;
                        }
                    }
                }
            }
        }
    }


    void EnableAutopilot()
    {
        //vc.SetMaxPitchRoll(enable?0.175f:0.3f);

        isAutopiloting = true;
        isRTH = false;
        if(!autopilot_initialized){
            autopilot_initialized = true;
            
        } else {
            int i = this.currentWaypointIndex;
            Vector3 target = Communication.flightTrajectory[i];
            float shortestDistance = (Communication.positionData.virtualPosition - target).magnitude;
            while(i < Communication.flightTrajectory.Length - 1){
                i++;
                target = Communication.flightTrajectory[i];
                if((Communication.positionData.virtualPosition - target).magnitude > shortestDistance){
                    this.currentWaypointIndex = i-1;
                    break;
                } else {
                    shortestDistance = (Communication.positionData.virtualPosition - target).magnitude;
                    i++;
                }
            }
        }
        //wordVis.currentWaypointIndex = this.currentWaypointIndex;
        ExperimentServer.RecordData("Autopilot From Waypoint", this.currentWaypointIndex +"", "");
    }


    void EnableRTH(){
        isAutopiloting = true;
        isRTH = true;
        //int idx = GetCurrentHomepoint();
        ExperimentServer.RecordData("Returning To Homepoint", "", "");
    }   

    void StopAutopilot(){
        isAutopiloting = false;
        isRTH = false;
    }

    

    //IEnumerator GetCurrentHomepointCoroutine()
    //{
    //    while(true){
    //        GetCurrentHomepoint();
    //        yield return new WaitForSeconds(1f);
    //    }
    //}

    //int GetCurrentHomepoint(){
    //    float shortestDistance = float.MaxValue;
    //    int shortestDistIndex = 0;
    //    for(int i = 0; i < homePoints.Length; i++)
    //    {
    //        Transform homepoint = homePoints[i];
    //        Vector3 distance = homepoint.position - vc.transform.position;
    //        if(distance.magnitude < shortestDistance)
    //        {
    //            shortestDistance = distance.magnitude;
    //            currentHomepoint = homepoint;
    //            //wordVis.currentHomepoint = currentHomepoint;
    //            shortestDistIndex = i;
    //        }
    //    }
    //    return shortestDistIndex;
    //}

    //float GetMissionProgress()
    //{
    //    return (currentWaypointIndex + 1f) / flightPlanning.GetTotalWaypointCount();
    //}
}
