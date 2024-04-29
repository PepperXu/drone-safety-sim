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

    const float bufferCautionThreahold = 1f, surfaceCautionThreshold = 6.0f, surfaceWarningThreshold = 4.0f;


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

    public void SetControlVisActive(bool active)
    {
        if(active){
            if(!visActive){
                visActive = true;
                foreach(VisType vis in GetComponentsInChildren<VisType>())
                    vis.showVisualization = true;
                StartCoroutine(UpdateControlVis());
            }
        } else {
            visActive = false;
            StopAllCoroutines();
            foreach(VisType vis in GetComponentsInChildren<VisType>())
                vis.showVisualization = false;
        }
    }
    IEnumerator UpdateControlVis(){
        while(true){
            transform.position = Communication.positionData.virtualPosition;
            transform.eulerAngles = new Vector3(0f, Communication.realPose.Angles.y, 0f);
            UpdatePosCircle();
            UpdateDistance2Ground();
            UpdateDistance2Bound();
            UpdateDistance2Surface();
            UpdateFutureTrajectory();
            UpdateAttitudeVis();
            UpdateCameraFrustum();
            UpdateWindVis();
            UpdateBatteryRing();
            UpdatePositioningIndicator();
            UpdateFlightStatus();
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
        dis2groundVis.SetTransparency(Mathf.Max(0, 2-Communication.positionData.signalLevel));
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

        if(dis2bound < bufferCautionThreahold){
            dis2boundVis.SwitchHiddenVisTypeLocal(true);
        } else {
            dis2boundVis.SwitchHiddenVisTypeLocal(false);
        }
        dis2boundVis.SetTransparency(Mathf.Max(0, 2-Communication.positionData.signalLevel));
    }

    void UpdateDistance2Surface()
    {
        if(!dis2SurfaceVis.gameObject.activeInHierarchy)
            return;
        float dis2surf = vectorToNearestSurface.magnitude;
        if (dis2surf > 12f)
        {
            dis2SurfaceVis.showVisualization = false;
            return;
        }
        dis2SurfaceVis.showVisualization = true;
        Vector3 hitPoint = transform.position + vectorToNearestSurface;
        Vector3 localHitPos = transform.InverseTransformPoint(hitPoint);
        localHitPos = new Vector3(localHitPos.x, 0f, localHitPos.z);
        float angle = Vector3.SignedAngle(localHitPos, Vector3.right, Vector3.up);
        //dis2SurfaceVis.visRoot.localEulerAngles = Vector3.up * angle;


        //LineRenderer lr = dis2SurfaceVis.transform.GetComponentInChildren<LineRenderer>();
        Transform projectionAnchor = dis2SurfaceVis.visRoot.GetChild(0);
        Transform projection = projectionAnchor.GetChild(0);
        Transform projectionDisc = dis2SurfaceVis.visRoot.GetChild(1);
        Transform textLabel = dis2SurfaceVis.visRoot.GetChild(2);

        projectionAnchor.rotation = Quaternion.LookRotation(vectorToNearestSurface, Vector3.up);
        projection.GetComponent<SpriteRenderer>().size = new Vector2(dis2surf/12f, 1f);
        projection.localPosition = Vector3.forward * dis2surf /2f;
        projectionDisc.position = hitPoint - vectorToNearestSurface.normalized * 0.01f;
        projectionDisc.localRotation =  Quaternion.LookRotation(localHitPos, Vector3.up);
        textLabel.localPosition = transform.InverseTransformPoint(hitPoint) / 2f;
        textLabel.GetComponentInChildren<TextMeshPro>().text = "" + Mathf.Round(dis2surf * 10f) / 10f + " m";

        
        if(dis2surf < DroneManager.surfaceCautionThreshold){
            dis2SurfaceVis.SwitchHiddenVisTypeLocal(true);
        } else {
            dis2SurfaceVis.SwitchHiddenVisTypeLocal(false);
        }

        dis2SurfaceVis.SetTransparency(Mathf.Max(0, 2-Communication.positionData.signalLevel));
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
        float pitch = droneParent.localEulerAngles.x;
        while (pitch >= 180f)
        {
            pitch -= 360f;
        }
        while (pitch < -180f)
        {
            pitch += 360f;
        }
        float roll = droneParent.localEulerAngles.z;
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

    void UpdatePosCircle(){
        posCircle.SetTransparency(Mathf.Max(0, 2-pos_sig_lvl));
    }

    void UpdateCameraFrustum(){
        if(!camFrustum.gameObject.activeInHierarchy)
            return;
        float dis2surf = vectorToNearestSurface.magnitude;
        if(dis2surf > 8f){
            camFrustum.transform.GetChild(0).GetChild(0).localScale = Vector3.one * 3f;
            //camFrustum.showVisualization = false;
            //return;
        } else {
            //camFrustum.showVisualization = true;
            camFrustum.transform.GetChild(0).GetChild(0).localScale = Vector3.one * dis2surf;
        }
        camFrustum.SetTransparency(Mathf.Max(0, 2-pos_sig_lvl));
    }

    void UpdateWindVis(){
        if(!windDir.gameObject.activeInHierarchy)
            return;
        if(windStrength < 50f){
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
        windDir.transform.localRotation = windRotation;
        float windStrengthCoeff = windStrength/50f;
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
        batteryRingImg.fillAmount = (batteryPercentage - 0.2f)/0.8f;
        int remainingTimeMinutes = Mathf.FloorToInt(remainingTimeInSeconds/60);
        batteryRemainingTimeText.text = remainingTimeMinutes + ":" + Mathf.FloorToInt(remainingTimeInSeconds - remainingTimeMinutes * 60);
        batteryRingTextAnchor.transform.localEulerAngles = new Vector3(0f,0f,-(1f-(batteryPercentage - 0.2f)/0.8f)*180f);
        if(batteryPercentage > 0.46667f) {
            batteryRingImg.color = Color.green;
            dis2groundVis.SwitchHiddenVisTypeLocal(false);
            batteryRing.SwitchHiddenVisTypeLocal(false);
        } else if(batteryPercentage > 0.3f){
            batteryRingImg.color = Color.yellow;
            dis2groundVis.SwitchHiddenVisTypeLocal(false);
            batteryRing.SwitchHiddenVisTypeLocal(true);
        } else {
            batteryRingImg.color = Color.red;
            dis2groundVis.SwitchHiddenVisTypeLocal(true);
            batteryRing.SwitchHiddenVisTypeLocal(true);
        }
        batteryRing.SetTransparency(Mathf.Max(0, 2-pos_sig_lvl));
    }

    void UpdatePositioningIndicator(){
        if(!posUncertainty.gameObject.activeInHierarchy)
            return;
        if(pos_sig_lvl == 3){
            posUncertainty.SwitchHiddenVisTypeLocal(false);
            posUncertainty.visRoot.localScale = Vector3.one * 1.5f;
            Color c = posUncertaintySprite.color;
            c.a = 0.5f;
            posUncertaintySprite.color = c;
        } else {
            posUncertainty.SwitchHiddenVisTypeLocal(true);
            posUncertainty.visRoot.localScale = Vector3.one * 10f;
            Color c = posUncertaintySprite.color;
            c.a = 0.2f;
            posUncertaintySprite.color = c;
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

    void UpdateFlightStatus(){
        if(DroneManager.currentFlightState == DroneManager.FlightState.TakingOff){
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
        flightStatusVis.SetTransparency(Mathf.Max(0, 2-pos_sig_lvl));
    }
}
