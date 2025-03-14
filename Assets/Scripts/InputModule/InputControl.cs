﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class InputControl : MonoBehaviour {

	//private bool inputEnabled = false;
	//public VelocityControl vc;

	private float abs_height = 1;

	private float horizontal_sensitivity = 8f;
	private float vertical_sensitivity = 0.08f;
	private float turning_sensitivity = 4f;

	private bool switched = false;

	private bool take_off_button_pressed;
	private bool rth_button_pressed; 
	private bool autopilot_toggled_on;
	private bool autopilot_toggled_off;

	[SerializeField] CustomRayController rightRayController;
	[SerializeField] CustomRayController leftRayController;
	bool resetTriggered = false;
	float buttonHoldTimer = 0f;

	[SerializeField] private ExperimentServer experimentServer;

	public enum InputStatus
	{
		Idle,
		Active
	}

	public static InputStatus inputStatus = InputStatus.Idle;

	//[SerializeField] UIUpdater uiUpdater;


	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        InputDevice rightController = rightRayController.GetController();
		InputDevice leftController = leftRayController.GetController();

        if (leftController.isValid)
        {
            if (leftController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool bbutton))
            {
                if (bbutton)
                {
                    buttonHoldTimer += Time.deltaTime;
                    if (buttonHoldTimer > 1.5f)
                    {
						experimentServer.ResetExperiment();
                        resetTriggered = true;
						return;
                    }
                }
                else
                {
                    resetTriggered = false;
                    buttonHoldTimer = 0f;
                }
            }
        }

		if (rightController.isValid)
		{
			if (rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool ybutton))
			{
				ExperimentServer.switching_flag = ybutton;
            }
		}
        
        //if(DroneManager.currentMissionState == DroneManager.MissionState.Planning)
        //	return;
        if (Input.GetButtonDown("TakeOff") || take_off_button_pressed)
		{
			take_off_button_pressed = false;
			DroneManager.take_off_flag = true;
		}

		float pitchAxis = Input.GetAxis("Pitch");
		float rollAxis = Input.GetAxis ("Roll");
		float yawAxis = Input.GetAxis ("Yaw");
		float throttleAxix = Input.GetAxis("Throttle");

		float vx = rollAxis * horizontal_sensitivity;
		float vy = pitchAxis * horizontal_sensitivity;

		float angle = Mathf.Atan2(vy, vx);
		Vector2 input = new Vector2(vx, vy);
		Vector2 normDir;

		if (Mathf.Abs(angle) < Mathf.PI / 8f)
		{
            normDir = Vector2.right;
		}
		else if (Mathf.Abs(angle) > Mathf.PI / 8f * 7f)
		{
            normDir = Vector2.left;
		}
		else if (angle > 0f)
		{
			if (Mathf.Abs(angle) >= Mathf.PI / 8f && Mathf.Abs(angle) <= Mathf.PI / 8f * 3f)
			{
                normDir = new Vector2(1f, 1f).normalized;
            }
			else if (Mathf.Abs(angle) > Mathf.PI / 8f * 3f && Mathf.Abs(angle) < Mathf.PI / 8f * 5f)
			{
                normDir = Vector2.up;
            }
			else 
			{
                normDir = new Vector2(-1f, 1f).normalized;
            }
		} else
		{
            if (Mathf.Abs(angle) >= Mathf.PI / 8f && Mathf.Abs(angle) <= Mathf.PI / 8f * 3f)
            {
                normDir = new Vector2(1f, -1f).normalized;
            }
            else if (Mathf.Abs(angle) > Mathf.PI / 8f * 3f && Mathf.Abs(angle) < Mathf.PI / 8f * 5f)
            {
                normDir = Vector2.down;
            }
            else 
            {
                normDir = new Vector2(-1f, -1f).normalized;
                
            }
        }

        float magnitude = Vector2.Dot(input, normDir);
        vx = (magnitude * normDir).x;
        vy = (magnitude * normDir).y;
		Debug.Log("vx:" + vx + ", vy: " + vy);

        float yaw = 0f;
		float height_diff = 0f;

		

		if(Mathf.Abs(yawAxis) > Mathf.Abs(throttleAxix)){
			yaw = yawAxis * turning_sensitivity;
		} else {
			height_diff = throttleAxix * vertical_sensitivity;
		}
		

		if(DroneManager.currentControlType == DroneManager.ControlType.Manual){
			DroneManager.desired_vx = vx;
			DroneManager.desired_vy = vy;
			DroneManager.desired_yaw = yaw;
			DroneManager.desired_height += height_diff;
            inputStatus = (Mathf.Abs(pitchAxis) > 0.01f || Mathf.Abs(rollAxis) > 0.01f || Mathf.Abs(yawAxis) > 0.01f || Mathf.Abs(throttleAxix) > 0.01f) ? InputStatus.Active : InputStatus.Idle;
        } else
		{
            if (Mathf.Abs(pitchAxis) > 0.1f || Mathf.Abs(rollAxis) > 0.1f || Mathf.Abs(yawAxis) > 0.1f || Mathf.Abs(throttleAxix) > 0.1f)
            {
                DroneManager.autopilot_stop_flag = true;
                autopilot_toggled_off = false;
            }
        }

        if (Input.GetButtonDown("AutoPilot"))
        {
            AutopilotTogglePressed(true);
        }

        if (autopilot_toggled_on)
        {
            autopilot_toggled_on = false;
            DroneManager.autopilot_flag = true;
        }

        if (autopilot_toggled_off)
        {
            DroneManager.autopilot_stop_flag = true;
            autopilot_toggled_off = false;
            //DroneManager.currentMissionState = DroneManager.MissionState.AutopilotInterupted;
        }

        if (Input.GetButtonDown("RTH") || rth_button_pressed)
        {
			rth_button_pressed = false;
			DroneManager.rth_flag = true;
        }

		if(Input.GetButtonDown("MarkDefect")){
			//uiUpdater.MarkDefect();
			DroneManager.mark_defect_flag = true;
		}


		
	}

	public void ButtonPressed(string buttonName){
		switch(buttonName){
			case "TakeOff":
				take_off_button_pressed = true;
				break;
			case "RTH":
				rth_button_pressed = true;
				break;
			default:
				Debug.LogWarning("Button Name Undefined");
				break;
		}
	}

	public void AutopilotTogglePressed(bool toggle_ON){
		if(toggle_ON && DroneManager.currentControlType != DroneManager.ControlType.Autonomous)
			autopilot_toggled_on = true;
		else if(!toggle_ON && DroneManager.currentControlType != DroneManager.ControlType.Manual)
			autopilot_toggled_off = true;
	}

	//public void EnableControl(bool enabled){
	//	inputEnabled = enabled;
	//}
}
