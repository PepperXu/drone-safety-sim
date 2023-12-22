using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(BoxCollider))]
public class FlightPlanning : MonoBehaviour
{
    private BoxCollider boxCollider;
    private Vector3[] flightTrajectory;
    [SerializeField] LineRenderer pathVisualization;
    [SerializeField] GameObject waypoint;
    private Vector3 boundCenter, boundExtends;
    private int currentHoveringSurfaceIndex = -1, currentSelectedSurfaceIndex = -1;
    private Vector3[,] surfaceVerts = new Vector3[4,4];
    [SerializeField] GameObject[] surfaceHighlights;
    [SerializeField] GameObject[] surfaceSelected;
    private XRRayInteractor currentRayInteractor;
    private bool selectingSurface = false;
    //private bool enablePlanning = true;

    private bool pathPlanned = false;
    private float verticalStep = 2.5f;
    private float distToSurface = 2.5f;
    private float waypointMaxDist = 10f;

    private float groundOffset = 15f;

    private bool isFromTop = false;
    
    [SerializeField] GameObject planningUI, monitoringUI;
    [SerializeField] private Transform[] startingPoints;
    [SerializeField] private Transform camRig, droneRig;
    // Start is called before the first frame update
    void Start()
    {
        boxCollider = GetComponent<BoxCollider>();
        UpdateBoundsGeometry();
    }

    // Update is called once per frame
    void Update()
    {
        if (DroneManager.currentMissionState != DroneManager.MissionState.Planning)
            return;

        if (selectingSurface)
        {
            HighlightSurface();
        } else
        {
            StopHighlightingSurface();
        }

        //for debugging
        if(Input.GetKeyDown(KeyCode.P)){
            currentSelectedSurfaceIndex = 0;
            GenerateFlightTrajectory();
        }
    }

    public void StartSelectingSurface(HoverEnterEventArgs args)
    {
        if (DroneManager.currentMissionState != DroneManager.MissionState.Planning || pathPlanned)
            return;
        selectingSurface = true;
        currentRayInteractor = (XRRayInteractor)args.interactorObject;
    }

    public void EndSelectingSurface(HoverExitEventArgs args)
    {
        if (DroneManager.currentMissionState != DroneManager.MissionState.Planning || pathPlanned)
            return;
        if ((XRRayInteractor)args.interactorObject == currentRayInteractor)
        {
            selectingSurface = false;
            currentRayInteractor = null;

        }
    }

    public void SelectSurface(ActivateEventArgs args)
    {
        if (DroneManager.currentMissionState != DroneManager.MissionState.Planning || pathPlanned)
            return;
        if ((XRRayInteractor)args.interactorObject == currentRayInteractor)
        {
            for (int i = 0; i < surfaceSelected.Length; i++)
            {
                if (i == currentHoveringSurfaceIndex)
                {
                    if (currentHoveringSurfaceIndex != currentSelectedSurfaceIndex)
                    {
                        surfaceSelected[i].SetActive(true);
                        if(!planningUI.activeInHierarchy)
                            planningUI.SetActive(true);
                        currentSelectedSurfaceIndex = currentHoveringSurfaceIndex;
                    }else
                    {
                        surfaceSelected[i].SetActive(false);
                        currentSelectedSurfaceIndex = -1;
                    }

                }
                else
                {
                    surfaceSelected[i].SetActive(false);
                }
            }
        }
    }

    void HighlightSurface()
    {
        Vector3 hitPosition, hitNormal;
        currentRayInteractor.TryGetHitInfo(out hitPosition, out hitNormal, out _, out _);
        Vector3 localNormal = transform.InverseTransformDirection(hitNormal).normalized;
        if ((localNormal + Vector3.right).magnitude < 0.02f)
        {
            currentHoveringSurfaceIndex = 0;
        }
        else if ((localNormal + Vector3.forward).magnitude < 0.02f)
        {
            currentHoveringSurfaceIndex = 1;
        }
        else if ((localNormal - Vector3.right).magnitude < 0.02f)
        {
            currentHoveringSurfaceIndex = 2;
        }
        else if ((localNormal - Vector3.forward).magnitude < 0.02f)
        {
            currentHoveringSurfaceIndex = 3;
        } else
        {
            currentHoveringSurfaceIndex = -1;
        }
        for(int i = 0; i < surfaceHighlights.Length; i++)
        {
            if(i == currentHoveringSurfaceIndex)
            {
                surfaceHighlights[i].SetActive(true);
            } else
            {
                surfaceHighlights[i].SetActive(false);
            }
        }
    }

