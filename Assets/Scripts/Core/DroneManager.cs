using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Policy;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Utilities.Tweenables.Primitives;

//A central script for decision making based on flight data.
//Should not be used for accepting input directly from the user.
public class DroneManager : MonoBehaviour
{

    public enum MissionState{
        Planning,
        MovingToFlightZone, //When landed, or just take off
        InFlightZone, //When in flight zone
        Inspecting, //When autopiloting
        AutopilotInterupted,
        Returning //When path completed or RTH is triggered.

    }

    //public enum SafetyState{
    //    Healthy,
    //    Caution,
    //    Warning,
    //    Emergency
    //}

    public enum ControlType
    {
        Autonomous,
        Manual
    }

    //[SerializeField] private UIUpdater uiUpdater;
    //[SerializeField] private ControlVisUpdater controlVisUpdater;
    //[SerializeField] private WorldVisUpdater worldVisUpdater;
    //[SerializeField] private StateFinder state;
    //[SerializeField] private VelocityControl vc;
    //[SerializeField] private InputControl ic;
    
    //private VisType[] safeVis;
    //private VisType[] misVis;

    //public static SafetyState currentSafetyState {get; private set;}
    public static ControlType currentControlType {get; private set;}
    public static MissionState currentMissionState {get; private set;}

    //private StateFinder.Pose originalPose;

    //private bool controlActive = false;

    public static bool take_off_flag = false, autopilot_flag = false, autopilot_stop_flag = false, rth_flag = false, take_photo_flag = false, mark_defect_flag = false, finish_planning_flag = false;

    //Events invoked as transition
    public static UnityEvent takeOffEvent = new UnityEvent(), autopilotEvent = new UnityEvent(), autopilotStopEvent = new UnityEvent(), returnToHomeEvent = new UnityEvent(), takePhotoEvent = new UnityEvent(), markDefectEvent = new UnityEvent(), finishPlanningEvent = new UnityEvent();
    //Events invoked repeatedly during state
    public static UnityEvent onFlightEvent = new UnityEvent(), landingEvent = new UnityEvent(), landedEvent = new UnityEvent();

    public static UnityEvent<float, float, float, float> setVelocityControlEvent = new UnityEvent<float, float, float, float>();

    public static UnityEvent resetAllEvent = new UnityEvent();

    public static float desired_height = 0.0f;
    public static float desired_vx = 0.0f;
    public static float desired_vy = 0.0f;
    public static float desired_yaw = 0.0f;

    //[SerializeField] float predictStepLength = 1f;
    //[SerializeField] int predictSteps = 3;

    //[SerializeField] FlightPlanning flightPlanning;

    //[SerializeField] Transform contingencyBuffer;

    //[SerializeField] LayerMask realObstacleLayerMask;

    //[SerializeField] AutopilotManager autopilotManager;
    //[SerializeField] CameraController camController;

    

    //[SerializeField] Battery battery;
    //[SerializeField] PositionalSensorSimulator posSensor;
    //[SerializeField] RandomPulseNoise wind;

    //[SerializeField] EventTriggerDetection eventTriggerDetection;
    //[SerializeField] CollisionSensing collisionSensing;

    //public static float bufferCautionThreahold = 1f, surfaceCautionThreshold = 6.0f, surfaceWarningThreshold = 4.0f;
    //private float windStrengthWarningCoolDownTimer, windStrengthWarningCoolDownTime = 3f;


    // Start is called before the first frame update
    void Start()
    {
        ResetAllStates();
    }

    public void ResetAllStates(){
        
        //currentFlightState = FlightState.Landed;
        currentMissionState = MissionState.Planning;
        currentControlType = ControlType.Manual;
        //currentSafetyState = SafetyState.Healthy;
        VisType.globalVisType = VisType.VisualizationType.None;
        
        //controlVisUpdater.SetControlVisActive(false);
        //autopilotManager.ResetAutopilot();
        //flightPlanning.ResetPathPlanning();
        //vc.transform.localPosition = Vector3.zero;
        //vc.transform.localEulerAngles = Vector3.zero;
        //vc.ResetVelocityControl();
        //worldVisUpdater.ResetWorldVis();
        //uiUpdater.ResetUI();
        //camController.ResetCamera();
        //battery.ResetBattery();
        //posSensor.ResetSignalLevel();
        ResetAllFlags();

        resetAllEvent.Invoke();
    }

