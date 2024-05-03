using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldVisUpdater : MonoBehaviour
{
    [SerializeField] VisType contingencyBuffer;

    [SerializeField] VisType flightPlan;
    [SerializeField] VisType coverage;
    [SerializeField] VisType landing_zones;

    //[SerializeField] LineRenderer pathVisualization;
    
    //public int currentWaypointIndex = -1;
    //public float missionProgress;

    //[SerializeField] Transform droneParent;
    [SerializeField] GameObject coverageObject, markedObject;

    //Gradient defaultGradient = new Gradient();
    Color inspectionTrajColor = new Color(0f, 1f, 1f);

    //public Vector3 vectorToSurface;
    //public Transform currentHomepoint;
    //[SerializeField] Transform currentEnabledHomepoint;

    List<Waypoint> waypoints = new List<Waypoint>();
    //public float currentBatteryPercentage;

    //public int pos_sig_lvl;

    //public bool inBuffer;
    //public float distToBuffer;

    //public RaycastHit? currentHit;

    List<GameObject> spawnedCoverageObjects = new List<GameObject>();


    void OnEnable(){
        DroneManager.finishPlanningEvent.AddListener(ResetWorldVis);
        DroneManager.markDefectEvent.AddListener(SpawnCamCoverageWithMark);
        DroneManager.takePhotoEvent.AddListener(SpawnCamCoverage);
    }
    // Start is called before the first frame update
    void OnDisable()
    {
        DroneManager.finishPlanningEvent.RemoveListener(ResetWorldVis);
        DroneManager.markDefectEvent.RemoveListener(SpawnCamCoverageWithMark);
        DroneManager.takePhotoEvent.RemoveListener(SpawnCamCoverage);
    }


    void ResetWorldVis(){
        //defaultGradient.colorKeys = new GradientColorKey[]{new(inspectionTrajColor, 0f), new(inspectionTrajColor, 1f)};
        //defaultGradient.alphaKeys = new GradientAlphaKey[]{new(0.5f, 0f), new(0.5f, 1f)};
        ResetTrajectoryVis();
        StopAllCoroutines();
        RemoveAllCoverageObject();
        StartCoroutine(WorldVisUpdateCoroutine());
    }

    void ResetTrajectoryVis(){
        LineRenderer traj = flightPlan.visRoot.GetChild(0).GetComponent<LineRenderer>();
        foreach(Transform t in traj.transform.GetChild(0)){
            Waypoint wp = t.GetComponent<Waypoint>();
            waypoints.Add(wp);
            wp.currentWaypointState = Waypoint.WaypointState.Neutral;
        }
        Gradient g = traj.colorGradient;
        GradientColorKey[] ck = g.colorKeys;
        GradientAlphaKey[] ak = g.alphaKeys;
        if(ck.Length != 2){
            Array.Resize(ref ck, 2);
        }
        ck[0].color = inspectionTrajColor;
        ck[1].color = inspectionTrajColor;
        if(ak.Length != 2)
            Array.Resize(ref ak, 2);
        ak[0].alpha = 0.5f;
        ak[0].time = 0f;
        ak[1].alpha = 0.5f;
        ak[1].time = 1f;
        g.colorKeys = ck;
        g.alphaKeys = ak;

        traj.colorGradient = g;
    }

    void UpdateHomepointVis(){



        if(DroneManager.currentMissionState == DroneManager.MissionState.Returning || Communication.battery.rth){
            //landing_zones.showVisualization = true;
            landing_zones.SwitchHiddenVisTypeLocal(true);
        } else {
            //landing_zones.showVisualization = false;
            landing_zones.SwitchHiddenVisTypeLocal(false);
        }
        //if(currentEnabledHomepoint != currentHomepoint){
        //    if(currentEnabledHomepoint != null){
        //        currentEnabledHomepoint.gameObject.SetActive(false);
        //    }
        //    currentHomepoint.gameObject.SetActive(true);
        //    currentEnabledHomepoint = currentHomepoint;
        //}
    }

    void UpdateFlightPlanVis(){
        if(!flightPlan.gameObject.activeInHierarchy)
            return;

        if(waypoints == null || waypoints.Count <= 0)
            return;
        
        //LineRenderer traj = flightPlan.visRoot.GetChild(0).GetComponent<LineRenderer>();
        if(DroneManager.currentMissionState == DroneManager.MissionState.Inspecting){
            for(int i = 0; i < waypoints.Count; i++){
                if(i == Communication.currentWaypointIndex) {
                    waypoints[i].currentWaypointState = Waypoint.WaypointState.Next;
                    waypoints[i].missionProgress = (float)Communication.currentWaypointIndex/waypoints.Count;
                }
                else if(i == Communication.currentWaypointIndex + 1)
                    waypoints[i].currentWaypointState = Waypoint.WaypointState.NextNext;
                else
                    waypoints[i].currentWaypointState = Waypoint.WaypointState.Hidden;
            }
            //Gradient g = traj.colorGradient;
            //GradientColorKey[] ck = g.colorKeys;
            //GradientAlphaKey[] ak = g.alphaKeys;
            //if(ck.Length != 2){
            //    Array.Resize(ref ck, 2);
            //}
            //ck[0].color = inspectionTrajColor;
            //ck[1].color = inspectionTrajColor;
            //if(ak.Length != 6)
            //    Array.Resize(ref ak, 6);
            //ak[0].alpha = 0f;
            //ak[0].time = 0f;
            //ak[1].alpha = 0f;
            //ak[1].time = Mathf.Max(0f, (currentWaypointIndex - 10f)/waypoints.Length);
            //ak[2].alpha = 1f;
            //ak[2].time = Mathf.Max(0f, (currentWaypointIndex - 3f)/waypoints.Length);
            //ak[3].alpha = 1f;
            //ak[3].time = Mathf.Min(1f, (currentWaypointIndex + 3f)/waypoints.Length);
            //ak[4].alpha = 0f;
            //ak[4].time = Mathf.Min(1f, (currentWaypointIndex + 10f)/waypoints.Length);
            //ak[5].alpha = 0f;
            //ak[5].time = 1f;
////
////
            //g.colorKeys = ck;
            //g.alphaKeys = ak;
////
            //traj.colorGradient = g;
        }
    }

    IEnumerator WorldVisUpdateCoroutine(){
        while(true){
            UpdateFlightPlanVis();
            UpdateHomepointVis();
            if(!Communication.positionData.inBuffer || Communication.positionData.v2bound.magnitude < 1f){
                contingencyBuffer.showVisualization = true;
            } else {
                contingencyBuffer.showVisualization = false;
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    //public void UpdateWaypontList(Waypoint[] wps){
    //    waypoints = wps;
    //}
//
    void SpawnCamCoverage(){
        if(Communication.positionData.signalLevel == 0)
            return;
        if (Communication.positionData.v2surf.magnitude > 9999f)
            return;

        GameObject covObj = Instantiate(coverageObject);
        covObj.transform.position = Communication.positionData.virtualPosition + Communication.positionData.v2surf;
        covObj.transform.rotation = Quaternion.LookRotation(Vector3.up, - Communication.positionData.v2surf.normalized);
        covObj.transform.localScale *=  Communication.positionData.v2surf.magnitude;
        covObj.transform.parent = coverage.visRoot;
        spawnedCoverageObjects.Add(covObj);
        Debug.Log("Coverage Spawned");
    }

    void SpawnCamCoverageWithMark(){
        if(Communication.positionData.signalLevel == 0)
            return;
        if (Communication.positionData.v2surf.magnitude > 9999f)
            return;

        GameObject covObj = Instantiate(coverageObject);
        covObj.transform.position = Communication.positionData.virtualPosition + Communication.positionData.v2surf;
        covObj.transform.rotation = Quaternion.LookRotation(Vector3.up, - Communication.positionData.v2surf.normalized);
        covObj.transform.localScale *=  Communication.positionData.v2surf.magnitude;
        covObj.transform.parent = coverage.visRoot;
        spawnedCoverageObjects.Add(covObj);
        if(Communication.markDefectHit != null){
            RaycastHit hit = (RaycastHit)Communication.markDefectHit;
            GameObject markObj = Instantiate(markedObject);
            markObj.transform.position = hit.point + hit.normal.normalized * 0.01f;
            markObj.transform.rotation = Quaternion.LookRotation(Vector3.up, hit.normal);
            markObj.transform.parent = covObj.transform;
            Debug.Log("Coverage Spawned with Mark");
        }

    }

    void RemoveAllCoverageObject(){
        foreach(GameObject obj in spawnedCoverageObjects){
            Destroy(obj);
        }
    }

}
