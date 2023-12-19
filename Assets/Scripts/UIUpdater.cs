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
    [SerializeField] AudioClip beep, monitoring_ok, monitoring_warn, monitoring_alert;
    public float healthyInterval = 1.2f, warningInterval = 0.8f, emergencyInterval = 0.4f;
    public bool enableSound = false;
    bool continuous = true;
    float currentMonitoringInterval = 1.2f;
    float monitoringTimer = 0f;

    
    // Start is called before the first frame update
    void Start()
    {

    }


    // Update is called once per frame
    void Update()
    {
        flightState.text = Enum.GetName(typeof(DroneManager.FlightState), DroneManager.currentFlightState);
        missionState.text = Enum.GetName(typeof(DroneManager.MissionState), DroneManager.currentMissionState);
        systemState.text = Enum.GetName(typeof(DroneManager.SystemState), DroneManager.currentSystemState);
        controlState.text = Enum.GetName(typeof(DroneManager.ControlType), DroneManager.currentControlType);

        if (enableSound)
        {
            if (continuous)
            {
                audioSource.loop = true;
                if(!audioSource.isPlaying)
                    audioSource.Play();
            } else
            {
                audioSource.loop = false;
                if(audioSource.clip != beep)
                    audioSource.clip = beep;
            }
            switch (DroneManager.currentSystemState)
            {
                case DroneManager.SystemState.Healthy:
                    if (continuous)
                    {
                        if (audioSource.clip != monitoring_ok)
                        {
                            audioSource.clip = monitoring_ok;
                            audioSource.Play();
                        }

                    }
                    else
                        currentMonitoringInterval = healthyInterval;
                    break;
                case DroneManager.SystemState.Warning:
                    if (continuous)
                    {
                        if (audioSource.clip != monitoring_warn)
                        {
                            audioSource.clip = monitoring_warn;
                            audioSource.Play();
                        }
                    }
                    else
                        currentMonitoringInterval = warningInterval;
                    break;
                case DroneManager.SystemState.Emergency:
                    if (continuous)
                    {
                        if (audioSource.clip != monitoring_alert)
                        {
                            audioSource.clip = monitoring_alert;
                            audioSource.Play();
                        }
                    }
                    else
                        currentMonitoringInterval = emergencyInterval;
                    break;
                default:
                    Debug.LogError("System State Undefined");
                    break;
            }
        } else
        {
            if (audioSource.isPlaying)
                audioSource.Stop();
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
