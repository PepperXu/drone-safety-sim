using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UIUpdater : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI flightState, missionState, systemState, controlState;
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
    }


}
