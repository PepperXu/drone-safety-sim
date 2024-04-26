using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VelocityControl : MonoBehaviour {

    //public Communicatio state;

    public GameObject propFL;
    public GameObject propFR;
    public GameObject propRR;
    public GameObject propRL;

    private float gravity = 9.81f;
    private float time_constant_z_velocity = 1.0f; // Normal-person coordinates
    private float time_constant_acceleration = 0.5f;
    private float time_constant_omega_xy_rate = 0.01f; // Normal-person coordinates (roll/pitch)
    private float time_constant_alpha_xy_rate = 0.01f; // Normal-person coordinates (roll/pitch)
    private float time_constant_alpha_z_rate = 0.05f; // Normal-person coordinates (yaw)

    private float max_pitch = 0.175f; // 10 Degrees in radians, otherwise small-angle approximation dies 
    private float max_roll = 0.175f; // 10 Degrees in radians, otherwise small-angle approximation dies
    private float max_alpha = 10.0f;
    //must set this
    private float desired_height = 0.0f;
    private float desired_vx = 0.0f;
    private float desired_vy = 0.0f;
    private float desired_yaw = 0.0f;
    //must set this
    [SerializeField] private float take_off_height = 4.0f;

    float groundOffset = 0.06f;

    private float previous_desired_height;
    
    float height_diff;

    //private bool wait = false;
    //private bool flag = true;
    //public bool take_off_flag = false;

    private float speedScale = 500.0f;

    private float landedHeight = 0f;

    private float landingHeightThreshold = 1.8f;

    private Rigidbody rb;

    //public Vector3 vectorToGround;
    //public int collisionHitCount = 0;
    //public bool out_of_balance = false;

    [SerializeField] AudioClip take_off, flying, landing;
    AudioSource audioSource;

    public enum FlightState{
        Landed,
        TakingOff,
        Hovering,
        Navigating,
        Landing,

        Collided
    }

    public static FlightState currentFlightState {get; private set;}

    //[SerializeField] AutopilotManager autopilotManager;


    // Use this for initialization
    void OnEnable () {
        DroneManager.setVelocityControlEvent.AddListener(SetVelocityParam);
        DroneManager.resetAllEvent.AddListener(ResetVelocityControl);
    }

    void OnDisable(){
        DroneManager.setVelocityControlEvent.RemoveListener(SetVelocityParam);
        DroneManager.resetAllEvent.RemoveListener(ResetVelocityControl);
    }

    //For both initialization and reset.
    void ResetVelocityControl(){
        currentFlightState = FlightState.Landed;
        desired_vx = 0.0f;
        desired_vy = 0.0f;
        desired_yaw = 0.0f;
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        rb.isKinematic = true;
        rb.useGravity = false;
        landedHeight = transform.position.y;
        desired_height = landedHeight;
        Communication.ResetCollision();
        Communication.ResetConstProps(landedHeight);
        //state.GetState();
        audioSource.Stop();
    }

    void SetVelocityParam(float vx, float vy, float yaw, float height_diff){
        this.desired_vx = vx;
        this.desired_vy = vy;
        this.desired_yaw = yaw;
        this.height_diff = height_diff;
    }

    void TakeOff()
    {
        if (currentFlightState != FlightState.Landed)
            return;
        
        desired_height = landedHeight + take_off_height;
        rb.isKinematic = false;
        rb.useGravity = true;
        Vector3 desiredForce = new Vector3(0.0f, gravity * Communication.constProps.Mass, 0.0f);
        rb.AddForce(desiredForce, ForceMode.Acceleration);
        currentFlightState = FlightState.TakingOff;
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
        //if (take_off_flag)
        //{
        //    take_off_flag = false;
        //    TakeOff();
        //}
        if(currentFlightState == FlightState.TakingOff){
            if(Mathf.Abs(Communication.realPose.Altitude - desired_height) < 0.1f)
                currentFlightState = FlightState.Hovering;
        }
        if (currentFlightState == FlightState.Navigating || currentFlightState == FlightState.Hovering || currentFlightState == FlightState.Landing)
        {
            float dis2ground = Communication.realPose.Altitude;
            if (currentFlightState != FlightState.Landing)
            {
                if (dis2ground < landingHeightThreshold)
                {
                    currentFlightState = FlightState.Landing;
                    ExperimentServer.RecordData("Landing at", transform.position.x + "|" + transform.position.y + "|" + transform.position.z, "");
                    DroneManager.autopilot_stop_flag = true;
                    //autopilotManager.StopAutopilot();
                    PlayLandingAudio();
                    desired_height = transform.position.y - dis2ground;
                    desired_vx = 0f;
                    desired_vy = 0f;
                    desired_yaw = 0f;
                }
            } else
            {   
                //Debug.Log("Landing");
                if(dis2ground <= groundOffset)
                {
                    landedHeight = transform.position.y;
                    desired_height = landedHeight;
                    currentFlightState = FlightState.Landed;
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
            }
        }
    }

    void FixedUpdate () {
        //state.GetState ();
        
        // NOTE: I'm using stupid vector order (sideways, up, forward) at the end

        Vector3 desiredTheta;
        Vector3 desiredOmega;

        height_diff = desired_height - previous_desired_height;

        float heightError = Communication.realPose.Altitude - desired_height + 3.27f;
        //Debug.Log(heightError);

        Vector3 desiredVelocity = new Vector3 (desired_vy, -1.0f * heightError / time_constant_z_velocity, desired_vx);
        Vector3 velocityError = Communication.realPose.LocalVelocityVector - desiredVelocity;

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

        Vector3 thetaError = Communication.realPose.Angles - desiredTheta;

        desiredOmega = thetaError * -1.0f / time_constant_omega_xy_rate;
        desiredOmega.y = desired_yaw;

        Vector3 omegaError = Communication.realPose.AngularVelocityVector - desiredOmega;

        Vector3 desiredAlpha = Vector3.Scale(omegaError, new Vector3(-1.0f/time_constant_alpha_xy_rate, -1.0f/time_constant_alpha_z_rate, -1.0f/time_constant_alpha_xy_rate));
        desiredAlpha = Vector3.Min (desiredAlpha, Vector3.one * max_alpha);
        desiredAlpha = Vector3.Max (desiredAlpha, Vector3.one * max_alpha * -1.0f);

        float desiredThrust = (gravity + desiredAcceleration.y) / (Mathf.Cos (Communication.realPose.Angles.z) * Mathf.Cos (Communication.realPose.Angles.x));
        desiredThrust = Mathf.Min (desiredThrust, 2.0f * gravity);
        desiredThrust = Mathf.Max (desiredThrust, 0.0f);

        Vector3 desiredTorque = Vector3.Scale (desiredAlpha, Communication.constProps.Inertia * 2f);
        Vector3 desiredForce = new Vector3 (0.0f, desiredThrust * Communication.constProps.Mass, 0.0f);

        rb.AddRelativeTorque (desiredTorque, ForceMode.Acceleration);
        rb.AddRelativeForce (desiredForce , ForceMode.Acceleration);

        //prop transforms

        if (currentFlightState != FlightState.Landed)
        {
            propFL.transform.Rotate(Vector3.forward * Time.deltaTime * desiredThrust * speedScale);
            propFR.transform.Rotate(Vector3.forward * Time.deltaTime * desiredThrust * speedScale);
            propRR.transform.Rotate(Vector3.forward * Time.deltaTime * desiredThrust * speedScale);
            propRL.transform.Rotate(Vector3.forward * Time.deltaTime * desiredThrust * speedScale);
        }

        previous_desired_height = desired_height;
    }


    private void OnCollisionEnter(Collision collision)
    {
        if(currentFlightState == FlightState.Navigating || currentFlightState == FlightState.Hovering)
        {
            //DroneManager.currentSystemState = DroneManager.SystemState.Emergency;
            Communication.collisionData.collisionCount++;
            if(transform.up.y < 0.6f)
                Communication.collisionData.out_of_balance = true;
            DroneManager.autopilot_stop_flag = true;
            currentFlightState = FlightState.Collided;
            //autopilotManager.StopAutopilot();
            ExperimentServer.RecordData("Collides with an obstacle at", transform.position.x + "|" + transform.position.y + "|" + transform.position.z, "");
        }
    }

}
