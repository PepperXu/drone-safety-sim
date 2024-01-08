using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    [SerializeField] Transform mainCamera;
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
        objectCameraDirection = new Vector3(objectCameraDirection.x, 0, objectCameraDirection.z);
        transform.LookAt(transform.position + objectCameraDirection.normalized);
        
    }
}
