using System;
using System.Collections; 
using System.Collections.Generic; 
using System.Net; 
using System.Net.Sockets; 
using System.Text; 
using System.Threading;
using UnityEngine;  
using Unity.XR.CoreUtils;
using System.IO;
using UnityEngine.UI;
using TMPro;
using System.Net.NetworkInformation;
using System.Linq;
using System.Globalization;

public class ExperimentServer : MonoBehaviour
{
	#region private members 	
	/// <summary> 	
	/// TCPListener to listen for incomming TCP connection 	
	/// requests. 	
	/// </summary> 	
	private TcpListener tcpListener;
	/// <summary> 
	/// Background thread for TcpServer workload. 	
	/// </summary> 	
	private Thread tcpListenerThread;
	/// <summary> 	
	/// Create handle to connected tcp client. 	
	/// </summary> 	
	private TcpClient connectedTcpClient;
	#endregion

	public enum VisualizationCondition
	{
		TwoDimensionOnly,
		All,
		//ControlFirst,
		Adaptive,
		//SafetyFirst,
	}

	private VisualizationCondition currentVisCondition = VisualizationCondition.TwoDimensionOnly;
	//private VisualizationCondition currentBufferedVisCondition;

	string[] visConditionString = { "2D Only", "All", "Adaptive" };

	public static bool switching_flag = false;

	[SerializeField] private FlightPlanning flightPlanning;
	//[SerializeField] private UIUpdater uIUpdater;
	[SerializeField] private RandomPulseNoise randomPulseNoise;
	[SerializeField] private Battery battery;
	[SerializeField] private PositionalSensorSimulator positionalSensorSimulator;
	[SerializeField] private DroneManager droneManager;
	[SerializeField] private Transform droneParent;

	[SerializeField] private XROrigin xrOrigin;
	//private string clientMessage = "";
	//Queue<string> msgQueue = new Queue<string>();
	string msgString;
	List<string> incomingMsgList = new List<string>();

	public static bool isRecording { get; private set; }
	const string baseFileName = "log";
	static string eventLogfilePath;
	static string fullLogFilePath;


	public static string folderPath { get; private set; }

	static float expTimer;

	[SerializeField] private Camera vrCamera;
	[SerializeField] private LayerMask Cam2DOnly, ARCam;
	[SerializeField] private GameObject MonitorUI, EXPUI;
	[SerializeField] private Toggle[] visConditionToggles, configToggles;

	string[] autopilotStatus = {"auto_nav", "auto_wait", "auto_return", "auto_off"};
	string[] flightStateString = {"landed", "taking off", "hovering", "navigating", "landing", "collided"};
	string[] configNames = { "basic_training", "interface_training", "config_1", "config_2", "config_3" };
	bool landed = true;

	int currentDebugMode = 0; //0: wind control (strength only), 1: battery control, 2: position control


	public static ConfigManager configManager;

	static int lastWaypointIndex = -1;
    [SerializeField] TextMeshPro result;

	//struct RecordedPoint
	//{
	//	public int closestWP;
    //    public Vector3 samplePoint;
    //    public RecordedPoint(int closestWP, Vector3 samplePoint)
	//	{
	//		this.closestWP = closestWP;
	//		this.samplePoint = samplePoint;
	//	}
	//}
	//private List<RecordedPoint> recordedPath = new List<RecordedPoint>();
	private float landedBattery;
	string pid = "test";
    // Start is called before the first frame update
    private void OnEnable()
    {
		DroneManager.logEvent.AddListener(LogResult);
    }
    private void OnDisable()
    {
        DroneManager.logEvent.RemoveListener(LogResult);
    }

    void Start()
    {
		//Networking: to be removed...
		tcpListenerThread = new Thread (new ThreadStart(ListenForIncommingRequests)); 		
		tcpListenerThread.IsBackground = true; 		
		tcpListenerThread.Start(); 


		//currentBufferedVisCondition = currentVisCondition;
		isRecording = false;
		StartCoroutine(UpdateCurrentState());
		StartCoroutine(DelayedInitializeTrackingOriginMode());
    }

	IEnumerator DelayedInitializeTrackingOriginMode(){
		yield return new WaitForSecondsRealtime(3f);
		xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
	}

