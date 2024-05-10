using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ControlVisUpdater : MonoBehaviour
{
    private bool visActive = false;

    [Header("3D Sensing")]
    [SerializeField] private VisType dis2groundVis;
    [SerializeField] private VisType dis2boundVis;
    [SerializeField] private VisType dis2SurfaceVis;

    [Header("Drone Telemetry")]
    [SerializeField] private VisType futureTrajectory;
    [SerializeField] private VisType attitude;
    [SerializeField] private Image cwise_Pitch_f, cwise_Pitch_b, acwise_Pitch_f, acwise_Pitch_b, cwise_Roll_l, cwise_Roll_r, acwise_Roll_l, acwise_Roll_r;
    [SerializeField] private VisType posCircle;
    //[SerializeField] private VisType heading;
    [SerializeField] private VisType camFrustum;

    [Header("Wind")]
    [SerializeField] private VisType windDir;
    [SerializeField] private ParticleSystem masterParticle;

    [SerializeField] private ParticleSystem[] windParticles;

    [Header("Battery")]
    [SerializeField] private VisType batteryRing;
    [SerializeField] private Image batteryRingImg;
    [SerializeField] private TextMeshProUGUI batteryRemainingTimeText;
    [SerializeField] private Transform batteryRingTextAnchor;

    [Header("GPS")]
    [SerializeField] private VisType positioning;
    [SerializeField] private VisType posUncertainty;
    [SerializeField] private SpriteRenderer posUncertaintySprite;


    [Header("Drone Status")]
    [SerializeField] private VisType flightStatusVis;
    [SerializeField] private GameObject flightStatusTakeOff;
    [SerializeField] private GameObject flightStatusInspecting, flightStatusLanding;
    [SerializeField] private VisType warningVis;
    [SerializeField] private GameObject[] warnings;
    private string previousBatteryState = "Normal";
    private bool isRTH = false;
    private float previousCollisionDistance;
    private int previousGPSLevel = 3;

    [Header("Collision Detection")]
    [SerializeField] private VisType collisionDetectVis;
    [SerializeField] private Image[] collisionDirections;

    


    //[Header("Other References")]
    //[SerializeField] private LayerMask realObstacleLayerMask;

    //[SerializeField] private Transform droneParent;


    //private float dis2ground;


    //[Header("Public Fields (Do not modify)")]
    //public Vector3[] predictedPoints;

    //public bool inBuffer;
    //public Vector3 vectorToNearestBufferBound, vectorToNearestSurface;

    //public Vector3 positionOffset = Vector3.zero;

    //public float windStrength;
    //public Quaternion windRotation;

    //public float batteryPercentage, remainingTimeInSeconds;

    //public int pos_sig_lvl;

    //[SerializeField] StateFinder droneState;

    //public float updateRate;

    //public bool updating = true;

    void Start(){
        //updateRate = Time.deltaTime;
        //updating = true;

    }

    void OnEnable(){
        DroneManager.landedEvent.AddListener(SetControlVisInactive);
        DroneManager.takeOffEvent.AddListener(SetControlVisActive);
        DroneManager.resetAllEvent.AddListener(SetControlVisInactive);
    }

    void OnDisable(){
        DroneManager.landedEvent.RemoveListener(SetControlVisInactive);
        DroneManager.takeOffEvent.RemoveListener(SetControlVisActive);
        DroneManager.resetAllEvent.RemoveListener(SetControlVisInactive);
    }

    void SetControlVisActive()
    {
        if(!visActive){
            visActive = true;
            foreach(VisType vis in GetComponentsInChildren<VisType>())
                vis.showVisualization = true;
            StartCoroutine(UpdateControlVis());
        }
    }

    void SetControlVisInactive(){
        visActive = false;
        StopAllCoroutines();
        foreach(VisType vis in GetComponentsInChildren<VisType>())
            vis.showVisualization = false;
    }

    IEnumerator UpdateControlVis(){
        while(true){
            transform.position = Communication.positionData.virtualPosition;
            transform.eulerAngles = new Vector3(0f, Communication.realPose.Angles.y, 0f);
            //UpdatePosCircle();
            UpdateDistance2Ground();
            UpdateDistance2Bound();
            UpdateDistance2Surface();
            //UpdateFutureTrajectory();
            UpdateAttitudeVis();
            UpdateCameraFrustum();
            UpdateWindVis();
            UpdateBatteryRing();
            UpdatePositioningIndicator();
            UpdateFlightStatus();
            UpdateCollisionDetection();
            UpdateWarningVis();
            yield return new WaitForEndOfFrame();
        }
    }

    void UpdateDistance2Ground(){
        if(!dis2groundVis.gameObject.activeInHierarchy)
            return;
    
        float dis2ground = Communication.realPose.Altitude;

        if(dis2ground > 1000f){
            dis2groundVis.showVisualization =false;
            return;
        }
        //LineRenderer lr = dis2groundVis.transform.GetComponentInChildren<LineRenderer>();

        Vector3 hitPoint = transform.position + Vector3.down * dis2ground;
        Transform projection = dis2groundVis.visRoot.GetChild(0);
        Transform projectionDisc = dis2groundVis.visRoot.GetChild(1);
        Transform textLabel = dis2groundVis.visRoot.GetChild(2);

        projection.localScale = new Vector3(0.15f, dis2ground, 1f);
        projection.localPosition = transform.InverseTransformPoint(hitPoint)/2f;

        projectionDisc.position = hitPoint + (-Vector3.down * dis2ground).normalized * 0.01f;
        textLabel.localPosition = transform.InverseTransformPoint(hitPoint) / 2f;
        textLabel.GetComponentInChildren<TextMeshPro>().text = "" + Mathf.Round(dis2ground * 10f) / 10f + " m";
    }

    //Deprecated. Fix it if re-using.
    void UpdateDistance2Bound()
    {
        if(!dis2boundVis.gameObject.activeInHierarchy)
            return;
        float dis2bound = Communication.positionData.v2bound.magnitude;
        if (dis2bound > 10f)
        {
            dis2boundVis.showVisualization = false;
            return;
        }
        dis2boundVis.showVisualization = true;
        //LineRenderer lr = dis2boundVis.transform.GetComponentInChildren<LineRenderer>();

        Vector3 hitPoint = transform.position + Communication.positionData.v2bound;
        Vector3 localHitPos = transform.InverseTransformPoint(hitPoint);
        localHitPos = new Vector3(localHitPos.x, 0f, localHitPos.z);

        Transform projection = dis2boundVis.visRoot.GetChild(0);
        Transform projectionDisc = dis2boundVis.visRoot.GetChild(1);
        Transform textLabel = dis2boundVis.visRoot.GetChild(2);

        projection.localScale = new Vector3(0.3f, dis2bound, 1f);
        projection.localPosition = localHitPos/2f;
            
        projectionDisc.position = hitPoint - Communication.positionData.v2bound.normalized * 0.01f;
        projectionDisc.localRotation =  Quaternion.LookRotation(localHitPos, Vector3.up);
        textLabel.localPosition = transform.InverseTransformPoint(hitPoint) / 2f;
        textLabel.GetComponentInChildren<TextMeshPro>().text = (Communication.positionData.inBuffer?"-":"") + Mathf.Round(dis2bound * 10f) / 10f + " m";

        if(dis2bound < PositionalSensorSimulator.bufferCautionThreahold){
            dis2boundVis.SwitchHiddenVisTypeLocal(true);
        } else {
            dis2boundVis.SwitchHiddenVisTypeLocal(false);
        }
    }

    void UpdateDistance2Surface()
    {
        if(!dis2SurfaceVis.gameObject.activeInHierarchy)
            return;
        Vector3 v2surf = Communication.collisionData.shortestDistance;
        float dis2surf = v2surf.magnitude;
        if (dis2surf > 12f)
        {
            dis2SurfaceVis.showVisualization = false;
            return;
        }
        dis2SurfaceVis.showVisualization = true;
        Vector3 hitPoint = transform.position + v2surf;
        Vector3 localHitPos = transform.InverseTransformPoint(hitPoint);
        localHitPos = new Vector3(localHitPos.x, 0f, localHitPos.z);
        float angle = Vector3.SignedAngle(localHitPos, Vector3.right, Vector3.up);
        //dis2SurfaceVis.visRoot.localEulerAngles = Vector3.up * angle;


        //LineRenderer lr = dis2SurfaceVis.transform.GetComponentInChildren<LineRenderer>();
        Transform projectionAnchor = dis2SurfaceVis.visRoot.GetChild(0);
        Transform projection = projectionAnchor.GetChild(0);
        Transform projectionDisc = dis2SurfaceVis.visRoot.GetChild(1);
        Transform textLabel = dis2SurfaceVis.visRoot.GetChild(2);

        projectionAnchor.rotation = Quaternion.LookRotation(v2surf, Vector3.up);
        projection.GetComponent<SpriteRenderer>().size = new Vector2(dis2surf/12f, 1f);
        projection.localPosition = Vector3.forward * dis2surf /2f;
        projectionDisc.position = hitPoint - v2surf.normalized * 0.01f;
        projectionDisc.localRotation =  Quaternion.LookRotation(localHitPos, Vector3.up);
        textLabel.localPosition = transform.InverseTransformPoint(hitPoint) / 2f;
        textLabel.GetComponentInChildren<TextMeshPro>().text = "" + Mathf.Round(dis2surf * 10f) / 10f + " m";

        
        if(dis2surf < CollisionSensing.surfaceCautionThreshold){
            dis2SurfaceVis.SwitchHiddenVisTypeLocal(true);
        } else {
            dis2SurfaceVis.SwitchHiddenVisTypeLocal(false);
        }

        
    }



    //void UpdateFutureTrajectory()
    //{
    //    if(!futureTrajectory.gameObject.activeInHierarchy)
    //        return;
    //    List<Vector3> trajectory = new List<Vector3>
    //    {
    //        Vector3.zero
    //    };
    //    foreach (Vector3 predictedPoint in predictedPoints)
    //    {
    //        Vector3 localPoint = transform.InverseTransformDirection(predictedPoint);
    //        trajectory.Add(localPoint);
    //    }
    //    LineRenderer lr = futureTrajectory.transform.GetComponentInChildren<LineRenderer>();
    //    if (lr)
    //    {
    //        lr.positionCount = trajectory.Count;
    //        lr.SetPositions(trajectory.ToArray());
    //    }
    //}

    void UpdateAttitudeVis()
    {
        if(!attitude.gameObject.activeInHierarchy)
            return;
        float pitch = Communication.realPose.Angles.x;
        while (pitch >= 180f)
        {
            pitch -= 360f;
        }
        while (pitch < -180f)
        {
            pitch += 360f;
        }
        float roll = Communication.realPose.Angles.z;
        while (roll >= 180f)
        {
            roll -= 360f;
        }
        while (roll < -180f)
        {
            roll += 360f;
        }
        if (pitch <= 0)
        {
            cwise_Pitch_f.fillAmount = -pitch/180f;
            cwise_Pitch_b.fillAmount = -pitch / 180f;
            acwise_Pitch_f.fillAmount = 0f;
            acwise_Pitch_b.fillAmount = 0f;
        } else
        {
            cwise_Pitch_f.fillAmount = 0f;
            cwise_Pitch_b.fillAmount = 0f;
            acwise_Pitch_f.fillAmount = pitch / 180f;
            acwise_Pitch_b.fillAmount = pitch / 180f;
        }
        if (roll <= 0)
        {
            cwise_Roll_l.fillAmount = -roll / 180f;
            cwise_Roll_r.fillAmount = -roll / 180f;
            acwise_Roll_l.fillAmount = 0f;
            acwise_Roll_r.fillAmount = 0f;
        }
        else
        {
            cwise_Roll_l.fillAmount = 0f;
            cwise_Roll_r.fillAmount = 0f;
            acwise_Roll_l.fillAmount = roll / 180f;
            acwise_Roll_r.fillAmount = roll / 180f;
        }

    }

    //void UpdatePosCircle(){
    //    posCircle.SetTransparency(Mathf.Max(0, 1-Communication.positionData.signalLevel));
    //}

    void UpdateCameraFrustum(){
        if(!camFrustum.gameObject.activeInHierarchy)
            return;
        float dis2surf = Communication.positionData.v2surf.magnitude;
        if(dis2surf > 8f){
            camFrustum.transform.GetChild(0).GetChild(0).localScale = Vector3.one * 3f;
            //camFrustum.showVisualization = false;
            //return;
        } else {
            //camFrustum.showVisualization = true;
            camFrustum.transform.GetChild(0).GetChild(0).localScale = Vector3.one * dis2surf;
        }
        //camFrustum.SetTransparency(Mathf.Max(0, 1-Communication.positionData.signalLevel));
    }

    void UpdateWindVis(){
        if(!windDir.gameObject.activeInHierarchy)
            return;
        if(Communication.wind.direction.magnitude < 50f){
            windDir.showVisualization = false;
            dis2SurfaceVis.SwitchHiddenVisTypeLocal(false);
            dis2boundVis.SwitchHiddenVisTypeLocal(false);
            windDir.SwitchHiddenVisTypeLocal(false);
            return;
        }
        dis2SurfaceVis.SwitchHiddenVisTypeLocal(true);
        dis2boundVis.SwitchHiddenVisTypeLocal(true);
        windDir.SwitchHiddenVisTypeLocal(true);
        windDir.showVisualization = true;
        windDir.transform.localRotation = Quaternion.LookRotation(Communication.wind.direction, Vector3.up);
        float windStrengthCoeff = Communication.wind.direction.magnitude/50f;
        //masterParticle.Stop();
        foreach(ParticleSystem par in windParticles){
            var main = par.main;
            main.startSpeed = windStrengthCoeff * 2f;
            main.startLifetime = 1.8f / windStrengthCoeff;
            main.startColor = new Color(1f, 2f-windStrengthCoeff, 2f-windStrengthCoeff);
            //main.duration = 1.8f / windStrengthCoeff * 2f;

            //var burst = par.emission.GetBurst(0);
            //burst.repeatInterval = 1.8f / windStrengthCoeff / 3f;
            //par.emission.SetBurst(0, burst);
        }
        //masterParticle.Play();
    }
    void UpdateBatteryRing(){
        if(!batteryRing.gameObject.activeInHierarchy)
            return;
        batteryRingImg.fillAmount = Communication.battery.batteryPercentage;
        int remainingTimeMinutes = Mathf.FloorToInt(Communication.battery.batteryRemainingTime/60);
        batteryRemainingTimeText.text = remainingTimeMinutes + ":" + Mathf.FloorToInt(Communication.battery.batteryRemainingTime - remainingTimeMinutes * 60);
        batteryRingTextAnchor.transform.localEulerAngles = new Vector3(0f,0f,-(1f-Communication.battery.batteryPercentage)*180f);
        if(Communication.battery.batteryState == "Low") {
            batteryRingImg.color = Color.yellow;
            dis2groundVis.SwitchHiddenVisTypeLocal(false);
            batteryRing.SwitchHiddenVisTypeLocal(true);
        } else if(Communication.battery.batteryState == "Critical"){
            
            batteryRingImg.color = Color.red;
            dis2groundVis.SwitchHiddenVisTypeLocal(true);
            batteryRing.SwitchHiddenVisTypeLocal(true);
        } else {
            batteryRingImg.color = Color.green;
            dis2groundVis.SwitchHiddenVisTypeLocal(false);
            batteryRing.SwitchHiddenVisTypeLocal(false);
            
        }
        //batteryRing.SetTransparency(Mathf.Max(0, 1-Communication.positionData.signalLevel));
    }

    void UpdateWarningVis()
    {
        if (!warningVis.gameObject.activeInHierarchy)
        {
            return;
        }
        if (Communication.battery.batteryState != previousBatteryState)
        {
            if (Communication.battery.batteryState == "Low")
                warnings[2].SetActive(true);
            if (Communication.battery.batteryState == "Critical")
                warnings[4].SetActive(true);
            previousBatteryState = Communication.battery.batteryState;
        }

        if (Communication.battery.rth && !isRTH)
        {
            warnings[3].SetActive(true);
            isRTH = true;
        }

        if(Communication.positionData.sigLevel != previousGPSLevel)
        {
            if (Communication.positionData.sigLevel < previousGPSLevel)
            {
                if (Communication.positionData.sigLevel == 2)
                    warnings[0].SetActive(true);
                else
                    warnings[1].SetActive(true);
            }
            previousGPSLevel = Communication.positionData.sigLevel; 
        }



        if(previousCollisionDistance > CollisionSensing.surfaceCautionThreshold && Communication.collisionData.shortestDistance.magnitude < CollisionSensing.surfaceCautionThreshold)
            warnings[5].SetActive(true);

        if (previousCollisionDistance > CollisionSensing.surfaceWarningThreshold && Communication.collisionData.shortestDistance.magnitude < CollisionSensing.surfaceWarningThreshold)
        {
            warnings[5].SetActive(false);
            warnings[6].SetActive(true);
        }

        previousCollisionDistance = Communication.collisionData.shortestDistance.magnitude;

    }

    void UpdatePositioningIndicator(){
        if(!posUncertainty.gameObject.activeInHierarchy)
            return;
        float currentPosUncertaintyScale = posUncertainty.visRoot.localScale.x;
        Vector3 offset = Communication.positionData.virtualPosition - Communication.realPose.WorldPosition;
        if(!Communication.positionData.gpsLost){
            //posUncertainty.SwitchHiddenVisTypeLocal(false);
            posUncertainty.visRoot.localScale = Vector3.one * offset.magnitude * 2f;
            
        } else {
            //posUncertainty.SwitchHiddenVisTypeLocal(true);
            posUncertainty.visRoot.localScale = Vector3.one * Mathf.Max(currentPosUncertaintyScale, offset.magnitude * 2f);
        }
        if (posUncertainty.visRoot.localScale.x > 3f)
        {
            Color c = posUncertaintySprite.color;
            c.a = 0.2f;
            posUncertaintySprite.color = c;
            posCircle.SetTransparency(1);
            dis2groundVis.SetTransparency(1);
            collisionDetectVis.SetTransparency(1);
            flightStatusVis.SetTransparency(1);
            posUncertainty.SwitchHiddenVisTypeLocal(true);


        } else
        {
            Color c = posUncertaintySprite.color;
            c.a = 0.5f;
            posUncertaintySprite.color = c;
            posCircle.SetTransparency(0);
            dis2groundVis.SetTransparency(0);
            collisionDetectVis.SetTransparency(0);
            flightStatusVis.SetTransparency(0);
            if(Communication.positionData.sigLevel < 3)
            {
                posUncertainty.SwitchHiddenVisTypeLocal(true);
            } else
            {
                posUncertainty.SwitchHiddenVisTypeLocal(false);
            }
        }
        //if(!positioning.gameObject.activeInHierarchy)
        //    return;
        //if(pos_sig_lvl == 3){
        //    positioning.showVisualization = false;
        //    positioning.SwitchHiddenVisTypeLocal(false);
        //    posCircle.SwitchHiddenVisTypeLocal(false);
        //    return;
        //}
        //positioning.showVisualization = true;
        //positioning.SwitchHiddenVisTypeLocal(true);
        //posCircle.SwitchHiddenVisTypeLocal(true);
        //if(pos_sig_lvl == 0){
        //    positioning.visRoot.GetChild(0).gameObject.SetActive(false);
        //    positioning.visRoot.GetChild(0).gameObject.SetActive(true);
        //} else {
        //    positioning.visRoot.GetChild(0).gameObject.SetActive(true);
        //    positioning.visRoot.GetChild(0).gameObject.SetActive(false);
        //}

    }



    void UpdateCollisionDetection()
    {
        if (!collisionDetectVis.gameObject.activeInHierarchy)
            return;
        for (int i = 0; i < Communication.collisionData.distances.Length; i++)
        {
            if (Communication.collisionData.distances[i].magnitude < CollisionSensing.surfaceWarningThreshold)
            {
                collisionDirections[i].gameObject.SetActive(true);
                Color c = Color.red;
                //c.a = 1f;
                collisionDirections[i].color = c;

            }
            else if (Communication.collisionData.distances[i].magnitude < CollisionSensing.surfaceCautionThreshold)
            {
                collisionDirections[i].gameObject.SetActive(true);
                Color c = Color.yellow;
                //c.a = 1f;
                collisionDirections[i].color = c;
            }
            else
            {
                collisionDirections[i].gameObject.SetActive(false);
               // Color c = Color.white;
                //c.a = 0f;
                //collisionDirections[i].color = c;
            }
        }
       
    }

    void UpdateFlightStatus(){
        if(VelocityControl.currentFlightState == VelocityControl.FlightState.TakingOff){
            flightStatusTakeOff.SetActive(true);
            flightStatusInspecting.SetActive(false);
            flightStatusLanding.SetActive(false);
        } else if(DroneManager.currentMissionState == DroneManager.MissionState.Inspecting) {
            flightStatusTakeOff.SetActive(false);
            flightStatusInspecting.SetActive(true);
            flightStatusLanding.SetActive(false);
        } else if(DroneManager.currentMissionState == DroneManager.MissionState.Returning) {
            flightStatusTakeOff.SetActive(false);
            flightStatusInspecting.SetActive(false);
            flightStatusLanding.SetActive(true);
        } else {
            flightStatusTakeOff.SetActive(false);
            flightStatusInspecting.SetActive(false);
            flightStatusLanding.SetActive(false);
        }
        
    }
}
