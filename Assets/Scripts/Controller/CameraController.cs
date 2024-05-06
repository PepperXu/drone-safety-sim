using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
     static int photoTaken = 0;
     static int defectMaked = 0;
    [SerializeField] RenderTexture camRT;
    [SerializeField] MeshRenderer frameFreezerRen;
    //[SerializeField] UIUpdater uIUpdater;

    Texture2D tex;
    // Start is called before the first frame update

    void OnEnable(){
        DroneManager.markDefectEvent.AddListener(TakePhotoAndMark);
        DroneManager.takePhotoEvent.AddListener(TakePhoto);
        DroneManager.resetAllEvent.AddListener(ResetCamera);
    }

    void OnDisable(){
        DroneManager.markDefectEvent.RemoveListener(TakePhotoAndMark);
        DroneManager.takePhotoEvent.RemoveListener(TakePhoto);
        DroneManager.resetAllEvent.RemoveListener(ResetCamera);
    }

    void ResetCamera(){
        photoTaken = 0;
        defectMaked = 0;
    }
    
    // Update is called once per frame
    void Update()
    {

    }

    void TakePhoto(){
        photoTaken++;
        if(ExperimentServer.isRecording)
            SaveRenderTextureToFile(false);
    }

    void TakePhotoAndMark(){
        photoTaken++;
        defectMaked++;
        if(ExperimentServer.isRecording)
            SaveRenderTextureToFile(false);
    }

    IEnumerator FreezeFrame(){
        frameFreezerRen.material.mainTexture = tex;
        frameFreezerRen.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.8f);
        frameFreezerRen.gameObject.SetActive(false);
        if (Application.isPlaying)
           Destroy(tex);
        else
           DestroyImmediate(tex);
    }

    void SaveRenderTextureToFile(bool marked)
    {
        tex = new Texture2D(camRT.width, camRT.height, TextureFormat.RGBAFloat, false, true);
        var oldRt = RenderTexture.active;
        RenderTexture.active = camRT;
        tex.ReadPixels(new Rect(0, 0, camRT.width, camRT.height), 0, 0);
        tex.Apply();
        
        RenderTexture.active = oldRt;
        string fileName = (marked?"marked_" + defectMaked :"capture_" + photoTaken) + "_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
        System.IO.File.WriteAllBytes(ExperimentServer.folderPath + "/" + fileName + ".png", tex.EncodeToPNG());

        if(marked)
            StartCoroutine(FreezeFrame());
        else{
            if (Application.isPlaying)
                Destroy(tex);
            else
                DestroyImmediate(tex);
        }
    }
}
