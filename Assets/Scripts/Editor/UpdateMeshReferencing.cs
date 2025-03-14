using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

public class UpdateMeshReferencing: EditorWindow
{

    GameObject sceneObject, assetObject; 
    List<MeshFilter> sceneMeshFilterList = new List<MeshFilter>();
    List<Mesh> assetMeshList = new List<Mesh>();

    [MenuItem("Tools/Update Mesh Referencing")]
    public static void ShowWindow(){
        EditorWindow.GetWindow(typeof(UpdateMeshReferencing));
    }

    void OnGUI()
    {
        sceneObject = (GameObject)EditorGUILayout.ObjectField("Scene Object", sceneObject, typeof(GameObject), true);
        assetObject = (GameObject)EditorGUILayout.ObjectField("Asset Object", assetObject, typeof(GameObject), false);
        if(GUILayout.Button("Replace Mesh in Scene Object with Matching Names")){
            PopulateMeshFilterList(ref sceneMeshFilterList, sceneObject.transform);
            PopulateMeshList(ref assetMeshList, assetObject.transform);
            int convertCount = 0;
            foreach(MeshFilter mf in sceneMeshFilterList){
                Mesh m = mf.sharedMesh;
                foreach(Mesh am in assetMeshList){
                    //Mesh am = assetMeshList[i];
                    if(m.name == am.name && m.vertexCount == am.vertexCount && Vector3.Distance(m.bounds.center, am.bounds.center) < 0.0001f){
                        mf.sharedMesh = am;
                        convertCount++;
                        break;
                    }
                }
                
            }
            Debug.Log("Converted " + convertCount + " objects!");
            sceneMeshFilterList.Clear();
            assetMeshList.Clear();
        }
        if(GUILayout.Button("Rebuild Mesh Collider")){
            PopulateMeshFilterList(ref sceneMeshFilterList, sceneObject.transform);
            foreach(MeshFilter mf in sceneMeshFilterList){
                MeshCollider mc = mf.GetComponent<MeshCollider>();
                if(mc != null && mc.sharedMesh == null){
                    mc.sharedMesh = mf.sharedMesh;
                }
            }
        }
    }

    void PopulateMeshFilterList(ref List<MeshFilter> meshFilterList, Transform root){
        foreach(Transform child in root){
            MeshFilter meshFilter = child.GetComponent<MeshFilter>();
            if(meshFilter != null) meshFilterList.Add(meshFilter);
            if(child.childCount > 0)
                PopulateMeshFilterList(ref meshFilterList, child);
        }
    }

    void PopulateMeshList(ref List<Mesh> meshList, Transform root){
        foreach(Transform child in root){
            MeshFilter meshFilter = child.GetComponent<MeshFilter>();
            if(meshFilter != null) meshList.Add(meshFilter.sharedMesh);
            if(child.childCount > 0)
                PopulateMeshList(ref meshList, child);
        }
    }
}
