using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;
public class ExperimentMonitor : MonoBehaviour
{
    #region private members 	
	private TcpClient socketConnection; 	
	private Thread clientReceiveThread; 	
	#endregion  	

    string serverIp = "127.0.0.1";
	[SerializeField] private TextMeshProUGUI flightStateText, missionStateText, controlTypeText, systemStateText;
	[SerializeField] private TMP_InputField ipInputField;
	string serverMessage = "";
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
				flightStateText.SetText(splitMsg[1]);
				missionStateText.SetText(splitMsg[2]);
				controlTypeText.SetText(splitMsg[3]);
				systemStateText.SetText(splitMsg[4]);
				break;
			default:
				Debug.Log("Undefined Header: " + serverMessage);
				break;
		}
		serverMessage = "";
	}
	/// <summary> 	
	/// Send message to server using socket connection. 	
	/// </summary> 	
	private new void SendMessage(string messageContent) {         
		if (socketConnection == null) {             
			return;         
		}  		
		try { 			
			// Get a stream object for writing. 			
			NetworkStream stream = socketConnection.GetStream(); 			
			if (stream.CanWrite) {                 
				string clientMessage = messageContent; 				
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
		SendMessage("starting-point;" + i);
	}

	public void SetServerIp(){
		serverIp = ipInputField.text;
		PlayerPrefs.SetString("server-ip", serverIp);
	}

	private void InitializeIpInputField(){
		ipInputField.text = serverIp;
	}
}
