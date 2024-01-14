using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputControl : MonoBehaviour {

	private bool inputEnabled = false;
	public VelocityControl vc;

	private float abs_height = 1;

	private float horizontal_sensitivity = 7f;
	private float vertical_sensitivity = 0.08f;
	private float turning_sensitivity = 4f;

	private bool switched = false;

	private bool take_off_button_pressed;
	private bool rth_button_pressed; 
	private bool autopilot_toggled_on;
	private bool autopilot_toggled_off;



	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		//if(ExperimentServer.currentVisCondition == ExperimentServer.VisualizationCondition.Manual){
		//	if (Input.GetButtonDown("Switch"))
		//	{
		//		VisType.SwitchVisType();
		//	}
		//} else if(ExperimentServer.currentVisCondition == ExperimentServer.VisualizationCondition.ManualProcedual){
		//	if(Input.GetButton("Switch")){
		//		VisType.RevealHiddenVisType(true);
		//		if(Input.GetAxisRaw("SwitchConfirm") > 0.5f){
		//			if(!switched){
		//				VisType.SwitchVisType();
		//				switched = true;
		//			}
		//		} else {
		//			switched = false;
		//		}
		//	} else {
		//		VisType.RevealHiddenVisType(false);
		//		switched = false;
		//	}
		//} else if(ExperimentServer.currentVisCondition == ExperimentServer.VisualizationCondition.SystemProcedual){
		//	if (Input.GetButtonDown("Switch"))
		//	{
		//		VisType.SwitchVisType();
		//	}
		//}
//
		if(DroneManager.currentMissionState == DroneManager.MissionState.Planning)
			return;

		if (Input.GetButtonDown("TakeOff") || take_off_button_pressed)
		{
			take_off_button_pressed = false;
			vc.take_off_flag = true;
		}


		if (inputEnabled){

			float pitchAxis = Input.GetAxisRaw("Pitch");
			float rollAxis = Input.GetAxisRaw ("Roll");
			float yawAxis = Input.GetAxisRaw ("Yaw");
			float throttleAxix = Input.GetAxisRaw("Throttle");

			float vx = pitchAxis * horizontal_sensitivity;
			float vy = rollAxis * horizontal_sensitivity;

			float yaw = 0f;
			float height_diff = 0f;

			if(Mathf.Abs(yawAxis) > Mathf.Abs(throttleAxix)){
				yaw = yawAxis * turning_sensitivity;
			} else {
				height_diff = throttleAxix * vertical_sensitivity;
			}

			if (DroneManager.currentControlType == DroneManager.ControlType.Manual)
			{
				vc.desired_vx = vx;
				vc.desired_vy = vy;
				vc.desired_yaw = yaw;
				vc.desired_height += height_diff;
				if (Input.GetButtonDown("AutoPilot") || autopilot_toggled_on)
            	{
					autopilot_toggled_on = false;
					DroneManager.autopilot_flag = true;
				}
			} else {
				if (Input.GetButtonDown("AutoPilot") || autopilot_toggled_off || Mathf.Abs(pitchAxis) > 0.1f || Mathf.Abs(rollAxis) > 0.1f || Mathf.Abs(yawAxis) > 0.1f || Mathf.Abs(throttleAxix) > 0.1f)
            	{
					DroneManager.autopilot_stop_flag = true;
					autopilot_toggled_off = false;
					DroneManager.currentMissionState = DroneManager.MissionState.AutopilotInterupted;
            	}
			}

            if (Input.GetButtonDown("RTH") || rth_button_pressed)
            {
				rth_button_pressed = false;
				DroneManager.rth_flag = true;
            }

			if(Input.GetButton("Switch"))
				ExperimentServer.switching_flag = true;
			else
				ExperimentServer.switching_flag = false;
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

	public void EnableControl(bool enabled){
		inputEnabled = enabled;
	}
}
