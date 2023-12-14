using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DroneManager : MonoBehaviour
{
    public enum FlightState{
        Landed,
        TakingOff,
        Hovering,
        Navigating,
        Landing
    }

    public enum MissionState{
        MovingToFlightZone, //When landed, or just take off
        Inspecting, //When in flight zone
        //Transiting,
        Returning //When path completed or RTH is triggered.

    }

    public enum SystemState{
        Healthy,
        Warning,
        Emergency
    }



    public enum ControlType
    {
        Autonomous,
        Manual
    }

    [SerializeField] private UIUpdater uiUpdater;
    [SerializeField] private ControlVisUpdater controlVisUpdater;
    [SerializeField] private WorldVisUpdater worldVisUpdater;
    [SerializeField] private StateFinder state;
    [SerializeField] private VelocityControl vc;
    [SerializeField] private InputControl ic;
    
    //private VisType[] safeVis;
    //private VisType[] misVis;

    public static FlightState currentFlightState = FlightState.Landed;
    public static SystemState currentSystemState = SystemState.Healthy;
    public static ControlType currentControlType = ControlType.Manual;
    public static MissionState currentMissionState = MissionState.MovingToFlightZone;

    private StateFinder.Pose originalPose;

    private bool controlActive = false;

    public static bool autopilot_flag = false, autopilot_stop_flag = false, rth_flag = false;

    [SerializeField] float predictStepLength = 1f;
    [SerializeField] int predictSteps = 3;

    [SerializeField] FlightPlanning flightPlanning;

    [SerializeField] Transform contingencyBuffer;



    // Start is called before the first frame update
    void Start()
    {
        state.GetState();
        originalPose = state.pose;
        currentFlightState = FlightState.Landed;
        currentMissionState = MissionState.MovingToFlightZone;
        currentControlType = ControlType.Manual;
        VisType.globalVisType = VisType.VisualizationType.MissionOnly;
        controlVisUpdater.SetControlVisActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if(currentFlightState == FlightState.TakingOff){
            if(Mathf.Abs(state.Altitude - vc.desired_height)< 0.1f){
                currentFlightState = FlightState.Hovering;
                ic.EnableControl(true);
            }
        }

        if(currentFlightState == FlightState.Navigating || currentFlightState == FlightState.Hovering){
            controlVisUpdater.SetControlVisActive(true);

            if(currentFlightState == FlightState.Navigating){
                if(Vector3.Magnitude(state.pose.WorldAcceleration) < 0.1f && Vector3.Magnitude(state.pose.WorldVelocity) < 0.1f){
                    currentFlightState = FlightState.Hovering;
                } else {
                    if(vc.desired_vx == 0 && vc.desired_vy == 0 && vc.desired_yaw == 0 && vc.height_diff == 0){ 
                        controlActive = false;
                    } else {
                        controlActive = true;
                    }
                    PredictFutureTrajectory();
                }
            } 

            if(currentFlightState == FlightState.Hovering){
                if(vc.desired_vx != 0 || vc.desired_vy != 0 || vc.desired_yaw != 0 || vc.height_diff != 0){
                    currentFlightState = FlightState.Navigating;
                    controlActive = true;
                }else{
                    controlVisUpdater.predictedPoints = new Vector3[0];
                }
            }
        } else
        {
            ic.EnableControl(false);
            if(currentFlightState == FlightState.Landing)
                controlVisUpdater.SetControlVisActive(false);
        }

        if(currentMissionState == MissionState.MovingToFlightZone){
            if(InContingencyBuffer()){
                currentMissionState = MissionState.Inspecting;
            }
        } else if (currentMissionState == MissionState.Inspecting){
            if(autopilot_flag){
                if(flightPlanning.isPathPlanned()){
                    EngageAutoPilot();
                    currentControlType = ControlType.Autonomous;
                }
                autopilot_flag = false;
            }
            if(!InContingencyBuffer()){
                currentSystemState = SystemState.Warning;
                autopilot_stop_flag = true;
            }
        }

        if(autopilot_stop_flag){
            DisengageAutoPilot();
            currentControlType = ControlType.Manual;
            autopilot_stop_flag = false;
        }

    }

    void PredictFutureTrajectory(){
        List<Vector3> trajectory = new List<Vector3>();
        if(controlActive){
            Vector3 travelOffset = Vector3.zero;
            float futureTimer = 0;
            for(int i = 0; i < predictSteps; i++){
                futureTimer += predictStepLength;
                travelOffset = state.pose.WorldVelocity * futureTimer + 0.5f * state.pose.WorldAcceleration * futureTimer * futureTimer;
                trajectory.Add(travelOffset);
            }
        } else {
            Vector3 travelOffset = Vector3.zero;
            float futureTimer = 0;
            for (int i = 0; i < predictSteps; i++){
                futureTimer += predictStepLength;
                if ((state.pose.WorldVelocity.magnitude - state.pose.WorldAcceleration.magnitude * futureTimer) <= 0f){
                    float timeUntilStop = state.pose.WorldVelocity.magnitude/state.pose.WorldAcceleration.magnitude;
                    travelOffset += state.pose.WorldVelocity * timeUntilStop + 0.5f * state.pose.WorldAcceleration * timeUntilStop * timeUntilStop;
                    trajectory.Add(travelOffset);
                    break;
                } else {
                    travelOffset += state.pose.WorldVelocity * futureTimer + 0.5f * state.pose.WorldAcceleration * futureTimer * futureTimer;
                    trajectory.Add(travelOffset);
                }
            }
        }
        controlVisUpdater.predictedPoints = trajectory.ToArray();
    }

    bool InContingencyBuffer(){
        Vector3 localDronePos = contingencyBuffer.InverseTransformPoint(vc.transform.position);
        return Mathf.Abs(localDronePos.x) < 0.5f && 
        Mathf.Abs(localDronePos.y) < 0.5f &&
        Mathf.Abs(localDronePos.z) < 0.5f;
    }

    void EngageAutoPilot(){
        AutopilotManager.EnableAutopilot(true);
    }

    void DisengageAutoPilot(){
        AutopilotManager.EnableAutopilot(false);
    }

    
}