    // Update is called once per frame
    void Update()
    {
		if(isRecording)
			expTimer += Time.deltaTime;
		
		

        //if (VelocityControl.currentFlightState == VelocityControl.FlightState.TakingOff && !isRecording){
		//	//StartRecording();
		//	
		//}

		//if(VelocityControl.currentFlightState == VelocityControl.FlightState.Landed && isRecording){
		//	StopRecording();
		//}
//
		//if(currentVisCondition != currentBufferedVisCondition){
		//	RecordData("Switch Visualization Condition to", visConditionString[(int)currentVisCondition] , "");
		//	currentBufferedVisCondition = currentVisCondition;
		//}

		if(incomingMsgList.Count > 0)
			ProcessClientMessage();
        
		//For Debugging
		ProcessKeyboardInput();
		if(currentVisCondition == VisualizationCondition.TwoDimensionOnly){
			VisType.globalVisType = VisType.VisualizationType.TwoDOnly;
			vrCamera.cullingMask = Cam2DOnly;
		} else {
			vrCamera.cullingMask = ARCam;
			if(currentVisCondition == VisualizationCondition.All || switching_flag){
				VisType.RevealHiddenVisType(false);
				VisType.globalVisType = VisType.VisualizationType.Both;
			}else {
				VisType.RevealHiddenVisType(true);
				if(DroneManager.currentControlType == DroneManager.ControlType.Manual || DroneManager.currentMissionState == DroneManager.MissionState.Returning){
					VisType.globalVisType = VisType.VisualizationType.SafetyOnly;
				} else {
                    VisType.globalVisType = VisType.VisualizationType.MissionOnly;
                }
			}
		}
		
    }