    void ResetAllFlags(){
        take_off_flag = false;
        autopilot_flag = false;
        autopilot_stop_flag = false;
        rth_flag = false; 
        take_photo_flag = false;
        mark_defect_flag = false;
        finish_planning_flag = false;
    }

    // Update is called once per frame
    //Mainly for controlling model activation and state update. Not for passing data [TODO]
    void Update()
    {
        
        if(currentMissionState == MissionState.Planning && finish_planning_flag){
            finish_planning_flag = false;
            currentMissionState = MissionState.MovingToFlightZone;
            finishPlanningEvent.Invoke();
        }

        if(take_off_flag){
            take_off_flag = false;
            takeOffEvent.Invoke();
        }

        if(VelocityControl.currentFlightState == VelocityControl.FlightState.Hovering || VelocityControl.currentFlightState == VelocityControl.FlightState.Navigating){

            onFlightEvent.Invoke();


            if(currentMissionState != MissionState.Inspecting && currentMissionState != MissionState.Returning)
                currentMissionState = Communication.positionData.inBuffer?MissionState.InFlightZone:MissionState.MovingToFlightZone;

            if(rth_flag)
            {
                //autopilotManager.EnableRTH();
                returnToHomeEvent.Invoke();
                currentControlType = ControlType.Autonomous;
                currentMissionState = MissionState.Returning;
                rth_flag = false;
            }

            if(mark_defect_flag){
                mark_defect_flag = false;
                markDefectEvent.Invoke();
                //camController.TakePhoto(true);
                //worldVisUpdater.SpawnCoverageObject(true); 
            }



            if (currentMissionState == MissionState.InFlightZone)
            {
                if (autopilot_flag)
                {
                    autopilotEvent.Invoke();
                    //autopilotManager.EnableAutopilot();
                    currentControlType = ControlType.Autonomous;
                    currentMissionState = MissionState.Inspecting;
                    autopilot_flag = false;
                }
            } else if(currentMissionState == MissionState.Inspecting){
                if(!Communication.positionData.inBuffer){
                    autopilot_stop_flag = true;
                } 
                if(take_photo_flag)
                {
                    take_photo_flag = false;
                    takePhotoEvent.Invoke();
                    //camController.TakePhoto(false);
                }
            }
            if(autopilot_stop_flag){
                autopilotStopEvent.Invoke();
                //autopilotManager.StopAutopilot();
                //(v2surf.magnitude, battery.GetBatteryLevel(), posSensor.GetSignalLevel(), wind.GetCurrentWindStrength());
                currentControlType = ControlType.Manual;
                currentMissionState = MissionState.AutopilotInterupted;
                autopilot_stop_flag = false;
            }
            setVelocityControlEvent.Invoke(desired_vy, desired_vx, desired_yaw, desired_height);

        } else {
            ResetAllFlags();
            if(VelocityControl.currentFlightState == VelocityControl.FlightState.Landing){
                landingEvent.Invoke();
            } else if(VelocityControl.currentFlightState == VelocityControl.FlightState.Landed){
                landedEvent.Invoke();
            }
        }



        


        //During normal flight
        //if (vc.currentFlightState == VelocityControl.FlightState.Navigating || vc.currentFlightState == VelocityControl.FlightState.Hovering) {
        //    ic.EnableControl(true);
        //    controlVisUpdater.SetControlVisActive(true);
        //    collisionSensing.collisionSensingEnabled = true;
//
//
        //    //bool inBuffer = false;
        //    Vector3 v2bound = CheckPositionInContingencyBuffer(out inBuffer);
        //    worldVisUpdater.inBuffer = inBuffer;
        //    worldVisUpdater.distToBuffer = v2bound.magnitude;
        //    controlVisUpdater.inBuffer = inBuffer;
        //    controlVisUpdater.vectorToNearestBufferBound = v2bound;
        //    Vector3 v2surf = CheckDistanceToBuildingSurface();
        //    Vector3 v2surfTrue = CheckTrueDistanceToBuildingSurface();
        //    controlVisUpdater.vectorToNearestSurface = v2surf;
        //    autopilotManager.vectorToBuildingSurface = v2surf;
        //    worldVisUpdater.vectorToSurface = v2surf;
        //    uiUpdater.vector2surface = v2surfTrue;
        //    
        //    //if(currentMissionState != MissionState.Inspecting && currentMissionState != MissionState.Returning)
        //    //    currentMissionState = inBuffer?MissionState.InFlightZone:MissionState.MovingToFlightZone;
//
//
        //    if (rth_flag)
        //    {
        //        autopilotManager.EnableRTH();
        //        currentControlType = ControlType.Autonomous;
        //        currentMissionState = MissionState.Returning;
        //        rth_flag = false;
        //    }
//
        //    if(mark_defect_flag){
        //        mark_defect_flag = false;
        //        camController.TakePhoto(true);
        //        //worldVisUpdater.SpawnCoverageObject(true); 
        //    }
//
        //    if (currentMissionState == MissionState.InFlightZone)
        //    {
        //        if (autopilot_flag)
        //        {
        //            if (flightPlanning.isPathPlanned())
        //            {
        //                autopilotManager.EnableAutopilot();
        //                currentControlType = ControlType.Autonomous;
        //                currentMissionState = MissionState.Inspecting;
        //            }
        //            autopilot_flag = false;
        //        }
        //    } else if(currentMissionState == MissionState.Inspecting){
        //        if(!inBuffer){
        //            autopilot_stop_flag = true;
        //        }
        //        if(take_photo_flag)
        //        {
        //            take_photo_flag = false;
        //            camController.TakePhoto(false);
        //            //worldVisUpdater.SpawnCoverageObject(false);
        //        }
        //    }
//
        //    //if (currentFlightState == FlightState.Navigating){
        //    //    if(Vector3.Magnitude(state.pose.WorldAcceleration) < 0.1f && Vector3.Magnitude(state.pose.WorldVelocity) < 0.1f){
        //    //        currentFlightState = FlightState.Hovering;
        //    //    } else {
        //    //        if(vc.desired_vx == 0 && vc.desired_vy == 0 && vc.desired_yaw == 0 && vc.height_diff == 0){ 
        //    //            controlActive = false;
        //    //        } else {
        //    //            controlActive = true;
        //    //        }
        //    //        PredictFutureTrajectory();
        //    //    }
        //    //} 
////
        //    //if(currentFlightState == FlightState.Hovering){
        //    //    if(vc.desired_vx != 0 || vc.desired_vy != 0 || vc.desired_yaw != 0 || vc.height_diff != 0){
        //    //        currentFlightState = FlightState.Navigating;
        //    //        controlActive = true;
        //    //    }else{
        //    //        controlVisUpdater.predictedPoints = new Vector3[0];
        //    //    }
        //    //}
//
        //    if(autopilot_stop_flag){
        //        autopilotManager.StopAutopilot();
        //        //(v2surf.magnitude, battery.GetBatteryLevel(), posSensor.GetSignalLevel(), wind.GetCurrentWindStrength());
        //        currentControlType = ControlType.Manual;
        //        currentMissionState = MissionState.AutopilotInterupted;
        //        autopilot_stop_flag = false;
        //    }
//
        //    //UpdateSafetyState(inBuffer, v2bound.magnitude, v2surf.magnitude, battery.GetBatteryLevel(), battery.GetBatteryVoltage(), posSensor.GetSignalLevel(), wind.GetCurrentWindStrength());
        //} 
        //else
        //{
        //    //uiUpdater.enableSound = false;
        //    
        //    ic.EnableControl(false);
        //    if(vc.currentFlightState == VelocityControl.FlightState.Landing){
        //        collisionSensing.collisionSensingEnabled = false;
        //        controlVisUpdater.SetControlVisActive(false);
        //    }
        //}
//
        ////if (currentFlightState != FlightState.Landed)
        ////{
        ////    //bool hitGround = false;
        ////    //Vector3 vectorToGround = Vector3.down * state.Altitude;
        ////    //bool trueHitGround = false;
        ////    //Vector3 trueVectorToGround = CheckTrueDistToGround(out trueHitGround);
        ////    //vc.vectorToGround = vectorToGround;
        ////    //controlVisUpdater.vectorToGround = vectorToGround;
        ////    //uiUpdater.vpsHeight = vectorToGround.magnitude;
        ////} else {
        //if (vc.currentFlightState == VelocityControl.FlightState.Landed){
        //    if(currentMissionState == MissionState.Returning){
        //        currentMissionState = MissionState.MovingToFlightZone;
        //    }
        //    eventTriggerDetection.ResetEventSimulation();
        //}
    }

