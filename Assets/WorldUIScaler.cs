using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldUIScaler : MonoBehaviour
{
    [SerializeField] private float scaleDistRatio = 0.02f;
    [SerializeField] Transform mainCamera;
    [SerializeField] bool Clamp;
    [SerializeField] float baseScale = 1f;
    // Start is called before the first frame update
    void Start()
    {
        if(!mainCamera)
            mainCamera = Camera.main.transform;
    }

    // Update is called once per frame
    void Update()
    {
        float distance = (mainCamera.position - transform.position).magnitude;
        float currentScale = scaleDistRatio * distance;
        transform.localScale = Vector3.one * (Clamp?Mathf.Max(baseScale, currentScale):currentScale);
    }
}