	void ProcessKeyboardInput(){

		if(Input.GetKeyDown(KeyCode.Space)){
			Debug.Log(msgString);
		}
		if(DroneManager.currentMissionState == DroneManager.MissionState.Planning){
        	
        	//if(Input.GetKeyDown(KeyCode.Alpha1)){
        	//    flightPlanning.SetStartingPoint(1);
        	//}
        	//if(Input.GetKeyDown(KeyCode.Alpha2)){
        	//    flightPlanning.SetStartingPoint(2);
        	//}
        	//if(Input.GetKeyDown(KeyCode.Alpha3)){
        	//    flightPlanning.SetStartingPoint(3);
        	//}
        	//if(Input.GetKeyDown(KeyCode.Alpha4)){
        	//    flightPlanning.SetStartingPoint(4);
        	//}
        	//if(Input.GetKeyDown(KeyCode.Alpha0)){
        	//    flightPlanning.SetStartingPoint(0);
        	//}
		} else {
			if(Input.GetKeyDown(KeyCode.Alpha1)){
				UpdateVisCondition(0);
        	}
        	if(Input.GetKeyDown(KeyCode.Alpha2)){
				UpdateVisCondition(1);

            }
        	if(Input.GetKeyDown(KeyCode.Alpha3)){
        	    UpdateVisCondition(2);
        	}
            //if (Input.GetKeyDown(KeyCode.F1))
            //{
            //    UpdateConfig(0);
            //}
            //if (Input.GetKeyDown(KeyCode.F2))
            //{
            //    UpdateConfig(1);
            //}
            //if (Input.GetKeyDown(KeyCode.F3))
            //{
            //    UpdateConfig(2);
            //}
            //if (Input.GetKeyDown(KeyCode.F4))
            //{
            //    UpdateConfig(3);
            //}
            //if (Input.GetKeyDown(KeyCode.F5))
            //{
            //    UpdateConfig(4);
            //}
            //if (Input.GetKeyDown(KeyCode.F6))
            //{
            //    UpdateConfig(5);
            //}
            //if (Input.GetKeyDown(KeyCode.Tab)){
			//	currentDebugMode = (currentDebugMode + 1) % 4;
			//	string debugModeText = "";
			//	switch(currentDebugMode){
			//		case 0:
			//			debugModeText = "wind turbulence";
			//			break;
			//		case 1:
			//			debugModeText = "reduce battery";
			//			break;
			//		case 2:
			//			debugModeText = "gps instable";
			//			break;
			//		case 3:
			//			debugModeText = "facade config";
			//			break;
			//		default:
			//			break;
			//	}
			//	Debug.LogWarning("Current Debug Mode: " + debugModeText);
			//}
			//switch(currentDebugMode){
			//	case 0:
			//		if(Input.GetKeyDown(KeyCode.F1)){
			//			randomPulseNoise.strength_mean = 20f;
			//			randomPulseNoise.wind_change_flag = true;
        	//		}
        	//		if(Input.GetKeyDown(KeyCode.F2)){
        	//		    randomPulseNoise.strength_mean = 40f;
			//			randomPulseNoise.wind_change_flag = true;
        	//		}
        	//		if(Input.GetKeyDown(KeyCode.F3)){
        	//		    randomPulseNoise.strength_mean = 60f;
			//			randomPulseNoise.wind_change_flag = true;
        	//		}
        	//		if(Input.GetKeyDown(KeyCode.F4)){
        	//		    randomPulseNoise.strength_mean = 80f;
			//			randomPulseNoise.wind_change_flag = true;
        	//		}
			//		if(Input.GetKeyDown(KeyCode.F5)){
        	//		    randomPulseNoise.strength_mean = 100f;
			//			randomPulseNoise.wind_change_flag = true;
        	//		}
			//		break;
			//	case 1:
			//		if(Input.GetKeyDown(KeyCode.F1)){
			//			battery.ReduceBatteryCap(0.1f);
        	//		}
        	//		if(Input.GetKeyDown(KeyCode.F2)){
        	//		    battery.ReduceBatteryCap(0.2f);
        	//		}
        	//		if(Input.GetKeyDown(KeyCode.F3)){
        	//		    battery.ReduceBatteryCap(0.3f);
        	//		}
			//		break;
			//	case 2:
			//		if(Input.GetKeyDown(KeyCode.F1)){
			//			positionalSensorSimulator.SetGPSLost(false);
        	//		}
        	//		if(Input.GetKeyDown(KeyCode.F2)){
        	//		    positionalSensorSimulator.SetGPSLost(true);
        	//		}
        	//		//if(Input.GetKeyDown(KeyCode.F3)){
        	//		//    positionalSensorSimulator.SetSignalLevel(1);
        	//		//}
        	//		//if(Input.GetKeyDown(KeyCode.F4)){
        	//		//    positionalSensorSimulator.SetSignalLevel(0);
        	//		//}
			//		break;
			//	case 3:
			//		
			//		break;
			//	default:
			//		break;
			//}
			if(Input.GetKeyDown(KeyCode.X)){
				ResetExperiment();
			}
			//if(Input.GetKeyDown(KeyCode.V)){
			//	flightPlanning.SetIsFromTop(1);
			//}
			//if(Input.GetKeyDown(KeyCode.B)){
			//	flightPlanning.SetIsFromTop(0);
			//}
			//if(Input.GetKeyDown(KeyCode.Z)){
			//	flightPlanning.SetIsTestRun((flightPlanning.GetIsTestRun() + 1)%2);
			//}
		}
	}
	public void SetPID(string pid)
	{
		this.pid = pid;
	}

	public void ResetExperiment()
	{
		StopRecording();
        //recordedPath.Clear();
        droneManager.ResetAllStates();
		MonitorUI.SetActive(false);
		EXPUI.SetActive(true);
    }

	public void StartExperiment()
	{
        droneManager.ResetAllStates();
        MonitorUI.SetActive(true);
        EXPUI.SetActive(false);
		StartRecording();
    }

	public void UpdateVisCondition(int index)
	{
		currentVisCondition = (VisualizationCondition)index;
		for(int i = 0; i < visConditionToggles.Length; i++)
		{
            visConditionToggles[i].SetIsOnWithoutNotify(index == i);
        }
    }

