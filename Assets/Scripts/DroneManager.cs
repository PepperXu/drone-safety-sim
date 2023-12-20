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
        Planning,
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
    private bool preInBuffer = false;

    public static bool autopilot_flag = false, autopilot_stop_flag = false, rth_flag = false;

    [SerializeField] float predictStepLength = 1f;
    [SerializeField] int predictSteps = 3;

    [SerializeField] FlightPlanning flightPlanning;

    [SerializeField] Transform contingencyBuffer;

    [SerializeField] LayerMask realObstacleLayerMask;

    [SerializeField] AutopilotManager autopilotManager;


    // Start is called before the first frame update
    void Start()
    {
        state.GetState();
        originalPose = state.pose;
        currentFlightState = FlightState.Landed;
        currentMissionState = MissionState.Planning;
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

        if (currentFlightState == FlightState.Navigating || currentFlightState == FlightState.Hovering) {
            controlVisUpdater.SetControlVisActive(true);
            uiUpdater.enableSound = true;

            bool inBuffer = false;
            controlVisUpdater.vectorToNearestBufferBound = CheckPositionInContingencyBuffer(out inBuffer);

            if (rth_flag)
            {
                EngageAutoPilot(true);
                currentControlType = ControlType.Autonomous;
                currentMissionState = MissionState.Returning;
                rth_flag = false;
            }

            if(inBuffer != preInBuffer){
                if(inBuffer)
                    OnEnterBuffer();
                else
                    OnExitBuffer();
            }

            preInBuffer = inBuffer;

            if (currentMissionState == MissionState.Inspecting)
            {
                if (autopilot_flag)
                {
                    if (flightPlanning.isPathPlanned())
                    {
                        EngageAutoPilot(false);
                        currentControlType = ControlType.Autonomous;
                    }
                    autopilot_flag = false;
                }
            }

            if (currentFlightState == FlightState.Navigating){
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
            uiUpdater.enableSound = false;
            ic.EnableControl(false);
            if(currentFlightState == FlightState.Landing)
                controlVisUpdater.SetControlVisActive(false);
        }

        if (currentFlightState != FlightState.Landed)
        {
            bool hitGround = false;
            Vector3 vectorToGround = CheckDistToGround(out hitGround);
            vc.vectorToGround = vectorToGround;
            controlVisUpdater.vectorToGround = vectorToGround;
            uiUpdater.vpsHeight = vectorToGround.magnitude;
        } else {
            if(currentMissionState == MissionState.Returning){
                currentMissionState = MissionState.MovingToFlightZone;
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

    Vector3 CheckPositionInContingencyBuffer(out bool inBuffer){
        Vector3 localDronePos = contingencyBuffer.InverseTransformPoint(vc.transform.position);
        inBuffer = Mathf.Abs(localDronePos.x) < 0.5f && Mathf.Abs(localDronePos.y) < 0.5f && Mathf.Abs(localDronePos.z) < 0.5f;
        if (Mathf.Abs(localDronePos.y) < 0.5f && (Mathf.Abs(localDronePos.x) < 0.5f || Mathf.Abs(localDronePos.z) < 0.5f))
        {
            if (inBuffer)
            {
                if (Mathf.Abs(Mathf.Abs(localDronePos.x) - 0.5f) < Mathf.Abs(Mathf.Abs(localDronePos.z) - 0.5f))
                {
                    return -contingencyBuffer.right * (Mathf.Abs(localDronePos.x) - 0.5f) * Mathf.Sign(localDronePos.x) * contingencyBuffer.localScale.x;
                }
                else
                {
                    return -contingencyBuffer.forward * (Mathf.Abs(localDronePos.z) - 0.5f) * Mathf.Sign(localDronePos.z) * contingencyBuffer.localScale.z;
                }
            } else
            {
                if(Mathf.Abs(localDronePos.z) < 0.5f)
                {
                    return -contingencyBuffer.right * (Mathf.Abs(localDronePos.x) - 0.5f) * Mathf.Sign(localDronePos.x) * contingencyBuffer.localScale.x;
                } else
                {
                    return -contingencyBuffer.forward * (Mathf.Abs(localDronePos.z) - 0.5f) * Mathf.Sign(localDronePos.z) * contingencyBuffer.localScale.z;
                }
            }
        } else
        {
            return Vector3.positiveInfinity;
        }
    }

    void OnEnterBuffer(){
        if (currentMissionState == MissionState.MovingToFlightZone){
            currentMissionState = MissionState.Inspecting;
            if(currentSystemState != SystemState.Emergency)
                currentSystemState = SystemState.Healthy;
        }
    }

    void OnExitBuffer(){
        if (currentMissionState == MissionState.Inspecting){
            if(currentSystemState != SystemState.Emergency)
                currentSystemState = SystemState.Warning;
            currentMissionState = MissionState.MovingToFlightZone;
            autopilot_stop_flag = true;
        }
    }

    Vector3 CheckDistToGround(out bool hitGround)
    {
        Ray rayDown = new Ray(vc.transform.position, Vector3.down);
        RaycastHit hit;
        if (Physics.Raycast(rayDown, out hit, float.MaxValue, realObstacleLayerMask))
        {
            hitGround = true;
            return (hit.point - vc.transform.position);
        } else
        {
            hitGround = false;
            return Vector3.down * float.PositiveInfinity;
        }
    }

    void EngageAutoPilot(bool rth){
        autopilotManager.EnableAutopilot(true, rth);
    }

    void DisengageAutoPilot(){
        autopilotManager.EnableAutopilot(false);
    }

    
}
