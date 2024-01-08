using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DefectsRandomizer : MonoBehaviour
{
    [SerializeField] private Transform[] facadeSlices;
    [SerializeField] private GameObject[] defectedFacade;
    [SerializeField] private GameObject[] doubleDefectFacade;
    //[SerializeField] private Texture2D normalFacade;
    private int defectCount = 24;
    
    // Start is called before the first frame update
    void Start()
    {
        Transform[] shuffledFacadeSlicesArray = facadeSlices.Shuffle().ToArray();
        GameObject[] shuffledArray = defectedFacade.Shuffle().ToArray();
        GameObject[] shuffledArray_2 = doubleDefectFacade.Shuffle().ToArray();
        int array_index = 0;
        int array_2_index = 0;
        for (int i = 0; i < shuffledFacadeSlicesArray.Length; i++)
        {
            if(i < defectCount / 3f)
            {
                GameObject obj = Instantiate(shuffledArray_2[array_2_index], shuffledFacadeSlicesArray[i]);
                //shuffledFacadeSlicesArray[i].material.mainTexture = shuffledTexturesArray_2[array_2_index];
                if(shuffledFacadeSlicesArray[i].tag == "Surface_2"){
                    obj.GetComponent<Renderer>().material.SetTextureScale("_MainTex", new Vector2(0.83363f, 1f));
                }
                array_2_index++;
            } else if (i < defectCount / 3f * 2f)
            {
                GameObject obj = Instantiate(shuffledArray[array_index], shuffledFacadeSlicesArray[i]);
                if(shuffledFacadeSlicesArray[i].tag == "Surface_2"){
                    obj.GetComponent<Renderer>().material.SetTextureScale("_MainTex", new Vector2(0.83363f, 1f));
                }
                array_index++;
            } 
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