    public void UpdateConfig(int index)
    {
        flightPlanning.ConfigIndex = index;
        for (int i = 0; i < configToggles.Length; i++)
        {
			configToggles[i].SetIsOnWithoutNotify(index == i);
        }
    }
#region Networking
	//Network Connection Deprecated: Will be removed
    private void ListenForIncommingRequests () { 		
		try { 			
			// Create listener on localhost port 8052. 			
			tcpListener = new TcpListener(IPAddress.Any, 8052);
			tcpListener.Start();              
			Debug.Log("Server is listening");              
			Byte[] bytes = new Byte[1024];  			
			while (true) { 				
				using (connectedTcpClient = tcpListener.AcceptTcpClient()) { 					
					// Get a stream object for reading 					
					using (NetworkStream stream = connectedTcpClient.GetStream()) { 						
						StreamReader sr = new StreamReader(stream);
						while(!sr.EndOfStream){
							incomingMsgList.Add(sr.ReadLine());
						}
					} 				
				} 			
			} 		
		} 		
		catch (SocketException socketException) { 			
			Debug.Log("SocketException " + socketException.ToString()); 		
		}     
	}

	private void ProcessClientMessage(){
		foreach(string incomingMsg in incomingMsgList){
			string clientMessage = incomingMsg;
			if(clientMessage == null)
				break;
			Debug.Log("Processing Client Message: " + clientMessage);
			string[] splitMsg = clientMessage.Split(';');
			switch(splitMsg[0]){
				case "vis-condition":
					currentVisCondition = (VisualizationCondition) int.Parse(splitMsg[1]);
					break;
				case "wind-condition":
					randomPulseNoise.yawCenter = float.Parse(splitMsg[1]);
					randomPulseNoise.strength_mean = float.Parse(splitMsg[2]);
					randomPulseNoise.wind_change_flag = true;
					break;
				case "reduce-battery-capacity":
					battery.ReduceBatteryCap(float.Parse(splitMsg[1]));
					break;
				case "reset-all-states":
					droneManager.ResetAllStates();
					SendServerMessage("state-reset-confirmed;");
					break;
				case "test-run":
					//flightPlanning.SetIsTestRun(int.Parse(splitMsg[1]));
					break;
				case "is-from-top":
					//flightPlanning.SetIsFromTop(int.Parse(splitMsg[1]));
					break;
				case "current-config":
					flightPlanning.ConfigIndex = int.Parse(splitMsg[1]);
					break;
				default:
					Debug.Log("Undefined Command: " + clientMessage);
					break;
			}
		}
		incomingMsgList.Clear();
	}

	private void SendServerMessage(string msg) { 	
		try {
			if(connectedTcpClient != null){
			// Get a stream object for writing. 			
				NetworkStream stream = connectedTcpClient.GetStream();	
				if (stream.CanWrite) {
					// Convert string message to byte array.                 
					//byte[] serverMessageAsByteArray = Encoding.ASCII.GetBytes(msgQueue.Dequeue());			
					byte[] serverMessageAsByteArray = Encoding.ASCII.GetBytes(msg);
					
					// Write byte array to socketConnection stream.               
					stream.Write(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);               
					msgString = "";
					Debug.Log("Server sent his message - should be received by client");   
				}  
			}     
		} 		
		catch (SocketException socketException) {             
			Debug.Log("Socket exception: " + socketException);         
		}
		catch(ObjectDisposedException e){
			Debug.Log("Object Disposed Exception: " + e);   
		}
	}

	private void SendCurrentState(){
		string currentState = "current-state;" + 
			(int)DroneManager.currentMissionState + ";" + 
			(int)DroneManager.currentControlType + ";" + 
			(int)currentVisCondition + ";" + 
			(switching_flag ? 3:(int)VisType.globalVisType) + "\n";
		//msgQueue.Enqueue(currentState);
		msgString += currentState;
	}


	private void SendDroneFlightStatus(){
		string msg = "drone-status;" + (int)VelocityControl.currentFlightState + "\n";
		//msgQueue.Enqueue(msg);
		msgString += msg;
	}
	private void SendCurrentDronePose(){
		string msg = "drone-position;" + droneParent.position.x + ";" + droneParent.position.y + ";" + droneParent.position.z + "\n";
		//msgQueue.Enqueue(msg);
		msgString += msg;
	}

