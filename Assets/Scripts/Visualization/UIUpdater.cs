using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.XR.CoreUtils;

public class UIUpdater : MonoBehaviour
{
    [Header("System and States")]
    [SerializeField] TextMeshProUGUI flightState;
    [SerializeField] TextMeshProUGUI positioningState;
    [SerializeField] Image systemState;
    [SerializeField] Color normalColor, cautiousColor, warningColor, emergencyColor;
    [SerializeField] Image batteryIcon;
    [SerializeField] Sprite[] batterySprites;
    [SerializeField] TextMeshProUGUI batteryPercentage;
    [SerializeField] TextMeshProUGUI batteryRemainingTime;

    [SerializeField] Slider batterySlider;

    [SerializeField] RectTransform rthBar;
    [SerializeField] RectTransform rthPoint;

    [SerializeField] Image GNSSIcon;
    [SerializeField] TextMeshProUGUI GNSSNumber;
    [SerializeField] Sprite[] GNSSSprites;
    [SerializeField]
    GameObject[] statusText;


    [Header("Flight Telemetry")]
    [SerializeField] TextMeshProUGUI distToHome;
    [SerializeField] TextMeshProUGUI altitude, horiSpeed, vertSpeed, vps;
    [SerializeField] Transform northIcon, headingIcon, attitudeIconAnchor;
    [SerializeField]
    Image[] col_detect;

    [SerializeField] Transform colDirAnchor;
    [SerializeField] TextMeshProUGUI colDistText;



    [Header("Mission States")]
    [SerializeField] TextMeshProUGUI missionState;
    [SerializeField] Image cameraBorderUI;

