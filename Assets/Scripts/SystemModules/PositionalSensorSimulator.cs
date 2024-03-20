using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionalSensorSimulator : MonoBehaviour
{
    //int currentSatelliteCount = 35;
    [SerializeField] UIUpdater uiUpdater;
    [SerializeField] ControlVisUpdater controlVisUpdater;
    [SerializeField] WorldVisUpdater worldVisUpdater;
    [SerializeField] AutopilotManager autopilotManager;
    [SerializeField] VelocityControl vc;

    int positional_signal_level = 3;
    float offsetRefreshIntervalMean = 3f, offsetRefreshIntervalVar = 1.5f;
    float offsetRefreshTimer = 0f;

    //float signalUpdateRateMean = 5f, signalUpdateRateVar = 5f;
    float maxPositionUncertaintyAbnormal = 10f;
    float maxPositionUncertaintyNormal = 0.5f;

    float currentMaxPosUncertainty;

    float gpsDriftSpeedNormal = 0.002f;
    float gpsDriftSpeedAbnormal = 0.01f;
    System.Random r;
    Vector3 lastDronePos;

    Vector3 positionOffset = new Vector3(0f, 0f, 0f);
    float updateRate = 0f;

    public static Vector3 dronePositionVirtual;

    public bool switch_gps_normal, switch_gps_faulty;
    // Start is called before the first frame update
    void Start()
    {
        r = new System.Random();
        updateRate = Time.deltaTime;
        StartCoroutine(UpdatePosition());
    }

    public void ResetSignalLevel(){
        SetSignalLevel(3);
        switch_gps_normal = true;
    }

    // Update is called once per frame
    void Update()
    {
        uiUpdater.positional_signal_level = positional_signal_level;
        controlVisUpdater.pos_sig_lvl = positional_signal_level;
        worldVisUpdater.pos_sig_lvl = positional_signal_level;

        if(switch_gps_normal){
            switch_gps_normal = false;
            dronePositionVirtual = vc.transform.position;
            updateRate = Time.deltaTime;
        }

        

        if(offsetRefreshTimer <= 0f){
            Vector3 newOffset = Random.onUnitSphere * Random.Range (0f, currentMaxPosUncertainty);
            positionOffset = (positionOffset + newOffset)/2f;
            offsetRefreshTimer = SamplePositive(offsetRefreshIntervalMean, offsetRefreshIntervalVar);
            //updateRate = SamplePositive(signalUpdateRateMean, signalUpdateRateVar);
        } 
        offsetRefreshTimer -= Time.deltaTime;

        
    }

    IEnumerator UpdatePosition(){
        while(true){
            Vector3 currentOffset;
            Vector3 targetOffset;
            switch(positional_signal_level){
                case 3:
                    //dronePositionVirtual = vc.transform.position;
                    currentMaxPosUncertainty = maxPositionUncertaintyNormal;
                    currentOffset = dronePositionVirtual - lastDronePos;
                    targetOffset = Vector3.MoveTowards(currentOffset, new Vector3(positionOffset.x, 0f , positionOffset.z), gpsDriftSpeedNormal);
                    dronePositionVirtual = vc.transform.position + targetOffset;
                    updateRate = Time.deltaTime;
                    break;
                case 2:
                    
                    //Vector3 targetPosition = Vector3.MoveTowards(dronePositionVirtual, vc.transform.position + new Vector3(positionOffset.x, 0f , positionOffset.z), 0.01f);
                    //dronePositionVirtual = targetPosition;
                    updateRate = Time.deltaTime;
                    break;
                case 1:
                    currentMaxPosUncertainty = maxPositionUncertaintyAbnormal;
                    currentOffset = dronePositionVirtual - lastDronePos;
                    targetOffset = Vector3.MoveTowards(currentOffset, new Vector3(positionOffset.x, 0f , positionOffset.z), gpsDriftSpeedAbnormal);
                    dronePositionVirtual = vc.transform.position + targetOffset;
                    updateRate = Time.deltaTime;
                    //updateRate = SamplePositive(signalUpdateRateMean, signalUpdateRateVar);
                    break;
                case 0:
                    break;
            }
            lastDronePos = vc.transform.position;
            yield return new WaitForSeconds(updateRate);
        }
    }


    public float Sample(float mean, float var)
    {
        float n = NextGaussianDouble();

        return n * Mathf.Sqrt(var) + mean;
    }

    public float SamplePositive(float mean, float var) {
        return Mathf.Abs(Sample(mean, var));
    }

    public float NextGaussianDouble()
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