	private void SendBatteryPercentage(){
		string msg = "battery-percentage;" + Communication.battery.batteryPercentage + "\n";
		//msgQueue.Enqueue(msg);
		msgString += msg;
	}

	private void SendWindStrength(){
		string msg = "wind-strength;" + randomPulseNoise.GetCurrentWindStrength() + "\n";
		//msgQueue.Enqueue(msg);
		msgString += msg;
	}



	private void SendConfiguration(){
		string msg = "current-config;" + flightPlanning.ConfigIndex + "\n";
		//msgQueue.Enqueue(msg);
		msgString += msg;
	}

	IEnumerator UpdateCurrentState(){
		while(true){
			msgString = "";
			SendCurrentState();
			SendDroneFlightStatus();
			SendCurrentDronePose();
			SendBatteryPercentage();
			SendWindStrength();
			SendConfiguration();

			SendServerMessage(msgString);
			yield return new WaitForSeconds(0.1f);
		}
	}

	private void OnApplicationQuit() {
		try{
			tcpListener.Stop();
		}
		catch(Exception e){
			Debug.Log(e.Message);
		}
	}
#endregion
	void StartRecording(){
		string folderName = baseFileName + "_" + configNames[flightPlanning.ConfigIndex] + "_" + visConditionString[(int)currentVisCondition] + "_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
        folderPath = Application.persistentDataPath + "/" + pid + "/" + folderName;
		Directory.CreateDirectory(folderPath);
		eventLogfilePath = folderPath + "/log_event.csv";
        fullLogFilePath = folderPath + "/log_full.csv";
        using (StreamWriter writer = new StreamWriter(eventLogfilePath, true)) {
			writer.WriteLine("Timestamp, Msg, DronePos, Param1, Param2");
		};
		using (StreamWriter writer = new StreamWriter(fullLogFilePath, true))
		{
			writer.WriteLine("Timestamp, DronePos, NearestWaypointPos, NearestWaypointIndex, ControlMode, FlightState, CollisionStatus, BatteryStatus, GPSStatus");
			
		};
		expTimer = 0f;
        isRecording = true;
		lastWaypointIndex = -1;
		RecordEventData("Current condition", configNames[flightPlanning.ConfigIndex], visConditionString[(int)currentVisCondition]);
        //recordedPath.Clear();
        StartCoroutine(RecordFullLog());
	}

	void StopRecording(){
		isRecording = false;
	}

	public static void RecordEventData(string logMsg, string param1, string param2){
		if (isRecording)
        {
            using (StreamWriter writer = new StreamWriter(eventLogfilePath, true)) {
				 writer.WriteLine(expTimer.ToString(CultureInfo.InvariantCulture) + "," + logMsg + "," + 
					 Communication.realPose.WorldPosition.x.ToString(CultureInfo.InvariantCulture) + "|" + Communication.realPose.WorldPosition.y.ToString(CultureInfo.InvariantCulture) + "|" + Communication.realPose.WorldPosition.z.ToString(CultureInfo.InvariantCulture) + "," + param1 + "," + param2 );
				 Debug.Log("log entry generated: " + logMsg);
			};
		}
	}

