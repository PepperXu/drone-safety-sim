using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Utilities.Tweenables.SmartTweenableVariables;

public class Battery : MonoBehaviour
{
    //private float hoveringDischargeRate = 7.66f;
    private const float batteryCapacity = 3830f;
    //private float optimalFlightSpeed = 6.94f;
    private const float normalDischargeRate = 8.754f;
    private const float normalWindStrength = 20f;
    private const float noWindDischargeRate = 7.66f;

    private const float normalBatteryVoltage = 11.4f;
    private const float voltageDropPerLevel = 1f;
    float abnormalDischargeRate = 0f;
    float voltageDropDischargeRateCoeff = 1.5f;
    float randomNoise = 0f;
    float currentBatteryPercentage = 1f;
    float currentBatteryCapacity = 3830f;
    float currentDischargeRate = 0f;
    float dischargeRateWindCoeff = 0f;
    float remainingTimeInSeconds;
    float currentVoltage = 0f;


    [SerializeField] StateFinder droneState;
    [SerializeField] UIUpdater uiUpdater;
    [SerializeField] RandomPulseNoise randomPulseNoise;


    System.Random r;

    public void ResetBattery(){
        dischargeRateWindCoeff = (normalDischargeRate - noWindDischargeRate) / normalWindStrength;
        r = new System.Random();
        currentVoltage = normalBatteryVoltage;
        currentBatteryCapacity = batteryCapacity;
    }

    // Update is called once per frame
    void Update()
    {
        if(DroneManager.currentFlightState != DroneManager.FlightState.Landed){
            randomNoise = Sample(abnormalDischargeRate, 0.01f);
            currentDischargeRate = Mathf.Abs(randomPulseNoise.GetCurrentWindStrength()) * dischargeRateWindCoeff + noWindDischargeRate + randomNoise;
        } else {
            currentDischargeRate = 0f;
        }
        currentBatteryCapacity -= Mathf.Max(0f, currentDischargeRate * Time.deltaTime / 3.6f);
        currentBatteryPercentage = currentBatteryCapacity/batteryCapacity;
        float predictedDischargeRate = randomPulseNoise.strength_mean * dischargeRateWindCoeff + noWindDischargeRate + abnormalDischargeRate;
        remainingTimeInSeconds = (currentBatteryCapacity - batteryCapacity * 0.2f) / predictedDischargeRate * 3.6f;
        uiUpdater.currentBatteryPercentage = currentBatteryPercentage;
        uiUpdater.remainingTime = remainingTimeInSeconds;
        uiUpdater.voltage = currentVoltage;

    }

    public void SetVoltageLevel(int level){
        abnormalDischargeRate = (3 - level) * voltageDropDischargeRateCoeff;
        currentVoltage = normalBatteryVoltage - (3 - level) * voltageDropPerLevel;
    }

    public float GetBatteryLevel(){
        return currentBatteryPercentage;
    }

    public int GetBatteryVoltageLevel(){
        return 3 - (int)((normalBatteryVoltage - currentVoltage)/voltageDropPerLevel);
    }

    public float GetBatteryVoltage(){
        return currentVoltage;
    }

    float Sample(float mean, float var)
    {
        float n = NextGaussianDouble();

        return n * Mathf.Sqrt(var) + mean;
    }

    float SamplePositive(float mean, float var) {
        return Mathf.Abs(Sample(mean, var));
    }

    float NextGaussianDouble()
    {
        float u, v, S;

        do
        {
            u = 2.0f * (float) r.NextDouble() - 1.0f;
            v = 2.0f * (float) r.NextDouble() - 1.0f;
            S = u * u + v * v;
        }
        while (S >= 1.0f);

        float fac = Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);
        return u * fac;
    }
}
