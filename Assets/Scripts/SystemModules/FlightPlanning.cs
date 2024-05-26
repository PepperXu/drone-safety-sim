using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

//[RequireComponent(typeof(BoxCollider))]
public class FlightPlanning : MonoBehaviour
{
    //private BoxCollider boxCollider;
    private Vector3[] flightTrajectory;
    [SerializeField] LineRenderer pathVisualization;
    [SerializeField] GameObject waypoint;
    private Vector3 boundCenter, boundExtends;
    //private int currentHoveringSurfaceIndex = -1, currentSelectedSurfaceIndex = -1;
    private Vector3[,] surfaceVerts = new Vector3[4,4];
    //[SerializeField] GameObject[] surfaceHighlights;
    //[SerializeField] GameObject[] surfaceSelected;
    //private XRRayInteractor currentRayInteractor;
    //private bool selectingSurface = false;
    //private bool enablePlanning = true;

    private bool pathPlanned = false;
    private int verticalSteps = 12;

    private float vertStepLength;
    private float distToSurface = 7f;
    private int horizontalSteps = 4;

    private float groundOffset = 12f;
    private float lrmargin = 1f;

    private bool isFromTop = false;

    //private bool isTest = true;

    private int configIndex = 0;

    public int ConfigIndex {get {return configIndex;} set {configIndex = value;}}

    [SerializeField] GameObject planningUI, monitoringUI;
    [SerializeField] private Transform[] startingPoints;

    //private int currentStartingPoint = 0;
    //[SerializeField] private Transform camRig, droneRig;

    //private int currentSurfIndex = -1;

    //[SerializeField] WorldVisUpdater worldVis;

    private bool autoPlan = true;

    private float eventZoneXmin = 830.5f, eventZoneXmax = 853.52f, eventZoneZmin = 1070.69f, eventZoneZmax = 1084.61f;


    //public GameObject GPS_Weak_Zone, Wind_Zone;
    //private List<GameObject> gpsZoneObjs = new List<GameObject>(), windZoneObjs = new List<GameObject>();

    //public GameObject[] facade_surfaces;

    public GameObject[] configs;

    private List<Waypoint> wpList = new List<Waypoint>();
    [SerializeField] Vector3 posLeft, posRight, angleLeft, angleRight;
    [SerializeField] Transform satelliteCam;

    private Color editorPathColor = new Color(0f, 1f, 0.93f);
    // Start is called before the first frame update

    void OnEnable(){
        DroneManager.resetAllEvent.AddListener(ResetPathPlanning);
    }

    void OnDisable(){
        DroneManager.resetAllEvent.RemoveListener(ResetPathPlanning);
    }