	IEnumerator RecordFullLog()
	{
		while (isRecording)
		{
			if(!landed){
                if (VelocityControl.currentFlightState == VelocityControl.FlightState.Landed)
                {
                    landed = true;
                }
				if (!landed)
				{
					string currentControlState = DroneManager.currentControlType == DroneManager.ControlType.Manual ? (InputControl.inputStatus == InputControl.InputStatus.Idle ? "idle" : "manual") : autopilotStatus[(int)AutopilotManager.autopilotStatus];
					string currentBatteryState = Communication.battery.batteryState == "Critical" ? "Critical" : (Communication.battery.rth ? "RTH" : Communication.battery.batteryState);
					string currentFlightState = flightStateString[(int)VelocityControl.currentFlightState];
					using (StreamWriter writer = new StreamWriter(fullLogFilePath, true))
					{

						writer.WriteLine(expTimer.ToString(CultureInfo.InvariantCulture) + "," +
							Communication.realPose.WorldPosition.x.ToString(CultureInfo.InvariantCulture) + "|" + Communication.realPose.WorldPosition.y.ToString(CultureInfo.InvariantCulture) + "|" + Communication.realPose.WorldPosition.z.ToString(CultureInfo.InvariantCulture) + "," +
							Communication.positionData.nearestWaypoint.x.ToString(CultureInfo.InvariantCulture) + "|" + Communication.positionData.nearestWaypoint.y.ToString(CultureInfo.InvariantCulture) + "|" + Communication.positionData.nearestWaypoint.z.ToString(CultureInfo.InvariantCulture) + "," + Communication.positionData.nearestWaypointIndex.ToString(CultureInfo.InvariantCulture) + "," +
							currentControlState + "," + currentFlightState + "," + Communication.collisionData.collisionStatus + "," +
							currentBatteryState + "," + Communication.positionData.sigLevel);
						//if (Communication.positionData.nearestWaypointIndex >= 0)
						//{
						//	recordedPath.Add(new RecordedPoint(Communication.positionData.nearestWaypointIndex, Communication.realPose.WorldPosition));
						//}
						landedBattery = Communication.battery.batteryPercentage;
					};
					lastWaypointIndex = Math.Max(lastWaypointIndex, Communication.positionData.nearestWaypointIndex);
				}
			} else {
				if (VelocityControl.currentFlightState != VelocityControl.FlightState.Landed)
				{
					landed = false;
					
                    result.gameObject.SetActive(false);
                }
			}
            yield return new WaitForFixedUpdate();
		}
	}

	void LogResult()
	{
        if (lastWaypointIndex > 0)
        {
			StartCoroutine(LogResultDelayed());
        }
    }

	IEnumerator LogResultDelayed()
	{
		yield return new WaitForSeconds(0.3f);
        List<Vector3> recordPath = new List<Vector3>();
        float[] waypointDistances = new float[lastWaypointIndex + 1];
        for (int i = 0; i < lastWaypointIndex + 1; i++)
        {
            waypointDistances[i] = float.MaxValue;
        }


		//foreach(RecordedPoint point in recordedPath) {
        //    waypointDistances[point.closestWP] = Mathf.Min(Vector3.Distance(point.samplePoint, Communication.waypoints[point.closestWP].transform.position), waypointDistances[point.closestWP]);
        //}
        using (StreamReader reader = new StreamReader(fullLogFilePath))
        {
            reader.ReadLine();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] values = line.Split(',');
                int closestWpIndex = int.Parse(values[3]);
                if (closestWpIndex >= 0)
                {
                    string[] coordSplit = values[1].Split("|");
                    Vector3 position = new Vector3(float.Parse(coordSplit[0]), float.Parse(coordSplit[1]), float.Parse(coordSplit[2]));
                    recordPath.Add(position);
                    waypointDistances[closestWpIndex] = Mathf.Min(Vector3.Distance(position, Communication.waypoints[closestWpIndex].transform.position), waypointDistances[closestWpIndex]);
                }
            }
        }

        for (int i = 0; i < lastWaypointIndex + 1; i++)
        {
            if (waypointDistances[i] > 9999f)
            {
                Vector3 wpPosition = Communication.waypoints[i].transform.position;
                foreach (Vector3 sample in recordPath)
                {
                    waypointDistances[i] = Mathf.Min(Vector3.Distance(sample, wpPosition), waypointDistances[i]);
                }
            }
        }

        float avgDistance = waypointDistances.Average();
        Debug.Log(configManager.markedDefect.Count + "/" + configManager.totalDefectCount + ", " + avgDistance);
        
        RecordEventData("Result", "Defect Marked: " + configManager.markedDefect.Count + "/" + configManager.totalDefectCount + ", Path Deviation: " + avgDistance.ToString(CultureInfo.InvariantCulture), lastWaypointIndex.ToString());
        
		if(flightPlanning.ConfigIndex < 2){
			result.text = "Defect Marked: " + configManager.markedDefect.Count + "/" + configManager.totalDefectCount + ", Path Deviation: " + avgDistance + ", Landed Battery: " + landedBattery;
		} else {
			result.text = "Landed Battery: " + landedBattery;
		}
		result.gameObject.SetActive(true);
		//recordedPath.Clear();

    }

}