    //public static void SetCurrentDesiredVelocityFromManualInput(float vx, float vy, float yaw, float heightDiff){
    //    desired_vx = vx;
    //    desired_vy = vy;
    //    desired_yaw = yaw;
    //    desired_height += heightDiff;
    //}

    //public void MarkDefect(){
    //    camController.TakePhoto(true);
    //}

    //Predict the future trajectory based on number of prediction steps and the predict step length (in seconds), 
    //and the current state.pose.WorldVelocity and state.pose.WorldAcceleration.
    //void PredictFutureTrajectory(){
    //    List<Vector3> trajectory = new List<Vector3>();
    //    if(controlActive){
    //        Vector3 travelOffset = Vector3.zero;
    //        float futureTimer = 0;
    //        for(int i = 0; i < predictSteps; i++){
    //            futureTimer += predictStepLength;
    //            travelOffset = StateFinder.pose.WorldVelocity * futureTimer + 0.5f * StateFinder.pose.WorldAcceleration * futureTimer * futureTimer;
    //            trajectory.Add(travelOffset);
    //        }
    //    } else {
    //        Vector3 travelOffset = Vector3.zero;
    //        float futureTimer = 0;
    //        for (int i = 0; i < predictSteps; i++){
    //            futureTimer += predictStepLength;
    //            if ((StateFinder.pose.WorldVelocity.magnitude - StateFinder.pose.WorldAcceleration.magnitude * futureTimer) <= 0f){
    //                float timeUntilStop = StateFinder.pose.WorldVelocity.magnitude/StateFinder.pose.WorldAcceleration.magnitude;
    //                travelOffset += StateFinder.pose.WorldVelocity * timeUntilStop + 0.5f * StateFinder.pose.WorldAcceleration * timeUntilStop * timeUntilStop;
    //                trajectory.Add(travelOffset);
    //                break;
    //            } else {
    //                travelOffset += StateFinder.pose.WorldVelocity * futureTimer + 0.5f * StateFinder.pose.WorldAcceleration * futureTimer * futureTimer;
    //                trajectory.Add(travelOffset);
    //            }
    //        }
    //    }
    //    controlVisUpdater.predictedPoints = trajectory.ToArray();
    //}

