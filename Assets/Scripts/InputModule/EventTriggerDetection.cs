using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EventTriggerDetection : MonoBehaviour {

    [SerializeField] RandomPulseNoise rpn;
    [SerializeField] Battery battery;

    
    [SerializeField] PositionalSensorSimulator pss;


    float normalDriftStrength = 10f, strongDriftStrength = 50f;
    //float windDuration = 20f, windDurationLong = 30f;
    float signalLostRecoverDuration = 5f;
    //int signalNormalIndex = 1, signalLostIndex = 0;

    bool batteryDropped = false;
    bool GPSDeniedZoneEntered = false;
    //bool isGPSDenied = false;

    
    void OnEnable(){
        DroneManager.resetAllEvent.AddListener(ResetEventSimulation);
        DroneManager.landedEvent.AddListener(ResetEventSimulation);
    }

    void OnDisable(){
        DroneManager.resetAllEvent.RemoveListener(ResetEventSimulation);
        DroneManager.landedEvent.RemoveListener(ResetEventSimulation);
    }

    void ResetEventSimulation(){
        //battery.ResetBattery();
        //pss.ResetSignalLevel();
        batteryDropped = false;
        GPSDeniedZoneEntered = false;
        StopAllCoroutines();
    }
    
    private void OnTriggerEnter(Collider other) {
        //if(batteryDropped)
        //    return;
        
        if (other.tag == "GPSWeakZone" && !GPSDeniedZoneEntered)
        {
            StopAllCoroutines();
            GPSDeniedZoneEntered = true;
            if(!Communication.positionData.gpsLost){
                rpn.strength_mean = strongDriftStrength;
                rpn.wind_change_flag = true;
                pss.SetGPSLost(true);
                ExperimentServer.RecordEventData("Enters GPS Denied Area at", "zone id: " + other.gameObject.name, "");
            }
        }

        if(other.tag == "BatteryDrop" && !batteryDropped){
            batteryDropped = true;
            other.gameObject.SetActive(false);
            battery.BatteryDropToCritical();
            ExperimentServer.RecordEventData("Battery dropped", "zone id: " + other.gameObject.name, "");
            //if (other.name.Contains("Strong")){
            //    pss.SetSignalLevel(signalLostIndex);
            //    ExperimentServer.RecordData("Battery dropped and signal lost", transform.position.x + "|" + transform.position.y + "|" + transform.position.z, "");
            //} else
            //{
            //    ExperimentServer.RecordData("Battery dropped", transform.position.x + "|" + transform.position.y + "|" + transform.position.z, "");
            //}
        }

            //if(other.tag == "GPSWeakZone" && !isGPSDenied){
            //    pss.SetSignalLevel(signalLostIndex);
            //    if(other.name.Contains("Weak")){
            //        ExperimentServer.RecordData("Enters GPS Denied Area at", transform.position.x + "|" + transform.position.y + "|" + transform.position.z, "");
            //    } else {
            //        rpn.yawCenter = other.transform.eulerAngles.y;
            //        rpn.strength_mean = strongWindStrength;
            //        rpn.pulse_duration_mean = 1000f;
            //        rpn.wind_change_flag = true;
            //        ExperimentServer.RecordData("Enters GPS Denied Area with wind at", transform.position.x + "|" + transform.position.y + "|" + transform.position.z, "strength:" + strongWindStrength);
            //    }
            //    isGPSDenied = true;
            //} else if(other.tag == "WindZone"){
            //    other.gameObject.SetActive(false);
            //    rpn.fixedDuration = true;
            //    if(other.name.Contains("Weak")){
            //        StartCoroutine(WindTurbulenceFixedDuration(other.transform.eulerAngles.y + 180));
            //        //rpn.strength_mean = windStrength;
            //        //rpn.pulse_duration_mean = windDuration;
            //        ExperimentServer.RecordData("Wind Drifting starts at", transform.position.x + "|" + transform.position.y + "|" + transform.position.z, "variation:away-from-surf");
            //    } else {
            //        //StartCoroutine(SetSignaForWindTurbulence(strongWindDuration));
            //        StartCoroutine(WindTurbulenceFixedDuration(other.transform.eulerAngles.y));
            //        
            //        ExperimentServer.RecordData("Wind Drifting starts at", transform.position.x + "|" + transform.position.y + "|" + transform.position.z, "variation:towards-surf");
            //    }
            //    
            //} else if(other.tag == "BatteryDrop"){
            //    batteryDropped = true;
            //    other.gameObject.SetActive(false);
            //    battery.BatteryDropToCritical();
            //    if(other.name.Contains("Strong")){
            //        pss.SetSignalLevel(signalLostIndex);
            //        ExperimentServer.RecordData("Battery dropped and signal lost", transform.position.x + "|" + transform.position.y + "|" + transform.position.z, "");
            //    } else {
            //        ExperimentServer.RecordData("Battery dropped", transform.position.x + "|" + transform.position.y + "|" + transform.position.z, "");
            //    }
            //}
     }

     private void OnCollisionEnter(Collision collision)
    {
        if(VelocityControl.currentFlightState == VelocityControl.FlightState.Navigating || VelocityControl.currentFlightState == VelocityControl.FlightState.Hovering)
        {
            //DroneManager.currentSystemState = DroneManager.SystemState.Emergency;
            Communication.collisionData.collisionCount++;
            if(Communication.droneRb.transform.up.y < 0.6f)
                Communication.collisionData.out_of_balance = true;
            DroneManager.autopilot_stop_flag = true;
            
            //autopilotManager.StopAutopilot();
            ExperimentServer.RecordEventData("Collides with an obstacle at", "out of balance?" + (Communication.collisionData.out_of_balance?"true":"false"), "GPS level: " + Communication.positionData.sigLevel);
        }
    }

    IEnumerator SignalLostRecoverFixedDuration()
    {
        yield return new WaitForSeconds(signalLostRecoverDuration);
        pss.SetGPSLost(false);
        rpn.strength_mean = normalDriftStrength;
        rpn.wind_change_flag = true;
        ExperimentServer.RecordEventData("GPS Recovered at", "", "");
    }

    private void OnTriggerExit(Collider other) {
        //if(batteryDropped)
        //    return;
        if(other.tag == "GPSWeakZone" && GPSDeniedZoneEntered){
            StartCoroutine(SignalLostRecoverFixedDuration());
            GPSDeniedZoneEntered = false;
        } 
    }


    //IEnumerator WindTurbulenceFixedDuration(float yawCenter){
    //    rpn.yawCenter = yawCenter;
    //    rpn.strength_mean = strongWindStrength;
    //    rpn.pulse_duration_mean = windDuration/4;
    //    rpn.wind_change_flag = true;
    //    yield return new WaitForSeconds(windDuration/4);
    //    rpn.strength_mean = windStrength;
    //    rpn.pulse_duration_mean = windDuration/2;
    //    rpn.wind_change_flag = true;
    //    yield return new WaitForSeconds(windDuration/2);
    //    rpn.strength_mean = strongWindStrength;
    //    rpn.pulse_duration_mean = windDuration/2;
    //    rpn.wind_change_flag = true;
    //    yield return new WaitForSeconds(windDuration/4);
    //}
}