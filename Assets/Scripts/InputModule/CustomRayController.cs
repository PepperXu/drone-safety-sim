using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.XR;

public class CustomRayController : MonoBehaviour
{
    [SerializeField] private LayerMask fpv_cam_layer;
    [SerializeField] private LayerMask ui_gaze_layer;
    [SerializeField] InputDeviceCharacteristics deviceCharacteristics;
    private InputDevice controller;
    private float triggerValue;
    private bool triggered = false;

    [SerializeField] bool headGaze = false;

    [SerializeField] bool mouseDebugging = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(mouseDebugging){
            RaycastHit hit;
            Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            if(Physics.Raycast(mouseRay, out hit, float.PositiveInfinity, fpv_cam_layer)){
                if(Input.GetMouseButtonDown(0)){
                    //Debug.Log(hit.transform.gameObject.name);
                    hit.transform.GetComponent<InteractiveCamera>().MarkDefectFromCamera(hit);
                }
            }
        } else if(headGaze){
            RaycastHit hit;
            Ray ray1 = new Ray(transform.position, Quaternion.AngleAxis(20f, transform.right) * transform.forward);
            Ray ray2 = new Ray(transform.position, transform.forward);

            if (Physics.Raycast(ray1, out hit, float.PositiveInfinity, ui_gaze_layer) || Physics.Raycast(ray2, out hit, float.PositiveInfinity, ui_gaze_layer))
            {
                VisType.SetGazed(true);
            } else
            {
                VisType.SetGazed(false);
            }
        }else{

            if(!controller.isValid){
                List<InputDevice> devices = new List<InputDevice>();
                InputDevices.GetDevicesWithCharacteristics(deviceCharacteristics, devices);
                if (devices.Count > 0)
                {
                    controller = devices[0];
                }
            }
            controller.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue);
            if(triggerValue < 0.2f){
                triggered = false;
            }

            RaycastHit hit;
            Ray ray = new Ray(transform.position, transform.forward);

            if(Physics.Raycast(ray, out hit, float.PositiveInfinity, fpv_cam_layer)){
                if(controller.isValid){
                    if(triggerValue > 0.8f && !triggered){
                        triggered = true;
                        hit.transform.GetComponent<InteractiveCamera>().MarkDefectFromCamera(hit);
                    }
                }
            }
        }
    }

    public InputDevice GetController()
    {
        return controller;
    }
}
