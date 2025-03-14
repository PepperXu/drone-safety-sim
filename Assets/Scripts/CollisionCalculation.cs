using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionCalculation : MonoBehaviour
{
    public GameObject[] configs;
    //public int currentConfig;
    //public int lastWaypointIndex;
    public LayerMask obstacleLayer;
    public FlightPlanning flightPlanning;
    

    public enum CalculationMode
    {
        AvgDist,
        NearColDuration,
        WarningDuration,
        EffectiveDuration,
        TotalDuration,
        AvgDeviationNoCol,
        WaypointCoverage,
        AutopilotPercentage
    }

    public CalculationMode currentCalculationMode;

    public string fileName = "log_full.csv";
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
