using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficSimulation : MonoBehaviour
{

    [SerializeField] Transform[] spawnLanes;
    [SerializeField] GameObject[] vehiclePrefabs;
    private int currentSpawnLane = 0;
    private const float spawnInterval = 3f;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(SpawnVehicles());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator SpawnVehicles()
    {
        while (true)
        {
            int randomIndex = Random.Range(0, vehiclePrefabs.Length);
            GameObject go = Instantiate(vehiclePrefabs[randomIndex]);
            go.transform.position = spawnLanes[currentSpawnLane].position;
            go.transform.eulerAngles = spawnLanes[currentSpawnLane].eulerAngles;
            go.transform.localScale *= 1.3f;
            go.GetComponent<Vehicle>().SetLane(spawnLanes[currentSpawnLane]);
            currentSpawnLane++;
            if(currentSpawnLane >= spawnLanes.Length)
            {
                currentSpawnLane = 0;
            }
            yield return new WaitForSeconds(spawnInterval);
        }
    }
}