    void ResetPathPlanning(){
        pathPlanned = false;
        UpdateBoundsGeometry();
        //SetStartingPoint(0);
        planningUI.SetActive(true);
        monitoringUI.SetActive(false);
        if(autoPlan){
            UpdatePlanning();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (DroneManager.currentMissionState != DroneManager.MissionState.Planning)
            return;


        //for debugging
        if(Input.GetKeyDown(KeyCode.P)){
            //currentSelectedSurfaceIndex = 0;
            UpdatePlanning();
        }
    }
    

    void UpdatePlanning(){
        //UpdateCurrentFacade();
        GenerateFlightTrajectory();
        //GenerateEventZone(2);
        FinishPlanning();
    }

    public List<Waypoint> VisualizeFlightPlanEditor(int surfaceIndex){
        UpdateBoundsGeometry();
        List<Vector3> path = new List<Vector3>();
        wpList.Clear();
        GameObject flightPlan = new GameObject("Flight Plan");
        LineRenderer flightPlanLR = flightPlan.AddComponent<LineRenderer>();
        flightPlanLR.widthMultiplier = 0.2f;
        flightPlanLR.material = new Material(Shader.Find("Sprites/Default"));
        flightPlanLR.startColor = editorPathColor;


        GenerateTrajectoryOnSurface(ref path, flightPlanLR.transform, surfaceIndex, false, surfaceIndex == 0);

        Vector3[] flightTrajectoryEditor = new Vector3[path.Count];
        flightTrajectoryEditor = path.ToArray();
        flightPlanLR.positionCount = flightTrajectoryEditor.Length;
        flightPlanLR.SetPositions(flightTrajectoryEditor);
        flightPlanLR.useWorldSpace = true;

        foreach(Waypoint wp in wpList){
            wp.ForceUpdateWaypointVisualization();
        }
        return wpList;
    }
    public void GenerateFlightTrajectory()
    {
        //if (currentSelectedSurfaceIndex < 0)
        //    return;

        //DroneManager.currentMissionState = DroneManager.MissionState.MovingToFlightZone;
        //currentSurfIndex = currentSelectedSurfaceIndex;
        
        List<Vector3> path = new List<Vector3>();
        wpList.Clear();
        Transform wpParent = pathVisualization.transform.GetChild(0);
        foreach(Transform waypoint in wpParent){
            Destroy(waypoint.gameObject);
        }

    
        if(configIndex == 0){
            configs[0].SetActive(true);
            configs[1].SetActive(false);
            configs[2].SetActive(false);
            configs[3].SetActive(false);
            configs[4].SetActive(false);
            configs[5].SetActive(false);
            configs[6].SetActive(false);
            GenerateTrajectoryOnSurface(ref path, wpParent, 0, false, true);
            PositionSatelliteCam(0);
            Communication.currentSurfaceIndex = 0;
            ExperimentServer.configManager = configs[0].GetComponent<ConfigManager>();
            //GenerateTrajectoryOnSurface(ref path, wpParent, 1, true, false);
            //GenerateTrajectoryOnSurface(ref path, wpParent, currentSelectedSurfaceIndex, isFromTop, false);
        }
        else if (configIndex == 1)
        {
            configs[0].SetActive(false);
            configs[1].SetActive(true);
            configs[2].SetActive(false);
            configs[3].SetActive(false);
            configs[4].SetActive(false);
            configs[5].SetActive(false);
            configs[6].SetActive(false);
            GenerateTrajectoryOnSurface(ref path, wpParent, 1, false, false);
            //GenerateTrajectoryOnSurface(ref path, wpParent, 0, true, true);
            PositionSatelliteCam(1);
            Communication.currentSurfaceIndex = 1;
            ExperimentServer.configManager = configs[1].GetComponent<ConfigManager>();
        }
        else if (configIndex == 2)
        {
            configs[0].SetActive(false);
            configs[1].SetActive(false);
            configs[2].SetActive(true);
            configs[3].SetActive(false);
            configs[4].SetActive(false);
            configs[5].SetActive(false);
            configs[6].SetActive(false);
            configs[6].SetActive(false);
            GenerateTrajectoryOnSurface(ref path, wpParent, 0, false, true);
            //GenerateTrajectoryOnSurface(ref path, wpParent, 1, true, false);
            PositionSatelliteCam(0);
            Communication.currentSurfaceIndex = 0;
            ExperimentServer.configManager = configs[2].GetComponent<ConfigManager>();
        }
        else if (configIndex == 3){
            configs[0].SetActive(false);
            configs[1].SetActive(false);
            configs[2].SetActive(false);
            configs[3].SetActive(true);
            configs[4].SetActive(false);
            configs[5].SetActive(false);
            configs[6].SetActive(false);
            GenerateTrajectoryOnSurface(ref path, wpParent, 0, false, true);
            //GenerateTrajectoryOnSurface(ref path, wpParent, 0, true, true);
            PositionSatelliteCam(0);
            Communication.currentSurfaceIndex = 0;
            ExperimentServer.configManager = configs[3].GetComponent<ConfigManager>();
        } else if (configIndex == 4) {
            configs[0].SetActive(false);
            configs[1].SetActive(false);
            configs[2].SetActive(false);
            configs[3].SetActive(false);
            configs[4].SetActive(true);
            configs[5].SetActive(false);
            configs[6].SetActive(false);
            GenerateTrajectoryOnSurface(ref path, wpParent, 1, false, false);
            //GenerateTrajectoryOnSurface(ref path, wpParent, 1, true, false);
            PositionSatelliteCam(1);
            Communication.currentSurfaceIndex = 1;
            ExperimentServer.configManager = configs[4].GetComponent<ConfigManager>();
        } else if (configIndex == 5){
            configs[0].SetActive(false);
            configs[1].SetActive(false);
            configs[2].SetActive(false);
            configs[3].SetActive(false);
            configs[4].SetActive(false);
            configs[5].SetActive(true);
            configs[6].SetActive(false);
            GenerateTrajectoryOnSurface(ref path, wpParent, 0, false, true);
            //GenerateTrajectoryOnSurface(ref path, wpParent, 0, true, true);
            PositionSatelliteCam(0);
            Communication.currentSurfaceIndex = 0;
            ExperimentServer.configManager = configs[5].GetComponent<ConfigManager>();
        } else if (configIndex == 6)
        {
            configs[0].SetActive(false);
            configs[1].SetActive(false);
            configs[2].SetActive(false);
            configs[3].SetActive(false);
            configs[4].SetActive(false);
            configs[5].SetActive(false);
            configs[6].SetActive(true);
            GenerateTrajectoryOnSurface(ref path, wpParent, 1, false, false);
            //GenerateTrajectoryOnSurface(ref path, wpParent, 0, true, true);
            PositionSatelliteCam(1);
            Communication.currentSurfaceIndex = 1;
            ExperimentServer.configManager = configs[6].GetComponent<ConfigManager>();
        }
        flightTrajectory = new Vector3[path.Count];
        flightTrajectory = path.ToArray();
        pathVisualization.positionCount = flightTrajectory.Length;
        pathVisualization.SetPositions(flightTrajectory);
        pathPlanned = true;
    }

    void PositionSatelliteCam(int index)
    {
        satelliteCam.position = (index == 0)?posLeft:posRight;
        satelliteCam.eulerAngles = (index == 0)?angleLeft:angleRight;
    }
    void GenerateTrajectoryOnSurface(ref List<Vector3> path, Transform wpParent, int surfaceIndex, bool fromTop, bool reverse){
        List<Vector3> surfacePath = new List<Vector3>();
        Vector3[] currentSurfaceVertices = new Vector3[4];
        if (reverse)
        {
            currentSurfaceVertices[0] = surfaceVerts[surfaceIndex, 0] + (surfaceVerts[surfaceIndex, 1] - surfaceVerts[surfaceIndex, 0]).normalized * 3.3f;
            currentSurfaceVertices[1] = surfaceVerts[surfaceIndex, 1] + (surfaceVerts[surfaceIndex, 0] - surfaceVerts[surfaceIndex, 1]).normalized * 8.6f;
            currentSurfaceVertices[2] = surfaceVerts[surfaceIndex, 2] + (surfaceVerts[surfaceIndex, 0] - surfaceVerts[surfaceIndex, 1]).normalized * 8.6f;
            currentSurfaceVertices[3] = surfaceVerts[surfaceIndex, 3] + (surfaceVerts[surfaceIndex, 1] - surfaceVerts[surfaceIndex, 0]).normalized * 3.3f;
        } else
        {
            currentSurfaceVertices[0] = surfaceVerts[surfaceIndex, 0] + (surfaceVerts[surfaceIndex, 1] - surfaceVerts[surfaceIndex, 0]).normalized * 8.8f;
            currentSurfaceVertices[1] = surfaceVerts[surfaceIndex, 1] + (surfaceVerts[surfaceIndex, 0] - surfaceVerts[surfaceIndex, 1]).normalized * 3.3f;
            currentSurfaceVertices[2] = surfaceVerts[surfaceIndex, 2] + (surfaceVerts[surfaceIndex, 0] - surfaceVerts[surfaceIndex, 1]).normalized * 3.3f;
            currentSurfaceVertices[3] = surfaceVerts[surfaceIndex, 3] + (surfaceVerts[surfaceIndex, 1] - surfaceVerts[surfaceIndex, 0]).normalized * 8.8f;
        }
        Debug.Log("path length: " + ((currentSurfaceVertices[0] - currentSurfaceVertices[1]).magnitude * (verticalSteps + 1f) + (currentSurfaceVertices[1] - currentSurfaceVertices[2]).magnitude));
        Vector3 horizontalOffset = (reverse?-1f:1f) * (currentSurfaceVertices[1] - currentSurfaceVertices[0]);
        bool flipped = false;
        Vector3 surfaceNormal = (surfaceVerts[surfaceIndex, 0] - surfaceVerts[(surfaceIndex + 3) % 4, 0]).normalized;
        vertStepLength = (currentSurfaceVertices[3].y - currentSurfaceVertices[0].y - groundOffset)/verticalSteps;

        if(fromTop){
            Vector3 v = reverse?currentSurfaceVertices[2]:currentSurfaceVertices[3];
            v -= Vector3.up * vertStepLength;
            for (int j = 0; j < verticalSteps; j++)
            {
                if (!flipped)
                {
                    surfacePath.Add(v + surfaceNormal * distToSurface + horizontalOffset.normalized * lrmargin);
                    surfacePath.Add(v + surfaceNormal * distToSurface + horizontalOffset - horizontalOffset.normalized * lrmargin);
                }
                else
                {
                    surfacePath.Add(v + surfaceNormal * distToSurface + horizontalOffset - horizontalOffset.normalized * lrmargin);
                    surfacePath.Add(v + surfaceNormal * distToSurface + horizontalOffset.normalized * lrmargin);
                }
                v -= Vector3.up * vertStepLength;
                flipped = !flipped;
            }
        } else{
            
            Vector3 v = (reverse?currentSurfaceVertices[1]:currentSurfaceVertices[0]) + Vector3.up * groundOffset;
            for (int j = 0; j < verticalSteps; j++)
            {
                if (!flipped)
                {
                    surfacePath.Add(v + surfaceNormal * distToSurface + horizontalOffset.normalized * lrmargin);
                    surfacePath.Add(v + surfaceNormal * distToSurface + horizontalOffset - horizontalOffset.normalized * lrmargin);
                }
                else
                {
                    surfacePath.Add(v + surfaceNormal * distToSurface + horizontalOffset - horizontalOffset.normalized * lrmargin);
                    surfacePath.Add(v + surfaceNormal * distToSurface + horizontalOffset.normalized * lrmargin);
                }
                v += Vector3.up * vertStepLength;
                flipped = !flipped;
            }
        }

        int i = 0;

        int steps = surfaceIndex == 0?horizontalSteps-2:horizontalSteps;
        float horizontalStepLength = (surfacePath[0] - surfacePath[1]).magnitude/steps;
        while (i < surfacePath.Count)
        {
            SpawnWaypoint(surfacePath[i], wpParent, surfaceIndex);
            Vector3 currentTrajectoryPoint = surfacePath[i];
            Vector3 nextTrajectoryPoint = surfacePath[i + 1];
            Vector3 nextTrajectoryPointDirection = (nextTrajectoryPoint - currentTrajectoryPoint).normalized;
            for(int j = 0; j < steps - 1; j++){
                currentTrajectoryPoint += nextTrajectoryPointDirection * horizontalStepLength;
                surfacePath.Insert(++i, currentTrajectoryPoint);
                SpawnWaypoint(currentTrajectoryPoint, wpParent, surfaceIndex);
            }
            i++;
            SpawnWaypoint(surfacePath[i], wpParent, surfaceIndex);
            i++;
        } 

        path.AddRange(surfacePath);
    }

    //For button interaction
    //public void GenerateFlightTrajectory(){
    //    if(currentSurfIndex == -1)
    //        return;
    //    GenerateFlightTrajectory(currentSurfIndex);
    //}
//
    private void SpawnWaypoint(Vector3 position, Transform parent, int surfIndex){
        GameObject wp = Instantiate(waypoint, parent);
        wp.transform.position = position;
        wp.transform.rotation = surfIndex == 0? transform.rotation*Quaternion.FromToRotation(Vector3.forward, Vector3.right):transform.rotation;
        wpList.Add(wp.GetComponent<Waypoint>());
    }

    void FinishPlanning(){
        if(pathPlanned){
            DroneManager.finish_planning_flag = true;
            planningUI.SetActive(false);
            monitoringUI.SetActive(true);
            Communication.waypoints = wpList.ToArray(); 
            //Communication.pathPlanned = true;
            //worldVis.UpdateWaypontList(wpList.ToArray());
            VisType.globalVisType = VisType.VisualizationType.SafetyOnly;
        }
    }

    //void GenerateEventZone(int num){
    //    foreach(GameObject obj in gpsZoneObjs)
    //        if(obj){
    //            Destroy(obj);
    //        }
    //            
    //    gpsZoneObjs.Clear();
//
    //    foreach(GameObject obj in windZoneObjs)
    //        if(obj){
    //            Destroy(obj);
    //        }
    //    
    //    windZoneObjs.Clear();
//
    //    if(num == 1){
    //        EventZonesInLevels(1, 6);
    //    }else if(num == 2){
    //        EventZonesInLevels(1, 6);
    //        EventZonesInLevels(6, 10);
    //    }
    //        
    //}

    //void EventZonesInLevels(int minInclusive, int maxExclusive){
    //    int i = isTest?(minInclusive+1):Random.Range(minInclusive, maxExclusive);
    //    float j = isTest?0.7f:Random.value;
    //    
    //    float spawnY;
    //    if(isFromTop){
    //        spawnY = groundOffset + (verticalSteps - 1 - i) * vertStepLength;
    //    } else {
    //        spawnY = groundOffset + i * vertStepLength;  
    //    }
    //    Vector3 dir = new Vector3(eventZoneXmin, 0f, eventZoneZmax) - new Vector3(eventZoneXmax, 0f, eventZoneZmin);
    //    Vector3 startPos = new Vector3(eventZoneXmax, 0f, eventZoneZmin);
    //    Vector3 spawnPos = startPos + dir * j + Vector3.up * spawnY;
    //    gpsZoneObjs.Add(Instantiate(GPS_Weak_Zone));
    //    gpsZoneObjs.Last().transform.position = spawnPos;
//
    //    int t = isTest?minInclusive:Random.Range(minInclusive, maxExclusive);
    //    if(!isTest){
    //        while(Mathf.Abs(t-i) < 1){
    //            t = Random.Range(minInclusive, maxExclusive);
    //        }
    //    }
    //    j = isTest?0.5f:Random.value;
    //    if(isFromTop){
    //        spawnY = groundOffset + (verticalSteps - 1 - t ) * vertStepLength;
    //    } else {
    //        spawnY = groundOffset + t * vertStepLength;  
    //    }
    //    dir = new Vector3(eventZoneXmin, 0f, eventZoneZmax) - new Vector3(eventZoneXmax, 0f, eventZoneZmin);
    //    startPos = new Vector3(eventZoneXmax, 0f, eventZoneZmin);
    //    spawnPos = startPos + dir * j + Vector3.up * spawnY;
    //    windZoneObjs.Add(Instantiate(Wind_Zone));
    //    windZoneObjs.Last().transform.position = spawnPos;
    //}

    //void UpdateCurrentFacade(){
    //    for(int i = 0; i < facade_surfaces.Length; i++){
    //        if(i == configIndex)
    //            facade_surfaces[i].SetActive(true);
    //        else    
    //            facade_surfaces[i].SetActive(false);
    //    }
    //}

    //void RandomizeStartFromTop(){
    //    int i = Random.Range(0, 2);
    //    isFromTop = i == 0?false:true;
    //}

    private void UpdateBoundsGeometry()
    {
        boundCenter = transform.position;
        boundExtends = transform.localScale/2f;
        
        Matrix4x4 m = new Matrix4x4();
        m.SetColumn(0, transform.right);
        m.SetColumn(1, transform.up);
        m.SetColumn(2, transform.forward);
        m.SetColumn(3, new Vector4(0,0,0,1));
        surfaceVerts[0,0] = boundCenter + (Vector3)(m * new Vector3(-boundExtends.x, -boundExtends.y, boundExtends.z));
        surfaceVerts[0,1] = boundCenter + (Vector3)(m * new Vector3(-boundExtends.x, -boundExtends.y, -boundExtends.z));
        surfaceVerts[0,2] = boundCenter + (Vector3)(m * new Vector3(-boundExtends.x, boundExtends.y, -boundExtends.z));
        surfaceVerts[0,3] = boundCenter + (Vector3)(m * new Vector3(-boundExtends.x, boundExtends.y, boundExtends.z));
                                          
        surfaceVerts[1,0] = boundCenter + (Vector3)(m * new Vector3(-boundExtends.x, -boundExtends.y, -boundExtends.z));
        surfaceVerts[1,1] = boundCenter + (Vector3)(m * new Vector3(boundExtends.x, -boundExtends.y, -boundExtends.z));
        surfaceVerts[1,2] = boundCenter + (Vector3)(m * new Vector3(boundExtends.x, boundExtends.y, -boundExtends.z));
        surfaceVerts[1,3] = boundCenter + (Vector3)(m * new Vector3(-boundExtends.x, boundExtends.y, -boundExtends.z));
                                           
        surfaceVerts[2,0] = boundCenter + (Vector3)(m * new Vector3(boundExtends.x, -boundExtends.y, -boundExtends.z));
        surfaceVerts[2,1] = boundCenter + (Vector3)(m * new Vector3(boundExtends.x, -boundExtends.y, boundExtends.z));
        surfaceVerts[2,2] = boundCenter + (Vector3)(m * new Vector3(boundExtends.x, boundExtends.y, boundExtends.z));
        surfaceVerts[2,3] = boundCenter + (Vector3)(m * new Vector3(boundExtends.x, boundExtends.y, -boundExtends.z));
                                          
        surfaceVerts[3,0] = boundCenter + (Vector3)(m * new Vector3(boundExtends.x, -boundExtends.y, boundExtends.z));
        surfaceVerts[3,1] = boundCenter + (Vector3)(m * new Vector3(-boundExtends.x, -boundExtends.y, boundExtends.z));
        surfaceVerts[3,2] = boundCenter + (Vector3)(m * new Vector3(-boundExtends.x, boundExtends.y, boundExtends.z));
        surfaceVerts[3,3] = boundCenter + (Vector3)(m * new Vector3(boundExtends.x, boundExtends.y, boundExtends.z));
    }



    public void SetVerticalGap(Slider slider){
        verticalSteps = (int)slider.value;
    }

    public void SetHorizontalGap(Slider slider){
        horizontalSteps = (int)slider.value;
    }

    public void SetDist2Surf(Slider slider){
        distToSurface = slider.value;
    }

    public void SetGroundOffset(Slider slider){
        groundOffset = slider.value;
    }

    public void SetIsFromTop(Toggle toggle){
        isFromTop = toggle.isOn; 
    }

    //public void SetIsFromTop(int isFromTop){
    //    this.isFromTop = isFromTop == 1;
    //}

    //public int GetIsFromTop(){
    //    return isFromTop?1:0;
    //}

    //public void SetIsTestRun(int isTestRun){
    //    isTest = isTestRun == 1;
    //}
//
    //public int GetIsTestRun(){
    //    return isTest?1:0;
    //}



    //public void SetStartingPoint(int index){
    //    switch(index){
    //        case 0:
    //            camRig.position = startingPoints[0].position;
    //            camRig.rotation = startingPoints[0].rotation;
    //            droneRig.position = startingPoints[0].GetChild(0).position;
    //            droneRig.rotation = startingPoints[0].GetChild(0).rotation;
    //            GenerateFlightTrajectory(2);
    //            break;
    //        case 1:
    //            camRig.position = startingPoints[1].position;
    //            camRig.rotation = startingPoints[1].rotation;
    //            droneRig.position = startingPoints[1].GetChild(0).position;
    //            droneRig.rotation = startingPoints[1].GetChild(0).rotation;
    //            GenerateFlightTrajectory(0);
    //            break;
    //        case 2:
    //            camRig.position = startingPoints[2].position;
    //            camRig.rotation = startingPoints[2].rotation;
    //            droneRig.position = startingPoints[2].GetChild(0).position;
    //            droneRig.rotation = startingPoints[2].GetChild(0).rotation;
    //            GenerateFlightTrajectory(0);
    //            break;
    //        case 3:
    //            camRig.position = startingPoints[3].position;
    //            camRig.rotation = startingPoints[3].rotation;
    //            droneRig.position = startingPoints[3].GetChild(0).position;
    //            droneRig.rotation = startingPoints[3].GetChild(0).rotation;
    //            GenerateFlightTrajectory(1);
    //            break;
    //        case 4:
    //            camRig.position = startingPoints[4].position;
    //            camRig.rotation = startingPoints[4].rotation;
    //            droneRig.position = startingPoints[4].GetChild(0).position;
    //            droneRig.rotation = startingPoints[4].GetChild(0).rotation;
    //            GenerateFlightTrajectory(1);
    //            break;
    //        default:
    //            break;
    //    }
    //    currentStartingPoint = index;
    //}
}//
