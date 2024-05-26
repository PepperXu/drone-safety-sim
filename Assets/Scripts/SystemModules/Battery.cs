using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Utilities.Tweenables.SmartTweenableVariables;

public class Battery : MonoBehaviour
{
    //private float hoveringDischargeRate = 7.66f;
    private float batteryCapacity = 850f;
    private const float batteryCapacityLow = 630f, batteryCapacityHigh = 756f;
    public static float autopilotFlightSpeed = 3f;
    private const float normalDischargeRate = 8.754f;
    float currentBatteryPercentage;
    float currentBatteryCapacity;
    float currentDischargeRate;
    float remainingTimeInSeconds;

    public static float batteryLowThreshold = 0.25f;
    public static float batteryCriticalThreshold = 0.1f;
    //System.Random r;

    [SerializeField] Transform Homepoint;


    void OnEnable(){
        DroneManager.finishPlanningEvent.AddListener(ResetBattery);
        DroneManager.landedEvent.AddListener(ResetBattery);
    }

    void OnDisable(){
        DroneManager.finishPlanningEvent.RemoveListener(ResetBattery);
        DroneManager.landedEvent.RemoveListener(ResetBattery);
    }

    void ResetBattery(){
        batteryCapacity = (Communication.currentSurfaceIndex == 0)?batteryCapacityLow:batteryCapacityHigh;
        currentBatteryCapacity = batteryCapacity;
        Communication.battery.rth = false;
        Communication.battery.batteryState = "Normal";
        Communication.battery.batteryDropped = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(VelocityControl.currentFlightState != VelocityControl.FlightState.Landed){
            currentDischargeRate = normalDischargeRate;
        } else {
            currentDischargeRate = 0f;
        }
        currentBatteryCapacity -= Mathf.Max(0f, currentDischargeRate * Time.deltaTime / 3.6f);
        currentBatteryPercentage = currentBatteryCapacity / batteryCapacity;
        //float predictedDischargeRate = randomPulseNoise.strength_mean * dischargeRateWindCoeff + noWindDischargeRate + abnormalDischargeRate;
        remainingTimeInSeconds = currentBatteryCapacity / currentDischargeRate * 3.6f;
        

        Vector3 vector2home = Communication.positionData.virtualPosition - Homepoint.position;
        
        float rthTimeThreshold = (new Vector2(vector2home.x, vector2home.z).magnitude + Mathf.Abs(vector2home.y))/autopilotFlightSpeed + batteryCapacity*batteryCriticalThreshold/normalDischargeRate * 3.6f;
        float rthThreshold = rthTimeThreshold * (normalDischargeRate / 3.6f) / batteryCapacity;
        float rthDistance = rthTimeThreshold * autopilotFlightSpeed;
        float lowBatDistance = batteryLowThreshold/rthThreshold * rthDistance;
        float criticalBatDistance = batteryCriticalThreshold/rthThreshold * rthDistance;

        Communication.battery.batteryPercentage = currentBatteryPercentage;
        Communication.battery.batteryRemainingTime = remainingTimeInSeconds;
        Communication.battery.vector2Home = vector2home;
        Communication.battery.rthThreshold = rthThreshold;
        
        Communication.battery.totalRemainingDistance = remainingTimeInSeconds * autopilotFlightSpeed;
        Communication.battery.distanceUntilCritical = remainingTimeInSeconds * autopilotFlightSpeed - criticalBatDistance;
        Communication.battery.distanceUntilRTH = remainingTimeInSeconds * autopilotFlightSpeed - rthDistance;
        Communication.battery.distanceUntilLowBat = remainingTimeInSeconds * autopilotFlightSpeed - lowBatDistance;


        //Debug.Log("current rth offset:"+ Communication.positionData.virtualPosition+ "::" +Homepoint.position);
        
        

        if(remainingTimeInSeconds < rthTimeThreshold){
            if(!Communication.battery.rth){
                ExperimentServer.RecordEventData("Battery RTH point reached", "battery: " + currentBatteryPercentage, "");
                Communication.battery.rth = true;
            }
        } 

        

        if(currentBatteryPercentage < batteryCriticalThreshold){
            if(Communication.battery.batteryState != "Critical"){
                Communication.battery.batteryState = "Critical";
                ExperimentServer.RecordEventData("Battery Critical point reached", "battery: " + currentBatteryPercentage, "");
                if(DroneManager.currentControlType == DroneManager.ControlType.Autonomous && DroneManager.currentMissionState != DroneManager.MissionState.Returning)
                {
                    DroneManager.autopilot_stop_flag = true;
                }
            }
        } else if (currentBatteryPercentage < batteryLowThreshold){
            if(Communication.battery.batteryState != "Low"){
                Communication.battery.batteryState = "Low";
                ExperimentServer.RecordEventData("Battery Low point reached", "battery: " + currentBatteryPercentage, "");
            }
        } else {
            Communication.battery.batteryState = "Normal";
        }

        
        //Communication.battery.voltageLevel = currentVoltage
        //uiUpdater.currentBatteryPercentage = currentBatteryPercentage;
        //uiUpdater.remainingTime = remainingTimeInSeconds;
        //uiUpdater.voltage = currentVoltage;
        //controlVisUpdater.batteryPercentage = currentBatteryPercentage;
        //controlVisUpdater.remainingTimeInSeconds = remainingTimeInSeconds;
        //worldVisUpdater.currentBatteryPercentage = currentBatteryPercentage;
    }

