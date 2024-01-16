using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    [SerializeField] Transform mainCamera;
    public bool alignX;
    // Start is called before the first frame update
    void Start()
    {
        if(!mainCamera)
            mainCamera = Camera.main.transform;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 objectCameraDirection = transform.position - mainCamera.position;
        
        if(alignX){
            Vector3 camDirLocal = transform.InverseTransformDirection(-objectCameraDirection);
            Vector3 camDirXY = transform.TransformDirection(new Vector3(camDirLocal.x, camDirLocal.y, 0f)).normalized;
            transform.rotation = Quaternion.LookRotation(transform.forward, Quaternion.AngleAxis(-90f, transform.forward) * camDirXY);
        }
        else{
            Vector3 objectCameraDirectionXZ = new Vector3(objectCameraDirection.x, 0, objectCameraDirection.z);
            transform.LookAt(transform.position + objectCameraDirectionXZ.normalized);
        }
        
    }
}
