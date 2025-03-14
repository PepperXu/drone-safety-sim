using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PositionalSensorSimulator : MonoBehaviour
{
    //int currentSatelliteCount = 35;
    //[SerializeField] UIUpdater uiUpdater;
    //[SerializeField] ControlVisUpdater controlVisUpdater;
    //[SerializeField] WorldVisUpdater worldVisUpdater;
    //[SerializeField] AutopilotManager autopilotManager;
    //[SerializeField] VelocityControl vc;
    //[SerializeField] StateFinder state;
    [SerializeField] Transform buildingCollision;
    [SerializeField] Transform contingencyBuffer;

    bool gps_lost = false;
    int sig_level = 3;
    float offsetRefreshIntervalMean = 2f, offsetRefreshIntervalVar = 1.5f;
    float offsetRefreshTimer = 0f;

    float signalUpdateRateMean = 1f, signalUpdateRateVar = 1f;
    float maxPositionUncertaintyAbnormal = 10f;
    float maxPositionUncertaintyNormal = 0.5f;

    public static float bufferCautionThreahold = 1f;

    //float currentMaxPosUncertainty;

    float gpsDriftSpeed = 0.7f;
    System.Random r;
    Vector3 lastDronePos;

    Vector3 targetOffset = new Vector3(0f, 0f, 0f);

    //Vector3 newOffset;

    //float updateRate = Time.deltaTime;

    //private Vector3 virtualDronePosition;
    //public static Vector3 dronePositionVirtual;

    bool switch_gps_normal;




    
    // Start is called before the first frame update
    
    void OnEnable(){
        DroneManager.resetAllEvent.AddListener(ResetSignalLevel);
        DroneManager.landedEvent.AddListener(ResetSignalLevel);
    }

    void OnDisable(){
        DroneManager.resetAllEvent.RemoveListener(ResetSignalLevel);
        DroneManager.landedEvent.RemoveListener(ResetSignalLevel);
    }
    
    void Start()
    {
        r = new System.Random();
        //updateRate = Time.deltaTime;
        StartCoroutine(UpdatePosition());
    }

    void ResetSignalLevel(){
        SetGPSLost(false);
        switch_gps_normal = true;
    }

    private void Update()
    {
        //Vector3 vdronePos = Communication.realPose.WorldPosition + newOffset;
        //Communication.positionData.virtualPosition = vdronePos;
        //Communication.positionData.v2bound = CheckPositionInContingencyBuffer(out Communication.positionData.inBuffer, vdronePos);
        //Communication.positionData.v2surf = CheckDistanceToBuildingSurface(vdronePos);
        //lastDronePos = Communication.realPose.WorldPosition;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        Communication.positionData.gpsLost = gps_lost;
        Communication.positionData.sigLevel = sig_level;

        if(switch_gps_normal){
            switch_gps_normal = false;
            Communication.positionData.virtualPosition = Communication.realPose.WorldPosition;
            lastDronePos = Communication.realPose.WorldPosition;
            targetOffset = Vector3.zero;
            //updateRate = Time.deltaTime;
        }

        if(offsetRefreshTimer <= 0f){
            Vector2 randomOffset;
            if (!gps_lost)
            {
                Vector2 randomVector = Random.insideUnitCircle;
                if (targetOffset.magnitude > 6f)
                    randomOffset = randomVector.normalized * (randomVector.magnitude * (targetOffset.magnitude-5f) + 5f);
                else if (targetOffset.magnitude > 4f)
                    randomOffset = randomVector.normalized * (randomVector.magnitude * (targetOffset.magnitude-3f) + 3f);
                else if(targetOffset.magnitude > 2f)
                    randomOffset = randomVector.normalized * (randomVector.magnitude * (targetOffset.magnitude-1f) + 1f);
                else
                    randomOffset = randomVector * maxPositionUncertaintyNormal;
            } else
            {
                Vector2 randomVector = Random.insideUnitCircle;
                if(targetOffset.magnitude < 2f)
                    randomOffset = randomVector.normalized * (randomVector.magnitude * (3f - targetOffset.magnitude) + targetOffset.magnitude);
                else if(targetOffset.magnitude < 4f)
                    randomOffset = randomVector.normalized * (randomVector.magnitude * (5f - targetOffset.magnitude) + targetOffset.magnitude);
                else
                    randomOffset = randomVector.normalized * (randomVector.magnitude * (maxPositionUncertaintyAbnormal - targetOffset.magnitude) + targetOffset.magnitude);
            }
            targetOffset = new Vector3(randomOffset.x, 0f, randomOffset.y);
            if (targetOffset.magnitude < 1f)
            {
                sig_level = 3;
            }
            else if (targetOffset.magnitude < 3f)
            {
                sig_level = 2;
            }
            else if (targetOffset.magnitude < 5f)
            {
                sig_level = 1;
                if (DroneManager.currentControlType == DroneManager.ControlType.Autonomous)
                    DroneManager.autopilot_stop_flag = true;
            }
            else
            {
                if (DroneManager.currentControlType == DroneManager.ControlType.Autonomous)
                    DroneManager.autopilot_stop_flag = true;
                sig_level = 0;
            }
            //targetOffset = (targetOffset + newOffset)/2f;
            //targetOffset = newOffset;
            Communication.positionData.currentTargetOffset = new Vector3(targetOffset.x, 0f, targetOffset.z); 
            offsetRefreshTimer = SamplePositive(offsetRefreshIntervalMean, offsetRefreshIntervalVar);
            //updateRate = SamplePositive(signalUpdateRateMean, signalUpdateRateVar);
        } 

        offsetRefreshTimer -= Time.deltaTime;


    }

    IEnumerator UpdatePosition(){
        while(true){
            Vector3 previousOffset;
            previousOffset = Communication.positionData.virtualPosition - lastDronePos;
            Vector3 newOffset;
            newOffset = Vector3.MoveTowards(previousOffset, new Vector3(targetOffset.x, 0f, targetOffset.z), gpsDriftSpeed * Time.deltaTime); ;
            //if (gps_lost){
            //        
            //        currentMaxPosUncertainty = maxPositionUncertaintyAbnormal;
            //
            //        newOffset = Vector3.MoveTowards(previousOffset, new Vector3(targetOffset.x, 0f, targetOffset.z), gpsDriftSpeed * Time.deltaTime);
            //
            //} else {
            //
            //        currentMaxPosUncertainty = maxPositionUncertaintyNormal;
            //        
            //        
            //
            //}



            

            Vector3 vdronePos = Communication.realPose.WorldPosition + newOffset;
            Communication.positionData.virtualPosition = vdronePos;
            Communication.positionData.v2bound = CheckPositionInContingencyBuffer(out Communication.positionData.inBuffer, vdronePos);
            Communication.positionData.v2surf = CheckDistanceToBuildingSurface(vdronePos);
            if (Communication.positionData.inBuffer)
            {
                int wpIndex = 0;
                float shortestDistance = float.MaxValue;
                for (int i = 0; i < Communication.waypoints.Length; i++)
                {
                    Vector3 target = Communication.waypoints[i].transform.position;
                    if ((Communication.realPose.WorldPosition - target).magnitude < shortestDistance)
                    {
                        wpIndex = i;
                        shortestDistance = (Communication.positionData.virtualPosition - target).magnitude;
                    }
                }
                Communication.positionData.nearestWaypoint = Communication.waypoints[wpIndex].transform.position;
                Communication.positionData.nearestWaypointIndex = wpIndex;
            } else
            {
                Communication.positionData.nearestWaypoint = Vector3.one * -1f;
                Communication.positionData.nearestWaypointIndex = -1;
            }



            lastDronePos = Communication.realPose.WorldPosition;

            yield return new WaitForSeconds(Time.deltaTime);
        }
    }


    Vector3 CheckDistanceToBuildingSurface(Vector3 virtualDronePos){
        Vector3 localDronePos = buildingCollision.InverseTransformPoint(virtualDronePos);
        if(Mathf.Abs(localDronePos.y) >= 0.5f)
            return Vector3.positiveInfinity;
        
        if(Mathf.Abs(localDronePos.x) < 0.5f && Mathf.Abs(localDronePos.z) < 0.5f)
            return Vector3.positiveInfinity;
//
        if(Mathf.Abs(localDronePos.x) >= 0.5f && Mathf.Abs(localDronePos.z) >= 0.5f)
            return Vector3.positiveInfinity;
//
        if(Mathf.Abs(localDronePos.x) < 0.5f)
            return -buildingCollision.forward * (Mathf.Abs(localDronePos.z) - 0.5f) * Mathf.Sign(localDronePos.z) * buildingCollision.localScale.z;
        
        return -buildingCollision.right * (Mathf.Abs(localDronePos.x) - 0.5f) * Mathf.Sign(localDronePos.x) * buildingCollision.localScale.x; 
    }

    Vector3 CheckPositionInContingencyBuffer(out bool inBuffer, Vector3 virtualDronePos){
        Vector3 localDronePos = contingencyBuffer.InverseTransformPoint(virtualDronePos);
        inBuffer = Mathf.Abs(localDronePos.x) < 0.5f && Mathf.Abs(localDronePos.y) < 0.5f && Mathf.Abs(localDronePos.z) < 0.5f;
        if (Mathf.Abs(localDronePos.y) < 0.5f && (Mathf.Abs(localDronePos.x) < 0.5f || Mathf.Abs(localDronePos.z) < 0.5f))
        {
            if (inBuffer)
            {
                Vector3 vectorToBufferWall;
                if (Mathf.Abs(Mathf.Abs(localDronePos.x) - 0.5f) < Mathf.Abs(Mathf.Abs(localDronePos.z) - 0.5f))
                {
                    vectorToBufferWall = -contingencyBuffer.right * (Mathf.Abs(localDronePos.x) - 0.5f) * Mathf.Sign(localDronePos.x) * contingencyBuffer.localScale.x;
                }
                else
                {
                    vectorToBufferWall = -contingencyBuffer.forward * (Mathf.Abs(localDronePos.z) - 0.5f) * Mathf.Sign(localDronePos.z) * contingencyBuffer.localScale.z;
                }
                return vectorToBufferWall;
            } else
            {
                if(Mathf.Abs(localDronePos.z) < 0.5f)
                {
                    return -contingencyBuffer.right * (Mathf.Abs(localDronePos.x) - 0.5f) * Mathf.Sign(localDronePos.x) * contingencyBuffer.localScale.x;
                } else
                {
                    return -contingencyBuffer.forward * (Mathf.Abs(localDronePos.z) - 0.5f) * Mathf.Sign(localDronePos.z) * contingencyBuffer.localScale.z;
                }
            }
        } else
        {
            return Vector3.positiveInfinity;
        }
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

    //public int GetSignalLevel(){
    //    return positional_signal_level;
    //}
    public void SetGPSLost(bool lost){
        gps_lost = lost;
    }


}
