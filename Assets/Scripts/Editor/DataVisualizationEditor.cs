using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(DataVisualization))]
public class DataVisualizationEditor : Editor
{
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DataVisualization dataVisualization = (DataVisualization)target;
    

        if (GUILayout.Button("Load Coordinates from CSV"))
        {

            List<Waypoint> waypoints = dataVisualization.flightPlanning.VisualizeFlightPlanEditor(dataVisualization.surfaceIndex);
            string filePath = EditorUtility.OpenFilePanel("log_full.csv", Application.persistentDataPath, "csv");
            if (!string.IsNullOrEmpty(filePath))
            {
                List<Vector3> currentPathSegment = new List<Vector3>();
                string currentStatus = "manual";
                string currentCollisionStatus = "Safe";
                GameObject lineSegParent = new GameObject("Line Segments");
                List<Vector3> inspectionPath = new List<Vector3>();
                Vector3[] waypointVectors = new Vector3[dataVisualization.lastWaypointIndex + 1];
                for (int i = 0; i < dataVisualization.lastWaypointIndex + 1; i++)
                {
                    waypointVectors[i] = Vector3.positiveInfinity;
                }
                try
                {
                    using (StreamReader reader = new StreamReader(filePath))
                    {
                        reader.ReadLine();
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            string[] values = line.Split(',');
                            string status = values[4];
                            string statusCollision = values[6];
                            if(status == "auto_nav" || status == "auto_wait")
                            {
                                status = "auto";
                            } else if(status == "auto_return")
                            {
                                status = "return";
                            } else
                            {
                                status = "manual";
                            }
                            if(statusCollision != "Warning")
                            {
                                statusCollision = "Safe";
                            }

                            if ((currentStatus != status || currentCollisionStatus != statusCollision) && currentPathSegment.Count > 0)
                            {
                                GameObject lineSeg = new GameObject("Line Segment");
                                lineSeg.transform.parent = lineSegParent.transform;
                                LineRenderer lineRenderer = lineSeg.AddComponent<LineRenderer>();
                                lineRenderer.positionCount = currentPathSegment.Count;
                                lineRenderer.SetPositions(currentPathSegment.ToArray());
                                lineRenderer.useWorldSpace = true;
                                lineRenderer.widthMultiplier = 0.1f;
                                lineRenderer.material = dataVisualization.defaultMaterial;
                                if (currentCollisionStatus == "Warning")
                                {
                                    lineRenderer.startColor = dataVisualization.nearColColor;
                                    lineRenderer.endColor = dataVisualization.nearColColor;
                                }
                                else
                                {
                                    switch (currentStatus)
                                    {
                                        case "manual":
                                            lineRenderer.startColor = dataVisualization.manualColor;
                                            lineRenderer.endColor = dataVisualization.manualColor;
                                            break;
                                        case "auto":
                                            lineRenderer.startColor = dataVisualization.autoColor;
                                            lineRenderer.endColor = dataVisualization.autoColor;
                                            break;
                                        case "return":
                                            lineRenderer.startColor = dataVisualization.autoReturnColor;
                                            lineRenderer.endColor = dataVisualization.autoReturnColor;
                                            break;
                                        default:
                                            lineRenderer.startColor = dataVisualization.errorColor; lineRenderer.endColor = dataVisualization.errorColor; break;
                                    }
                                }

                                currentPathSegment.Clear();
                            }
                            string[] coordSplit = values[1].Split("|");
                            Vector3 position = new Vector3(float.Parse(coordSplit[0]), float.Parse(coordSplit[1]), float.Parse(coordSplit[2]));
                            currentPathSegment.Add(position);
                            currentStatus = status;
                            currentCollisionStatus = statusCollision;

                            int closestWpIndex = int.Parse(values[3]);
                            if (closestWpIndex >= 0)
                            {
                                inspectionPath.Add(position);
                                if (Vector3.Distance(position, waypoints[closestWpIndex].transform.position) < waypointVectors[closestWpIndex].magnitude)
                                {
                                    waypointVectors[closestWpIndex] = position - waypoints[closestWpIndex].transform.position;
                                }
                            }
                        }
                    }

                    for (int i = 0; i < dataVisualization.lastWaypointIndex + 1; i++)
                    {
                        if (waypointVectors[i].magnitude > 9999f)
                        {
                            Vector3 wpPosition = waypoints[i].transform.position;
                            foreach (Vector3 sample in inspectionPath)
                            {
                                if (Vector3.Distance(sample, wpPosition) < waypointVectors[i].magnitude)
                                    waypointVectors[i] = sample - wpPosition;
                            }
                        }
                    }
                    for (int j = 0; j < dataVisualization.lastWaypointIndex + 1; j++)
                    {
                        if (waypointVectors[j].magnitude < 9999f)
                        {
                            GameObject deviation = new GameObject("Deviation");
                            deviation.transform.parent = lineSegParent.transform;
                            LineRenderer lineRenderer = deviation.AddComponent<LineRenderer>();
                            lineRenderer.positionCount = 2;
                            lineRenderer.SetPosition(0, waypoints[j].transform.position);
                            lineRenderer.SetPosition(1, waypoints[j].transform.position + waypointVectors[j]);
                            lineRenderer.useWorldSpace = true;
                            lineRenderer.widthMultiplier = 0.2f;
                            lineRenderer.material = dataVisualization.defaultMaterial;
                            lineRenderer.startColor = dataVisualization.deviationColor;
                            lineRenderer.endColor = dataVisualization.deviationColor;
                        }
                    }

                    // Normalize timestamps to start from zero

                }
                catch (System.Exception e)
                {
                    Debug.LogError("Error reading CSV file: " + e.Message);
                }
            }
        }

    }
}
