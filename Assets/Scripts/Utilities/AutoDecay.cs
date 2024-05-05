using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AutoDecay : MonoBehaviour
{
    public float holdTime = 3f;
    public float decaySpeed = 3f;
    private float timer;
    private TextMeshPro text;
    private SpriteRenderer spriteRenderer;
    private float originalTextAlpha, originalGraphicAlpha;
    // Start is ubl before the first frame update
    void OnEnable()
    {
        if (text == null)
        {
            text = GetComponentInChildren<TextMeshPro>();
            if(text != null)
                originalTextAlpha = text.color.a;
        }
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
                originalGraphicAlpha = spriteRenderer.color.a;
        }
        ResetAlpha();
    }

    // Update is called once per frame
    void Update()
    {
        if (timer < holdTime)
        {
            timer += Time.deltaTime;
            return;
        }
        if(text != null){
            if (text.color.a <= 0f)
                gameObject.SetActive(false); 

            Color c = text.color;
            c = new Color(c.r, c.g, c.b, c.a - decaySpeed * Time.deltaTime);
            text.color = c;
        } 

        if(spriteRenderer != null){
            if (spriteRenderer.color.a <= 0f)
                gameObject.SetActive(false);

            Color c = spriteRenderer.color;
            c = new Color(c.r, c.g, c.b, c.a - decaySpeed * Time.deltaTime);
            spriteRenderer.color = c;
        }
    }

    public void ResetAlpha()
    {
        if (text != null)
        {
            Color c = text.color;
            c = new Color(c.r, c.g, c.b, originalTextAlpha);
            text.color = c;
        }
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c = new Color(c.r, c.g, c.b, originalGraphicAlpha);
            spriteRenderer.color = c;
        }
        timer = 0f;
    }
}
