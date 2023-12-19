using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExperimentorController : MonoBehaviour
{

    [SerializeField] private FlightPlanning flightPlanning;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(DroneManager.currentMissionState != DroneManager.MissionState.Planning)
            return;

        if(Input.GetKeyDown(KeyCode.Alpha1)){
            flightPlanning.SetStartingPoint(0);
        }
        if(Input.GetKeyDown(KeyCode.Alpha2)){
            flightPlanning.SetStartingPoint(1);
        }
        if(Input.GetKeyDown(KeyCode.Alpha3)){
            flightPlanning.SetStartingPoint(2);
        }
        if(Input.GetKeyDown(KeyCode.Alpha4)){
            flightPlanning.SetStartingPoint(3);
        }
        if(Input.GetKeyDown(KeyCode.Alpha5)){
            flightPlanning.SetStartingPoint(4);
        }
    }
}
