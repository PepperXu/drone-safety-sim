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
        All,
		ControlFirst,
        Mixed,
		SafetyFirst,
		TwoDimensionOnly,
    }

	public VisualizationCondition currentVisCondition = VisualizationCondition.TwoDimensionOnly;
	private VisualizationCondition currentBufferedVisCondition;

	string[] visConditionString = {"All", "Control First", "Mixed", "Safety First", "2D Only"};

	public static bool switching_flag = false;

    [SerializeField] private FlightPlanning flightPlanning;
	[SerializeField] private UIUpdater uIUpdater;
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

	public static bool isRecording {get; private set;}
	const string baseFileName = "log";
	static string filePath;

	public static string folderPath {get; private set;}

	static float expTimer;

	[SerializeField] private Camera vrCamera;
	[SerializeField] private LayerMask excludeMark, includeMark;

	int currentDebugMode = 0; //0: wind control (strength only), 1: battery control, 2: position control
    // Start is called before the first frame update
    void Start()
    {
		tcpListenerThread = new Thread (new ThreadStart(ListenForIncommingRequests)); 		
		tcpListenerThread.IsBackground = true; 		
		tcpListenerThread.Start(); 
		currentBufferedVisCondition = currentVisCondition;
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


		

		if(VelocityControl.currentFlightState == VelocityControl.FlightState.TakingOff && !isRecording){
			StartRecording();
			RecordData("Visualization Condition", visConditionString[(int)currentVisCondition] , "");
		}

		if(VelocityControl.currentFlightState == VelocityControl.FlightState.Landed && isRecording){
			StopRecording();
		}

		if(currentVisCondition != currentBufferedVisCondition){
			RecordData("Switch Visualization Condition to", visConditionString[(int)currentVisCondition] , "");
			currentBufferedVisCondition = currentVisCondition;
		}

		if(incomingMsgList.Count > 0)
			ProcessClientMessage();
        
		//For Debugging
		ProcessKeyboardInput();
		if(currentVisCondition == VisualizationCondition.TwoDimensionOnly){
			VisType.globalVisType = VisType.VisualizationType.None;
			vrCamera.cullingMask = excludeMark;
		} else {
			vrCamera.cullingMask = includeMark;
			if(currentVisCondition == VisualizationCondition.All || switching_flag){
				VisType.RevealHiddenVisType(false);
				VisType.globalVisType = VisType.VisualizationType.Both;
			}else {
				if(currentVisCondition == VisualizationCondition.Mixed){
					VisType.RevealHiddenVisType(true);
				} else {
					VisType.RevealHiddenVisType(false);
				}
				if(DroneManager.currentControlType == DroneManager.ControlType.Manual || DroneManager.currentMissionState == DroneManager.MissionState.Returning){
					VisType.globalVisType = VisType.VisualizationType.SafetyOnly;
				} else {
					if(currentVisCondition == VisualizationCondition.SafetyFirst){
						VisType.globalVisType = VisType.VisualizationType.Both;
					} else {
						VisType.globalVisType = VisType.VisualizationType.MissionOnly;
					}
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
        	    currentVisCondition = VisualizationCondition.TwoDimensionOnly;
        	}
        	if(Input.GetKeyDown(KeyCode.Alpha2)){
        	    currentVisCondition = VisualizationCondition.All;
        	}
        	if(Input.GetKeyDown(KeyCode.Alpha3)){
        	    currentVisCondition = VisualizationCondition.Mixed;
        	}
			if(Input.GetKeyDown(KeyCode.Tab)){
				currentDebugMode = (currentDebugMode + 1) % 4;
				string debugModeText = "";
				switch(currentDebugMode){
					case 0:
						debugModeText = "wind turbulence";
						break;
					case 1:
						debugModeText = "reduce battery";
						break;
					case 2:
						debugModeText = "gps instable";
						break;
					case 3:
						debugModeText = "facade config";
						break;
					default:
						break;
				}
				Debug.LogWarning("Current Debug Mode: " + debugModeText);
			}
			switch(currentDebugMode){
				case 0:
					if(Input.GetKeyDown(KeyCode.F1)){
						randomPulseNoise.strength_mean = 20f;
						randomPulseNoise.wind_change_flag = true;
        			}
        			if(Input.GetKeyDown(KeyCode.F2)){
        			    randomPulseNoise.strength_mean = 40f;
						randomPulseNoise.wind_change_flag = true;
        			}
        			if(Input.GetKeyDown(KeyCode.F3)){
        			    randomPulseNoise.strength_mean = 60f;
						randomPulseNoise.wind_change_flag = true;
        			}
        			if(Input.GetKeyDown(KeyCode.F4)){
        			    randomPulseNoise.strength_mean = 80f;
						randomPulseNoise.wind_change_flag = true;
        			}
					if(Input.GetKeyDown(KeyCode.F5)){
        			    randomPulseNoise.strength_mean = 100f;
						randomPulseNoise.wind_change_flag = true;
        			}
					break;
				case 1:
					if(Input.GetKeyDown(KeyCode.F1)){
						battery.ReduceBatteryCap(0.1f);
        			}
        			if(Input.GetKeyDown(KeyCode.F2)){
        			    battery.ReduceBatteryCap(0.2f);
        			}
        			if(Input.GetKeyDown(KeyCode.F3)){
        			    battery.ReduceBatteryCap(0.3f);
        			}
					break;
				case 2:
					if(Input.GetKeyDown(KeyCode.F1)){
						positionalSensorSimulator.SetGPSLost(false);
        			}
        			if(Input.GetKeyDown(KeyCode.F2)){
        			    positionalSensorSimulator.SetGPSLost(true);
        			}
        			//if(Input.GetKeyDown(KeyCode.F3)){
        			//    positionalSensorSimulator.SetSignalLevel(1);
        			//}
        			//if(Input.GetKeyDown(KeyCode.F4)){
        			//    positionalSensorSimulator.SetSignalLevel(0);
        			//}
					break;
				case 3:
					if(Input.GetKeyDown(KeyCode.F1)){
						flightPlanning.ConfigIndex = 0;
        			}
        			if(Input.GetKeyDown(KeyCode.F2)){
        			    flightPlanning.ConfigIndex = 1;
        			}
        			if(Input.GetKeyDown(KeyCode.F3)){
        			   flightPlanning.ConfigIndex = 2;
        			}
        			if(Input.GetKeyDown(KeyCode.F4)){
        			    flightPlanning.ConfigIndex = 3;
        			}
					break;
				default:
					break;
			}
			if(Input.GetKeyDown(KeyCode.X)){
				droneManager.ResetAllStates();
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
	/// <summary> 	
	/// Send message to client using socket connection. 	
	/// </summary> 	
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

	//private void SendFlightPlanningInfo(){
	//	string msg = "flight-planning;" + flightPlanning.GetCurrentStartingPointIndex() + "\n";
	//	//msgQueue.Enqueue(msg);
	//	msgString += msg;
	//}
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

	//private void SendIsTestRun(){
	//	string msg = "test-run;" + flightPlanning.GetIsTestRun() + "\n";
	//	//msgQueue.Enqueue(msg);
	//	msgString += msg;
	//}
//
	//private void SendIsFromTop(){
	//	string msg = "is-from-top;" + flightPlanning.GetIsFromTop() + "\n";
	//	//msgQueue.Enqueue(msg);
	//	msgString += msg;
	//}

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

	void StartRecording(){
		string folderName = baseFileName + "_" + (flightPlanning.ConfigIndex == 0?"training":"full") + "_config_" + flightPlanning.ConfigIndex + "_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
        folderPath = Application.persistentDataPath + "/" + folderName;
		Directory.CreateDirectory(folderPath);
		filePath = Application.persistentDataPath + "/"  + folderName + "/log.csv";
		using (StreamWriter writer = new StreamWriter(filePath, true)) {
			writer.WriteLine("Timestamp, Msg, Param, Param2");
		};
		expTimer = 0f;
        isRecording = true;
	}

	void StopRecording(){
		isRecording = false;
	}

	public static void RecordData(string logMsg, string param, string param2){
		if (isRecording)
        {
            using (StreamWriter writer = new StreamWriter(filePath, true)) {
				 writer.WriteLine(expTimer + "," + logMsg + "," + param + "," + param2);
				 Debug.Log("log entry generated: " + logMsg);
			};
		}
	}

}
