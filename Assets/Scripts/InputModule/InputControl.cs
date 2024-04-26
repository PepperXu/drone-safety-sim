using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputControl : MonoBehaviour {

	//private bool inputEnabled = false;
	//public VelocityControl vc;

	private float abs_height = 1;

	private float horizontal_sensitivity = 7f;
	private float vertical_sensitivity = 0.03f;
	private float turning_sensitivity = 4f;

	private bool switched = false;

	private bool take_off_button_pressed;
	private bool rth_button_pressed; 
	private bool autopilot_toggled_on;
	private bool autopilot_toggled_off;

	//[SerializeField] private DroneManager droneManager;

	//[SerializeField] UIUpdater uiUpdater;


	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

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

			float vx = pitchAxis * horizontal_sensitivity;
			float vy = rollAxis * horizontal_sensitivity;

			float yaw = 0f;
			float height_diff = 0f;

			if(Mathf.Abs(yawAxis) > Mathf.Abs(throttleAxix)){
				yaw = yawAxis * turning_sensitivity;
			} else {
				height_diff = throttleAxix * vertical_sensitivity;
			}
			

			if (Input.GetButtonDown("AutoPilot") || autopilot_toggled_on)
            {
				autopilot_toggled_on = false;
				DroneManager.autopilot_flag = true;
			}

			if(Mathf.Abs(pitchAxis) > 0.1f || Mathf.Abs(rollAxis) > 0.1f || Mathf.Abs(yawAxis) > 0.1f || Mathf.Abs(throttleAxix) > 0.1f){
				DroneManager.autopilot_stop_flag = true;
				autopilot_toggled_off = false;
				DroneManager.desired_vx = vx;
				DroneManager.desired_vy = vy;
				DroneManager.desired_yaw = yaw;
				DroneManager.desired_height += height_diff;
			//	
			}

			if (Input.GetButtonDown("AutoPilot") || autopilot_toggled_off)
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


			//if (DroneManager.currentControlType == DroneManager.ControlType.Manual)
			//{
			//	vc.desired_vx = vx;
			//	vc.desired_vy = vy;
			//	vc.desired_yaw = yaw;
			//	vc.desired_height += height_diff;
			//	
			//} else {
			//	
			//}
//
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
