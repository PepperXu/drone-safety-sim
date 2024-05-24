using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(ConfigManager))]
public class ConfigManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ConfigManager configManager = (ConfigManager)target;

        if (GUILayout.Button("Draw Flight Path For this Config"))
        {
            configManager.flightPlanning.VisualizeFlightPlanEditor(configManager.surfaceIndex);
        }

    }
}
