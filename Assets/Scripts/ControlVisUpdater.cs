using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ControlVisUpdater : MonoBehaviour
{
    private bool visActive = false;
    [SerializeField] private LineRenderer dis2groundVis;
    [SerializeField] private LineRenderer futureTrajectory;
    [SerializeField] private Image cwise_Pitch_f, cwise_Pitch_b, acwise_Pitch_f, acwise_Pitch_b, cwise_Roll_l, cwise_Roll_r, acwise_Roll_l, acwise_Roll_r;

    [SerializeField] private LayerMask realObstacleLayerMask;

    [SerializeField] private Transform droneParent;

    private float dis2ground;

    public Vector3[] predictedPoints;

    void Update(){
        transform.position = droneParent.position;
        transform.eulerAngles = new Vector3(0f, droneParent.eulerAngles.y, 0f);
        if (visActive)
        {
            UpdateDistance2Ground();
            UpdateFutureTrajectory();
            UpdateAttitudeVis();
        }
    }

    void UpdateDistance2Ground(){
        Ray rayDown = new Ray(transform.position, Vector3.down);
        RaycastHit hit;
        if(Physics.Raycast(rayDown, out hit, float.MaxValue, realObstacleLayerMask)){
            dis2ground = hit.distance;
            dis2groundVis.SetPosition(1, transform.InverseTransformPoint(hit.point));
            dis2groundVis.transform.GetChild(0).position = hit.point + (transform.position-hit.point).normalized*0.01f;
            dis2groundVis.transform.GetChild(1).GetComponentInChildren<TextMeshPro>().text = "" + Mathf.Round(dis2ground*10f)/10f + " m";
        }
    }

    public void SetControlVisActive(bool active)
    {
        visActive = active;
    }

    void UpdateFutureTrajectory(){
        List<Vector3> trajectory = new List<Vector3>
        {
            Vector3.zero
        };
        foreach(Vector3 predictedPoint in predictedPoints){
            trajectory.Add(predictedPoint);
        }
        futureTrajectory.positionCount = trajectory.Count;
        futureTrajectory.SetPositions(trajectory.ToArray());
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