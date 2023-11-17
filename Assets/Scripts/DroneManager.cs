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
        Navigating
    }

    public enum SystemState{
        Healthy,
        Warning,
        Emergency
    }

    [SerializeField] private UIUpdater uiUpdater;
    [SerializeField] private ControlVisUpdater controlVisUpdater;
    [SerializeField] private WorldVisUpdater worldVisUpdater;
    [SerializeField] private StateFinder state;
    [SerializeField] private VelocityControl vc;
    [SerializeField] private InputControl ic;
    
    private VisType[] safeVis;
    private VisType[] misVis;

    public static FlightState currentFlightState = FlightState.Landed;
    public static SystemState currentSystemState = SystemState.Healthy;

    private StateFinder.Pose originalPose;

    private bool controlActive = false;

    [SerializeField] float predictStepLength = 1f;
    [SerializeField] int predictSteps = 3;

    // Start is called before the first frame update
    void Start()
    {
        state.GetState();
        originalPose = state.pose;
        currentFlightState = FlightState.TakingOff;
        List<VisType> safeVisTemp = new List<VisType>(), missionVisTemp = new List<VisType>();
        foreach(VisType vis in Resources.FindObjectsOfTypeAll<VisType>()){
            if(vis.visType == VisType.VisualizationType.SafetyOnly){
                safeVisTemp.Add(vis);
            }
            else if(vis.visType == VisType.VisualizationType.MissionOnly){
                missionVisTemp.Add(vis);
            }
            else if(vis.visType == VisType.VisualizationType.Both){
                safeVisTemp.Add(vis);
                missionVisTemp.Add(vis);
            }
        }
        safeVis = safeVisTemp.ToArray();
        misVis = missionVisTemp.ToArray();
    }

    // Update is called once per frame
    void Update()
    {
        if(currentFlightState == FlightState.TakingOff){
            if(Mathf.Abs(state.Altitude - vc.initial_height)< 0.1f){
                currentFlightState = FlightState.Hovering;
                ic.EnableControl(true);
            }
            controlVisUpdater.SetControlVisActive(false);
        }

        if(currentFlightState != FlightState.TakingOff && currentFlightState != FlightState.Landed){
            controlVisUpdater.SetControlVisActive(true);
            if(currentSystemState == SystemState.Healthy){
                foreach(VisType vis in safeVis){
                    vis.gameObject.SetActive(false);
                }
                foreach(VisType vis in misVis){
                    vis.gameObject.SetActive(true);
                }
            } else {
                foreach(VisType vis in misVis){
                    vis.gameObject.SetActive(false);
                }
                foreach(VisType vis in safeVis){
                    vis.gameObject.SetActive(true);
                }
            }

            if(currentFlightState == FlightState.Navigating){
                if(Vector3.Magnitude(state.pose.WorldVelocity) < 0.1f && Vector3.Magnitude(state.pose.WorldVelocity) < 0.01f){
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
        }

        
    }

    void PredictFutureTrajectory(){
        List<Vector3> trajectory = new List<Vector3>();
        if(controlActive){
            Vector3 travelOffset = Vector3.zero;
            for(int i = 0; i < predictSteps; i++){
                travelOffset += state.pose.WorldVelocity * predictStepLength + 0.5f * state.pose.WorldAcceleration * predictStepLength * predictStepLength;
                trajectory.Add(travelOffset);
            }
        } else {
            Vector3 travelOffset = Vector3.zero;
            for(int i = 0; i < predictSteps; i++){
                if(state.pose.WorldVelocity.magnitude - state.pose.WorldAcceleration.magnitude * predictStepLength <= 0f){
                    float timeUntilStop = state.pose.WorldVelocity.magnitude/state.pose.WorldAcceleration.magnitude;
                    travelOffset += state.pose.WorldVelocity * timeUntilStop + 0.5f * state.pose.WorldAcceleration * timeUntilStop * timeUntilStop;
                    trajectory.Add(travelOffset);
                    break;
                } else {
                    travelOffset += state.pose.WorldVelocity * predictStepLength + 0.5f * state.pose.WorldAcceleration * predictStepLength * predictStepLength;
                }
            }
        }
        controlVisUpdater.predictedPoints = trajectory.ToArray();
    }
}
