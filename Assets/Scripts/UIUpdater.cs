using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEditorInternal;
using UnityEngine.XR.Interaction.Toolkit;

public class UIUpdater : MonoBehaviour
{
    [Header("System and States")]
    [SerializeField] TextMeshProUGUI flightState, controlState;
    [SerializeField] Image systemState;
    [SerializeField] Image batteryIcon;
    [SerializeField] Sprite[] batterySprites;
    [SerializeField] TextMeshProUGUI batteryPercentage;
    [SerializeField] TextMeshProUGUI batteryVoltage;

    [Header("Flight Telemetry")]
    [SerializeField] TextMeshProUGUI distToHome, altitude, horiSpeed, vertSpeed, vps;
    [SerializeField] Transform northIcon, headingIcon, attitudeIconAnchor;


    [Header("Mission States")]
    [SerializeField] TextMeshProUGUI missionState, defectCountUI, progressPercentageUI;
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
    
    private bool attachedToHead = false;
    private bool uiSelected = false;
    private XRRayInteractor currentRayInteractor = null;

    [Header("Public Parameters")]
    public float healthyInterval = 1.2f, warningInterval = 0.8f, emergencyInterval = 0.4f;
    public bool enableSound = false;
    public float vpsHeight = 0f;
    public float missionProgress = 0f;

    bool continuous = true;
    float currentMonitoringInterval = 1.2f;
    float monitoringTimer = 0f;
    int defectCount = 0;
    //float progressPercentage = 0f;
    string[] flightStateString = {"Landed", "Taking Off", "Hovering", "Navigating", "Landing"};
    string[] missionStateString = {"Planning", "Moving to Flight Zone", "In Flight Zone", "Inspecting", "Interrupted", "Returning"};
    string[] systemStateString = {"Healthy", "Warning", "Emergency"};
    string[] controlStateString = {"Auto", "Manual"};

    
    // Start is called before the first frame update
    void Start()
    {

    }


    // Update is called once per frame
    void Update()
    {
        flightState.text = flightStateString[(int)DroneManager.currentFlightState];
        missionState.text = missionStateString[(int)DroneManager.currentMissionState];
        //systemState.text = Enum.GetName(typeof(DroneManager.SystemState), DroneManager.currentSystemState);
        controlState.text = controlStateString[(int)DroneManager.currentControlType];
        distToHome.text = ((int)(transform.position-droneState.pose.WorldPosition).magnitude).ToString();
        altitude.text = ((int)droneState.Altitude).ToString();
        horiSpeed.text = ((int)new Vector3(droneState.pose.WorldVelocity.x, 0f, droneState.pose.WorldVelocity.z).magnitude).ToString();
        vertSpeed.text = ((int)Mathf.Abs(droneState.pose.WorldVelocity.y)).ToString();
        vps.text = ((int)vpsHeight).ToString();
        defectCountUI.text = defectCount.ToString();
        progressPercentageUI.text = (int)(missionProgress*100f) + "%";

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

        //if(uiSelected){
        //    XRControllerState state = currentRayInteractor.transform.parent.GetComponent<ActionBasedController>().currentControllerState;
        //    if(state.activateInteractionState.activatedThisFrame){
        //        ToggleAttachToHead();
        //    }
        //}
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
        switch (DroneManager.currentSystemState)
        {
            case DroneManager.SystemState.Healthy:
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
            case DroneManager.SystemState.Warning:
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
            case DroneManager.SystemState.Emergency:
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
        if (uiSelected || DroneManager.currentMissionState != DroneManager.MissionState.Inspecting)
            return;

        defectCount++;
        Color c = cameraBorderUI.color;
        cameraBorderUI.color = new Color(c.r, c.g, c.b, 1f);
        DroneManager.take_photo_flag = true;
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

}
