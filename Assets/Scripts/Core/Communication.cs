using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class Communication : MonoBehaviour
{

    public struct Frame{
        public Texture2D frameTexture;
        public float capturedTime;
        public void Capture(RenderTexture renderTexture){
            if(frameTexture == null)
                frameTexture = new Texture2D( renderTexture.width, renderTexture.height );
            RenderTexture.active = renderTexture;
            frameTexture.ReadPixels(new Rect( 0, 0, renderTexture.width, renderTexture.height ), 0, 0);
            frameTexture.Apply();

            capturedTime = Time.time;
        }
    }

    public struct CollisionData{
        public Vector3[] distances;

        public int collisionCount;

        public bool out_of_balance;
        public Vector3 v2ground;
        public string collisionStatus;
        public Vector3 shortestDistance;


    }

    public struct PositionData{
        public Vector3 virtualPosition;
        public bool gpsLost;
        public int sigLevel; 
        public Vector3 v2surf;
        public Vector3 v2bound;
        
        public bool inBuffer;
        public Vector3 currentTargetOffset;
        public Vector3 nearestWaypoint;
        public int nearestWaypointIndex;
    }

    public struct RealPose{
		public Vector3 WorldPosition;
		public Vector3 Angles;
		public Vector3 WorldVelocity;
		public Vector3 WorldAcceleration;
		public float Altitude;
        public Vector3 LocalVelocityVector; 
        public Vector3 previousWorldVelocity;
	    public Vector3 AngularVelocityVector; 
	}

    public struct ConstantProperties{
        public float landedHeight;
        public Vector3 Inertia;
	    public float Mass;
    }

    public struct Battery{
        public float batteryPercentage;
        public float batteryRemainingTime;
        public float voltageLevel;
        public string batteryState;
        public float rthThreshold;
        public bool rth;

        public float distanceUntilRTH;
        public float distanceUntilLowBat;
        public float distanceUntilCritical;
        public float totalRemainingDistance;

        public Vector3 vector2Home;

        public bool batteryDropped;
    }

    public struct Wind{
        public Vector3 direction;
    }


    public static RealPose realPose;
    public static CollisionData collisionData;
    public static PositionData positionData;

    public static ConstantProperties constProps;
    public static Battery battery;

    public static Wind wind;

    //public static Vector3[] flightTrajectory;

    public static Waypoint[] waypoints;
    
    public static int currentWaypointIndex = -1;
    public static int currentSurfaceIndex = 0;

    public static Rigidbody droneRb;

    public static RaycastHit? markDefectHit;

    //public static UnityEvent finishPlanning = new UnityEvent();


    [SerializeField] float inputLatency;
    [SerializeField] float signalReceivingLatency;
    [SerializeField] float cameraLatency;
    [SerializeField] Camera FPVCamera;
    RenderTexture renderTexture;
    [SerializeField] RenderTexture destRT;

    [SerializeField] Transform droneTransform;

    

    int bufferSize = 16;
    Frame[] storedFrames;
    public static bool cameraImageReceived = true;

    int currentFrameIndex = 0;
    int renderedFrameBufferIndex = 0;


    void OnEnable(){
        droneRb = droneTransform.GetComponent<Rigidbody> ();
    }

    // Start is called before the first frame update
    void Start()
    {
        if (cameraLatency > 0f)
        {
            storedFrames = new Frame[bufferSize];
            renderTexture = new RenderTexture(960, 540, 16);
            FPVCamera.targetTexture = renderTexture;
            StartCoroutine(LaggedTransferCameraImages());
        } else
        {
            FPVCamera.targetTexture = destRT;
        }
    }

    // Update is called once per frame
    void Update()
    {

		Vector3 worldDown = droneTransform.InverseTransformDirection (Vector3.down);
		float Pitch = worldDown.z; // Small angle approximation
		float Roll = -worldDown.x; // Small angle approximation
		float Yaw = droneTransform.eulerAngles.y;

		realPose.Angles = new Vector3 (Pitch, Yaw, Roll);

		realPose.Altitude = droneTransform.position.y - constProps.landedHeight;

		realPose.WorldVelocity = droneRb.linearVelocity;
		realPose.LocalVelocityVector = droneTransform.InverseTransformDirection (realPose.WorldVelocity);

		realPose.WorldAcceleration = (realPose.WorldVelocity - realPose.previousWorldVelocity)/Time.fixedDeltaTime;

		realPose.AngularVelocityVector = droneRb.angularVelocity;
		realPose.AngularVelocityVector = droneTransform.InverseTransformDirection (realPose.AngularVelocityVector);

		realPose.WorldPosition = droneTransform.transform.position;
		realPose.previousWorldVelocity = realPose.WorldVelocity;

    }

    public static void ResetConstProps(){
        constProps.Inertia = droneRb.inertiaTensor;
        constProps.Mass = droneRb.mass;
        constProps.landedHeight = droneRb.transform.position.y;
    }

    public static void ResetCollision(){
        collisionData.collisionCount = 0;
        collisionData.out_of_balance = false;
        collisionData.distances = new Vector3[16];
    }

    IEnumerator LaggedTransferInputCommand(){
        yield return new WaitForEndOfFrame();
    }
    IEnumerator LaggedTransferCameraCommand(){
        yield return new WaitForEndOfFrame();
    }
    IEnumerator LaggedTransferCameraImages(){   
        while(true){
            yield return new WaitForEndOfFrame();
            if(cameraImageReceived){
                storedFrames[currentFrameIndex % bufferSize].Capture(renderTexture);
                for ( ; storedFrames[renderedFrameBufferIndex].capturedTime < ( Time.time - cameraLatency ) ; renderedFrameBufferIndex = ( renderedFrameBufferIndex + 1 ) % bufferSize ) ;
                Graphics.Blit(storedFrames[renderedFrameBufferIndex].frameTexture, destRT);
                currentFrameIndex++;
                cameraImageReceived = false;
            }
        }
    }

}
