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
    [SerializeField] TextMeshProUGUI flightState, missionState, controlState;
    [SerializeField] TextMeshProUGUI distToHome, altitude, horiSpeed, vertSpeed, vps;
    [SerializeField] TextMeshProUGUI defectCount, progressPercentage;
    [SerializeField] Image systemState;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip beep, monitoring_ok, monitoring_warn, monitoring_alert;
    
    [SerializeField] StateFinder droneState;
    
    [SerializeField] GameObject monitoringUI;
    [SerializeField] Transform monitoringUIAnchor, originalAnchor;
    [SerializeField] Image movement_enabled, movement_locked;
    private bool attachedToHead = false;
    private bool uiSelected = false;
    private XRRayInteractor currentRayInteractor = null;
    public float healthyInterval = 1.2f, warningInterval = 0.8f, emergencyInterval = 0.4f;
    public bool enableSound = false;

    public float vpsHeight = 0f;
    public int defectsMarking = 0;
    public float progress = 0f;
    bool continuous = true;
    float currentMonitoringInterval = 1.2f;
    float monitoringTimer = 0f;
    string[] flightStateString = {"Landed", "Taking Off", "Hovering", "Navigating", "Landing"};
    string[] missionStateString = {"Planning", "Moving to Flight Zone", "Inspecting", "Returning"};
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
        defectCount.text = defectsMarking.ToString();
        progressPercentage.text = (int)(progress*100f) + "%";

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
        } else
        {
            if (audioSource.isPlaying)
                audioSource.Stop();
        }

        CheckingSystemState();

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
                monitoringUI.transform.parent = monitoringUIAnchor;
            } else {
                monitoringUI.transform.parent = originalAnchor;
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


}
