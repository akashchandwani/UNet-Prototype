using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;

public class Player {

    public string playername;
    public GameObject avatar;
    public int connectionId;
}

public class Client : MonoBehaviour
{
    private int ourClientId;

    private const int MAX_CONNECTION = 100;

    private int port = 5701;

    private int hostId;
    private int webHostId;

    private int reliableChannel;
    private int unreliableChannel;

    private float connectionTime;
    private bool isConnected = false;
    private bool isStarted = false;
    private int connectionId;
    private byte error;


    private string input;
	private Button connectButton;

    public void Connect()
    {
        NetworkTransport.Init();
        ConnectionConfig cc = new ConnectionConfig();

        reliableChannel = cc.AddChannel(QosType.Reliable);
        unreliableChannel = cc.AddChannel(QosType.Unreliable);

        HostTopology topo = new HostTopology(cc, MAX_CONNECTION);

        hostId = NetworkTransport.AddHost(topo, 0);
        connectionId = NetworkTransport.Connect(hostId, "127.0.0.1", port, 0, out error);

        connectionTime = Time.time;
        isConnected = true;
    }

    private void Update()
    {
        if (!isConnected)
            return;
        int recHostId;
        int connectionId;
        int channelId;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte error;
        NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, bufferSize, out dataSize, out error);
        switch (recData)
        {
		case NetworkEventType.DataEvent:
			string msg = Encoding.Unicode.GetString (recBuffer, 0, dataSize);
			Debug.Log ("Receiving : " + msg);
			if (msg.Equals("CONNECTION_SUCCESS")) {
				onConnectionSuccess ();
				Send ("Client Received from server successful", reliableChannel);
			}
            break;
        }
    }

	public void SendInput() {
		input = GameObject.Find("NameInput").GetComponent<InputField>().text;
		if (input == "") {
			Debug.Log ("You must enter something");
			return;
		}
		Send ("Input field data is " + input, reliableChannel);
	}

    private void Send(string message, int channelId) {
        Debug.Log("Sending : " + message);
        byte[] msg = Encoding.Unicode.GetBytes(message);
        NetworkTransport.Send(hostId, connectionId, channelId, msg, message.Length*sizeof(char), out error);  
    }

	private void onConnectionSuccess() {
		Debug.Log ("Player Connected");
		connectButton = GameObject.Find ("ConnectButton").GetComponent<Button> ();
		connectButton.interactable = false;
	}
}