using System.Collections;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;

public class CollisionSensing : MonoBehaviour
{

    float[] distances = new float[8];
    [SerializeField] LayerMask obstacleLayer;
    public bool collisionSensingEnabled = false;   
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(collisionSensingEnabled)
            RaySpray();
    }

    void RaySpray(){
        int index = 0;
        for(float angle = 22.5f; angle < 360f; angle += 45f){
            RaycastHit hit;
            if(Physics.Raycast(transform.position, Quaternion.AngleAxis(angle, Vector3.up) * transform.forward, out hit, 10f, obstacleLayer)){
                distances[index] = hit.distance;
            } else {
                distances[index] = float.MaxValue;
            }
            index++;
        }
        Communication.collisionData.distances = distances;
    }

}
