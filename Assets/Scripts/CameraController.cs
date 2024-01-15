using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static int photoTaken = 0;
    [SerializeField] RenderTexture camRT;
    [SerializeField] MeshRenderer frameFreezerRen;

    Texture2D tex;
    // Start is called before the first frame update

    public void ResetCamera(){
        photoTaken = 0;
    }
    // Update is called once per frame
    void Update()
    {

    }

    public void TakePhoto(bool marked){
        photoTaken++;
        SaveRenderTextureToFile(marked);
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
        string fileName = "capture_" +DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + (marked?"marked_":"") ;
        System.IO.File.WriteAllBytes(Application.persistentDataPath + "/" + fileName + ".png", tex.EncodeToPNG());

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
