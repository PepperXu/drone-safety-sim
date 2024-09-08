
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using System;
using System.Globalization;

[CustomEditor(typeof(CollisionCalculation))]
public class CollisionCalculationEditor : Editor
{
    const int steps = 64;
    int[][] unsafeWaypointsForConfig = new int[3][];

    Dictionary<string, Dictionary<int, PData>> participantData = new Dictionary<string, Dictionary<int, PData>>();
    struct PData
    {
        public DateTime time;
        public float data;
    }

    string outputFileName;

    private void OnEnable()
    {
        unsafeWaypointsForConfig[0] = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 35, 36, 37, 38, 39 };
        unsafeWaypointsForConfig[1] = new int[] { 0, 1, 2, 3, 4, 5, 27, 28, 29 };
        unsafeWaypointsForConfig[2] = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 50, 51, 52, 53, 54 };
    }


    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CollisionCalculation collisionCalculation = (CollisionCalculation)target;

        if (GUILayout.Button("Load Coordinates from CSV"))
        {
            participantData.Clear();
            outputFileName = Enum.GetName(typeof(CollisionCalculation.CalculationMode), collisionCalculation.currentCalculationMode);


            string rootDir = EditorUtility.OpenFolderPanel("", "", "");
            if (!string.IsNullOrEmpty(rootDir))
            {
                string[] files = Directory.GetFiles(rootDir, collisionCalculation.fileName, SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    if (Path.GetRelativePath(rootDir, file).Split('\\').Length > 3)
                        continue;
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
                            switch (collisionCalculation.currentCalculationMode)
                            {
                                case CollisionCalculation.CalculationMode.AvgDist:
                                    CalculateAvgCollisionDistance(6f, file, rootDir, currentConfig, collisionCalculation);
                                    break;
                                case CollisionCalculation.CalculationMode.NearColDuration:
                                    CalculateNearColDuration(1f, file, rootDir, currentConfig, collisionCalculation);
                                    break;
                                case CollisionCalculation.CalculationMode.WarningDuration:
                                    CalculateWarningDuration(file, rootDir, currentConfig, collisionCalculation);
                                    break;
                                case CollisionCalculation.CalculationMode.EffectiveDuration:
                                    CalculateOptimalDuration(6f, 8f, file, rootDir, currentConfig, collisionCalculation);
                                    break;
                                case CollisionCalculation.CalculationMode.TotalDuration:
                                    CalculateTotalMissionDuration(file, rootDir, currentConfig);
                                    break;
                                case CollisionCalculation.CalculationMode.AvgDeviationNoCol:
                                    CalculateAvgDeviationNoCollisionLayers(currentConfig, currentConfig == 1 ? 0:1, file, rootDir, currentConfig, collisionCalculation);
                                    break;
                                case CollisionCalculation.CalculationMode.WaypointCoverage:
                                    CalculatingWaypointCoverage(currentConfig, currentConfig == 1 ? 0 : 1, file, rootDir, currentConfig, collisionCalculation);
                                    break;
                                case CollisionCalculation.CalculationMode.AutopilotPercentage:
                                    CalculateAutoFlightPercentage(file, rootDir, currentConfig);
                                    break;
                                default:
                                    break;
                            }
                            

                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning("Error reading CSV file: " + e.Message + ", Error Path: " + file) ;
                        }
                    }
                }

                WriteToCSV(rootDir, collisionCalculation);
            }
        }
    }

    void CalculateAvgCollisionDistance(float threshold, string file, string rootDir, int configID, CollisionCalculation colCal)
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
                        float hitDistance = RaySpray(position, colCal.obstacleLayer).magnitude;
                        if (hitDistance < threshold)
                        {
                            totalCollisionDistance += hitDistance;
                            sampleCount += 1;
                        }
                    }
                }
            }
        }


        RecordInDictionary(rootDir, file, configID, totalCollisionDistance / sampleCount);
        
        Debug.Log(Path.GetRelativePath(rootDir, file) + ": " + sampleCount);
        //Debug.Log(Path.GetRelativePath(rootDir, file) + ": " + totalCollisionDistance / sampleCount + ". sample count: " + sampleCount);
    }

    void CalculateNearColDuration(float threshold, string file, string rootDir, int configID, CollisionCalculation colCal)
    {
        float duration = 0f;
        float lastTimestamp = 0f;
        using (StreamReader reader = new StreamReader(file))
        {
            reader.ReadLine();
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                string[] values = line.Split(',');
                string[] coordSplit = values[1].Split("|");
                Vector3 position = new Vector3(float.Parse(coordSplit[0]), float.Parse(coordSplit[1]), float.Parse(coordSplit[2]));
                float timestamp = float.Parse(values[0]);
                string missionStatus = values[5];
                string collisionStatus = values[6];
                if (missionStatus != "taking off" && missionStatus != "landing")
                {
                    if (collisionStatus != "Safe")
                    {
                        float hitDistance = RaySpray(position, colCal.obstacleLayer).magnitude;
                        if (hitDistance < threshold)
                        {
                            duration += timestamp - lastTimestamp;
                        }
                    }
                }
                lastTimestamp = timestamp;
            }
        }

        RecordInDictionary(rootDir, file, configID, duration);
        Debug.Log(Path.GetRelativePath(rootDir, file) + ": " + duration);
    }

    void CalculateWarningDuration(string file, string rootDir, int configID, CollisionCalculation colCal)
    {
        float duration = 0f;
        float lastTimestamp = 0f;
        using (StreamReader reader = new StreamReader(file))
        {
            reader.ReadLine();
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                string[] values = line.Split(',');
                string[] coordSplit = values[1].Split("|");
                Vector3 position = new Vector3(float.Parse(coordSplit[0]), float.Parse(coordSplit[1]), float.Parse(coordSplit[2]));
                float timestamp = float.Parse(values[0]);
                string missionStatus = values[5];
                string collisionStatus = values[6];
                if (missionStatus != "taking off" && missionStatus != "landing")
                {
                    if (collisionStatus != "Safe")
                    {
                        duration += timestamp - lastTimestamp;
                    }
                }
                lastTimestamp = timestamp;
            }
        }

        RecordInDictionary(rootDir, file, configID, duration);
        Debug.Log(Path.GetRelativePath(rootDir, file) + ": " + duration);
    }

    void CalculateOptimalDuration(float lowerThreshold, float upperThreshold, string file, string rootDir, int configID, CollisionCalculation colCal)
    {
        float duration = 0f;
        float lastTimestamp = 0f;
        using (StreamReader reader = new StreamReader(file))
        {
            reader.ReadLine();
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                string[] values = line.Split(',');
                string[] coordSplit = values[1].Split("|");
                Vector3 position = new Vector3(float.Parse(coordSplit[0]), float.Parse(coordSplit[1]), float.Parse(coordSplit[2]));
                float timestamp = float.Parse(values[0]);
                string missionStatus = values[5];
                string collisionStatus = values[6];
                if (missionStatus != "taking off" && missionStatus != "landing")
                {
                    if (collisionStatus == "Safe")
                    {
                        float hitDistance = RaySpray(position, colCal.obstacleLayer).magnitude;
                        if (hitDistance < upperThreshold && hitDistance > lowerThreshold)
                        {
                            duration += timestamp - lastTimestamp;
                        }
                    }
                }
                lastTimestamp = timestamp;
            }
        }
        RecordInDictionary(rootDir, file, configID, duration);
        Debug.Log(Path.GetRelativePath(rootDir, file) + ": " + duration);
    }

    void CalculateTotalMissionDuration(string file, string rootDir, int configID)
    {
        float startTime = 0f, endTime = 0f;
        bool initialZoneEntry = false, inZone = false;
        using (StreamReader reader = new StreamReader(file))
        {
            reader.ReadLine();
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                string[] values = line.Split(',');

                //string[] coordSplit = values[1].Split("|");
                //Vector3 position = new Vector3(float.Parse(coordSplit[0]), float.Parse(coordSplit[1]), float.Parse(coordSplit[2]));
                float timestamp = float.Parse(values[0]);
                int wpIndex = int.Parse(values[3]);
                if(wpIndex >= 0)
                {
                    if (!initialZoneEntry)
                    {
                        startTime = timestamp;
                        initialZoneEntry = true;
                    }
                    inZone = true;
                } else
                {
                    if (inZone)
                    {
                        endTime = timestamp;
                        inZone = false;
                    }
                }
            }
        }
        RecordInDictionary(rootDir, file, configID, (endTime - startTime));
        Debug.Log(Path.GetRelativePath(rootDir, file) + ": " + (endTime-startTime));
    }

    void CalculateAutoFlightPercentage(string file, string rootDir, int configID)
    {
        float totalDuration = 0f;
        float autoDuration = 0f;
        float lastTimestamp = 0f;

        using (StreamReader reader = new StreamReader(file))
        {
            reader.ReadLine();
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                string[] values = line.Split(',');
                //string[] coordSplit = values[1].Split("|");
                //Vector3 position = new Vector3(float.Parse(coordSplit[0]), float.Parse(coordSplit[1]), float.Parse(coordSplit[2]));
                float timestamp = float.Parse(values[0]);
                int wpIndex = int.Parse(values[3]);
                string controlStatus = values[4];
                string missionStatus = values[5];
                string collisionStatus = values[6];
                int gpsStatus = int.Parse(values[8]);
                if (gpsStatus >= 2 && collisionStatus == "Safe" && wpIndex >= 0)
                {
                    if (controlStatus.Contains("auto"))
                    {

                        autoDuration += timestamp - lastTimestamp;

                    }
                    totalDuration += timestamp - lastTimestamp;
                }
                lastTimestamp = timestamp;
            }
        }
        RecordInDictionary(rootDir, file, configID, autoDuration/totalDuration);
    }

    void CalculateAvgDeviationNoCollisionLayers(int configIndex, int surfaceIndex, string file, string rootDir, int configID, CollisionCalculation colCal)
    {

        List<Vector3> waypoints = colCal.flightPlanning.GenerateFlightPlanEditor(surfaceIndex);
        float[] waypointDeviations = new float[waypoints.Count];

        for (int i = 0; i < waypoints.Count; i++)
        {
            waypointDeviations[i] = float.PositiveInfinity;
        }
        using (StreamReader reader = new StreamReader(file))
        {
            reader.ReadLine();
            string line;
            

            while ((line = reader.ReadLine()) != null)
            {
                string[] values = line.Split(',');

                string[] coordSplit = values[1].Split("|");
                Vector3 position = new Vector3(float.Parse(coordSplit[0]), float.Parse(coordSplit[1]), float.Parse(coordSplit[2]));
                //float timestamp = float.Parse(values[0]);
                //int wpIndex = int.Parse(values[3]);
                for (int i = 0; i < waypoints.Count; i++)
                {
                    if (!unsafeWaypointsForConfig[configIndex].Contains(i))
                    {
                        float curDeviation = (position - waypoints[i]).magnitude;
                        if (curDeviation < waypointDeviations[i])
                        {
                            waypointDeviations[i] = curDeviation;
                        }
                    }
                }
                //if (wpIndex >= 0)
                //{
                //    for (int i = math.max(0, wpIndex - 10); i < math.min(waypoints.Count, wpIndex + 11); i++){
                //        if (!unsafeWaypointsForConfig[configIndex].Contains(i))
                //        {
                //            float curDeviation = (position - waypoints[i]).magnitude;
                //            if (curDeviation < waypointDeviations[i])
                //            {
                //                waypointDeviations[i] = curDeviation;
                //            }
                //        }
                //    }
                //} else
                //{
                //    for(int i = 0; i < waypoints.Count; i++)
                //    {
                //        if (!unsafeWaypointsForConfig[configIndex].Contains(i))
                //        {
                //            float curDeviation = (position - waypoints[i]).magnitude;
                //            if (curDeviation < waypointDeviations[i])
                //            {
                //                waypointDeviations[i] = curDeviation;
                //            }
                //        }
                //    }
                //}
            }
            
        }
        int safeWpCount = 0;
        float totalDevi = 0f;
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (!unsafeWaypointsForConfig[configIndex].Contains(i))
            {
                totalDevi += waypointDeviations[i];
                safeWpCount++;
            }
        }
        RecordInDictionary(rootDir, file, configID, totalDevi / safeWpCount);
        Debug.Log(Path.GetRelativePath(rootDir, file) + ": " + totalDevi/safeWpCount);
        
    }

    void CalculatingWaypointCoverage(int configIndex, int surfaceIndex, string file, string rootDir, int configID, CollisionCalculation colCal)
    {
        List<Vector3> waypoints = colCal.flightPlanning.GenerateFlightPlanEditor(surfaceIndex);
        List<int> waypointCoverageList = new List<int>();
        using (StreamReader reader = new StreamReader(file))
        {
            reader.ReadLine();
            string line;


            while ((line = reader.ReadLine()) != null)
            {
                string[] values = line.Split(',');

                string[] coordSplit = values[1].Split("|");
                Vector3 position = new Vector3(float.Parse(coordSplit[0]), float.Parse(coordSplit[1]), float.Parse(coordSplit[2]));
                //float timestamp = float.Parse(values[0]);
                int wpIndex = int.Parse(values[3]);
                if (wpIndex != -1) {
                    if ((waypoints[wpIndex] - position).magnitude < 1f)
                    {
                        if (!waypointCoverageList.Contains(wpIndex))
                        {
                            waypointCoverageList.Add(wpIndex);
                        }
                    }
                }
            }


        }
        RecordInDictionary(rootDir, file, configID, (float)waypointCoverageList.Count/waypoints.Count);
        Debug.Log(Path.GetRelativePath(rootDir, file) + ": " + string.Join(",", waypointCoverageList.ToArray()));
    }

    void RecordInDictionary(string rootDir, string file, int configID, float data)
    {
        string[] splitedPath = Path.GetRelativePath(rootDir, file).Split('\\');
        string pid = splitedPath[0];

        string dateTimeString = splitedPath[1].Substring(splitedPath[1].Length - 8);
        DateTime dateTime = DateTime.ParseExact(dateTimeString, "HH_mm_ss", CultureInfo.InvariantCulture);
        

        if (!participantData.ContainsKey(pid))
        {
            Dictionary<int, PData> configData = new Dictionary<int, PData>
            {
                { configID, new PData { time = dateTime, data = data } }
            };
            participantData.Add(pid, configData);
        }
        else
        {
            Dictionary<int, PData> currentConfigData = participantData.GetValueOrDefault(pid);
            if (!currentConfigData.ContainsKey(configID))
            {
                currentConfigData.Add(configID, new PData { time = dateTime, data = data });
            }
            else
            {
                DateTime existingTime = currentConfigData.GetValueOrDefault(configID).time;
                if (existingTime.CompareTo(dateTime) < 0)
                {
                    currentConfigData[configID] = new PData { time = dateTime, data = data };
                }
            }
        }
        
    }

    void WriteToCSV(string rootDir, CollisionCalculation colCal)
    {
        List<string> lines = new List<string>();
        foreach (KeyValuePair<string, Dictionary<int, PData>> parData in participantData)
        {
            string pid = parData.Key;
            List<DateTime> timestamps = new List<DateTime>();
            List<string> sublines = new List<string>();
            foreach (KeyValuePair<int, PData> configData in parData.Value)
            {
                
                int configID = configData.Key;
                DateTime time = configData.Value.time;
                float data = configData.Value.data;
                string line = time.ToString("HH_mm_ss") + "," + pid + "," + (configID + 1) + "," + data;
                //bool inserted = false;
                int insertIndex = -1;
                foreach (DateTime t in timestamps)
                {
                    if(time.CompareTo(t) < 0)
                    {
                        insertIndex = timestamps.IndexOf(t);
                        break;
                    }
                }

                
                if (insertIndex >= 0)
                {
                    timestamps.Insert(insertIndex, time);
                    sublines.Insert(insertIndex, line);
                } else
                {
                    timestamps.Add(time);
                    sublines.Add(line);
                }
            }
            lines.AddRange(sublines);
        }
        File.WriteAllLines(rootDir + "/" + outputFileName + "_" + colCal.fileName, lines);
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