    //Check drone position related to the contingency buffer. 
    

    //void UpdateSafetyState(bool inBuffer, float distToBuffer, float distToSurface, int batteryLevel, float voltage, int positional_signal_level, float wind_strength){
    //    if(currentSafetyState == SafetyState.Emergency)
    //        return;
    //    
//
    //    SafetyState tempState = SafetyState.Healthy;
    //    
//
    //    if(!inBuffer){
    //        tempState = SafetyState.Caution;
    //    }
    //    if(distToSurface < surfaceCautionThreshold){
    //        tempState = SafetyState.Caution;
    //        
    //    }
    //    if(batteryLevel == 2){
    //        
    //        tempState = SafetyState.Caution;
    //    }
    //    if(voltage < 10f){
    //        tempState = SafetyState.Caution;
    //    }
    //    //if(wind_strength > 20f){
    //    //    tempState = SafetyState.Caution;
    //    //}
    //    
//
    //    if(distToSurface < surfaceWarningThreshold){
    //        tempState = SafetyState.Warning;
    //    }
    //    if(batteryLevel == 1){
    //        tempState = SafetyState.Warning;
    //    }
    //    if(voltage < 9f){
    //        tempState = SafetyState.Warning;
    //    }
    //    if(vc.collisionHitCount > 0){
    //        tempState = SafetyState.Warning;
    //    }
    //    if(positional_signal_level == 2){
    //        tempState = SafetyState.Caution;
    //    }
    //    if(positional_signal_level == 1){
    //        tempState = SafetyState.Warning;
    //    }
    //    if(positional_signal_level == 0){
    //        tempState = SafetyState.Warning;
    //    }
    //    //if(wind_strength > 40f){
    //    //    tempState = SafetyState.Warning;
    //    //}
    //    
//
    //    if(vc.collisionHitCount > 0 || vc.out_of_balance){
    //        tempState = SafetyState.Emergency;
    //    }
    //    if(batteryLevel == 0){
    //        tempState = SafetyState.Emergency;
    //    }
//
    //    
    //    if(currentSafetyState != tempState){
    //        string tempText = "inbuffer:" + (inBuffer?"true":"false") +"|dist2suf:" + distToSurface + "|windStrength:" + wind_strength + "|gpsLevel:" + positional_signal_level + "|batteryLevel:" + batteryLevel;
    //        ExperimentServer.RecordData("System state change to", uiUpdater.GetSystemStateText()[(int)tempState], tempText);
    //    }
    //    currentSafetyState = tempState;
    //}


