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

		if (Input.GetButtonDown("TakeOff"))
		{
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
				if (Input.GetButtonDown("AutoPilot"))
            	{
					DroneManager.autopilot_flag = true;
				}
			} else {
				if (Input.GetButtonDown("AutoPilot") || Mathf.Abs(pitchAxis) > 0.1f || Mathf.Abs(rollAxis) > 0.1f || Mathf.Abs(yawAxis) > 0.1f || Mathf.Abs(throttleAxix) > 0.1f)
            	{
					DroneManager.autopilot_stop_flag = true;
					DroneManager.currentMissionState = DroneManager.MissionState.AutopilotInterupted;
            	}
			}

            if (Input.GetButtonDown("RTH"))
            {
				DroneManager.rth_flag = true;
            }

			if(Input.GetButton("Switch"))
				ExperimentServer.switching_flag = true;
			else
				ExperimentServer.switching_flag = false;
		}
	}

	public void EnableControl(bool enabled){
		inputEnabled = enabled;
	}
}
