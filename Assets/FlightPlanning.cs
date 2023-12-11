using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(BoxCollider))]
public class FlightPlanning : MonoBehaviour
{
    private BoxCollider boxCollider;
    private Vector3[] flightTrajectory;
    private Vector3 boundCenter, boundExtends;
    private int currentHoveringSurfaceIndex = -1, currentSelectedSurfaceIndex = -1;
    private Vector3[,] surfaceVerts = new Vector3[4,4];
    [SerializeField] GameObject[] surfaceHighlights;
    [SerializeField] GameObject[] surfaceSelected;
    private XRRayInteractor currentRayInteractor;
    private bool selectingSurface = false;
    private bool enablePlanning = true;
    private float verticalStep = 2.5f;

    // Start is called before the first frame update
    void Start()
    {
        UpdateBoundsGeometry();
    }

    // Update is called once per frame
    void Update()
    {
        if (selectingSurface)
        {
            HighlightSurface();
        }
    }

    public void StartSelectingSurface(HoverEnterEventArgs args)
    {
        selectingSurface = true;
        currentRayInteractor = (XRRayInteractor)args.interactorObject;
    }

    public void EndSelectingSurface(HoverExitEventArgs args)
    {
        if((XRRayInteractor)args.interactorObject == currentRayInteractor)
        {
            selectingSurface = false;
            currentRayInteractor = null;

        }
    }

    public void SelectSurface(SelectEnterEventArgs args)
    {
        if ((XRRayInteractor)args.interactorObject == currentRayInteractor)
        {
            for (int i = 0; i < surfaceSelected.Length; i++)
            {
                if (i == currentHoveringSurfaceIndex)
                {
                    if (currentHoveringSurfaceIndex != currentSelectedSurfaceIndex)
                    {
                        surfaceSelected[i].SetActive(true);
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

    public void GenerateFlightTrajectory()
    {
        enablePlanning = false;
        Vector3[] currectSurfaceVertices = new Vector3[4];
        for(int i = 0; i < 4; i++)
            currectSurfaceVertices[i] = surfaceVerts[currentSelectedSurfaceIndex, i];
        List<Vector3> path = new List<Vector3>();
        

    }

    private void UpdateBoundsGeometry()
    {
        boundCenter = boxCollider.bounds.center;
        boundExtends = boxCollider.bounds.extents;
        surfaceVerts[0,0] = boundCenter + new Vector3(-boundExtends.x, -boundExtends.y, boundExtends.z);
        surfaceVerts[0,1] = boundCenter + new Vector3(-boundExtends.x, -boundExtends.y, -boundExtends.z);
        surfaceVerts[0,2] = boundCenter + new Vector3(-boundExtends.x, boundExtends.y, -boundExtends.z);
        surfaceVerts[0,3] = boundCenter + new Vector3(-boundExtends.x, boundExtends.y, boundExtends.z);

        surfaceVerts[1,0] = boundCenter + new Vector3(-boundExtends.x, -boundExtends.y, -boundExtends.z);
        surfaceVerts[1,1] = boundCenter + new Vector3(boundExtends.x, -boundExtends.y, -boundExtends.z);
        surfaceVerts[1,2] = boundCenter + new Vector3(boundExtends.x, boundExtends.y, -boundExtends.z);
        surfaceVerts[1,3] = boundCenter + new Vector3(-boundExtends.x, boundExtends.y, -boundExtends.z);

        surfaceVerts[2,0] = boundCenter + new Vector3(boundExtends.x, -boundExtends.y, -boundExtends.z);
        surfaceVerts[2,1] = boundCenter + new Vector3(boundExtends.x, -boundExtends.y, boundExtends.z);
        surfaceVerts[2,2] = boundCenter + new Vector3(boundExtends.x, boundExtends.y, boundExtends.z);
        surfaceVerts[2,3] = boundCenter + new Vector3(boundExtends.x, boundExtends.y, -boundExtends.z);
                      
        surfaceVerts[3,0] = boundCenter + new Vector3(boundExtends.x, -boundExtends.y, boundExtends.z);
        surfaceVerts[3,1] = boundCenter + new Vector3(-boundExtends.x, -boundExtends.y, boundExtends.z);
        surfaceVerts[3,2] = boundCenter + new Vector3(-boundExtends.x, boundExtends.y, boundExtends.z);
        surfaceVerts[3,3] = boundCenter + new Vector3(boundExtends.x, boundExtends.y, boundExtends.z);
    }
}
