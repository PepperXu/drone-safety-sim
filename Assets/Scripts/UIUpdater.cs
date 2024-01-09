using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.XR.Interaction.Toolkit;

public class UIUpdater : MonoBehaviour
{
    [Header("System and States")]
    [SerializeField] TextMeshProUGUI flightState, controlState;
    [SerializeField] Image systemState;
    [SerializeField] Image batteryIcon;
    [SerializeField] Sprite[] batterySprites;
    [SerializeField] TextMeshProUGUI batteryPercentage;
    [SerializeField] TextMeshProUGUI batteryRemainingTime, batteryVoltage;

    [SerializeField] Image GNSSIcon;
    [SerializeField] Sprite[] GNSSSprites;


    [Header("Flight Telemetry")]
    [SerializeField] TextMeshProUGUI distToHome, altitude, horiSpeed, vertSpeed, vps;
    [SerializeField] Transform northIcon, headingIcon, attitudeIconAnchor;


    [Header("Mission States")]
    [SerializeField] TextMeshProUGUI missionState, cameraCountUI, defectCountUI, progressPercentageUI;
    [SerializeField] Image cameraBorderUI;

    [Header("Audio")]
    [SerializeField] AudioSource audioSource, secondaryAudioSource;
    [SerializeField] AudioClip beep, monitoring_ok, monitoring_warn, monitoring_alert, camCapture;
    
    [Header("External Anchors")]
    [SerializeField] StateFinder droneState;
    [SerializeField] Transform headAnchor, bodyAnchor;

    [Header("General References")]
    [SerializeField] GameObject monitoringUI;
    [SerializeField] Image movement_enabled, movement_locked;

    [Header("Current Interface")]
    [SerializeField] GameObject pilotView, taskView;
    
    private bool attachedToHead = false;
    private bool uiSelected = false;
    private XRRayInteractor currentRayInteractor = null;

    [Header("Public Parameters")]
    public float healthyInterval = 1.2f, warningInterval = 0.8f, emergencyInterval = 0.4f;
    public bool enableSound = false;
    public float vpsHeight = 0f;
    public float missionProgress = 0f;
    public float currentBatteryPercentage = 1f;
    public float remainingTime;
    public float positional_signal_level;
    //public int satelliteCount = 35;

    public float voltage;

    public Vector3 vector2surface;

    bool continuous = true;
    float currentMonitoringInterval = 1.2f;
    float monitoringTimer = 0f;
    int defectCount = 0;
    //float progressPercentage = 0f;
    string[] flightStateString = {"Landed", "Taking Off", "Hovering", "Navigating", "Landing"};
    string[] missionStateString = {"Planning", "Moving to Flight Zone", "In Flight Zone", "Inspecting", "Interrupted", "Returning"};
    string[] systemStateString = {"Healthy", "Caution", "Warning", "Emergency"};
    string[] controlStateString = {"Auto", "Manual"};

    public void ResetUI(){
        defectCount = 0;
    }


