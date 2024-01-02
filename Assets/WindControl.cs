using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class WindControl : MonoBehaviour
{
    GraphicRaycaster m_Raycaster;
    PointerEventData m_PointerEventData;
    EventSystem m_EventSystem;
    bool dragging = false;
    Vector2 mouseInitialPosition;
    float currentScale;
    [SerializeField] Transform windControlTransform, windCone;
    [SerializeField] TextMeshProUGUI windStrengthText;
    [SerializeField] ExperimentMonitor experimentMonitor;
    float currentWindStrength;
    public int minWindStrength = 20;
    public int maxWindStrength = 80;

    int steps = 4;
    float stepLength = 15f;
    float minScale = 0.8f;
    float maxScale = 2.1f;
    // Start is called before the first frame update
    void Start()
    {
        //Fetch the Raycaster from the GameObject (the Canvas)
        m_Raycaster = GetComponent<GraphicRaycaster>();
        //Fetch the Event System from the Scene
        m_EventSystem = GetComponent<EventSystem>();
        steps = (int) ((maxWindStrength - minWindStrength)/stepLength);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            //Set up the new Pointer Event
            m_PointerEventData = new PointerEventData(m_EventSystem);
            //Set the Pointer Event Position to that of the mouse position
            m_PointerEventData.position = Input.mousePosition;

            //Create a list of Raycast Results
            List<RaycastResult> results = new List<RaycastResult>();

            //Raycast using the Graphics Raycaster and mouse click position
            m_Raycaster.Raycast(m_PointerEventData, results);

            //For every result returned, output the name of the GameObject on the Canvas hit by the Ray
            foreach (RaycastResult result in results)
            {
                if(result.gameObject.tag == "WindControl"){
                    dragging = true;
                    mouseInitialPosition = Input.mousePosition;
                    currentScale = windControlTransform.localScale.x;
                }
            }
        }

        if(Input.GetKey(KeyCode.Mouse0) && dragging){
            //Debug.Log(mouseInitialPosition + ";" + windControlTransform.position);
            Vector2 originalOffset = mouseInitialPosition - (Vector2)windControlTransform.position;
            Vector2 currentOffset = Input.mousePosition - windControlTransform.position;
            float angleOfCurrentOffset = Vector2.SignedAngle(Vector2.up, currentOffset);
            windCone.localEulerAngles = Vector3.forward * (angleOfCurrentOffset + 20f);
            float scaleFactor = currentOffset.magnitude / originalOffset.magnitude;
            windControlTransform.localScale = Vector3.one * minScale;
            int currentStep = 0;
            for(int i = 0; i < steps + 1; i++){
                if(currentScale * scaleFactor >= minScale + i * (maxScale - minScale)/steps){
                    windControlTransform.localScale = Vector3.one * (minScale + i * (maxScale - minScale)/steps);
                    currentStep = i;
                }
            }
            currentWindStrength = minWindStrength + currentStep * stepLength;
            windStrengthText.text = "" + (int)currentWindStrength;
        } else
        {
            experimentMonitor.SendWindCondition(windCone.localEulerAngles.z - 20f, currentWindStrength);
            dragging = false;
        }
    }
}
