using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VelocityControl : MonoBehaviour {

    public StateFinder state;

    public GameObject propFL;
    public GameObject propFR;
    public GameObject propRR;
    public GameObject propRL;

    private float gravity = 9.81f;
    private float time_constant_z_velocity = 1.0f; // Normal-person coordinates
    private float time_constant_acceleration = 0.5f;
    private float time_constant_omega_xy_rate = 0.1f; // Normal-person coordinates (roll/pitch)
    private float time_constant_alpha_xy_rate = 0.05f; // Normal-person coordinates (roll/pitch)
    private float time_constant_alpha_z_rate = 0.05f; // Normal-person coordinates (yaw)

    private float max_pitch = 0.175f; // 10 Degrees in radians, otherwise small-angle approximation dies 
    private float max_roll = 0.175f; // 10 Degrees in radians, otherwise small-angle approximation dies
    private float max_alpha = 10.0f;
    //must set this
    public float desired_height = 0.0f;
    public float desired_vx = 0.0f;
    public float desired_vy = 0.0f;
    public float desired_yaw = 0.0f;
    //must set this
    public float take_off_height = 4.0f;

    public float groundOffset = 0.06f;

    private float previous_desired_height;
    [HideInInspector]
    public float height_diff;

    private bool wait = false;
    private bool flag = true;
    public bool take_off_flag = false;

    private float speedScale = 500.0f;

    private float landedHeight = 0f;

    private float landingHeightThreshold = 1f;

    private Rigidbody rb;

    public Vector3 vectorToGround;

    public int collisionHitCount = 0;
    public bool out_of_balance = false;

    [SerializeField] AudioClip take_off, flying, landing;
    AudioSource audioSource;

    [SerializeField] PositionalSensorSimulator pss;
    [SerializeField] RandomPulseNoise rpn;

    // Use this for initialization
    void Start () {
          
    }

    public void ResetVelocityControl(){
        state.Reset();
        desired_vx = 0.0f;
        desired_vy = 0.0f;
        desired_yaw = 0.0f;
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        rb.isKinematic = true;
        rb.useGravity = false;
        landedHeight = transform.position.y;
        desired_height = landedHeight;
        collisionHitCount = 0;
        out_of_balance = false;
        state.GetState();
        audioSource.Stop();
    }

    void TakeOff()
    {
        if (DroneManager.currentFlightState != DroneManager.FlightState.Landed)
            return;
        
        desired_height = landedHeight + take_off_height;
        rb.isKinematic = false;
        rb.useGravity = true;
        Vector3 desiredForce = new Vector3(0.0f, gravity * state.Mass, 0.0f);
        rb.AddForce(desiredForce, ForceMode.Acceleration);
        DroneManager.currentFlightState = DroneManager.FlightState.TakingOff;
        StartCoroutine(PlayTakeOffAudio());
    }

    IEnumerator PlayTakeOffAudio()
    {
        audioSource.Stop();
        audioSource.clip = take_off;
        audioSource.loop = false;
        audioSource.Play();
        float clipLength = take_off.length;
        float timer = 0f;
        while (timer < clipLength)
        {
            timer += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        audioSource.Stop();
        audioSource.clip = flying;
        audioSource.loop = true;
        audioSource.Play();
    }

    void PlayLandingAudio()
    {
        audioSource.Stop();
        audioSource.clip = landing;
        audioSource.loop = false;
        audioSource.Play();
    }
    // Update is called once per frame
    private void Update()
    {
        if (take_off_flag)
        {
            take_off_flag = false;
            TakeOff();
        }

        if (DroneManager.currentFlightState == DroneManager.FlightState.Navigating || DroneManager.currentFlightState == DroneManager.FlightState.Hovering || DroneManager.currentFlightState == DroneManager.FlightState.Landing)
        {
            float dis2ground = vectorToGround.magnitude;
            if (DroneManager.currentFlightState != DroneManager.FlightState.Landing)
            {
                if (dis2ground < landingHeightThreshold)
                {
                    DroneManager.currentFlightState = DroneManager.FlightState.Landing;
                    PlayLandingAudio();
                    desired_height = transform.position.y - dis2ground;
                    desired_vx = 0f;
                    desired_vy = 0f;
                    desired_yaw = 0f;
                }
            } else
            {   
                Debug.Log("Landing");
                if(dis2ground <= groundOffset)
                {
                    landedHeight = transform.position.y;
                    desired_height = landedHeight;
                    DroneManager.currentFlightState = DroneManager.FlightState.Landed;
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
            }
        }


    }

    void FixedUpdate () {
        state.GetState ();
        
        // NOTE: I'm using stupid vector order (sideways, up, forward) at the end

        Vector3 desiredTheta;
        Vector3 desiredOmega;

        height_diff = desired_height - previous_desired_height;

        float heightError = state.Altitude - desired_height + 3.27f;
        //Debug.Log(heightError);

        Vector3 desiredVelocity = new Vector3 (desired_vy, -1.0f * heightError / time_constant_z_velocity, desired_vx);
        Vector3 velocityError = state.VelocityVector - desiredVelocity;

        Vector3 desiredAcceleration = velocityError * -1.0f / time_constant_acceleration;

        desiredTheta = new Vector3 (desiredAcceleration.z / gravity, 0.0f, -desiredAcceleration.x / gravity);
        if (desiredTheta.x > max_pitch) {
            desiredTheta.x = max_pitch;
        } else if (desiredTheta.x < -1.0f * max_pitch) {
            desiredTheta.x = -1.0f * max_pitch;
        }
        if (desiredTheta.z > max_roll) {
            desiredTheta.z = max_roll;
        } else if (desiredTheta.z < -1.0f * max_roll) {
            desiredTheta.z = -1.0f * max_roll;
        }

        Vector3 thetaError = state.pose.Angles - desiredTheta;

        desiredOmega = thetaError * -1.0f / time_constant_omega_xy_rate;
        desiredOmega.y = desired_yaw;

        Vector3 omegaError = state.AngularVelocityVector - desiredOmega;

        Vector3 desiredAlpha = Vector3.Scale(omegaError, new Vector3(-1.0f/time_constant_alpha_xy_rate, -1.0f/time_constant_alpha_z_rate, -1.0f/time_constant_alpha_xy_rate));
        desiredAlpha = Vector3.Min (desiredAlpha, Vector3.one * max_alpha);
        desiredAlpha = Vector3.Max (desiredAlpha, Vector3.one * max_alpha * -1.0f);

        float desiredThrust = (gravity + desiredAcceleration.y) / (Mathf.Cos (state.pose.Angles.z) * Mathf.Cos (state.pose.Angles.x));
        desiredThrust = Mathf.Min (desiredThrust, 2.0f * gravity);
        desiredThrust = Mathf.Max (desiredThrust, 0.0f);

        Vector3 desiredTorque = Vector3.Scale (desiredAlpha, state.Inertia);
        Vector3 desiredForce = new Vector3 (0.0f, desiredThrust * state.Mass, 0.0f);

        rb.AddRelativeTorque (desiredTorque, ForceMode.Acceleration);
        rb.AddRelativeForce (desiredForce , ForceMode.Acceleration);

        //prop transforms

        if (DroneManager.currentFlightState != DroneManager.FlightState.Landed)
        {
            propFL.transform.Rotate(Vector3.forward * Time.deltaTime * desiredThrust * speedScale);
            propFR.transform.Rotate(Vector3.forward * Time.deltaTime * desiredThrust * speedScale);
            propRR.transform.Rotate(Vector3.forward * Time.deltaTime * desiredThrust * speedScale);
            propRL.transform.Rotate(Vector3.forward * Time.deltaTime * desiredThrust * speedScale);
        }

        //Debug.Log ("Velocity" + state.VelocityVector);
        //Debug.Log ("Desired Velocity" + desiredVelocity);
        //Debug.Log ("Desired Acceleration" + desiredAcceleration);
        //Debug.Log ("Angles" + state.Angles);
        //Debug.Log ("Desired Angles" + desiredTheta);
        //Debug.Log ("Angular Velocity" + state.AngularVelocityVector);
        //Debug.Log ("Desired Angular Velocity" + desiredOmega);
        //Debug.Log ("Desired Angular Acceleration" + desiredAlpha);
        //Debug.Log ("Desired Torque" + desiredTorque);

        previous_desired_height = desired_height;
    }

    public void Reset() {

        state.VelocityVector = Vector3.zero;
        state.AngularVelocityVector = Vector3.zero;

        desired_vx = 0.0f;
        desired_vy = 0.0f;
        desired_yaw = 0.0f;
        desired_height = landedHeight;

        state.Reset ();
    
        enabled = true;
    }

    IEnumerator Waiting(float time) {
        wait = true;
        yield return new WaitForSeconds(time);
        wait = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(DroneManager.currentFlightState == DroneManager.FlightState.Navigating || DroneManager.currentFlightState == DroneManager.FlightState.Hovering)
        {
            //DroneManager.currentSystemState = DroneManager.SystemState.Emergency;
            collisionHitCount++;
            if(transform.up.y < 0.6f)
                out_of_balance = true;
            DroneManager.autopilot_stop_flag = true;
        }
    }

    private void OnTriggerEnter(Collider other) {
        if(other.tag == "GPSWeakZone"){
            pss.SetSignalLevel(1);
        } else if(other.tag == "WindZone"){
            other.gameObject.SetActive(false);
            rpn.strength_mean = 60f;
			rpn.wind_change_flag = true;
        }
    }

    private void OnTriggerExit(Collider other) {
        if(other.tag == "GPSWeakZone"){
            pss.switch_gps_normal = true;
            pss.SetSignalLevel(3);
        } 
    }
}
