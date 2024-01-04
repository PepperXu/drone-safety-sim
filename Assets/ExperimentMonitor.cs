using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
public class ExperimentMonitor : MonoBehaviour
{
    #region private members 	
	private TcpClient socketConnection; 	
	private Thread clientReceiveThread; 	
	#endregion  	

    string serverIp = "127.0.0.1";
	[SerializeField] private TextMeshProUGUI flightStateText, missionStateText, controlTypeText, systemStateText, visCondiText, batteryPercentageText, batteryVoltageText, positionalSignalStatusText, defectCountText;
	[SerializeField] private TMP_InputField ipInputField;

	[SerializeField] private Button[] startingPoints;
	
	private int currentStartingPointIndex = 0;
	private Vector3 currentDronePosition;
	private float currentBatteryPercentage;
	private int currentDroneStatus;
	[SerializeField] private Transform droneParent;
	string serverMessage = "";

	string[] visConditionString = {"Manual", "Manual Procedual", "System", "System Procedual"};
	string[] posStatusString = {"Signal Lost", "Unstable Connection", "Position Offset", "Normal"};

	string[] flightStateString = {"Landed", "Taking Off", "Hovering", "Navigating", "Landing"};
    string[] missionStateString = {"Planning", "Moving to Flight Zone", "In Flight Zone", "Inspecting", "Interrupted", "Returning"};
    string[] systemStateString = {"Healthy", "Caution", "Warning", "Emergency"};
    string[] controlStateString = {"Auto", "Manual"};

	bool isRecording = false;
	string baseFileName = "exp_recording";
	string filePath;
	int[] statusArray = new int[7];

	private const float normalBatteryVoltage = 11.4f;
    private const float voltageDropPerLevel = 1f;

	private float expTimer = 0f;

	Queue<string> msgQueue = new Queue<string>();
	// Use this for initialization 	
	private void Awake() {
		serverIp = PlayerPrefs.GetString("server-ip");
		InitializeIpInputField();
	}
	void Start () {
		ConnectToTcpServer();     
	}  	
	// Update is called once per frame
	void Update () {
		ProcessReceivedMessage();
		droneParent.position = currentDronePosition;
		if(msgQueue.Count > 0)
			SendMessageFromQueue();
		if(isRecording)
			expTimer += Time.deltaTime;
	}  	
	/// <summary> 	
	/// Setup socket connection. 	
	/// </summary> 	
	public void ConnectToTcpServer () { 
		if(clientReceiveThread != null){
			clientReceiveThread.Abort();
		}		
		try {  			
			clientReceiveThread = new Thread (new ThreadStart(ListenForData)); 			
			clientReceiveThread.IsBackground = true; 			
			clientReceiveThread.Start();  		
		} 		
		catch (Exception e) { 			
			Debug.Log("On client connect exception " + e); 		
		} 	
	}  	
	/// <summary> 	
	/// Runs in background clientReceiveThread; Listens for incomming data. 	
	/// </summary>     
	private void ListenForData() { 		
		try { 			
			socketConnection = new TcpClient(serverIp, 8052);
			
			Byte[] bytes = new Byte[1024];
			while (true) { 				
				// Get a stream object for reading 				
				using (NetworkStream stream = socketConnection.GetStream()) { 					
					int length; 					
					// Read incomming stream into byte arrary. 					
					while ((length = stream.Read(bytes, 0, bytes.Length)) != 0) { 						
						var incommingData = new byte[length]; 						
						Array.Copy(bytes, 0, incommingData, 0, length); 						
						// Convert byte array to string message. 						
						serverMessage = Encoding.ASCII.GetString(incommingData);	
						//ProcessReceivedMessage(serverMessage);			
						Debug.Log("server message received as: " + serverMessage); 					
					} 				
				} 			
			}         
		}         
		catch (SocketException socketException) {             
			Debug.Log("Socket exception: " + socketException);         
		}     
	}  	


	private void ProcessReceivedMessage(){
		if(serverMessage == "")
			return;
		string[] splitMsg = serverMessage.Split(';');
		switch(splitMsg[0]){
			case "current-state":
				int[] currentStatus = {int.Parse(splitMsg[1]), int.Parse(splitMsg[2]), int.Parse(splitMsg[3]), int.Parse(splitMsg[4]), 
				int.Parse(splitMsg[5]), int.Parse(splitMsg[6]), int.Parse(splitMsg[7])};
				UpdateStatusText(currentStatus);
				break;
			case "flight-planning":
				int startingPoint = int.Parse(splitMsg[1]);
				if(startingPoint != currentStartingPointIndex){
					currentStartingPointIndex = startingPoint;
					UpdateStartingPointsVisual();
				}
				break;
			case "drone-status":
				currentDroneStatus = int.Parse(splitMsg[1]);
				flightStateText.SetText(flightStateString[currentDroneStatus]);
				if(currentDroneStatus != 0 && !isRecording){
					StartRecording();
				} else if(currentDroneStatus == 0){
					StopRecording();
				}
				break;
			case "drone-position":
				if(currentDroneStatus == 0)
					currentDronePosition = Vector3.zero;
				else
					currentDronePosition = new Vector3(float.Parse(splitMsg[1]), float.Parse(splitMsg[2]), float.Parse(splitMsg[3]));
				break;
			case "battery-percentage":
				currentBatteryPercentage = float.Parse(splitMsg[1]);
				batteryPercentageText.SetText(((int) ((currentBatteryPercentage- 0.2f)/ 0.8f * 100f)) + "%");
				break;
			default:
				Debug.Log("Undefined Header: " + serverMessage);
				break;
		}
		serverMessage = "";
	}

