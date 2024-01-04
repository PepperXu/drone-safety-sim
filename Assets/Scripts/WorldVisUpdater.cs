using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldVisUpdater : MonoBehaviour
{
    [SerializeField] VisType contingencyBuffer;

    [SerializeField] VisType flightPlan;
    [SerializeField] VisType coverage;
    Waypoint[] waypoints;
    public int currentWaypointIndex = -1;

    [SerializeField] Transform droneParent;
    [SerializeField] GameObject coverageObject, markedObject;

    Gradient defaultGradient = new Gradient();
    Color inspectionTrajColor = new Color(0f, 1f, 1f);

    public Vector3 vectorToSurface;

    List<GameObject> spawnedCoverageObjects = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        
    }


    public void ResetWorldVis(){
        defaultGradient.colorKeys = new GradientColorKey[]{new(Color.white, 0f), new(Color.white, 1f)};
        defaultGradient.alphaKeys = new GradientAlphaKey[]{new(0.5f, 0f), new(0.5f, 1f)};
        StopAllCoroutines();
        RemoveAllCoverageObject();
        StartCoroutine(WorldVisUpdateCoroutine());
    }

    void UpdateFlightPlanVis(){
        if(!flightPlan.gameObject.activeInHierarchy)
            return;

        if(waypoints == null || waypoints.Length <= 0)
            return;
        
        LineRenderer traj = flightPlan.visRoot.GetChild(0).GetComponent<LineRenderer>();
        if(DroneManager.currentMissionState != DroneManager.MissionState.Inspecting){
            foreach(Waypoint wp in waypoints){
                wp.currentWaypointState = Waypoint.WaypointState.Neutral;
            }
            traj.colorGradient = defaultGradient;
        } else {
            for(int i = 0; i < waypoints.Length; i++){
                if(i == currentWaypointIndex)
                    waypoints[i].currentWaypointState = Waypoint.WaypointState.Next;
                else if(i == currentWaypointIndex+1)
                    waypoints[i].currentWaypointState = Waypoint.WaypointState.NextNext;
                else
                    waypoints[i].currentWaypointState = Waypoint.WaypointState.Hidden;
            }
            Gradient g = traj.colorGradient;
            GradientColorKey[] ck = g.colorKeys;
            GradientAlphaKey[] ak = g.alphaKeys;
            if(ck.Length != 2){
                Array.Resize(ref ck, 2);
            }
            ck[0].color = inspectionTrajColor;
            ck[1].color = inspectionTrajColor;
            if(ak.Length != 6)
                Array.Resize(ref ak, 6);
            ak[0].alpha = 0f;
            ak[0].time = 0f;
            ak[1].alpha = 0f;
            ak[1].time = Mathf.Max(0f, (currentWaypointIndex - 2f)/waypoints.Length);
            ak[2].alpha = 1f;
            ak[2].time = Mathf.Max(0f, (currentWaypointIndex - 1f)/waypoints.Length);
            ak[3].alpha = 1f;
            ak[3].time = Mathf.Min(1f, (float)currentWaypointIndex/waypoints.Length);
            ak[4].alpha = 0f;
            ak[4].time = Mathf.Min(1f, (currentWaypointIndex + 2f)/waypoints.Length);
            ak[5].alpha = 0f;
            ak[5].time = 1f;


            g.colorKeys = ck;
            g.alphaKeys = ak;

            traj.colorGradient = g;
        }
    }

    IEnumerator WorldVisUpdateCoroutine(){
        while(true){
            UpdateFlightPlanVis();
            yield return new WaitForSeconds(0.5f);
        }
    }

    public void UpdateWaypontList(){
        List<Waypoint> wpList = new List<Waypoint>();
        foreach(Transform wp in flightPlan.visRoot.GetChild(0).GetChild(0)){
            wpList.Add(wp.GetComponent<Waypoint>());
        }
        waypoints = wpList.ToArray();
    }

    public void SpawnCoverageObject(bool marked){
        GameObject covObj = Instantiate(coverageObject);
        covObj.transform.position = droneParent.position + vectorToSurface;
        covObj.transform.rotation = Quaternion.LookRotation(Vector3.up, -vectorToSurface.normalized);
        covObj.transform.localScale *= vectorToSurface.magnitude;
        covObj.transform.parent = coverage.visRoot;
        spawnedCoverageObjects.Add(covObj);
        if(marked){
            GameObject markObj = Instantiate(markedObject, covObj.transform);
            markObj.transform.localPosition = Vector3.zero;
            markObj.transform.localEulerAngles = Vector3.zero;
            markObj.transform.localScale = Vector3.one;
        }
    }

    void RemoveAllCoverageObject(){
        foreach(GameObject obj in spawnedCoverageObjects){
            Destroy(obj);
        }
    }

}
