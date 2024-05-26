using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConfigManager : MonoBehaviour
{
    // Start is called before the first frame update

    public GameObject[] activeEventTriggers;
    public FlightPlanning flightPlanning;
    public int surfaceIndex;
    public int totalDefectCount;
    public List<GameObject> markedDefect = new List<GameObject>();
    private void OnEnable()
    {
        foreach(GameObject obj in activeEventTriggers)
        {
            obj.SetActive(true);
        }
        markedDefect.Clear();   
    }

    public void TryAddMarkedDefect(GameObject obj)
    {
        if(!markedDefect.Contains(obj))
            markedDefect.Add(obj);
    }
}