    //public void SetVoltageLevel(int level){
    //    abnormalDischargeRate = (3 - level) * voltageDropDischargeRateCoeff;
    //    currentVoltage = normalBatteryVoltage - (3 - level) * voltageDropPerLevel;
    //}

    public void ReduceBatteryCap(float percentage){
        currentBatteryCapacity = Mathf.Max(0f, currentBatteryCapacity-batteryCapacity*percentage);
        
    }

    public void BatteryDropToCritical(){
        currentBatteryCapacity = batteryCapacity * 0.27f;
        Communication.battery.batteryDropped = true;
    }

    //public float GetBatteryPercentage(){
    //    return currentBatteryPercentage;
    //}

    //public int GetBatteryLevel(){
    //    int batteryLevel;
    //    if(currentBatteryPercentage > 0.46667f){
    //        batteryLevel = 3;
    //    } else if(currentBatteryPercentage > 0.3f){
    //        batteryLevel = 2;
    //    } else if(currentBatteryPercentage > 0.2){
    //        batteryLevel = 1;
    //    } else {
    //        batteryLevel = 0;
    //    }
//
    //    if(this.batteryLevel != batteryLevel){
    //        if(batteryLevel == 3) {
    //            ExperimentServer.RecordData("Battery full", "", "");
    //        } else if (batteryLevel == 2){
    //            ExperimentServer.RecordData("Battery low", "", "");
    //        } else if (batteryLevel == 1){
    //            ExperimentServer.RecordData("Battery critically low!", "", "");
    //        } else {
    //            ExperimentServer.RecordData("Battery empty!", "", "");
    //        }
    //    }
//
    //    this.batteryLevel = batteryLevel;
    //    return batteryLevel;
    //}

    //public int GetBatteryVoltageLevel(){
    //    return 3 - (int)((normalBatteryVoltage - currentVoltage)/voltageDropPerLevel);
    //}
//
    //public float GetBatteryVoltage(){
    //    return currentVoltage;
    //}
//
    //float Sample(float mean, float var)
    //{
    //    float n = NextGaussianDouble();
//
    //    return n * Mathf.Sqrt(var) + mean;
    //}
//
    //float SamplePositive(float mean, float var) {
    //    return Mathf.Abs(Sample(mean, var));
    //}
//
    //float NextGaussianDouble()
    //{
    //    float u, v, S;
//
    //    do
    //    {
    //        u = 2.0f * (float) r.NextDouble() - 1.0f;
    //        v = 2.0f * (float) r.NextDouble() - 1.0f;
    //        S = u * u + v * v;
    //    }
    //    while (S >= 1.0f);
//
    //    float fac = Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);
    //    return u * fac;
    //}
}//
