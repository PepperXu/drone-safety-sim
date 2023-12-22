using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DefectsRandomizer : MonoBehaviour
{
    [SerializeField] private MeshRenderer[] facadeSlices;
    [SerializeField] private Texture2D[] defectedFacadeTextures;
    [SerializeField] private Texture2D[] doubleDefectFacadeTexture;
    [SerializeField] private Texture2D normalFacade;
    private int defectCount = 24;
    
    // Start is called before the first frame update
    void Start()
    {
        MeshRenderer[] shuffledFacadeSlicesArray = facadeSlices.Shuffle().ToArray();
        Texture2D[] shuffledTexturesArray = defectedFacadeTextures.Shuffle().ToArray();
        Texture2D[] shuffledTexturesArray_2 = doubleDefectFacadeTexture.Shuffle().ToArray();
        int array_index = 0;
        int array_2_index = 0;
        for (int i = 0; i < shuffledFacadeSlicesArray.Length; i++)
        {
            if(i < defectCount / 3f)
            {
                shuffledFacadeSlicesArray[i].material.mainTexture = shuffledTexturesArray_2[array_2_index];
                array_2_index++;
            } else if (i < defectCount / 3f * 2f)
            {
                shuffledFacadeSlicesArray[i].material.mainTexture = shuffledTexturesArray[array_index];
                array_index++;
            } else
            {
                shuffledFacadeSlicesArray[i].material.mainTexture = normalFacade;
            }

        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