	private void UpdateStatusText(int[] currentStatus){
		if(currentStatus == statusArray)
			return;
		statusArray = currentStatus;
		missionStateText.SetText(missionStateString[statusArray[0]]);
		controlTypeText.SetText(controlStateString[statusArray[1]]);
		systemStateText.SetText(systemStateString[statusArray[2]]);
		visCondiText.SetText(visConditionString[statusArray[3]]);
		batteryVoltageText.SetText((int) (normalBatteryVoltage - (3 - statusArray[4]) * voltageDropPerLevel) * 10f / 10f + "V");
		positionalSignalStatusText.SetText(posStatusString[statusArray[5]]);
		defectCountText.SetText(statusArray[6] + "");
		if(isRecording)
			RecordData(expTimer + "," + missionStateString[statusArray[0]] + "," +
			controlStateString[statusArray[1]]+ "," +
			systemStateString[statusArray[2]] + "," +
			visConditionString[statusArray[3]] + "," +
			((int) (normalBatteryVoltage - (3 - statusArray[4]) * voltageDropPerLevel * 10f)) / 10f + "V" + "," +
			posStatusString[statusArray[5]] + "," +
			statusArray[6] + "," + 
			currentDronePosition.x + "," + currentDronePosition.y + "," + currentDronePosition.z + "," + 
			currentBatteryPercentage
			);
	}
	/// <summary> 	
	/// Send message to server using socket connection. 	
	/// </summary> 	
	private void SendMessageFromQueue() {         
		if (socketConnection == null) {             
			return;         
		}  		
		try { 			
			// Get a stream object for writing. 			
			NetworkStream stream = socketConnection.GetStream(); 			
			if (stream.CanWrite) {
				string clientMessage = msgQueue.Dequeue();
				// Convert string message to byte array.                 
				byte[] clientMessageAsByteArray = Encoding.ASCII.GetBytes(clientMessage); 				
				// Write byte array to socketConnection stream.                 
				stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);                 
				Debug.Log("Client sent his message - should be received by server");         
			}         
		} 		
		catch (SocketException socketException) {             
			Debug.Log("Socket exception: " + socketException);         
		}     
	}

	public void SetStartingPoint(int i){
		msgQueue.Enqueue("starting-point;" + i);
		currentStartingPointIndex = i;
		UpdateStartingPointsVisual();
	}

	void UpdateStartingPointsVisual(){
		for(int t = 0; t < startingPoints.Length; t++){
			if(t == currentStartingPointIndex){
				startingPoints[t].transform.GetChild(1).gameObject.SetActive(true);
			} else {
				startingPoints[t].transform.GetChild(1).gameObject.SetActive(false);
			}
		}
	}



	public void SetVisCondition(int i){
		msgQueue.Enqueue("vis-condition;" + i);
	}

	public void SendWindCondition(float direction, float strength){
		msgQueue.Enqueue("wind-condition;" + direction + ";" + strength);
	}

	public void SendPositoinalSignalLevel(int level){
		msgQueue.Enqueue("positional-signal-level;" + level);
	}

	public void SendBatteryVoltageLevel(Slider slider){
		msgQueue.Enqueue("battery-voltage-level;" + slider.value);
	}

	public void ResetAllStates(){
		msgQueue.Enqueue("reset-all-states");
	}

	public void SetServerIp(){
		serverIp = ipInputField.text;
		PlayerPrefs.SetString("server-ip", serverIp);
	}

	private void InitializeIpInputField(){
		ipInputField.text = serverIp;
	}


	
	void StartRecording(){
		string fileName = baseFileName + "_starting+point_" + currentStartingPointIndex + "_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
        filePath = Application.persistentDataPath + "/" + fileName + ".csv";
		using (StreamWriter writer = new StreamWriter(filePath, true)) {
			writer.WriteLine("Timestamp, Mission State, Control State, SystemState, Visualization Condition, Battery Voltage, "+
			"Positional Signal State, Defect Count, Drone Position X, Drone Position Y, Drone Position Z, Battery Percentage");
		};
		expTimer = 0f;
        isRecording = true;
	}

	void StopRecording(){
		isRecording = false;
	}

	void RecordData(string line){
		if (isRecording)
        {
            using (StreamWriter writer = new StreamWriter(filePath, true)) {
				 writer.WriteLine(line);
			};
			
		}
	}

	private void OnApplicationQuit() {
		try{
			socketConnection.Close();
		}
		catch(Exception e){
			Debug.Log(e.Message);
		}
	}
}
