using System;
using System.Collections; 
using System.Collections.Generic; 
using System.Net; 
using System.Net.Sockets; 
using System.Text; 
using System.Threading;
using UnityEngine;  

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
    [SerializeField] private FlightPlanning flightPlanning;
	[SerializeField] private UIUpdater uIUpdater;
    // Start is called before the first frame update
    void Start()
    {
		tcpListenerThread = new Thread (new ThreadStart(ListenForIncommingRequests)); 		
		tcpListenerThread.IsBackground = true; 		
		tcpListenerThread.Start(); 
		StartCoroutine(UpdateCurrentState());	       
    }

    // Update is called once per frame
    void Update()
    {
        if(DroneManager.currentMissionState != DroneManager.MissionState.Planning)
            return;

        if(!flightPlanning.isPathPlanned()){
            flightPlanning.SetStartingPoint(4);
        }

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
							string clientMessage = Encoding.ASCII.GetString(incommingData);
							ProcessClientMessage(clientMessage);
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

	private void ProcessClientMessage(string msg){
		if(msg == "")
			return;
		string[] splitMsg = msg.Split(';');
		switch(splitMsg[0]){
			case "starting-point":
				flightPlanning.SetStartingPoint(int.Parse(splitMsg[1]));
				break;
			default:
				Debug.Log("Undefined Command: " + msg);
				break;
		}
	}
	/// <summary> 	
	/// Send message to client using socket connection. 	
	/// </summary> 	
	private new void SendMessage(string msg) { 		
		try { 			
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
		catch (SocketException socketException) {             
			Debug.Log("Socket exception: " + socketException);         
		} 	
	}

	private void SendCurrentState(){
		string currentState = "current-state;" + uIUpdater.flightStateString[(int)DroneManager.currentFlightState] + ";" +
			uIUpdater.missionStateString[(int)DroneManager.currentMissionState] + ";" + 
			uIUpdater.controlStateString[(int)DroneManager.currentControlType] + ";" + 
			uIUpdater.systemStateString[(int)DroneManager.currentSystemState];
		
		SendMessage(currentState);

	}

	IEnumerator UpdateCurrentState(){
		while(true){
			SendCurrentState();
			yield return new WaitForSeconds(0.5f);
		}
	}
}
