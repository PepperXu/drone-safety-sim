using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConfigManager : MonoBehaviour
{
    // Start is called before the first frame update

    public GameObject[] activeEventTriggers;
    private void OnEnable()
    {
        foreach(GameObject obj in activeEventTriggers)
        {
            obj.SetActive(true);
        }
    }
}