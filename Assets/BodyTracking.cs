using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyTracking : MonoBehaviour
{
    [SerializeField] Transform vrCamera;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = vrCamera.position;
        transform.eulerAngles = vrCamera.eulerAngles.y * Vector3.up;
    }
}
