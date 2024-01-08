using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionalSensorSimulator : MonoBehaviour
{
    //int currentSatelliteCount = 35;
    [SerializeField] UIUpdater uiUpdater;
    [SerializeField] ControlVisUpdater controlVisUpdater;
    [SerializeField] AutopilotManager autopilotManager;

    int positional_signal_level = 3;
    float offsetRefreshIntervalMean = 8f, offsetRefreshIntervalVar = 3f;
    float offsetRefreshTimer = 0f;

    float signalUpdateRateMean = 3f, signalUpdateRateVar = 3f;
    float maxPositionUncertainty = 3f;
    System.Random r;

    Vector3 positionOffset = Vector3.zero;
    float updateRate = 0f;
    // Start is called before the first frame update
    void Start()
    {
        r = new System.Random();
    }

    // Update is called once per frame
    void Update()
    {
        uiUpdater.positional_signal_level = positional_signal_level;
        controlVisUpdater.pos_sig_lvl = positional_signal_level;

        if(offsetRefreshTimer <= 0f){
            Vector3 newOffset = Random.onUnitSphere * Random.Range (0f, maxPositionUncertainty);
            positionOffset = (positionOffset + newOffset)/2f;
            offsetRefreshTimer = SamplePositive(offsetRefreshIntervalMean, offsetRefreshIntervalVar);
            updateRate = SamplePositive(signalUpdateRateMean, signalUpdateRateVar);
        } 
        offsetRefreshTimer -= Time.deltaTime;

        switch(positional_signal_level){
            case 3:
                controlVisUpdater.positionOffset = Vector3.zero;
                controlVisUpdater.updateRate = Time.deltaTime;
                controlVisUpdater.updating = true;
                break;
            case 2:
                controlVisUpdater.positionOffset = positionOffset;
                controlVisUpdater.updateRate = Time.deltaTime;
                controlVisUpdater.updating = true;
                break;
            case 1:
                controlVisUpdater.positionOffset = positionOffset;
                controlVisUpdater.updateRate = updateRate;
                controlVisUpdater.updating = true;
                break;
            case 0:
                controlVisUpdater.updating = false;
                break;
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
