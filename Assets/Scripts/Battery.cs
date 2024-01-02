using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Utilities.Tweenables.SmartTweenableVariables;

public class Battery : MonoBehaviour
{
    //private float hoveringDischargeRate = 7.66f;
    private float batteryCapacity = 3830f;
    //private float optimalFlightSpeed = 6.94f;
    private float normalDischargeRate = 8.754f;
    private float normalWindStrength = 20f;
    private float noWindDischargeRate = 7.66f;
    private float resistance = 1.488f;
    float abnormalDischargeRate = 0f;
    float voltageDropDischargeRateCoeff = 1.5f;
    float randomNoise = 0f;
    float currentBatteryPercentage = 100f;
    float currentBatteryCapacity = 3830f;
    float currentDischargeRate = 0f;
    float dischargeRateWindCoeff = 0f;
    float remainingTimeInSeconds;
    float currentVoltage = 0f;

    [SerializeField] StateFinder droneState;
    [SerializeField] UIUpdater uiUpdater;
    [SerializeField] RandomPulseNoise randomPulseNoise;

    System.Random r;

    // Start is called before the first frame update
    void Start()
    {
        dischargeRateWindCoeff = (normalDischargeRate - noWindDischargeRate) / normalWindStrength;
        r = new System.Random();
    }

    // Update is called once per frame
    void Update()
    {
        if(DroneManager.currentFlightState != DroneManager.FlightState.Landed){
            randomNoise = Sample(abnormalDischargeRate, 3f);
            currentDischargeRate = Mathf.Abs(randomPulseNoise.GetCurrentWindStrength()) * dischargeRateWindCoeff + noWindDischargeRate + randomNoise;
        } else {
            currentDischargeRate = 0f;
        }
        currentBatteryCapacity -= currentDischargeRate * Time.deltaTime / 3.6f;
        currentBatteryPercentage = currentBatteryCapacity/batteryCapacity;
        remainingTimeInSeconds = currentBatteryCapacity / currentDischargeRate * 3.6f;
    }

    public void SimulateVoltageDrop(int dropLevel){
        abnormalDischargeRate = dropLevel * voltageDropDischargeRateCoeff;
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
