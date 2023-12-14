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
    [SerializeField] private VisType futureTrajectory;
    [SerializeField] private VisType attitude;
    [SerializeField] private Image cwise_Pitch_f, cwise_Pitch_b, acwise_Pitch_f, acwise_Pitch_b, cwise_Roll_l, cwise_Roll_r, acwise_Roll_l, acwise_Roll_r;

    [SerializeField] private LayerMask realObstacleLayerMask;

    [SerializeField] private Transform droneParent;

    private float dis2ground;

    public Vector3[] predictedPoints;

    public Vector3 vectorToNearestBufferBound, vectorToGround;


    void Update(){
        transform.position = droneParent.position;
        transform.eulerAngles = new Vector3(0f, droneParent.eulerAngles.y, 0f);
        if (visActive)
        {
            dis2groundVis.showVisualization = true;
            futureTrajectory.showVisualization = true;
            attitude.showVisualization = true;
            UpdateDistance2Ground();
            UpdateDistance2Bound();
            UpdateFutureTrajectory();
            UpdateAttitudeVis();
        } else
        {
            dis2groundVis.showVisualization = false;
            futureTrajectory.showVisualization = false;
            attitude.showVisualization = false;
            dis2boundVis.showVisualization = false;
        }
    }

    void UpdateDistance2Ground(){
        Ray rayDown = new Ray(transform.position, Vector3.down);
        RaycastHit hit;
        if(Physics.Raycast(rayDown, out hit, float.MaxValue, realObstacleLayerMask)){
            dis2ground = hit.distance;
            LineRenderer lr = dis2groundVis.transform.GetComponentInChildren<LineRenderer>();
            if (lr)
            {
                lr.SetPosition(1, transform.InverseTransformPoint(hit.point));
                lr.transform.GetChild(0).position = hit.point + (transform.position - hit.point).normalized * 0.01f;
                lr.transform.GetChild(1).position = transform.InverseTransformPoint(hit.point) / 2f;
                lr.transform.GetChild(1).GetComponentInChildren<TextMeshPro>().text = "" + Mathf.Round(dis2ground * 10f) / 10f + " m";
            }
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
            lr.SetPosition(1, transform.InverseTransformPoint(hitPoint));
            lr.transform.GetChild(0).position = hitPoint - vectorToNearestBufferBound.normalized * 0.01f;
            lr.transform.GetChild(1).position = transform.InverseTransformPoint(hitPoint) / 2f;
            lr.transform.GetChild(1).GetComponentInChildren<TextMeshPro>().text = "" + Mathf.Round(dis2bound * 10f) / 10f + " m";
        }
    }

    public void SetControlVisActive(bool active)
    {
        visActive = active;
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
        float pitch = droneParent.eulerAngles.x;
        while (pitch >= 180f)
        {
            pitch -= 360f;
        }
        while (pitch < -180f)
        {
            pitch += 360f;
        }
        float roll = droneParent.eulerAngles.z;
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
}
