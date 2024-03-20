using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DebugText : MonoBehaviour
{
    public static DebugText Instance;
    private TextMeshProUGUI textContainer;
    [SerializeField] float fadeTime;
    float fadeTimer = 0f;
    [SerializeField] Color textColor = Color.red;
    // Start is called before the first frame update
    private void Awake()
    {
        if (!Instance)
        {
            Instance = this;
            textContainer = GetComponent<TextMeshProUGUI>();
        } else
        {
            Destroy(gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(fadeTimer > 0f)
        {
            fadeTimer -= Time.deltaTime;
        }
        textContainer.color = new Color(textColor.r, textColor.g, textColor.b, fadeTimer / fadeTime);
    }

    public void SetText(string text)
    {
        textContainer.text = text;
        fadeTimer = fadeTime;
    }
}
