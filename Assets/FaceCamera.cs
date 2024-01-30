using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    [SerializeField] Transform mainCamera;
    public bool fixZ_alignX, fixX_alighZ;
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
        
        if(fixZ_alignX){
            Vector3 camDirLocal = transform.InverseTransformDirection(-objectCameraDirection);
            Vector3 camDirXY = transform.TransformDirection(new Vector3(camDirLocal.x, camDirLocal.y, 0f)).normalized;
            transform.rotation = Quaternion.LookRotation(transform.forward, Quaternion.AngleAxis(-90f, transform.forward) * camDirXY);
        } else if(fixX_alighZ){
            Vector3 camDirLocal = transform.InverseTransformDirection(-objectCameraDirection);
            Vector3 camDirLocalYZ = new Vector3(0, camDirLocal.y, camDirLocal.z);
            Vector3 camDirYZ = transform.TransformDirection(camDirLocalYZ);
            //float angleOffset = Vector3.SignedAngle(Vector3.forward, camDirLocalYZ.normalized, Vector3.right);
            //transform.rotation = Quaternion.AngleAxis(angleOffset, transform.right);
            transform.rotation = Quaternion.LookRotation(camDirYZ, Vector3.Cross(camDirYZ, transform.right));
        }
        else{
            Vector3 objectCameraDirectionXZ = new Vector3(objectCameraDirection.x, 0, objectCameraDirection.z);
            transform.LookAt(transform.position + objectCameraDirectionXZ.normalized);
        }
        
    }
}
