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
        InFlightZone, //When in flight zone
        Inspecting, //When autopiloting
        AutopilotInterupted,
        Returning //When path completed or RTH is triggered.

    }

    public enum SafetyState{
        Healthy,
        Caution,
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
    public static SafetyState currentSafetyState {get; private set;}
    public static ControlType currentControlType {get; private set;}
    public static MissionState currentMissionState {get; private set;}

    //private StateFinder.Pose originalPose;

    private bool controlActive = false;

    public static bool autopilot_flag = false, autopilot_stop_flag = false, rth_flag = false, take_photo_flag = false, mark_defect_flag = false, finish_planning_flag = false;

    [SerializeField] float predictStepLength = 1f;
    [SerializeField] int predictSteps = 3;

    [SerializeField] FlightPlanning flightPlanning;

    [SerializeField] Transform contingencyBuffer;

    [SerializeField] LayerMask realObstacleLayerMask;

    [SerializeField] AutopilotManager autopilotManager;
    [SerializeField] CameraController camController;

    [SerializeField] Transform buildingCollision;

    [SerializeField] Battery battery;
    [SerializeField] PositionalSensorSimulator posSensor;
    [SerializeField] RandomPulseNoise wind;

    public static float bufferCautionThreahold = 2.5f, surfaceCautionThreshold = 6.0f, surfaceWarningThreshold = 2.0f;
    private float windStrengthWarningCoolDownTimer, windStrengthWarningCoolDownTime = 3f;


    // Start is called before the first frame update
    void Start()
    {
        ResetAllStates();
    }

    public void ResetAllStates(){
        finish_planning_flag = false;
        currentFlightState = FlightState.Landed;
        currentMissionState = MissionState.Planning;
        currentControlType = ControlType.Manual;
        currentSafetyState = SafetyState.Healthy;
        VisType.globalVisType = VisType.VisualizationType.MissionOnly;
        controlVisUpdater.SetControlVisActive(false);
        autopilotManager.ResetAutopilot();
        flightPlanning.ResetPathPlanning();
        vc.transform.localPosition = Vector3.zero;
        vc.transform.localEulerAngles = Vector3.zero;
        vc.ResetVelocityControl();
        worldVisUpdater.ResetWorldVis();
        uiUpdater.ResetUI();
        camController.ResetCamera();
        battery.ResetBattery();
        posSensor.ResetSignalLevel();
        autopilot_flag = false;
        autopilot_stop_flag = false;  
        rth_flag = false; 
        take_photo_flag = false;
        mark_defect_flag = false;
    }

    // Update is called once per frame
    void Update()
    {

        if(currentMissionState == MissionState.Planning && finish_planning_flag){
            finish_planning_flag = false;
            currentMissionState = MissionState.MovingToFlightZone;
        }

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
            Vector3 v2bound = CheckPositionInContingencyBuffer(out inBuffer);
            worldVisUpdater.inBuffer = inBuffer;
            worldVisUpdater.distToBuffer = v2bound.magnitude;
            controlVisUpdater.inBuffer = inBuffer;
            controlVisUpdater.vectorToNearestBufferBound = v2bound;
            Vector3 v2surf = CheckDistanceToBuildingSurface();
            controlVisUpdater.vectorToNearestSurface = v2surf;
            autopilotManager.vectorToBuildingSurface = v2surf;
            worldVisUpdater.vectorToSurface = v2surf;
            uiUpdater.vector2surface = v2surf;
            
            if(currentMissionState != MissionState.Inspecting && currentMissionState != MissionState.Returning)
                currentMissionState = inBuffer?MissionState.InFlightZone:MissionState.MovingToFlightZone;


            if (rth_flag)
            {
                EngageAutoPilot(true);
                currentControlType = ControlType.Autonomous;
                currentMissionState = MissionState.Returning;
                rth_flag = false;
            }

            if(mark_defect_flag){
                mark_defect_flag = false;
                camController.TakePhoto(true);
                worldVisUpdater.SpawnCoverageObject(true); 
            }

            if (currentMissionState == MissionState.InFlightZone)
            {
                if (autopilot_flag)
                {
                    if (flightPlanning.isPathPlanned())
                    {
                        EngageAutoPilot(false);
                        currentControlType = ControlType.Autonomous;
                        currentMissionState = MissionState.Inspecting;
                    }
                    autopilot_flag = false;
                }
            } else if(currentMissionState == MissionState.Inspecting){
                if(!inBuffer){
                    autopilot_stop_flag = true;
                }
                if(take_photo_flag)
                {
                    take_photo_flag = false;
                    camController.TakePhoto(false);
                    worldVisUpdater.SpawnCoverageObject(false);
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

            if(autopilot_stop_flag){
                DisengageAutoPilot();
                currentControlType = ControlType.Manual;
                currentMissionState = MissionState.AutopilotInterupted;
                autopilot_stop_flag = false;
            }

            UpdateSafetyState(inBuffer, v2bound.magnitude, v2surf.magnitude, battery.GetBatteryLevel(), battery.GetBatteryVoltage(), posSensor.GetSignalLevel(), wind.GetCurrentWindStrength());

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
        Vector3 localDronePos = contingencyBuffer.InverseTransformPoint(PositionalSensorSimulator.dronePositionVirtual);
        inBuffer = Mathf.Abs(localDronePos.x) < 0.5f && Mathf.Abs(localDronePos.y) < 0.5f && Mathf.Abs(localDronePos.z) < 0.5f;
        if (Mathf.Abs(localDronePos.y) < 0.5f && (Mathf.Abs(localDronePos.x) < 0.5f || Mathf.Abs(localDronePos.z) < 0.5f))
        {
            if (inBuffer)
            {
                Vector3 vectorToBufferWall;
                if (Mathf.Abs(Mathf.Abs(localDronePos.x) - 0.5f) < Mathf.Abs(Mathf.Abs(localDronePos.z) - 0.5f))
                {
                    vectorToBufferWall = -contingencyBuffer.right * (Mathf.Abs(localDronePos.x) - 0.5f) * Mathf.Sign(localDronePos.x) * contingencyBuffer.localScale.x;
                }
                else
                {
                    vectorToBufferWall = -contingencyBuffer.forward * (Mathf.Abs(localDronePos.z) - 0.5f) * Mathf.Sign(localDronePos.z) * contingencyBuffer.localScale.z;
                }
                return vectorToBufferWall;
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

    void UpdateSafetyState(bool inBuffer, float distToBuffer, float distToSurface, int batteryLevel, float voltage, int positional_signal_level, float wind_strength){
        if(currentSafetyState == SafetyState.Emergency)
            return;
        

        SafetyState tempState = SafetyState.Healthy;

        if(!inBuffer){
            tempState = SafetyState.Caution;
        }
        if(distToSurface < surfaceCautionThreshold){
            tempState = SafetyState.Caution;
        }
        if(batteryLevel == 2){
            
            tempState = SafetyState.Caution;
        }
        if(voltage < 10f){
            tempState = SafetyState.Caution;
        }
        if(wind_strength > 20f){
            tempState = SafetyState.Caution;
        }


        if(distToSurface < surfaceWarningThreshold){
            tempState = SafetyState.Warning;
        }
        if(batteryLevel == 1){
            tempState = SafetyState.Warning;
        }
        if(voltage < 9f){
            tempState = SafetyState.Warning;
        }
        if(vc.collisionHitCount > 0){
            tempState = SafetyState.Warning;
        }
        if(positional_signal_level == 2){
            tempState = SafetyState.Caution;
        }
        if(positional_signal_level == 1){
            tempState = SafetyState.Warning;
        }
        if(positional_signal_level == 0){
            tempState = SafetyState.Warning;
        }
        if(wind_strength > 40f){
            tempState = SafetyState.Warning;
        }
        

        if(vc.collisionHitCount > 0 || vc.out_of_balance){
            tempState = SafetyState.Emergency;
        }
        if(batteryLevel == 0){
            tempState = SafetyState.Emergency;
        } 

        currentSafetyState = tempState;

    }

    Vector3 CheckDistanceToBuildingSurface(){
        Vector3 localDronePos = buildingCollision.InverseTransformPoint(PositionalSensorSimulator.dronePositionVirtual);
        if(Mathf.Abs(localDronePos.y) >= 0.5f)
            return Vector3.positiveInfinity;
        
        if(Mathf.Abs(localDronePos.x) < 0.5f && Mathf.Abs(localDronePos.z) < 0.5f)
            return Vector3.positiveInfinity;

        if(Mathf.Abs(localDronePos.x) >= 0.5f && Mathf.Abs(localDronePos.z) >= 0.5f)
            return Vector3.positiveInfinity;

        if(Mathf.Abs(localDronePos.x) < 0.5f)
            return -buildingCollision.forward * (Mathf.Abs(localDronePos.z) - 0.5f) * Mathf.Sign(localDronePos.z) * buildingCollision.localScale.z;
        
        return -buildingCollision.right * (Mathf.Abs(localDronePos.x) - 0.5f) * Mathf.Sign(localDronePos.x) * buildingCollision.localScale.x; 
    }

    Vector3 CheckTrueDistanceToBuildingSurface(){
        Vector3 localDronePos = buildingCollision.InverseTransformPoint(vc.transform.position);
        if(Mathf.Abs(localDronePos.y) >= 0.5f)
            return Vector3.positiveInfinity;
        
        if(Mathf.Abs(localDronePos.x) < 0.5f && Mathf.Abs(localDronePos.z) < 0.5f)
            return Vector3.positiveInfinity;

        if(Mathf.Abs(localDronePos.x) >= 0.5f && Mathf.Abs(localDronePos.z) >= 0.5f)
            return Vector3.positiveInfinity;

        if(Mathf.Abs(localDronePos.x) < 0.5f)
            return -buildingCollision.forward * (Mathf.Abs(localDronePos.z) - 0.5f) * Mathf.Sign(localDronePos.z) * buildingCollision.localScale.z;
        
        return -buildingCollision.right * (Mathf.Abs(localDronePos.x) - 0.5f) * Mathf.Sign(localDronePos.x) * buildingCollision.localScale.x; 
    }


    Vector3 CheckDistToGround(out bool hitGround)
    {
        Ray rayDown = new Ray(PositionalSensorSimulator.dronePositionVirtual, Vector3.down);
        RaycastHit hit;
        if (Physics.Raycast(rayDown, out hit, float.MaxValue, realObstacleLayerMask))
        {
            hitGround = true;
            return hit.point - PositionalSensorSimulator.dronePositionVirtual;
        } else
        {
            hitGround = false;
            return Vector3.down * float.PositiveInfinity;
        }
    }

    Vector3 CheckTrueDistToGround(out bool hitGround){
        Ray rayDown = new Ray(vc.transform.position, Vector3.down);
        RaycastHit hit;
        if (Physics.Raycast(rayDown, out hit, float.MaxValue, realObstacleLayerMask))
        {
            hitGround = true;
            return hit.point - PositionalSensorSimulator.dronePositionVirtual;
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
        autopilotManager.EnableAutopilot(false, false);
    }

    
}
