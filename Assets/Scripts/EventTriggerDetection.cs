using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EventTriggerDetection : MonoBehaviour {

    [SerializeField] RandomPulseNoise rpn;
    [SerializeField] Battery battery;

    
    [SerializeField] PositionalSensorSimulator pss;


    float windStrength = 50f, strongWindStrength = 65f;
    float windDuration = 20f, windDurationLong = 30f;
    int sigAbnormalLevel = 1;
    int sigLostLevel = 0;

    bool batteryDropped = false;
    bool isGPSDenied = false;
    
    public void ResetEventSimulation(){
        battery.ResetBattery();
        pss.ResetSignalLevel();
        batteryDropped = false;
    }
    
    private void OnTriggerEnter(Collider other) {
        if(batteryDropped)
            return;
        if(other.tag == "GPSWeakZone" && !isGPSDenied){
            pss.SetSignalLevel(sigAbnormalLevel);
            if(other.name.Contains("Weak")){
                ExperimentServer.RecordData("Enters GPS Denied Area at", transform.position.x + "|" + transform.position.y + "|" + transform.position.z, "");
            } else {
                rpn.yawCenter = other.transform.eulerAngles.y;
                rpn.strength_mean = strongWindStrength;
                rpn.pulse_duration_mean = 1000f;
                rpn.wind_change_flag = true;
                ExperimentServer.RecordData("Enters GPS Denied Area with wind at", transform.position.x + "|" + transform.position.y + "|" + transform.position.z, "strength:" + strongWindStrength);
            }
            isGPSDenied = true;
        } else if(other.tag == "WindZone"){
            other.gameObject.SetActive(false);
            rpn.fixedDuration = true;
            if(other.name.Contains("Weak")){
                StartCoroutine(WindTurbulenceFixedDuration(other.transform.eulerAngles.y + 180));
                //rpn.strength_mean = windStrength;
                //rpn.pulse_duration_mean = windDuration;
                ExperimentServer.RecordData("Wind Drifting starts at", transform.position.x + "|" + transform.position.y + "|" + transform.position.z, "variation:away-from-surf");
            } else {
                //StartCoroutine(SetSignaForWindTurbulence(strongWindDuration));
                StartCoroutine(WindTurbulenceFixedDuration(other.transform.eulerAngles.y));
                
                ExperimentServer.RecordData("Wind Drifting starts at", transform.position.x + "|" + transform.position.y + "|" + transform.position.z, "variation:towards-surf");
            }
            
        } else if(other.tag == "BatteryDrop"){
            batteryDropped = true;
            other.gameObject.SetActive(false);
            battery.BatteryDropToCritical();
            if(other.name.Contains("Strong")){
                pss.SetSignalLevel(sigAbnormalLevel);
                ExperimentServer.RecordData("Battery dropped and signal lost", transform.position.x + "|" + transform.position.y + "|" + transform.position.z, "");
            } else {
                ExperimentServer.RecordData("Battery dropped", transform.position.x + "|" + transform.position.y + "|" + transform.position.z, "");
            }
        }
    }

    private void OnTriggerExit(Collider other) {
        if(batteryDropped)
            return;
        if(other.tag == "GPSWeakZone" && isGPSDenied){
            pss.SetSignalLevel(3);
            pss.switch_gps_normal = true;
            ExperimentServer.RecordData("Exits GPS Denied Area at", transform.position.x + "|" + transform.position.y + "|" + transform.position.z, "");
            rpn.strength_mean = 0f;
            rpn.pulse_duration_mean = 1f;
            rpn.wind_change_flag = true;
            isGPSDenied = false;
        } 
    }


    IEnumerator WindTurbulenceFixedDuration(float yawCenter){
        rpn.yawCenter = yawCenter;
        rpn.strength_mean = strongWindStrength;
        rpn.pulse_duration_mean = windDuration/4;
        rpn.wind_change_flag = true;
        yield return new WaitForSeconds(windDuration/4);
        rpn.strength_mean = windStrength;
        rpn.pulse_duration_mean = windDuration/2;
        rpn.wind_change_flag = true;
        yield return new WaitForSeconds(windDuration/2);
        rpn.strength_mean = strongWindStrength;
        rpn.pulse_duration_mean = windDuration/2;
        rpn.wind_change_flag = true;
        yield return new WaitForSeconds(windDuration/4);
    }
}