    // Update is called once per frame
    void Update()
    {
        flightState.text = flightStateString[(int)DroneManager.currentFlightState];
        missionState.text = missionStateString[(int)DroneManager.currentMissionState];
        //systemState.text = Enum.GetName(typeof(DroneManager.SystemState), DroneManager.currentSystemState);
        controlState.text = controlStateString[(int)DroneManager.currentControlType];

        batteryPercentage.text = ((int) ((currentBatteryPercentage - 0.2f)/ 0.8f * 100f)) + "%";
        batteryVoltage.text = ((int) (voltage * 10f)) / 10f + "V";
        
        int remainingTimeMinutes = Mathf.FloorToInt(remainingTime/60);
        batteryRemainingTime.text = remainingTimeMinutes + ":" + Mathf.FloorToInt(remainingTime - remainingTimeMinutes * 60);

        if(currentBatteryPercentage >= 1f){
            batteryIcon.sprite = batterySprites[0];
            batteryIcon.color = Color.green;
            batteryPercentage.color = Color.white;
            batteryRemainingTime.color = Color.white;
        } else if (currentBatteryPercentage > 0.73333f){
            batteryIcon.sprite = batterySprites[0];
            batteryIcon.color = Color.white;
            batteryPercentage.color = Color.white;
            batteryRemainingTime.color = Color.green;
        } else if(currentBatteryPercentage > 0.46667f) {
            batteryIcon.sprite = batterySprites[1];
            batteryIcon.color = Color.white;
            batteryPercentage.color = Color.white;
            batteryRemainingTime.color = Color.green;
        } else if(currentBatteryPercentage > 0.3f){
            batteryIcon.sprite = batterySprites[2];
            batteryIcon.color = Color.yellow;
            batteryPercentage.color = Color.yellow;
            batteryRemainingTime.color = Color.yellow;
        } else if(currentBatteryPercentage > 0.2f){
            batteryIcon.sprite = batterySprites[3];
            batteryIcon.color = Color.red;
            batteryPercentage.color = Color.red;
            batteryRemainingTime.color = Color.red;
        } else {
            batteryIcon.sprite = batterySprites[3];
            batteryIcon.color = Color.red;
            batteryPercentage.color = Color.red;
            batteryRemainingTime.color = Color.red;
        }

        if(voltage > 10f){
            batteryVoltage.color = Color.white;
        } else if(voltage > 9f){
            batteryVoltage.color = Color.yellow;
        } else {
            batteryVoltage.color = Color.red;
        }

        switch(positional_signal_level){
            case 3:
                GNSSIcon.sprite = GNSSSprites[0];
                GNSSIcon.color = Color.white;
                break;
            case 2:
                GNSSIcon.sprite = GNSSSprites[1];
                GNSSIcon.color = Color.yellow;
                break;
            case 1:
                GNSSIcon.sprite = GNSSSprites[2];
                GNSSIcon.color = Color.yellow;
                break;
            case 0:
                GNSSIcon.sprite = GNSSSprites[3];
                GNSSIcon.color = Color.red;
                break;
        }


        distToHome.text = ((int)(transform.position-droneState.pose.WorldPosition).magnitude).ToString();
        altitude.text = ((int)droneState.Altitude).ToString();
        horiSpeed.text = ((int)new Vector3(droneState.pose.WorldVelocity.x, 0f, droneState.pose.WorldVelocity.z).magnitude).ToString();
        vertSpeed.text = ((int)Mathf.Abs(droneState.pose.WorldVelocity.y)).ToString();
        vps.text = ((int)vpsHeight).ToString();
        cameraCountUI.text = CameraController.photoTaken.ToString();
        defectCountUI.text = defectCount.ToString();
        progressPercentageUI.text = (int)(missionProgress*100f) + "%";

        if(VisType.globalVisType == VisType.VisualizationType.MissionOnly){
            taskView.SetActive(true);
            pilotView.SetActive(false);
        } else {
            taskView.SetActive(false);
            pilotView.SetActive(true);
        }


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
                if (audioSource.clip != beep)
                {
                    audioSource.clip = beep;
                    StartCoroutine(PlayMonitoringSound());
                }
            }
        } else
        {
            if (audioSource.isPlaying)
                audioSource.Stop();
        }

        CheckingSystemState();

        UpdateCompassUI();
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

    void CheckingSystemState(){
        switch (DroneManager.currentSafetyState)
        {
            case DroneManager.SafetyState.Healthy:
                systemState.color = Color.green;
                if(enableSound){
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
                }
                break;
            case DroneManager.SafetyState.Caution:
                systemState.color = Color.yellow;
                if(enableSound){
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
                }
                break;
            case DroneManager.SafetyState.Warning:
                systemState.color = new Color(1f, 0.5f, 0f);
                if(enableSound){
                    if (continuous)
                    {
                        if (audioSource.clip != monitoring_alert)
                        {
                            audioSource.clip = monitoring_alert;
                            audioSource.Play();
                        }
                    }
                    else
                        currentMonitoringInterval = warningInterval;
                }
                break;
            case DroneManager.SafetyState.Emergency:
                systemState.color = Color.red;
                if(enableSound){
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
                }
                break;
            default:
                Debug.LogError("System State Undefined");
                break;
        }
    }


    public void SelectUI(SelectEnterEventArgs args){
        if(!uiSelected){
            uiSelected = true;
            currentRayInteractor = (XRRayInteractor)args.interactorObject;
        }
    }

    public void UnSelectUI(SelectExitEventArgs args){
        if(currentRayInteractor == (XRRayInteractor)args.interactorObject){
            uiSelected = false;
            currentRayInteractor = null;
            if(attachedToHead){
                monitoringUI.transform.parent = headAnchor;
            } else {
                monitoringUI.transform.parent = bodyAnchor;
            }
        }
    }


    public void ToggleAttachToHead(ActivateEventArgs args){
        if(uiSelected && currentRayInteractor == (XRRayInteractor)args.interactorObject){
            if(!attachedToHead){
                attachedToHead = true;
                movement_enabled.gameObject.SetActive(false);
                movement_locked.gameObject.SetActive(true);
            } else {
                attachedToHead = false;
                movement_enabled.gameObject.SetActive(true);
                movement_locked.gameObject.SetActive(false);
            }
        }
    }

    public void MarkDefect(ActivateEventArgs args)
    {
        if (uiSelected)
            return;
        
        if(vector2surface.magnitude > 8f)
            return;

        defectCount++;
        Color c = cameraBorderUI.color;
        cameraBorderUI.color = new Color(c.r, c.g, c.b, 1f);
        DroneManager.mark_defect_flag = true;
        secondaryAudioSource.PlayOneShot(camCapture);
    }

    void UpdateCompassUI()
    {
        float northAngle = -headAnchor.transform.eulerAngles.y;
        northAngle = NormalizeAngle(northAngle);
        northIcon.localEulerAngles = new Vector3(0f, 0f, -northAngle);
       
        float relativeHeading = droneState.transform.eulerAngles.y - headAnchor.transform.eulerAngles.y;
        relativeHeading = NormalizeAngle(relativeHeading);
        headingIcon.localEulerAngles = new Vector3(0f, 0f, -relativeHeading);
        Vector3 relativeOffsetLocal = headAnchor.InverseTransformPoint(droneState.transform.position);
        Vector2 relativeOffset2D = new Vector2(relativeOffsetLocal.x, relativeOffsetLocal.z);
        if (relativeOffset2D.magnitude <= 35f)
        {
            headingIcon.localPosition = relativeOffset2D;
        }
        else
        {
            float offsetAngle = Mathf.Atan2(relativeOffset2D.x, relativeOffset2D.y);
            headingIcon.localPosition = new Vector3(35f * Mathf.Sin(offsetAngle), 35f * Mathf.Cos(offsetAngle), 0f);
        }
        float pitch = NormalizeAngle(droneState.transform.localEulerAngles.x);
        float roll = NormalizeAngle(droneState.transform.localEulerAngles.z);
        if (Mathf.Abs(relativeHeading) < 90f)
        {
            attitudeIconAnchor.localPosition = new Vector3(0f, pitch, 0f);
            attitudeIconAnchor.localEulerAngles = new Vector3(0f, 0f, roll);
        } else
        {
            attitudeIconAnchor.localPosition = new Vector3(0f, -pitch, 0f);
            attitudeIconAnchor.localEulerAngles = new Vector3(0f, 0f, -roll);
        }
    }

    float NormalizeAngle(float originalAngularValue)
    {
        float normalizedAngularValue = originalAngularValue;
        while (normalizedAngularValue >= 180f)
        {
            normalizedAngularValue -= 360f;
        }
        while (normalizedAngularValue < -180f)
        {
            normalizedAngularValue += 360f;
        }
        return normalizedAngularValue;
    }

    public int GetDefectCount(){
        return defectCount;
    }

}
