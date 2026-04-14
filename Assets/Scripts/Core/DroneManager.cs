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
    public static UnityEvent onFlightEvent = new UnityEvent(), landingEvent = new UnityEvent(), logEvent = new UnityEvent(), landedEvent = new UnityEvent();

    public static UnityEvent<float, float, float, float> setVelocityControlEvent = new UnityEvent<float, float, float, float>();

    public static UnityEvent resetAllEvent = new UnityEvent();

    public static float desired_height = 0.0f;
    public static float desired_vx = 0.0f;
    public static float desired_vy = 0.0f;
    public static float desired_yaw = 0.0f;

    VelocityControl.FlightState previousFlightState;

    void Start()
    {
        //ResetAllStates();
    }

    public void ResetAllStates(){
        
        currentMissionState = MissionState.Planning;
        currentControlType = ControlType.Manual;
        VisType.globalVisType = VisType.VisualizationType.Both;
        previousFlightState = VelocityControl.FlightState.Landed;
        
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


        if (currentMissionState == MissionState.Planning && finish_planning_flag){
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
                if (take_photo_flag)
                {
                    take_photo_flag = false;
                    takePhotoEvent.Invoke();
                    //camController.TakePhoto(false);
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
            } 
            
            if(previousFlightState != VelocityControl.FlightState.Landed && VelocityControl.currentFlightState == VelocityControl.FlightState.Landed){
                logEvent.Invoke();
                landedEvent.Invoke();
            }
        }

        previousFlightState = VelocityControl.currentFlightState;
    }
   
}
