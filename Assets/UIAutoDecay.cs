using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIAutoDecay : MonoBehaviour
{
    private float decaySpeed = 3f;
    private Image img;
    // Start is called before the first frame update
    void Start()
    {
        img = GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        if (img.color.a <= 0f)
            return;

        Color c = img.color;
        c = new Color(c.r, c.g, c.b, c.a - decaySpeed * Time.deltaTime);
        img.color = c;
    }
}
