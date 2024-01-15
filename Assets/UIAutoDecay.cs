using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIAutoDecay : MonoBehaviour
{
    public float decaySpeed = 3f;
    private Image img;
    private TextMeshProUGUI text;
    // Start is called before the first frame update
    void Start()
    {
        img = GetComponent<Image>();
        text = GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        if(img != null){
            if (img.color.a <= 0f)
                return;

            Color c = img.color;
            c = new Color(c.r, c.g, c.b, c.a - decaySpeed * Time.deltaTime);
            img.color = c;
        } 

        if(text != null){
            if (text.color.a <= 0f)
                return;

            Color c = text.color;
            c = new Color(c.r, c.g, c.b, c.a - decaySpeed * Time.deltaTime);
            text.color = c;
        }
    }
}
