using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WaypointNext : MonoBehaviour
{
    [SerializeField] LayerMask groundLayer;
    [SerializeField] Transform projection, projectionDisc;
    [SerializeField] TextMeshPro progressText;
    // Start is called before the first frame update
    void OnEnable()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, float.MaxValue, groundLayer))
        {
            Vector3 hitPoint = hit.point;

            projection.localPosition = transform.InverseTransformPoint(hitPoint) / 2f;
            projection.localScale = new Vector3(projection.localScale.x, projection.localPosition.z * 2f, projection.localScale.z);
            projectionDisc.position = hitPoint + Vector3.up * 0.01f;
        }

        if (Communication.waypoints != null)
        {
            progressText.text = (int)((float)Communication.currentWaypointIndex / Communication.waypoints.Length * 100f) + "%";
        }
    }
}
