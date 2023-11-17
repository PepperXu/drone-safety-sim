using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldUIScaler : MonoBehaviour
{
    [SerializeField] private float scaleDistRatio = 0.02f;
    [SerializeField] Transform mainCamera;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float distance = (mainCamera.position - transform.position).magnitude;
        float currentScale = scaleDistRatio * distance;
        transform.localScale = Vector3.one * currentScale;
    }
}
