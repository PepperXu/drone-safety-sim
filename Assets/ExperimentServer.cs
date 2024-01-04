using System;
using System.Collections; 
using System.Collections.Generic; 
using System.Net; 
using System.Net.Sockets; 
using System.Text; 
using System.Threading;
using UnityEngine;  
using Unity.XR.CoreUtils;

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
        Manual,
		ManualProcedual,
        System,
		SystemProcedual
    }

	public static VisualizationCondition currentVisCondition = VisualizationCondition.Manual;




    [SerializeField] private FlightPlanning flightPlanning;
	[SerializeField] private UIUpdater uIUpdater;
	[SerializeField] private RandomPulseNoise randomPulseNoise;
	[SerializeField] private Battery battery;
	[SerializeField] private PositionalSensorSimulator positionalSensorSimulator;
	[SerializeField] private DroneManager droneManager;
	[SerializeField] private Transform droneParent;
	
    [SerializeField] private XROrigin xrOrigin;
	private string clientMessage = "";
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
		
		ProcessClientMessage();
        
		//For Debugging
		ProcessKeyboardInput();

		if(currentVisCondition == VisualizationCondition.System){
			if(DroneManager.currentControlType == DroneManager.ControlType.Manual || DroneManager.currentSystemState != DroneManager.SystemState.Healthy){
				VisType.globalVisType = VisType.VisualizationType.SafetyOnly;
			} else {
				VisType.globalVisType = VisType.VisualizationType.MissionOnly;
			}
		} else {
			if(currentVisCondition == VisualizationCondition.SystemProcedual){
				VisType.RevealHiddenVisType(true);
			}  else if (currentVisCondition == VisualizationCondition.Manual){
				VisType.RevealHiddenVisType(false);
			}
		}

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
        	    currentVisCondition = VisualizationCondition.Manual;
        	}
        	if(Input.GetKeyDown(KeyCode.Alpha2)){
        	    currentVisCondition = VisualizationCondition.ManualProcedual;
        	}
        	if(Input.GetKeyDown(KeyCode.Alpha3)){
        	    currentVisCondition = VisualizationCondition.System;
        	}
        	if(Input.GetKeyDown(KeyCode.Alpha4)){
        	    currentVisCondition = VisualizationCondition.SystemProcedual;
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
						int length; 						
						// Read incomming stream into byte arrary. 						
						while ((length = stream.Read(bytes, 0, bytes.Length)) != 0) { 							
							var incommingData = new byte[length]; 							
							Array.Copy(bytes, 0, incommingData, 0, length);  							
							// Convert byte array to string message. 							
							clientMessage = Encoding.ASCII.GetString(incommingData);
							
							Debug.Log("client message received as: " + clientMessage); 						
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
		if(clientMessage == "")
			return;
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
			case "positional-signal-level":
				positionalSensorSimulator.SetSignalLevel(int.Parse(splitMsg[1]));
				break;
			case "reset-all-state":
				droneManager.ResetAllStates();
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
	private new void SendMessage(string msg) { 		

		try {
			if(connectedTcpClient != null){
			// Get a stream object for writing. 			
				NetworkStream stream = connectedTcpClient.GetStream(); 			
				if (stream.CanWrite) {                 
					//string serverMessage = "This is a message from your server."; 			
					// Convert string message to byte array.                 
					byte[] serverMessageAsByteArray = Encoding.ASCII.GetBytes(msg); 				
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
			(int)DroneManager.currentSystemState + ";" + 
			(int)currentVisCondition + ";" + 
			battery.GetBatteryVoltageLevel() + ";" + 
			positionalSensorSimulator.GetSignalLevel() + ";" + 
			uIUpdater.GetDefectCount();
		SendMessage(currentState);
	}

	private void SendFlightPlanningInfo(){
		string currentBatPer = "flight-planning;" + flightPlanning.GetCurrentStartingPointIndex();
		SendMessage(currentBatPer);
	}
	private void SendDroneFlightStatus(){
		string currentBatPer = "drone-status;" + (int)DroneManager.currentFlightState;
		SendMessage(currentBatPer);
	}
	private void SendCurrentDronePose(){
		string currentDronePose = "drone-position;" + droneParent.position.x + ";" + droneParent.position.y + ";" + droneParent.position.z;
		SendMessage(currentDronePose);
	}

	private void SendBatteryPercentage(){
		string currentBatPer = "battery-percentage;" + battery.GetBatteryLevel();
		SendMessage(currentBatPer);
	}

	IEnumerator UpdateCurrentState(){
		while(true){
			SendCurrentState();
			SendFlightPlanningInfo();
			SendDroneFlightStatus();
			SendCurrentDronePose();
			SendBatteryPercentage();
			yield return new WaitForSeconds(0.5f);
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
