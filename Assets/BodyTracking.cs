using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyTracking : MonoBehaviour
{
    [SerializeField] Transform vrCamera;
    float rotationThreshold = 20f;
    // Start is called before the first frame update

    void OnEnable()
    {
        DroneManager.resetAllEvent.AddListener(ResetBodyOrientation);
    }

    void OnDisable()
    {
        DroneManager.resetAllEvent.RemoveListener(ResetBodyOrientation);
    }
    void ResetBodyOrientation()
    {
        transform.position = vrCamera.position;
        transform.eulerAngles = vrCamera.eulerAngles.y * Vector3.up;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = vrCamera.position;
        if(Mathf.Abs(transform.eulerAngles.y - vrCamera.eulerAngles.y) > rotationThreshold)
            transform.eulerAngles = vrCamera.eulerAngles.y * Vector3.up;
    }
}
