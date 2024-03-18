using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class DefectsData : MonoBehaviour
{
    float errorRate = 0.1f;
    Transform[] defects;
    Camera fpvCamera;

    public bool visEnabled = false;
    
    // Start is called before the first frame update
    void Start()
    {   
        if(!visEnabled){
            foreach(Transform t in transform){
                t.gameObject.SetActive(false);
            }
            return;
        }
        List<Transform> defectsListTemp = new List<Transform>();
        foreach(Transform t in transform)
            defectsListTemp.Add(t);
        //defects = defectsListTemp.ToArray();
        float r = Random.value;
        if(r > errorRate){
            defectsListTemp[defectsListTemp.Count-1].gameObject.SetActive(false);
            defectsListTemp.RemoveAt(defectsListTemp.Count-1);
        }
        defects = defectsListTemp.ToArray();
        fpvCamera = GameObject.FindGameObjectWithTag("FPVCam").GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        if(visEnabled){
            foreach(Transform t in defects){
                Vector3 upperLeft = fpvCamera.WorldToScreenPoint(t.TransformPoint(new Vector3(-t.localScale.x, t.localScale.y, 0f)));
                if(upperLeft.z > 7f){
                    t.gameObject.SetActive(false);
                    break;
                }
                Vector3 upperRight = fpvCamera.WorldToScreenPoint(t.TransformPoint(new Vector3(t.localScale.x, t.localScale.y, 0f)));
                Vector3 lowerLeft = fpvCamera.WorldToScreenPoint(t.TransformPoint(new Vector3(-t.localScale.x, -t.localScale.y, 0f)));
                Vector3 lowerRight = fpvCamera.WorldToScreenPoint(t.TransformPoint(new Vector3(t.localScale.x, -t.localScale.y, 0f)));

                if(IsInsideCameraView(upperLeft) && IsInsideCameraView(upperRight) && IsInsideCameraView(lowerLeft) && IsInsideCameraView(lowerRight)){
                    t.gameObject.SetActive(true);
                } else {
                    t.gameObject.SetActive(false);
                }
            }
        }
    }

    bool IsInsideCameraView(Vector3 screenPos){
        return screenPos.x > 0f && screenPos.x < fpvCamera.pixelWidth && screenPos.y > 0f && screenPos.y < fpvCamera.pixelHeight;
    }
}
