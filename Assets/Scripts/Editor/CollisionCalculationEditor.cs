using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(CollisionCalculation))]
public class CollisionCalculationEditor : Editor
{
    const int steps = 64;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CollisionCalculation collisionCalculation = (CollisionCalculation)target;

        if (GUILayout.Button("Load Coordinates from CSV"))
        {
            
            string rootDir = EditorUtility.OpenFolderPanel("", "", "");
            if (!string.IsNullOrEmpty(rootDir))
            {
                string[] files = Directory.GetFiles(rootDir, "log_full.csv", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    if (file.Contains("config_1") || file.Contains("config_2") || file.Contains("config_3"))
                    {
                        int currentConfig = 0;
                        if (file.Contains("config_1"))
                        {
                            currentConfig = 0;
                        } else if (file.Contains("config_2"))
                        {
                            currentConfig = 1;
                        } else if (file.Contains("config_3"))
                        {
                            currentConfig = 2;
                        }
                            
                        for (int i = 0; i < collisionCalculation.configs.Length; i++)
                        {
                            collisionCalculation.configs[i].SetActive(i == currentConfig);
                        }
                        try
                        {
                            float totalCollisionDistance = 0f;
                            int sampleCount = 0;
                            using (StreamReader reader = new StreamReader(file))
                            {
                                reader.ReadLine();
                                string line;

                                //bool enteredZone = false, exitedZone = false;
                                while ((line = reader.ReadLine()) != null)
                                {
                                    string[] values = line.Split(',');
                                    string[] coordSplit = values[1].Split("|");
                                    Vector3 position = new Vector3(float.Parse(coordSplit[0]), float.Parse(coordSplit[1]), float.Parse(coordSplit[2]));
                                    //string droneStatus = values[4];
                                    string missionStatus = values[5];
                                    string collisionStatus = values[6];

                                    if (missionStatus != "taking off" && missionStatus != "landing")
                                    {
                                        if (collisionStatus != "Safe")
                                        {
                                            float hitDistance = RaySpray(position, collisionCalculation.obstacleLayer).magnitude;
                                            if (hitDistance < 6f)
                                            {
                                                totalCollisionDistance += hitDistance;
                                                sampleCount += 1;
                                            }
                                        }
                                    }
                                }
                            }
                            Debug.Log(Path.GetRelativePath(rootDir, file) + ": " + totalCollisionDistance / sampleCount + ". sample count: " + sampleCount);
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning("Error reading CSV file: " + e.Message);
                        }
                    }
                }
            }
        }
    }

    Vector3 RaySpray(Vector3 origin, LayerMask obstacleLayer)
    {
        //int index = 0;
        //Vector3[] distances = new Vector3[16];
        Vector3 shortestDist = Vector3.positiveInfinity;
        
        for (float angle = 360f / (2 * steps); angle < 360f; angle += (360f / steps))
        {
            RaycastHit hit;
            if (Physics.Raycast(origin, Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward, out hit, 10f, obstacleLayer))
            {
                
                float hitDistance = hit.distance;
                //Debug.Log(hitDistance);
                if (hitDistance < shortestDist.magnitude)
                {
                    shortestDist = hitDistance * (hit.point - origin).normalized;
                }
            }
            //index++;
        }
        return shortestDist;
    }
}