    public void StopHighlightingSurface()
    {
        for (int i = 0; i < surfaceHighlights.Length; i++)
        {
            surfaceHighlights[i].SetActive(false);
        }
    }

    public void GenerateFlightTrajectory()
    {
        if (currentSelectedSurfaceIndex < 0)
            return;

        //DroneManager.currentMissionState = DroneManager.MissionState.MovingToFlightZone;
        Vector3[] currentSurfaceVertices = new Vector3[4];
        for (int t = 0; t < 4; t++)
            currentSurfaceVertices[t] = surfaceVerts[currentSelectedSurfaceIndex, t];
        Vector3 horizontalOffset = currentSurfaceVertices[1] - currentSurfaceVertices[0];
        List<Vector3> path = new List<Vector3>();
        bool flipped = false;
        Vector3 surfaceNormal = (surfaceVerts[currentSelectedSurfaceIndex, 0] - surfaceVerts[(currentSelectedSurfaceIndex + 3) % 4, 0]).normalized;
        if(isFromTop){
            for (Vector3 v = currentSurfaceVertices[3]; v.y > groundOffset; v -= Vector3.up * verticalStep)
            {

                if (!flipped)
                {
                    path.Add(v + surfaceNormal * distToSurface);
                    path.Add(v + surfaceNormal * distToSurface + horizontalOffset);
                }
                else
                {
                    path.Add(v + surfaceNormal * distToSurface + horizontalOffset);
                    path.Add(v + surfaceNormal * distToSurface);
                }
                flipped = !flipped;
            }
        } else{
            for (Vector3 v = currentSurfaceVertices[0]; v.y < currentSurfaceVertices[3].y; v += Vector3.up * verticalStep)
            {
                if (v.y > groundOffset)
                {
                    if (!flipped)
                    {
                        path.Add(v + surfaceNormal * distToSurface);
                        path.Add(v + surfaceNormal * distToSurface + horizontalOffset);
                    }
                    else
                    {
                        path.Add(v + surfaceNormal * distToSurface + horizontalOffset);
                        path.Add(v + surfaceNormal * distToSurface);
                    }
                    flipped = !flipped;
                }
            }
            if (flipped)
            {
                path.Add(currentSurfaceVertices[2] + surfaceNormal * distToSurface);
                path.Add(currentSurfaceVertices[3] + surfaceNormal * distToSurface);
            }
            else
            {
                path.Add(currentSurfaceVertices[3] + surfaceNormal * distToSurface);
                path.Add(currentSurfaceVertices[2] + surfaceNormal * distToSurface);
            }
        }

        int i = 0;

        foreach(Transform waypoint in pathVisualization.transform){
            Destroy(waypoint.gameObject);
        }

        while (i < path.Count)
        {
            GameObject wp = Instantiate(waypoint, pathVisualization.transform) as GameObject;
            wp.transform.position = path[i];


            if (i == path.Count - 1)
            {
                break;
            }
            else
            {
                Vector3 currentTrajectoryPoint = path[i];
                Vector3 nextTrajectoryPoint = path[i + 1];
                Vector3 nextTrajectoryPointDirection = (nextTrajectoryPoint - currentTrajectoryPoint).normalized;
                while (Vector3.Distance(currentTrajectoryPoint, nextTrajectoryPoint) > waypointMaxDist)
                {
                    currentTrajectoryPoint += nextTrajectoryPointDirection * waypointMaxDist;
                    path.Insert(++i, currentTrajectoryPoint);
                    wp = Instantiate(waypoint, pathVisualization.transform) as GameObject;
                    wp.transform.position = currentTrajectoryPoint;
                }
            }
            i++;
        }

        flightTrajectory = new Vector3[path.Count];
        flightTrajectory = path.ToArray();
        pathVisualization.positionCount = flightTrajectory.Length;
        pathVisualization.SetPositions(flightTrajectory);
        for (int t = 0; t < surfaceSelected.Length; t++)
        {
            surfaceSelected[t].SetActive(false);
        }
        pathPlanned = true;
    }

