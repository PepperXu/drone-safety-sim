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
    [SerializeField] TextMeshProUGUI flightState;
    [SerializeField] TextMeshProUGUI controlState;
    [SerializeField] Image systemState;
    [SerializeField] Color normalColor, cautiousColor, warningColor, emergencyColor;
    [SerializeField] Image batteryIcon;
    [SerializeField] Sprite[] batterySprites;
    [SerializeField] TextMeshProUGUI batteryPercentage;
    [SerializeField] TextMeshProUGUI batteryRemainingTime;

    [SerializeField] Image GNSSIcon;
    [SerializeField] Sprite[] GNSSSprites;


    [Header("Flight Telemetry")]
    [SerializeField] TextMeshProUGUI distToHome;
    [SerializeField] TextMeshProUGUI altitude, horiSpeed, vertSpeed, vps;
    [SerializeField] Transform northIcon, headingIcon, attitudeIconAnchor;


    [Header("Mission States")]
    [SerializeField] TextMeshProUGUI missionState;
    [SerializeField] Image cameraBorderUI;

    [Header("Audio")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip camCapture;

    
    [Header("External Anchors")]
    [SerializeField] Transform headAnchor, bodyAnchor;

    [Header("Buttons")]
    [SerializeField] Toggle autoPilotToggle;

    //[SerializeField] StateFinder droneState;
    //int defectCount = 0;
    string[] flightStateString = {"Landed", "Taking Off", "Hovering", "Navigating", "Landing"};
    string[] missionStateString = {"Planning", "Moving to Flight Zone", "In Flight Zone", "Inspecting", "Interrupted", "Returning"};
    string[] controlStateString = {"Auto", "Manual"};
    
    void OnEnable(){
        DroneManager.markDefectEvent.AddListener(MarkDefect);
    }

    void OnDisable(){
        DroneManager.markDefectEvent.RemoveListener(MarkDefect);
    }


    // Update is called once per frame
    void Update()
    {
        flightState.text = flightStateString[(int)VelocityControl.currentFlightState];
        missionState.text = missionStateString[(int)DroneManager.currentMissionState];
        //systemState.text = Enum.GetName(typeof(DroneManager.SystemState), DroneManager.currentSystemState);
        controlState.text = controlStateString[(int)DroneManager.currentControlType];
        if(DroneManager.currentControlType == DroneManager.ControlType.Autonomous)
            autoPilotToggle.isOn = true;
        else   
            autoPilotToggle.isOn = false;

        batteryPercentage.text = ((int) (Communication.battery.batteryPercentage * 100f)) + "%";
        
        int remainingTimeMinutes = Mathf.FloorToInt(Communication.battery.batteryRemainingTime/60);
        batteryRemainingTime.text = remainingTimeMinutes + ":" + Mathf.FloorToInt(Communication.battery.batteryRemainingTime - remainingTimeMinutes * 60);

        if(Communication.battery.batteryPercentage >= 1f){
            batteryIcon.sprite = batterySprites[0];
            batteryIcon.color = Color.green;
            batteryPercentage.color = Color.white;
            batteryRemainingTime.color = Color.white;
            //batteryPercentageCircular.color = Color.white;
        } else if (Communication.battery.batteryPercentage > 0.75f){
            batteryIcon.sprite = batterySprites[0];
            batteryIcon.color = Color.white;
            batteryPercentage.color = Color.white;
            batteryRemainingTime.color = Color.green;
            //batteryPercentageCircular.color = Color.white;
        } else if(Communication.battery.batteryPercentage > 0.5f) {
            batteryIcon.sprite = batterySprites[1];
            batteryIcon.color = Color.white;
            batteryPercentage.color = Color.white;
            batteryRemainingTime.color = Color.green;
            //batteryPercentageCircular.color = Color.white;
        } else if (Communication.battery.batteryPercentage > 0.3) {
            batteryIcon.sprite = batterySprites[2];
            batteryIcon.color = Color.white;
            batteryPercentage.color = Color.white;
            batteryRemainingTime.color = Color.green;
        }
        else if(Communication.battery.batteryPercentage > 0.25f){
            batteryIcon.sprite = batterySprites[2];
            batteryIcon.color = Color.yellow;
            batteryPercentage.color = Color.yellow;
            batteryRemainingTime.color = Color.yellow;
            //batteryPercentageCircular.color = Color.yellow;
        } else if(Communication.battery.batteryPercentage > 0.2f){
            batteryIcon.sprite = batterySprites[3];
            batteryIcon.color = Color.yellow;
            batteryPercentage.color = Color.yellow;
            batteryRemainingTime.color = Color.yellow;
        } else {
            batteryIcon.sprite = batterySprites[3];
            batteryIcon.color = Color.red;
            batteryPercentage.color = Color.red;
            batteryRemainingTime.color = Color.red;
        }


        switch(Communication.positionData.signalLevel){
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


        distToHome.text = ((int)(transform.position-Communication.realPose.WorldPosition).magnitude).ToString();
        altitude.text = ((int)Communication.realPose.Altitude).ToString();
        horiSpeed.text = ((int)new Vector3(Communication.realPose.WorldVelocity.x, 0f, Communication.realPose.WorldVelocity.z).magnitude).ToString();
        vertSpeed.text = ((int)Mathf.Abs(Communication.realPose.WorldVelocity.y)).ToString();

        UpdateCompassUI();
    }



    //public void MarkDefect(ActivateEventArgs args)
    //{
    //    //if (uiSelected)
    //    //    return;
    //    
    //    MarkDefect();
    //}
//
    void MarkDefect()
    {
        if(Communication.positionData.v2surf.magnitude > 8f)
            return;

       // defectCount++;
        Color c = cameraBorderUI.color;
        cameraBorderUI.color = new Color(c.r, c.g, c.b, 1f);
        //DroneManager.mark_defect_flag = true;
        //defectCountPlusUI.color = Color.red;
        audioSource.PlayOneShot(camCapture);
    }

    void UpdateCompassUI()
    {
        float northAngle = -headAnchor.transform.eulerAngles.y;
        northAngle = NormalizeAngle(northAngle);
        northIcon.localEulerAngles = new Vector3(0f, 0f, -northAngle);
       
        float relativeHeading = Communication.realPose.Angles.y - Communication.realPose.Angles.y;
        relativeHeading = NormalizeAngle(relativeHeading);
        headingIcon.localEulerAngles = new Vector3(0f, 0f, -relativeHeading);
        Vector3 relativeOffsetLocal = headAnchor.InverseTransformPoint(Communication.realPose.WorldPosition);
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
        float pitch = NormalizeAngle(Communication.realPose.Angles.x);
        float roll = NormalizeAngle(Communication.realPose.Angles.z);
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

    //void UpdateDistances(){
//
    //    distance2surfaceCursor.anchoredPosition = new Vector2(Mathf.Clamp(vector2surface.magnitude/12f * 60f - 30f, -30f, 30f), distance2surfaceCursor.anchoredPosition.y);
    //    distance2groundCursor.anchoredPosition = new Vector2(distance2groundCursor.anchoredPosition.x, Mathf.Clamp(vpsHeight/70f * 105.56f - 52.78f, -52.78f, 52.78f) );
//
    //}
//
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

    //int GetDefectCount(){
    //   // return defectCount;
    //}
//
    //public string[] GetSystemStateText(){
    //    return systemStateString;
    //}

}