    //Distance check to building surface as specified by pilot (the pre-defined bounding box of the building)
    //Not equal to the actual collision detection
    

    //To be replaced by the actually collision detection
    //Vector3 CheckTrueDistanceToBuildingSurface(){
    //    Vector3 localDronePos = buildingCollision.InverseTransformPoint(vc.transform.position);
    //    if(Mathf.Abs(localDronePos.y) >= 0.5f)
    //        return Vector3.positiveInfinity;
    //    
    //    if(Mathf.Abs(localDronePos.x) < 0.5f && Mathf.Abs(localDronePos.z) < 0.5f)
    //        return Vector3.positiveInfinity;
//
    //    if(Mathf.Abs(localDronePos.x) >= 0.5f && Mathf.Abs(localDronePos.z) >= 0.5f)
    //        return Vector3.positiveInfinity;
//
    //    if(Mathf.Abs(localDronePos.x) < 0.5f)
    //        return -buildingCollision.forward * (Mathf.Abs(localDronePos.z) - 0.5f) * Mathf.Sign(localDronePos.z) * buildingCollision.localScale.z;
    //    
    //    return -buildingCollision.right * (Mathf.Abs(localDronePos.x) - 0.5f) * Mathf.Sign(localDronePos.x) * buildingCollision.localScale.x; 
    //}

    //To be replaced by visual positioning system
    //Vector3 CheckDistToGround(out bool hitGround)
    //{
    //    Ray rayDown = new Ray(PositionalSensorSimulator.dronePositionVirtual, Vector3.down);
    //    RaycastHit hit;
    //    if (Physics.Raycast(rayDown, out hit, float.MaxValue, realObstacleLayerMask))
    //    {
    //        hitGround = true;
    //        return hit.point - PositionalSensorSimulator.dronePositionVirtual;
    //    } else
    //    {
    //        hitGround = false;
    //        return Vector3.down * float.PositiveInfinity;
    //    }
    //}
    //To be replaced by visual positioning syste
    //Vector3 CheckTrueDistToGround(out bool hitGround){
    //    Ray rayDown = new Ray(vc.transform.position, Vector3.down);
    //    RaycastHit hit;
    //    if (Physics.Raycast(rayDown, out hit, float.MaxValue, realObstacleLayerMask))
    //    {
    //        hitGround = true;
    //        return hit.point - vc.transform.position;
    //    } else
    //    {
    //        hitGround = false;
    //        return Vector3.down * float.PositiveInfinity;
    //    }
    //}

    //void EngageAutoPilot(bool rth){
    //    autopilotManager.EnableAutopilot(true, rth);
    //}
//
    //void DisengageAutoPilot(float distToSurface, int batteryLevel, int positional_signal_level, float wind_strength){
    //    autopilotManager.EnableAutopilot(false, false);
    //    string tempText = "dist2suf:" + distToSurface + "|windStrength:" + wind_strength + "|gpsLevel:" + positional_signal_level + "|batteryLevel:" + batteryLevel;
    //    ExperimentServer.RecordData("Manual Piloting",vc.transform.position.x + "|" + vc.transform.position.y + "|" + vc.transform.position.z, tempText);
    //}

    
}
