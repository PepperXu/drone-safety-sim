using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UIUpdater : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI flightState, missionState, systemState, controlState;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip monitoringClip;
    const float healthyInterval = 1.2f, warningInterval = 0.8f, emergencyInterval = 0.4f;
    bool enableSound = true;
    float currentMonitoringInterval = 1.2f;
    float monitoringTimer = 0f;
    // Start is called before the first frame update
    void Start()
    {
        audioSource.clip = monitoringClip;
        StartCoroutine(PlayMonitoringSound());
    }

    // Update is called once per frame
    void Update()
    {
        flightState.text = Enum.GetName(typeof(DroneManager.FlightState), DroneManager.currentFlightState);
        missionState.text = Enum.GetName(typeof(DroneManager.MissionState), DroneManager.currentMissionState);
        systemState.text = Enum.GetName(typeof(DroneManager.SystemState), DroneManager.currentSystemState);
        controlState.text = Enum.GetName(typeof(DroneManager.ControlType), DroneManager.currentControlType);

        switch (DroneManager.currentSystemState)
        {
            case DroneManager.SystemState.Healthy:
                currentMonitoringInterval = healthyInterval;
                break;
            case DroneManager.SystemState.Warning:
                currentMonitoringInterval = warningInterval;
                break;
            case DroneManager.SystemState.Emergency:
                currentMonitoringInterval = emergencyInterval;
                break;
            default:
                Debug.LogError("System State Undefined");
                break;
        }
    }

    IEnumerator PlayMonitoringSound()
    {
        while (enableSound)
        {
            if(monitoringTimer >= currentMonitoringInterval)
            {
                monitoringTimer = 0f;
                audioSource.Play();
            } else
            {
                monitoringTimer += Time.deltaTime;
            }
            yield return new WaitForEndOfFrame();
        }
    }


}