    public void FinishPlanning(){
        if(pathPlanned){
            DroneManager.currentMissionState = DroneManager.MissionState.MovingToFlightZone;
            planningUI.SetActive(false);
            currentSelectedSurfaceIndex = -1;
            monitoringUI.SetActive(true);
        }
    }

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

    public bool isPathPlanned () {
        return pathPlanned;
    }

    public Vector3 GetCurrentWaypoint(int index, out bool out_of_bound){
        if(index >= flightTrajectory.Length)
        {
            out_of_bound = true;
            return Vector3.zero;
        } else {
            out_of_bound = false;
            return flightTrajectory[index];
        }
    }

    public int GetTotalWaypointCount()
    {
        return flightTrajectory.Length;
    }

    public void SetVerticalGap(Slider slider){
        verticalStep = slider.value;
    }

    public void SetHorizontalGap(Slider slider){
        waypointMaxDist = slider.value;
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

    public void SetStartingPoint(Dropdown dropdown){
        switch(dropdown.value){
            case 0:
                camRig.position = startingPoints[0].position;
                camRig.rotation = startingPoints[0].rotation;
                droneRig.position = startingPoints[0].GetChild(0).position;
                droneRig.rotation = startingPoints[0].GetChild(0).rotation;
                break;
            case 1:
                camRig.position = startingPoints[1].position;
                camRig.rotation = startingPoints[1].rotation;
                droneRig.position = startingPoints[1].GetChild(0).position;
                droneRig.rotation = startingPoints[1].GetChild(0).rotation;
                break;
            case 2:
                camRig.position = startingPoints[2].position;
                camRig.rotation = startingPoints[2].rotation;
                droneRig.position = startingPoints[2].GetChild(0).position;
                droneRig.rotation = startingPoints[2].GetChild(0).rotation;
                break;
            case 3:
                camRig.position = startingPoints[3].position;
                camRig.rotation = startingPoints[3].rotation;
                droneRig.position = startingPoints[3].GetChild(0).position;
                droneRig.rotation = startingPoints[3].GetChild(0).rotation;
                break;
            case 4:
                camRig.position = startingPoints[4].position;
                camRig.rotation = startingPoints[4].rotation;
                droneRig.position = startingPoints[4].GetChild(0).position;
                droneRig.rotation = startingPoints[4].GetChild(0).rotation;
                break;
            default:
                break;
        }
    }

    public void SetStartingPoint(int index){
        switch(index){
            case 0:
                camRig.position = startingPoints[0].position;
                camRig.rotation = startingPoints[0].rotation;
                droneRig.position = startingPoints[0].GetChild(0).position;
                droneRig.rotation = startingPoints[0].GetChild(0).rotation;
                break;
            case 1:
                camRig.position = startingPoints[1].position;
                camRig.rotation = startingPoints[1].rotation;
                droneRig.position = startingPoints[1].GetChild(0).position;
                droneRig.rotation = startingPoints[1].GetChild(0).rotation;
                break;
            case 2:
                camRig.position = startingPoints[2].position;
                camRig.rotation = startingPoints[2].rotation;
                droneRig.position = startingPoints[2].GetChild(0).position;
                droneRig.rotation = startingPoints[2].GetChild(0).rotation;
                break;
            case 3:
                camRig.position = startingPoints[3].position;
                camRig.rotation = startingPoints[3].rotation;
                droneRig.position = startingPoints[3].GetChild(0).position;
                droneRig.rotation = startingPoints[3].GetChild(0).rotation;
                break;
            case 4:
                camRig.position = startingPoints[4].position;
                camRig.rotation = startingPoints[4].rotation;
                droneRig.position = startingPoints[4].GetChild(0).position;
                droneRig.rotation = startingPoints[4].GetChild(0).rotation;
                break;
            default:
                break;
        }
    }
}
