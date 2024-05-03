using System.Collections;
using System.Collections.Generic;
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

    int positional_signal_level = 3;
    float offsetRefreshIntervalMean = 2f, offsetRefreshIntervalVar = 1.5f;
    float offsetRefreshTimer = 0f;

    float signalUpdateRateMean = 1f, signalUpdateRateVar = 1f;
    float maxPositionUncertaintyAbnormal = 10f;
    float maxPositionUncertaintyNormal = 0.5f;

    float currentMaxPosUncertainty;

    float gpsDriftSpeed = 0.7f;
    System.Random r;
    Vector3 lastDronePos;

    Vector3 targetOffset = new Vector3(0f, 0f, 0f);
    float updateRate = 0f;

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
        updateRate = Time.deltaTime;
        StartCoroutine(UpdatePosition());
    }

    void ResetSignalLevel(){
        SetSignalLevel(1);
        switch_gps_normal = true;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        Communication.positionData.signalLevel = positional_signal_level;

        if(switch_gps_normal){
            switch_gps_normal = false;
            Communication.positionData.virtualPosition = Communication.realPose.WorldPosition;
            lastDronePos = Communication.realPose.WorldPosition;
            updateRate = Time.deltaTime;
        }

        if(offsetRefreshTimer <= 0f){
            Vector2 randomOffset = Random.insideUnitCircle * currentMaxPosUncertainty;
            Vector3 newOffset = new Vector3(randomOffset.x, 0f, randomOffset.y);
            targetOffset = (targetOffset + newOffset)/2f;
            Communication.positionData.currentTargetOffset = new Vector3(targetOffset.x, 0f, targetOffset.z); 
            offsetRefreshTimer = SamplePositive(offsetRefreshIntervalMean, offsetRefreshIntervalVar);
            //updateRate = SamplePositive(signalUpdateRateMean, signalUpdateRateVar);
        } 

        offsetRefreshTimer -= Time.deltaTime;

        
    }

    IEnumerator UpdatePosition(){
        while(true){
            Vector3 currentOffset;
            currentOffset = Communication.positionData.virtualPosition - lastDronePos;
            Vector3 newOffset;
            switch(positional_signal_level){
                //case 3:
                //    //dronePositionVirtual = vc.transform.position;
                //    currentMaxPosUncertainty = maxPositionUncertaintyNormal;
                //    currentOffset = Communication.positionData.virtualPosition - lastDronePos;
                //    targetOffset = Vector3.MoveTowards(currentOffset, new Vector3(positionOffset.x, 0f , positionOffset.z), gpsDriftSpeedNormal);
                //    
                //    Vector3 virtualDronePosCurrent = Communication.realPose.WorldPosition + targetOffset;
                //    
                //    Communication.positionData.virtualPosition = virtualDronePosCurrent;
                //    Communication.positionData.v2bound = CheckPositionInContingencyBuffer(out Communication.positionData.inBuffer, virtualDronePosCurrent);
                //    Communication.positionData.v2surf = CheckDistanceToBuildingSurface(virtualDronePosCurrent);
                //    updateRate = Time.deltaTime;
                //    break;
                //case 2:
                //    
                //    //Vector3 targetPosition = Vector3.MoveTowards(dronePositionVirtual, vc.transform.position + new Vector3(positionOffset.x, 0f , positionOffset.z), 0.01f);
                //    //dronePositionVirtual = targetPosition;
                //    updateRate = Time.deltaTime;
                //    break;
                //case 1:
                //    currentMaxPosUncertainty = maxPositionUncertaintyAbnormal;
                //    currentOffset = Communication.positionData.virtualPosition - lastDronePos;
                //    targetOffset = Vector3.MoveTowards(currentOffset, new Vector3(positionOffset.x, 0f , positionOffset.z), gpsDriftSpeedAbnormal);
                //    Vector3 vdronePos = Communication.realPose.WorldPosition + targetOffset;
                //    
                //    Communication.positionData.virtualPosition = vdronePos;
                //    Communication.positionData.v2bound = CheckPositionInContingencyBuffer(out Communication.positionData.inBuffer, vdronePos);
                //    Communication.positionData.v2surf = CheckDistanceToBuildingSurface(vdronePos);
                //    updateRate = Time.deltaTime;
                //    //updateRate = SamplePositive(signalUpdateRateMean, signalUpdateRateVar);
                //    break;
                case 1:
                    //dronePositionVirtual = vc.transform.position;
                    updateRate = Time.deltaTime;
                    currentMaxPosUncertainty = maxPositionUncertaintyNormal;
                    
                    newOffset = Vector3.MoveTowards(currentOffset, new Vector3(targetOffset.x, 0f, targetOffset.z), gpsDriftSpeed * updateRate);

                    Vector3 virtualDronePosCurrent = Communication.realPose.WorldPosition + newOffset;

                    Communication.positionData.virtualPosition = virtualDronePosCurrent;
                    Communication.positionData.v2bound = CheckPositionInContingencyBuffer(out Communication.positionData.inBuffer, virtualDronePosCurrent);
                    Communication.positionData.v2surf = CheckDistanceToBuildingSurface(virtualDronePosCurrent);
                    
                    break;
                case 0:
                    //updateRate = SamplePositive(signalUpdateRateMean, signalUpdateRateVar);
                    updateRate = Time.deltaTime;
                    currentMaxPosUncertainty = maxPositionUncertaintyAbnormal;

                    newOffset = Vector3.MoveTowards(currentOffset, new Vector3(targetOffset.x, 0f, targetOffset.z), gpsDriftSpeed * updateRate);
                    Vector3 vdronePos = Communication.realPose.WorldPosition + newOffset;

                    Communication.positionData.virtualPosition = vdronePos;
                    Communication.positionData.v2bound = CheckPositionInContingencyBuffer(out Communication.positionData.inBuffer, vdronePos);
                    Communication.positionData.v2surf = CheckDistanceToBuildingSurface(vdronePos);
                    //updateRate = Time.deltaTime;
                    
                    break;
            }
            lastDronePos = Communication.realPose.WorldPosition;
            yield return new WaitForSeconds(updateRate);
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

    public int GetSignalLevel(){
        return positional_signal_level;
    }
    public void SetSignalLevel(int level){
        positional_signal_level = level;
    }
}
