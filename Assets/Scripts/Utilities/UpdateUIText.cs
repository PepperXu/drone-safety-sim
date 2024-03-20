using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpdateUIText : MonoBehaviour
{
    private Text textUI;
    // Start is called before the first frame update
    void Start()
    {
       textUI = GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateSliderValueUI(Slider slider){
        if(textUI)
            textUI.text = (((int)(slider.value * 10f)) * 0.1f).ToString();
    }
}
