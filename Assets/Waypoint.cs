using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    public enum WaypointState{
        Hidden,
        Neutral,
        Next,
        NextNext
    }

    public WaypointState currentWaypointState = WaypointState.Neutral;
    [SerializeField] GameObject wp_neutral, wp_next, wp_next_next;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        switch(currentWaypointState){
            case WaypointState.Hidden:
                wp_neutral.SetActive(false);
                wp_next.SetActive(false);
                wp_next_next.SetActive(false);
                break;
            case WaypointState.Neutral:
                wp_neutral.SetActive(true);
                wp_next.SetActive(false);
                wp_next_next.SetActive(false);
                break;
            case WaypointState.Next:
                wp_neutral.SetActive(false);
                wp_next.SetActive(true);
                wp_next_next.SetActive(false);
                break;
            case WaypointState.NextNext:
                wp_neutral.SetActive(false);
                wp_next.SetActive(false);
                wp_next_next.SetActive(true);
                break;
            default:
                break;
        }
    }
}
