using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StateFinder : MonoBehaviour {
//	public float Pitch; // The current pitch for the given transform in radians
//	public float Roll; // The current roll for the given transform in radians
//	public float Yaw; // The current Yaw for the given transform in radians
	
	public struct Pose{
		public Vector3 WorldPosition;
		public Vector3 Angles;
		public Vector3 WorldVelocity;
		public Vector3 WorldAcceleration;
		public float Altitude;
	}
	public static Pose pose;

	float landedHeight;
	public float Altitude; // The current altitude from the zero position

	public Vector3 LocalVelocityVector; // Velocity vector

	private Vector3 previousWorldVelocity;
	public Vector3 AngularVelocityVector; // Angular Velocity
	
	public Vector3 Inertia;
	public float Mass;

	private bool flag = true; // Only get mass and inertia once 

	public Transform droneTransform; // linked externally

	void FixedUpdate(){
		UpdateState();
	}
	void UpdateState() {

		Vector3 worldDown = droneTransform.transform.InverseTransformDirection (Vector3.down);
		float Pitch = worldDown.z; // Small angle approximation
		float Roll = -worldDown.x; // Small angle approximation
		float Yaw = droneTransform.transform.eulerAngles.y;

//		float Pitch = cc.transform.eulerAngles.x;
//		Pitch = (Pitch > 180) ? Pitch - 360 : Pitch;
//		Pitch = Pitch / 180.0f * 3.1416f; // Convert to radians
//
//		float Roll = cc.transform.eulerAngles.z;
//		Roll = (Roll > 180.0f) ? Roll - 360.0f : Roll;
//		Roll = Roll / 180.0f * 3.1416f; // Convert to radians
//
//		float Yaw = cc.transform.eulerAngles.y;
//		Yaw = (Yaw > 180.0f) ? Yaw - 360.0f : Yaw;
//		Yaw = Yaw / 180.0f * 3.1416f; // Convert to radians

//		Altitude = cc.transform.position.y;
//
		pose.Angles = new Vector3 (Pitch, Yaw, Roll);

		Altitude = droneTransform.transform.position.y - landedHeight;

		pose.WorldVelocity = droneTransform.transform.GetComponent<Rigidbody> ().velocity;
		LocalVelocityVector = droneTransform.transform.InverseTransformDirection (pose.WorldVelocity);

		pose.WorldAcceleration = (pose.WorldVelocity-previousWorldVelocity)/Time.fixedDeltaTime;

		AngularVelocityVector = droneTransform.transform.GetComponent<Rigidbody> ().angularVelocity;
		AngularVelocityVector = droneTransform.transform.InverseTransformDirection (AngularVelocityVector);

		pose.WorldPosition = droneTransform.transform.position;

		if (flag) {
			Inertia = droneTransform.transform.GetComponent<Rigidbody> ().inertiaTensor;
			//Debug.Log(Inertia.ToString());
			Mass = droneTransform.transform.GetComponent<Rigidbody> ().mass;
			flag = false;
		}

		previousWorldVelocity = pose.WorldVelocity;

	}

	public void Reset(float landedHeight) {
		flag = true;
		LocalVelocityVector = Vector3.zero;
		AngularVelocityVector = Vector3.zero;
		pose.Angles = Vector3.zero;
		this.landedHeight = landedHeight;
		Altitude = 0.0f;

		enabled = true;
	}

}
