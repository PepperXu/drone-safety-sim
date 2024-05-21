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

    [SerializeField] VisType rth_path;

    [SerializeField] LineRenderer path_critical, path_warning, path_safe;
    [SerializeField] GameObject point_critical, point_rth, point_warning;

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

    //List<Waypoint> waypoints = new List<Waypoint>();
    //public float currentBatteryPercentage;

    //public int pos_sig_lvl;

    //public bool inBuffer;
    //public float distToBuffer;

    //public RaycastHit? currentHit;

    List<GameObject> spawnedCoverageObjects = new List<GameObject>();


    void OnEnable(){
        DroneManager.finishPlanningEvent.AddListener(ResetWorldVis);
        DroneManager.markDefectEvent.AddListener(SpawnMark);
        DroneManager.takePhotoEvent.AddListener(SpawnCamCoverage);
    }
    // Start is called before the first frame update
    void OnDisable()
    {
        DroneManager.finishPlanningEvent.RemoveListener(ResetWorldVis);
        DroneManager.markDefectEvent.RemoveListener(SpawnMark);
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
        if (VisType.globalVisType == VisType.VisualizationType.TwoDOnly)
        {
            traj.widthMultiplier = 0.2f;
            foreach (Waypoint wp in Communication.waypoints)
            {
                wp.transform.localScale = Vector3.one * 2f;
                wp.currentWaypointState = Waypoint.WaypointState.Neutral;
            }
        }
        else
        {
            traj.widthMultiplier = 0.1f;
            foreach (Waypoint wp in Communication.waypoints)
            {
                wp.transform.localScale = Vector3.one;
                wp.currentWaypointState = Waypoint.WaypointState.Neutral;
            }
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

        if(Communication.waypoints == null || Communication.waypoints.Length <= 0)
            return;
        
        //LineRenderer traj = flightPlan.visRoot.GetChild(0).GetComponent<LineRenderer>();
        if(DroneManager.currentMissionState == DroneManager.MissionState.InFlightZone || DroneManager.currentMissionState == DroneManager.MissionState.Inspecting){
            for(int i = 0; i < Communication.waypoints.Length; i++){
                if(i == Communication.currentWaypointIndex) {
                    Communication.waypoints[i].currentWaypointState = Waypoint.WaypointState.Next;
                }
                else if(i == Communication.currentWaypointIndex + 1)
                    Communication.waypoints[i].currentWaypointState = Waypoint.WaypointState.NextNext;
                else
                    Communication.waypoints[i].currentWaypointState = Waypoint.WaypointState.Hidden;
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
            UpdateRTHBatterySufficiency();
            if(!Communication.positionData.inBuffer && !Communication.positionData.gpsLost){
                contingencyBuffer.showVisualization = true;
            } else {
                contingencyBuffer.showVisualization = false;
            }
            yield return new WaitForSeconds(Time.deltaTime*2f);
        }
    }

    //public void UpdateWaypontList(Waypoint[] wps){
    //    waypoints = wps;
    //}
//
    void SpawnCamCoverage(){
        if(Communication.positionData.sigLevel <= 1)
            return;
        if(Communication.positionData.v2surf.magnitude > 999f)
            return;

        GameObject covObj = Instantiate(coverageObject);
        covObj.transform.position = Communication.positionData.virtualPosition + Communication.positionData.v2surf;
        covObj.transform.rotation = Quaternion.LookRotation(Vector3.up, - Communication.positionData.v2surf.normalized);
        covObj.transform.localScale *=  Communication.positionData.v2surf.magnitude;
        covObj.transform.parent = coverage.visRoot;
        spawnedCoverageObjects.Add(covObj);
        Debug.Log("Coverage Spawned");
    }

    void SpawnMark(){
        //if(Communication.positionData.sigLevel <= 1)
        //    return;

        if(Communication.positionData.v2surf.magnitude > 999f)
            return;

        //GameObject covObj = Instantiate(coverageObject);
        //covObj.transform.position = Communication.positionData.virtualPosition + Communication.positionData.v2surf;
        //covObj.transform.rotation = Quaternion.LookRotation(Vector3.up, - Communication.positionData.v2surf.normalized);
        //covObj.transform.localScale *=  Communication.positionData.v2surf.magnitude;
        //covObj.transform.parent = coverage.visRoot;
        //spawnedCoverageObjects.Add(covObj);
        if(Communication.markDefectHit != null){
            RaycastHit hit = (RaycastHit)Communication.markDefectHit;
            GameObject markObj = Instantiate(markedObject);
            markObj.transform.position = hit.point + hit.normal.normalized * 0.01f;
            markObj.transform.rotation = Quaternion.LookRotation(Vector3.up, hit.normal);
            markObj.transform.parent = transform;
            spawnedCoverageObjects.Add(markObj);
            Debug.Log("Coverage Spawned with Mark");
        }

    }

    void UpdateRTHBatterySufficiency()
    {
        if (!rth_path.gameObject.activeInHierarchy)
            return;

        float horizontalDistance = new Vector3(Communication.battery.vector2Home.x, 0f, Communication.battery.vector2Home.z).magnitude;
        float verticalDistance = Mathf.Abs(Communication.battery.vector2Home.y);
        if (VelocityControl.currentFlightState == VelocityControl.FlightState.Hovering || VelocityControl.currentFlightState == VelocityControl.FlightState.Navigating)
        {
            if (Communication.battery.distanceUntilRTH > horizontalDistance + verticalDistance)
            {
                path_critical.positionCount = 0;
                path_warning.positionCount = 0;
                path_safe.positionCount = 3;
                path_safe.SetPositions(new Vector3[] { landing_zones.visRoot.GetChild(0).position, landing_zones.visRoot.GetChild(0).position + Vector3.up * verticalDistance, Communication.positionData.virtualPosition });

            }
            else if (Communication.battery.distanceUntilCritical > horizontalDistance + verticalDistance)
            {
                path_critical.positionCount = 0;
                float warningDistance = horizontalDistance + verticalDistance - Communication.battery.distanceUntilRTH;
                if (warningDistance > verticalDistance)
                {
                    Vector3 midPoint = landing_zones.visRoot.GetChild(0).position + Vector3.up * verticalDistance + new Vector3(Communication.battery.vector2Home.x, 0f, Communication.battery.vector2Home.z).normalized * (warningDistance - verticalDistance);
                    path_warning.positionCount = 3;
                    path_safe.positionCount = 2;
                    path_warning.SetPositions(new Vector3[] { landing_zones.visRoot.GetChild(0).position, landing_zones.visRoot.GetChild(0).position + Vector3.up * verticalDistance, midPoint });
                    path_safe.SetPositions(new Vector3[] { midPoint, Communication.positionData.virtualPosition });
                }
                else
                {
                    Vector3 midPoint = landing_zones.visRoot.GetChild(0).position + Vector3.up * warningDistance;
                    path_warning.positionCount = 2;
                    path_safe.positionCount = 3;
                    path_warning.SetPositions(new Vector3[] { landing_zones.visRoot.GetChild(0).position, midPoint });
                    path_safe.SetPositions(new Vector3[] { midPoint, landing_zones.visRoot.GetChild(0).position + Vector3.up * verticalDistance, Communication.positionData.virtualPosition });
                }
            }
            else if (Communication.battery.distanceUntilRTH > 0)
            {
                float warningDistance = Communication.battery.distanceUntilCritical - Communication.battery.distanceUntilRTH;
                float criticalDistance = horizontalDistance + verticalDistance - Communication.battery.distanceUntilCritical;
                if (criticalDistance > verticalDistance)
                {
                    Vector3 midPoint = landing_zones.visRoot.GetChild(0).position + Vector3.up * verticalDistance + new Vector3(Communication.battery.vector2Home.x, 0f, Communication.battery.vector2Home.z).normalized * (criticalDistance - verticalDistance);
                    Vector3 midPoint2 = midPoint + new Vector3(Communication.battery.vector2Home.x, 0f, Communication.battery.vector2Home.z).normalized * warningDistance;
                    path_critical.positionCount = 3;
                    path_warning.positionCount = 2;
                    path_safe.positionCount = 2;
                    path_critical.SetPositions(new Vector3[] { landing_zones.visRoot.GetChild(0).position, landing_zones.visRoot.GetChild(0).position + Vector3.up * verticalDistance, midPoint });
                    path_warning.SetPositions(new Vector3[] { midPoint, midPoint2 });
                    path_safe.SetPositions(new Vector3[] { midPoint2, Communication.positionData.virtualPosition });
                }
                else if (criticalDistance + warningDistance > verticalDistance)
                {
                    Vector3 midPoint = landing_zones.visRoot.GetChild(0).position + Vector3.up * criticalDistance;
                    Vector3 midPoint2 = landing_zones.visRoot.GetChild(0).position + Vector3.up * verticalDistance + new Vector3(Communication.battery.vector2Home.x, 0f, Communication.battery.vector2Home.z).normalized * (criticalDistance + warningDistance - verticalDistance);
                    path_critical.positionCount = 2;
                    path_warning.positionCount = 3;
                    path_safe.positionCount = 2;
                    path_critical.SetPositions(new Vector3[] { landing_zones.visRoot.GetChild(0).position, midPoint });
                    path_warning.SetPositions(new Vector3[] { midPoint, landing_zones.visRoot.GetChild(0).position + Vector3.up * verticalDistance, midPoint2 });
                    path_safe.SetPositions(new Vector3[] { midPoint2, Communication.positionData.virtualPosition });
                }
                else
                {
                    Vector3 midPoint = landing_zones.visRoot.GetChild(0).position + Vector3.up * criticalDistance;
                    Vector3 midPoint2 = landing_zones.visRoot.GetChild(0).position + Vector3.up * (criticalDistance + warningDistance);
                    path_critical.positionCount = 2;
                    path_warning.positionCount = 2;
                    path_safe.positionCount = 3;
                    path_critical.SetPositions(new Vector3[] { landing_zones.visRoot.GetChild(0).position, midPoint });
                    path_warning.SetPositions(new Vector3[] { midPoint, midPoint2 });
                    path_safe.SetPositions(new Vector3[] { midPoint2, landing_zones.visRoot.GetChild(0).position + Vector3.up * verticalDistance, Communication.positionData.virtualPosition });
                }
            }
            else if (Communication.battery.distanceUntilCritical > 0)
            {
                path_safe.positionCount = 0;
                float criticalDistance = horizontalDistance + verticalDistance - Communication.battery.distanceUntilCritical;
                if (criticalDistance > verticalDistance)
                {
                    Vector3 midPoint = landing_zones.visRoot.GetChild(0).position + Vector3.up * verticalDistance + new Vector3(Communication.battery.vector2Home.x, 0f, Communication.battery.vector2Home.z).normalized * (criticalDistance - verticalDistance);

                    path_critical.positionCount = 3;
                    path_warning.positionCount = 2;
                    path_critical.SetPositions(new Vector3[] { landing_zones.visRoot.GetChild(0).position, landing_zones.visRoot.GetChild(0).position + Vector3.up * verticalDistance, midPoint });
                    path_warning.SetPositions(new Vector3[] { midPoint, Communication.positionData.virtualPosition });
                }
                else
                {
                    Vector3 midPoint = landing_zones.visRoot.GetChild(0).position + Vector3.up * criticalDistance;
                    path_critical.positionCount = 2;
                    path_warning.positionCount = 3;
                    path_critical.SetPositions(new Vector3[] { landing_zones.visRoot.GetChild(0).position, midPoint });
                    path_warning.SetPositions(new Vector3[] { midPoint, landing_zones.visRoot.GetChild(0).position + Vector3.up * verticalDistance, Communication.positionData.virtualPosition });
                }
            }
            else
            {
                path_safe.positionCount = 0;
                path_warning.positionCount = 0;
                path_critical.positionCount = 3;
                path_critical.SetPositions(new Vector3[] { landing_zones.visRoot.GetChild(0).position, landing_zones.visRoot.GetChild(0).position + Vector3.up * verticalDistance, Communication.positionData.virtualPosition });
            }

            SetPointsOnRTHPath(point_warning, Communication.battery.distanceUntilLowBat);
            SetPointsOnRTHPath(point_rth, Communication.battery.distanceUntilRTH);
            SetPointsOnRTHPath(point_critical, Communication.battery.distanceUntilCritical);

        }
        else
        {
            path_safe.positionCount = 0;
            path_warning.positionCount = 0;
            path_critical.positionCount = 0;

            point_critical.SetActive(false);
            point_rth.SetActive(false);
            point_warning.SetActive(false);
        }

        if (Communication.battery.batteryState != "Normal")
        {
            rth_path.SwitchHiddenVisTypeLocal(true);
        } else
        {
            rth_path.SwitchHiddenVisTypeLocal(false);
        }
    }

    void RemoveAllCoverageObject(){
        foreach(GameObject obj in spawnedCoverageObjects){
            Destroy(obj);
        }
    }

    void SetPointsOnRTHPath(GameObject point, float distanceUntilPoint)
    {
        float horizontalDistance = new Vector3(Communication.battery.vector2Home.x, 0f, Communication.battery.vector2Home.z).magnitude;
        float verticalDistance = Mathf.Abs(Communication.battery.vector2Home.y);
        if (distanceUntilPoint > 0 && distanceUntilPoint < verticalDistance + horizontalDistance)
        {
            point.SetActive(true);
            float distanceFromLandingPoint = verticalDistance + horizontalDistance - distanceUntilPoint;
            if (distanceFromLandingPoint < verticalDistance)
            {
                point.transform.position = landing_zones.visRoot.GetChild(0).position + Vector3.up * distanceFromLandingPoint;
            }
            else
            {
                point.transform.position = landing_zones.visRoot.GetChild(0).position + Vector3.up * verticalDistance + new Vector3(Communication.battery.vector2Home.x, 0f, Communication.battery.vector2Home.z).normalized * (distanceFromLandingPoint - verticalDistance);
            }
        }
        else
        {
            point.SetActive(false);
        }
    }

}
