using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ControlVisUpdater : MonoBehaviour
{
    private bool visActive = false;
    [SerializeField] private VisType dis2groundVis;
    [SerializeField] private VisType dis2boundVis;
    [SerializeField] private VisType dis2SurfaceVis;
    [SerializeField] private VisType futureTrajectory;
    [SerializeField] private VisType attitude;
    [SerializeField] private Image cwise_Pitch_f, cwise_Pitch_b, acwise_Pitch_f, acwise_Pitch_b, cwise_Roll_l, cwise_Roll_r, acwise_Roll_l, acwise_Roll_r;
    [SerializeField] private VisType posCircle;
    //[SerializeField] private VisType heading;
    [SerializeField] private VisType camFrustum;

    [SerializeField] private VisType windDir;
    [SerializeField] private ParticleSystem masterParticle;

    [SerializeField] private ParticleSystem[] windParticles;
    [SerializeField] private VisType batteryRing;
    [SerializeField] private Image batteryRingImg;
    [SerializeField] private TextMeshProUGUI batteryRemainingTimeText;
    [SerializeField] private Transform batteryRingTextAnchor;

    [SerializeField] private VisType positioning;
    [SerializeField] private LayerMask realObstacleLayerMask;

    [SerializeField] private Transform droneParent;

    private float dis2ground;

    public Vector3[] predictedPoints;

    public bool inBuffer;
    public Vector3 vectorToNearestBufferBound, vectorToGround, vectorToNearestSurface;

    //public Vector3 positionOffset = Vector3.zero;

    public float windStrength;
    public Quaternion windRotation;

    public float batteryPercentage, remainingTimeInSeconds;
    public int pos_sig_lvl;

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
                dis2groundVis.showVisualization = true;
                futureTrajectory.showVisualization = true;
                attitude.showVisualization = true;
                batteryRing.showVisualization = true;
                StartCoroutine(UpdateControlVis());
            }
        } else {
            visActive = false;
            StopAllCoroutines();
            dis2groundVis.showVisualization = false;
            futureTrajectory.showVisualization = false;
            attitude.showVisualization = false;
            dis2boundVis.showVisualization = false;
            dis2SurfaceVis.showVisualization = false;
            camFrustum.showVisualization = false;
            windDir.showVisualization = false;
            batteryRing.showVisualization = false;
            positioning.showVisualization = false;
        }
    }
    IEnumerator UpdateControlVis(){
        while(true){
            transform.position = PositionalSensorSimulator.dronePositionVirtual;
            transform.eulerAngles = new Vector3(0f, droneParent.eulerAngles.y, 0f);
            UpdateDistance2Ground();
            UpdateDistance2Bound();
            UpdateDistance2Surface();
            UpdateFutureTrajectory();
            UpdateAttitudeVis();
            UpdateCameraFrustum();
            UpdateWindVis();
            UpdateBatteryRing();
            UpdatePositioningIndicator();
            yield return new WaitForEndOfFrame();
        }
    }

    void UpdateDistance2Ground(){
        dis2ground = vectorToGround.magnitude;
        LineRenderer lr = dis2groundVis.transform.GetComponentInChildren<LineRenderer>();
        if (lr)
        {
            Vector3 hitPoint = transform.position + vectorToGround;
            lr.SetPosition(1, transform.InverseTransformPoint(hitPoint));
            lr.transform.GetChild(0).position = hitPoint + (-vectorToGround).normalized * 0.01f;
            lr.transform.GetChild(1).localPosition = transform.InverseTransformPoint(hitPoint) / 2f;
            lr.transform.GetChild(1).GetComponentInChildren<TextMeshPro>().text = "" + Mathf.Round(dis2ground * 10f) / 10f + " m";
        }
        
    }

    void UpdateDistance2Bound()
    {
        float dis2bound = vectorToNearestBufferBound.magnitude;
        if (dis2bound > 10f)
        {
            dis2boundVis.showVisualization = false;
            return;
        }
        dis2boundVis.showVisualization = true;
        LineRenderer lr = dis2boundVis.transform.GetComponentInChildren<LineRenderer>();
        if (lr)
        {
            Vector3 hitPoint = transform.position + vectorToNearestBufferBound;
            Vector3 localHitPos = transform.InverseTransformPoint(hitPoint);
            localHitPos = new Vector3(localHitPos.x, 0f, localHitPos.z);
            lr.SetPosition(1, localHitPos);
            
            lr.transform.GetChild(0).position = hitPoint - vectorToNearestBufferBound.normalized * 0.01f;
            lr.transform.GetChild(0).localRotation =  Quaternion.LookRotation(localHitPos, Vector3.up);
            lr.transform.GetChild(1).localPosition = transform.InverseTransformPoint(hitPoint) / 2f;
            lr.transform.GetChild(1).GetComponentInChildren<TextMeshPro>().text = (inBuffer?"-":"") + Mathf.Round(dis2bound * 10f) / 10f + " m";
        }
        if(dis2bound < DroneManager.bufferCautionThreahold){
            dis2boundVis.SwitchHiddenVisTypeLocal(true);
        } else {
            dis2boundVis.SwitchHiddenVisTypeLocal(false);
        }
    }

    void UpdateDistance2Surface()
    {
        float dis2surf = vectorToNearestSurface.magnitude;
        if (dis2surf > 10f)
        {
            dis2SurfaceVis.showVisualization = false;
            return;
        }
        dis2SurfaceVis.showVisualization = true;
        LineRenderer lr = dis2SurfaceVis.transform.GetComponentInChildren<LineRenderer>();
        if (lr)
        {
            Vector3 hitPoint = transform.position + vectorToNearestSurface;
            Vector3 localHitPos = transform.InverseTransformPoint(hitPoint);
            localHitPos = new Vector3(localHitPos.x, 0f, localHitPos.z);
            lr.SetPosition(1, localHitPos);
            
            lr.transform.GetChild(0).position = hitPoint - vectorToNearestSurface.normalized * 0.01f;
            lr.transform.GetChild(0).localRotation =  Quaternion.LookRotation(localHitPos, Vector3.up);
            lr.transform.GetChild(1).localPosition = transform.InverseTransformPoint(hitPoint) / 2f;
            lr.transform.GetChild(1).GetComponentInChildren<TextMeshPro>().text = "" + Mathf.Round(dis2surf * 10f) / 10f + " m";
        }
        if(dis2surf < DroneManager.surfaceCautionThreshold){
            dis2SurfaceVis.SwitchHiddenVisTypeLocal(true);
        } else {
            dis2SurfaceVis.SwitchHiddenVisTypeLocal(false);
        }
    }



    void UpdateFutureTrajectory()
    {
        List<Vector3> trajectory = new List<Vector3>
        {
            Vector3.zero
        };
        foreach (Vector3 predictedPoint in predictedPoints)
        {
            Vector3 localPoint = transform.InverseTransformDirection(predictedPoint);
            trajectory.Add(localPoint);
        }
        LineRenderer lr = futureTrajectory.transform.GetComponentInChildren<LineRenderer>();
        if (lr)
        {
            lr.positionCount = trajectory.Count;
            lr.SetPositions(trajectory.ToArray());
        }
    }

    void UpdateAttitudeVis()
    {
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

    void UpdateCameraFrustum(){
        float dis2surf = vectorToNearestSurface.magnitude;
        if(dis2surf > 8f){
            camFrustum.showVisualization = false;
            return;
        } 
        camFrustum.showVisualization = true;

        camFrustum.transform.GetChild(0).GetChild(0).localScale = Vector3.one * dis2surf;
    }

    void UpdateWindVis(){
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
    }

    void UpdatePositioningIndicator(){
        if(pos_sig_lvl == 3){
            positioning.showVisualization = false;
            positioning.SwitchHiddenVisTypeLocal(false);
            posCircle.SwitchHiddenVisTypeLocal(false);
            return;
        }
        positioning.showVisualization = true;
        positioning.SwitchHiddenVisTypeLocal(true);
        posCircle.SwitchHiddenVisTypeLocal(true);
        //if(pos_sig_lvl == 0){
        //    positioning.visRoot.GetChild(0).gameObject.SetActive(false);
        //    positioning.visRoot.GetChild(0).gameObject.SetActive(true);
        //} else {
        //    positioning.visRoot.GetChild(0).gameObject.SetActive(true);
        //    positioning.visRoot.GetChild(0).gameObject.SetActive(false);
        //}
    }
}
