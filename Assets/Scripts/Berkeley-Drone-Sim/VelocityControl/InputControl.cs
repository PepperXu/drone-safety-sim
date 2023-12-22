using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputControl : MonoBehaviour {

	private bool inputEnabled = false;
	public VelocityControl vc;

	private float abs_height = 1;

	private float horizontal_sensitivity = 7f;
	private float vertical_sensitivity = 0.2f;
	private float turning_sensitivity = 4f;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
		if (Input.GetButtonDown("Switch"))
		{
			VisType.SwitchVisType();
		}

		if(DroneManager.currentMissionState == DroneManager.MissionState.Planning)
			return;

		if (Input.GetButtonDown("TakeOff"))
		{
			vc.take_off_flag = true;
		}


		if (inputEnabled){

			float vx = Input.GetAxisRaw("Pitch") * horizontal_sensitivity;
			float vy = Input.GetAxisRaw ("Roll")* horizontal_sensitivity;
			float yaw = Input.GetAxisRaw ("Yaw")* turning_sensitivity;
			float height_diff = Input.GetAxisRaw("Throttle") * vertical_sensitivity;

			if (vx > 0.01f || vy > 0.01f || yaw > 0.01f || height_diff > 0.01f)
            {
				DroneManager.autopilot_stop_flag = true;
            }

			if (DroneManager.currentControlType == DroneManager.ControlType.Manual)
			{
				vc.desired_vx = vx;
				vc.desired_vy = vy;
				vc.desired_yaw = yaw;
				vc.desired_height += height_diff;
			}

            if (Input.GetButtonDown("AutoPilot"))
            {
				DroneManager.autopilot_flag = true;
			}

            if (Input.GetButtonDown("RTH"))
            {
				DroneManager.rth_flag = true;
            }
		}
	}

	public void EnableControl(bool enabled){
		inputEnabled = enabled;
	}
}
