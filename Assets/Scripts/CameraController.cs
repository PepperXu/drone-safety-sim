using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static int photoTaken = 0;
    [SerializeField] RenderTexture camRT;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TakePhoto(){
        photoTaken++;
        SaveRenderTextureToFile();
    }

    void SaveRenderTextureToFile()
    {
        Texture2D tex;
        tex = new Texture2D(camRT.width, camRT.height, TextureFormat.RGBAFloat, false, true);
        var oldRt = RenderTexture.active;
        RenderTexture.active = camRT;
        tex.ReadPixels(new Rect(0, 0, camRT.width, camRT.height), 0, 0);
        tex.Apply();
        RenderTexture.active = oldRt;
        string fileName = "capture_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
        System.IO.File.WriteAllBytes(Application.persistentDataPath + "/" + fileName + ".png", tex.EncodeToPNG());
        if (Application.isPlaying)
           Destroy(tex);
        else
           DestroyImmediate(tex);
 
    }
}