    [Header("Audio")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip camCapture;

    
    [Header("External Anchors")]
    [SerializeField] Transform headAnchor;
    [SerializeField] Transform handAnchor;

    [Header("Buttons")]
    [SerializeField] Toggle autoPilotToggle;

    //[SerializeField] StateFinder droneState;
    //int defectCount = 0;
    string[] flightStateString = {"Landed", "Taking Off", "Hovering", "Navigating", "Landing"};
    string[] missionStateString = {"Planning", "Moving to Flight Zone", "In Flight Zone", "Inspecting", "Interrupted", "Returning"};
    string[] controlStateString = {"Auto", "Manual"};

    [SerializeField] Transform uiAnchor;

    [SerializeField] Vector3 posLOS, posLow, angLOS, angLow, posHand, angHand, scaleHead, scaleHand;

    //Vector3 targetPos, targetAng;

    //const float surfaceCautionThreshold = 5.0f, surfaceWarningThreshold = 3.0f;

    private int previousSigLevel = -1;

    void OnEnable(){
        DroneManager.markDefectEvent.AddListener(MarkDefect);
        StartCoroutine(AnimateUIReposition());
    }

    void OnDisable(){
        DroneManager.markDefectEvent.RemoveListener(MarkDefect);
        StopAllCoroutines();
    }


    // Update is called once per frame
    void Update()
    {
        //flightState.text = flightStateString[(int)VelocityControl.currentFlightState];
        //missionState.text = missionStateString[(int)DroneManager.currentMissionState];
        //systemState.text = Enum.GetName(typeof(DroneManager.SystemState), DroneManager.currentSystemState);
        //controlState.text = controlStateString[(int)DroneManager.currentControlType];
        if(DroneManager.currentControlType == DroneManager.ControlType.Autonomous)
            autoPilotToggle.isOn = true;
        else   
            autoPilotToggle.isOn = false;

        batteryPercentage.text = ((int) (Communication.battery.batteryPercentage * 100f)) + "%";
        if (Mathf.Abs(Communication.battery.batteryRemainingTime) > 999999f)
        {
            batteryRemainingTime.text = "--:--";
        }
        else
        {
            int remainingTimeMinutes = Mathf.FloorToInt(Communication.battery.batteryRemainingTime / 60);
            batteryRemainingTime.text = remainingTimeMinutes + ":" + Mathf.FloorToInt(Communication.battery.batteryRemainingTime - remainingTimeMinutes * 60);
        }

        batterySlider.value = 1f - Communication.battery.batteryPercentage;
        rthBar.offsetMax = new Vector2(-(1f-Communication.battery.rthThreshold) * 288f, rthBar.offsetMax.y);
        
        rthPoint.anchoredPosition = new Vector2(-(1f-Communication.battery.rthThreshold) * 288f, rthPoint.anchoredPosition.y);

        if(Communication.battery.batteryPercentage >= 1f){
            batteryIcon.sprite = batterySprites[0];
            batteryIcon.color = Color.white;
            batteryPercentage.color = Color.white;
            //batteryRemainingTime.color = Color.white;
            //batteryPercentageCircular.color = Color.white;
        } else if (Communication.battery.batteryPercentage > 0.75f){
            batteryIcon.sprite = batterySprites[0];
            batteryIcon.color = Color.white;
            batteryPercentage.color = Color.white;
            //batteryRemainingTime.color = Color.green;
            //batteryPercentageCircular.color = Color.white;
        } else if(Communication.battery.batteryPercentage > 0.5f) {
            batteryIcon.sprite = batterySprites[1];
            batteryIcon.color = Color.white;
            batteryPercentage.color = Color.white;
            //batteryRemainingTime.color = Color.green;
            //batteryPercentageCircular.color = Color.white;
        } 
        else if(Communication.battery.batteryPercentage > 0.25f){
            batteryIcon.sprite = batterySprites[2];
            batteryIcon.color = Color.white;
            batteryPercentage.color = Color.white;
            //batteryRemainingTime.color = Color.yellow;
            //batteryPercentageCircular.color = Color.yellow;
        } else if(Communication.battery.batteryPercentage > 0.1f){
            batteryIcon.sprite = batterySprites[2];
            batteryIcon.color = Color.yellow;
            batteryPercentage.color = Color.yellow;
            //batteryRemainingTime.color = Color.yellow;
        } else {
            batteryIcon.sprite = batterySprites[3];
            batteryIcon.color = Color.red;
            batteryPercentage.color = Color.red;
            //batteryRemainingTime.color = Color.red;
        }



        switch(Communication.positionData.sigLevel){
            case 3:
                GNSSIcon.sprite = GNSSSprites[0];
                GNSSIcon.color = Color.white;
                positioningState.text = "GPS";
                break;
            case 2:
                GNSSIcon.sprite = GNSSSprites[1];
                GNSSIcon.color = Color.yellow;
                positioningState.text = "GPS";
                break;
            case 1:
                GNSSIcon.sprite = GNSSSprites[2];
                GNSSIcon.color = Color.red;
                positioningState.text = "ATTI";
                break;
            case 0:
                GNSSIcon.sprite = GNSSSprites[3];
                GNSSIcon.color = Color.red;
                positioningState.text = "ATTI";
                break;
        }

        if(Communication.positionData.sigLevel != previousSigLevel)
        {
            GNSSNumber.text = "" + Random.Range(Communication.positionData.sigLevel*5, Communication.positionData.sigLevel*5+5);
            previousSigLevel = Communication.positionData.sigLevel;
        }


        for(int i = 0; i < Communication.collisionData.distances.Length; i++)
        {
            if (Communication.collisionData.distances[i].magnitude < CollisionSensing.surfaceWarningThreshold)
            {
                Color c = Color.red;
                c.a = 1f;
                col_detect[i].color = c;

            } else if (Communication.collisionData.distances[i].magnitude < CollisionSensing.surfaceCautionThreshold)
            {
                Color c = Color.yellow;
                c.a = 1f;
                col_detect[i].color = c;
            } else
            {
                Color c = Color.white;
                c.a = 0f;
                col_detect[i].color = c;
            }
        }

        

        Vector3 col_dir = Communication.collisionData.shortestDistance;
        

        if(col_dir.magnitude < CollisionSensing.surfaceCautionThreshold){
            colDirAnchor.gameObject.SetActive(true);
            colDistText.gameObject.SetActive(true);
            float col_angle = -Vector3.SignedAngle(new Vector3(Communication.droneRb.transform.forward.x, 0f, Communication.droneRb.transform.forward.z), col_dir, Vector3.up);
            colDirAnchor.transform.localEulerAngles = Vector3.forward * col_angle;
            colDistText.text = (int)(col_dir.magnitude * 10f)/10f + " m";
        } else {
            colDirAnchor.gameObject.SetActive(false);
            colDistText.gameObject.SetActive(false);
        }




        distToHome.text = ((int)(transform.position-Communication.realPose.WorldPosition).magnitude).ToString();
        altitude.text = ((int)Communication.realPose.Altitude).ToString();
        float horizonSpeed = new Vector3(Communication.realPose.WorldVelocity.x, 0f, Communication.realPose.WorldVelocity.z).magnitude;
        horiSpeed.text = (((int)(horizonSpeed * 10f))/10f).ToString();
        vertSpeed.text = (((int)(Mathf.Abs(Communication.realPose.WorldVelocity.y)*10f))/10f).ToString();

        UpdateCompassUI();
        UpdateUIStatusText();

        
    }


    IEnumerator AnimateUIReposition()
    {
        while (true)
        {
            if(VisType.globalVisType == VisType.VisualizationType.TwoDOnly){
                if(uiAnchor.parent != handAnchor){
                    uiAnchor.parent = handAnchor;
                    uiAnchor.localPosition = posHand;
                    uiAnchor.localEulerAngles = angHand;
                    uiAnchor.localScale = scaleHand;
                }
            } else {
                if(uiAnchor.parent != headAnchor){
                    uiAnchor.parent = headAnchor;
                    uiAnchor.localPosition = posLow;
                    uiAnchor.localEulerAngles = angLow;
                    uiAnchor.localScale = scaleHead;
                }
                Vector3 targetPos, targetAng;
                if(VisType.globalVisType == VisType.VisualizationType.MissionOnly)
                {
                    targetPos = posLOS;
                    targetAng = angLOS;
                } else
                {
                    targetPos = posLow;
                    targetAng = angLow;
                }

                if((uiAnchor.localPosition - targetPos).magnitude > 0.05f)
                {
                    uiAnchor.localPosition = Vector3.MoveTowards(uiAnchor.localPosition, targetPos, 0.5f * Time.deltaTime);
                }
                if ((uiAnchor.localEulerAngles - targetAng).magnitude > 0.5f)
                {
                    uiAnchor.localEulerAngles = Vector3.MoveTowards(uiAnchor.localEulerAngles, targetAng, 33f * Time.deltaTime);
                }
            }
            
            yield return new WaitForEndOfFrame();
        }
    }

    void UpdateUIStatusText()
    {
        int currentTextIndex = 1;

        if (Communication.battery.batteryDropped)
            currentTextIndex = 2;

        if(DroneManager.currentMissionState == DroneManager.MissionState.Returning)
            currentTextIndex = 3;

        if (VelocityControl.currentFlightState == VelocityControl.FlightState.Landing)
            currentTextIndex = 4;

        

        if (Communication.battery.batteryState == "Low") {
            currentTextIndex = 5;
            if (DroneManager.currentMissionState == DroneManager.MissionState.Returning)
                currentTextIndex = 6;
        }




        if (Communication.battery.batteryState == "Critical")
        {
            currentTextIndex = 7;
            if (DroneManager.currentMissionState == DroneManager.MissionState.Returning)
                currentTextIndex = 8;
        }

        if (Communication.positionData.sigLevel == 2)
            currentTextIndex = 9;

        if (Communication.positionData.sigLevel < 2)
            currentTextIndex = 10;

        


        if (Communication.collisionData.shortestDistance.magnitude < CollisionSensing.surfaceCautionThreshold)
            currentTextIndex = 11;

        if (Communication.collisionData.shortestDistance.magnitude < CollisionSensing.surfaceWarningThreshold)
            currentTextIndex = 12;

        if (VelocityControl.currentFlightState == VelocityControl.FlightState.Landed)
            currentTextIndex = 0;

        for (int i = 0; i < statusText.Length; i++)
            statusText[i].SetActive(i==currentTextIndex);

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
