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
            string filePath = EditorUtility.OpenFilePanel("log_full.csv", Application.persistentDataPath, "csv");
            if (!string.IsNullOrEmpty(filePath))
            {
                List<Vector3> currentPositions = new List<Vector3>();
                string currentStatus = "idle";
                GameObject lineSegParent = new GameObject("Line Segments");
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

                            if (currentStatus != status && currentPositions.Count > 0)
                            {
                                GameObject lineSeg = new GameObject("Line Segment");
                                lineSeg.transform.parent = lineSegParent.transform;
                                LineRenderer lineRenderer = lineSeg.AddComponent<LineRenderer>();
                                lineRenderer.positionCount = currentPositions.Count;
                                lineRenderer.SetPositions(currentPositions.ToArray());
                                lineRenderer.useWorldSpace = true;
                                lineRenderer.widthMultiplier = 0.1f;
                                lineRenderer.material = dataVisualization.defaultMaterial;
                                switch(currentStatus){
                                    case "idle":
                                        lineRenderer.startColor = dataVisualization.idleColor;
                                        lineRenderer.endColor = dataVisualization.idleColor;
                                        break;
                                    case "manual":
                                        lineRenderer.startColor = dataVisualization.manualColor;
                                        lineRenderer.endColor = dataVisualization.manualColor;
                                        break;
                                    case "auto_nav":
                                        lineRenderer.startColor = dataVisualization.autoNavColor;
                                        lineRenderer.endColor = dataVisualization.autoNavColor;
                                        break;
                                    case "auto_wait":
                                        lineRenderer.startColor = dataVisualization.autoWaitColor;
                                        lineRenderer.endColor = dataVisualization.autoWaitColor;
                                        break;
                                    case "auto_return":
                                        lineRenderer.startColor = dataVisualization.autoReturnColor;
                                        lineRenderer.endColor = dataVisualization.autoReturnColor;
                                        break;
                                    case "auto_off":
                                        lineRenderer.startColor=dataVisualization.idleColor;
                                        lineRenderer.endColor=dataVisualization.idleColor;
                                        break;
                                    default:
                                        lineRenderer.startColor = dataVisualization.errorColor; lineRenderer.endColor = dataVisualization.errorColor;break;
                                }
                                
                                currentPositions.Clear();
                            }
                            string[] coordSplit = values[1].Split("|");
                            Vector3 position = new Vector3(float.Parse(coordSplit[0]), float.Parse(coordSplit[1]), float.Parse(coordSplit[2]));
                            currentPositions.Add(position);
                            currentStatus = status;
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
