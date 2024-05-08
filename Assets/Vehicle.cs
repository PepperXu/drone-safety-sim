using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vehicle : MonoBehaviour
{
    private Transform currentLane;
    private const float speed = 20f, spinningAngularSpeed = 5000f;
    private int currentWaypointIndex = 0;
    private List<Transform> wheels = new List<Transform>();

    // Start is called before the first frame update
    void Start()
    {
        foreach(Transform t in transform)
        {
            if (t.gameObject.name.Contains("Wheel"))
            {
                wheels.Add(t);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(currentWaypointIndex < currentLane.childCount)
        {
            float distanceBetweenNowAndWp = Vector3.Distance(transform.position, currentLane.GetChild(currentWaypointIndex).position);
            if (distanceBetweenNowAndWp < 0.2f)
            {
                currentWaypointIndex++;
            } else
            {
                float anglesBetweenNowAndWP = Vector3.SignedAngle(transform.forward, currentLane.GetChild(currentWaypointIndex).position - transform.position, Vector3.up);
                float angularTurnSpeed = anglesBetweenNowAndWP * (speed/5f);
                transform.eulerAngles += transform.up * angularTurnSpeed * Time.deltaTime;
                transform.position += transform.forward * speed * Time.deltaTime;
                
            }
        } else
        {
            Destroy(gameObject);
        }
        foreach(Transform t in wheels)
        {
            t.transform.Rotate(Vector3.right, spinningAngularSpeed * Time.deltaTime);
        }
    }

    public void SetLane(Transform lane)
    {
        currentLane = lane; 
    }
}
