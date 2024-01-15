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
		SafetyFirst
    }

	public VisualizationCondition currentVisCondition = VisualizationCondition.All;

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
	Queue<string> msgQueue = new Queue<string>();
	Queue<string> incomingMsgQueue = new Queue<string>();
	int msgSendCounter = 0;

	int currentDebugMode = 0; //0: wind control (strength only), 1: battery control, 2: position control
    // Start is called before the first frame update
    void Start()
    {
		tcpListenerThread = new Thread (new ThreadStart(ListenForIncommingRequests)); 		
		tcpListenerThread.IsBackground = true; 		
		tcpListenerThread.Start(); 
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
		if(incomingMsgQueue.Count > 0)
			ProcessClientMessage();
        
		//For Debugging
		ProcessKeyboardInput();

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
					if(DroneManager.currentSafetyState != DroneManager.SafetyState.Healthy){
						VisType.globalVisType = VisType.VisualizationType.Both;
					}else {
						VisType.globalVisType = VisType.VisualizationType.MissionOnly;
					}
				} else {
					VisType.globalVisType = VisType.VisualizationType.MissionOnly;
				}
			}
		}

		if(msgQueue.Count > 0)
			SendMessageFromQueue();
    }

	void ProcessKeyboardInput(){

		if(DroneManager.currentMissionState == DroneManager.MissionState.Planning){
        	
        	if(Input.GetKeyDown(KeyCode.Alpha1)){
        	    flightPlanning.SetStartingPoint(1);
        	}
        	if(Input.GetKeyDown(KeyCode.Alpha2)){
        	    flightPlanning.SetStartingPoint(2);
        	}
        	if(Input.GetKeyDown(KeyCode.Alpha3)){
        	    flightPlanning.SetStartingPoint(3);
        	}
        	if(Input.GetKeyDown(KeyCode.Alpha4)){
        	    flightPlanning.SetStartingPoint(4);
        	}
        	if(Input.GetKeyDown(KeyCode.Alpha0)){
        	    flightPlanning.SetStartingPoint(0);
        	}
		} else {
			if(Input.GetKeyDown(KeyCode.Alpha1)){
        	    currentVisCondition = VisualizationCondition.All;
        	}
        	if(Input.GetKeyDown(KeyCode.Alpha2)){
        	    currentVisCondition = VisualizationCondition.ControlFirst;
        	}
        	if(Input.GetKeyDown(KeyCode.Alpha3)){
        	    currentVisCondition = VisualizationCondition.Mixed;
        	}
        	if(Input.GetKeyDown(KeyCode.Alpha4)){
        	    currentVisCondition = VisualizationCondition.SafetyFirst;
        	}
			if(Input.GetKeyDown(KeyCode.Tab)){
				currentDebugMode = (currentDebugMode + 1) % 3;
				Debug.LogWarning("Current Debug Mode: " + currentDebugMode);
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
						positionalSensorSimulator.SetSignalLevel(3);
        			}
        			if(Input.GetKeyDown(KeyCode.F2)){
        			    positionalSensorSimulator.SetSignalLevel(2);
        			}
        			if(Input.GetKeyDown(KeyCode.F3)){
        			    positionalSensorSimulator.SetSignalLevel(1);
        			}
        			if(Input.GetKeyDown(KeyCode.F4)){
        			    positionalSensorSimulator.SetSignalLevel(0);
        			}
					break;
				default:
					break;
			}
			if(Input.GetKeyDown(KeyCode.X)){
				droneManager.ResetAllStates();
			}
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
						do{
							incomingMsgQueue.Enqueue(sr.ReadLine());
						} while(sr.ReadLine() != null);		
					} 				
				} 			
			} 		
		} 		
		catch (SocketException socketException) { 			
			Debug.Log("SocketException " + socketException.ToString()); 		
		}     
	}

	private void ProcessClientMessage(){
		string clientMessage = incomingMsgQueue.Dequeue();
		string[] splitMsg = clientMessage.Split(';');
		switch(splitMsg[0]){
			case "starting-point":
				if(DroneManager.currentMissionState == DroneManager.MissionState.Planning){
					flightPlanning.SetStartingPoint(int.Parse(splitMsg[1]));
				}
				break;
			case "vis-condition":
				currentVisCondition = (VisualizationCondition) int.Parse(splitMsg[1]);
				break;
			case "wind-condition":
				randomPulseNoise.yawCenter = float.Parse(splitMsg[1]);
				randomPulseNoise.strength_mean = float.Parse(splitMsg[2]);
				randomPulseNoise.wind_change_flag = true;
				break;
			case "battery-voltage-level":
				battery.SetVoltageLevel(int.Parse(splitMsg[1]));
				break;
			case "reduce-battery-capacity":
				battery.ReduceBatteryCap(float.Parse(splitMsg[1]));
				break;
			case "positional-signal-level":
				positionalSensorSimulator.SetSignalLevel(int.Parse(splitMsg[1]));
				break;
			case "reset-all-states":
				droneManager.ResetAllStates();
				break;
			case "test-run":
				flightPlanning.SetIsTestRun(int.Parse(splitMsg[1]));
				break;
			case "is-from-top":
				flightPlanning.SetIsFromTop(int.Parse(splitMsg[1]));
				break;
			case "current-config":
				flightPlanning.SetCurrentFacadeConfig(int.Parse(splitMsg[1]));
				break;
			default:
				Debug.Log("Undefined Command: " + clientMessage);
				break;
		}
		clientMessage = "";
	}
	/// <summary> 	
	/// Send message to client using socket connection. 	
	/// </summary> 	
	private void SendMessageFromQueue() { 		
		try {
			if(connectedTcpClient != null){
			// Get a stream object for writing. 			
				NetworkStream stream = connectedTcpClient.GetStream();	
				if (stream.CanWrite) {
					// Convert string message to byte array.                 
					byte[] serverMessageAsByteArray = Encoding.ASCII.GetBytes(msgQueue.Dequeue());			
					// Write byte array to socketConnection stream.               
					stream.Write(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);               
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
			(int)DroneManager.currentSafetyState + ";" + 
			(int)currentVisCondition + ";" + 
			battery.GetBatteryVoltageLevel() + ";" + 
			positionalSensorSimulator.GetSignalLevel() + ";" + 
			uIUpdater.GetDefectCount() +  ";" + 
			(switching_flag?3:(int)VisType.globalVisType) + "\n";
		msgQueue.Enqueue(currentState);
	}

	private void SendFlightPlanningInfo(){
		string msg = "flight-planning;" + flightPlanning.GetCurrentStartingPointIndex() + "\n";
		msgQueue.Enqueue(msg);
	}
	private void SendDroneFlightStatus(){
		string msg = "drone-status;" + (int)DroneManager.currentFlightState + "\n";
		msgQueue.Enqueue(msg);
	}
	private void SendCurrentDronePose(){
		string msg = "drone-position;" + droneParent.position.x + ";" + droneParent.position.y + ";" + droneParent.position.z + "\n";
		msgQueue.Enqueue(msg);
	}

	private void SendBatteryPercentage(){
		string msg = "battery-percentage;" + battery.GetBatteryLevel() + "\n";
		msgQueue.Enqueue(msg);
	}

	private void SendWindStrength(){
		string msg = "wind-strength;" + randomPulseNoise.GetCurrentWindStrength() + "\n";
		msgQueue.Enqueue(msg);
	}

	private void SendIsTestRun(){
		string msg = "test-run;" + flightPlanning.GetIsTestRun() + "\n";
		msgQueue.Enqueue(msg);
	}

	private void SendIsFromTop(){
		string msg = "is-from-top;" + flightPlanning.GetIsFromTop() + "\n";
		msgQueue.Enqueue(msg);
	}

	private void SendConfiguration(){
		string msg = "current-config;" + flightPlanning.GetCurrentFacadeConfig() + "\n";
		msgQueue.Enqueue(msg);
	}

	IEnumerator UpdateCurrentState(){
		while(true){
			switch (msgSendCounter)
			{
				case 0:
					SendCurrentState();
					break;
				case 1:
					SendFlightPlanningInfo();
					break;
				case 2:
					SendDroneFlightStatus();
					break;
				case 3:
					SendCurrentDronePose();
					break;
				case 4:
					SendBatteryPercentage();
					break;
				case 5:
					SendWindStrength();
					break;
				case 6:
					SendIsTestRun();
					break;
				case 7:
					SendIsFromTop();
					break;
				case 8:
					SendConfiguration();
					break;
				default:
					break;
			}
			msgSendCounter++;
			if(msgSendCounter >= 9){
				msgSendCounter = 0;
			}
			yield return new WaitForEndOfFrame();
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


}
