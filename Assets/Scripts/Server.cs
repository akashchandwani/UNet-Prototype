using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

class ServerClient
{
    public int connectionId;
    public string playerName;
}

public class Server : MonoBehaviour
{

    private const int MAX_CONNECTION = 100;

    private int port = 5701;

    private int hostId;
    private int webHostId;

    private int reliableChannel;
    private int unreliableChannel;

    private bool isStarted = false;
    private byte error;

    private List<ServerClient> clients = new List<ServerClient>();

    private void Start()
    {
        NetworkTransport.Init();
        ConnectionConfig cc = new ConnectionConfig();

        reliableChannel = cc.AddChannel(QosType.Reliable);
        unreliableChannel = cc.AddChannel(QosType.Unreliable);

        HostTopology topo = new HostTopology(cc, MAX_CONNECTION);

        hostId = NetworkTransport.AddHost(topo, port, null);
    
        isStarted = true;
    }

    private void Update()
    {

        if (!isStarted)
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
            case NetworkEventType.Nothing:         
                break;
            case NetworkEventType.ConnectEvent:    
                Debug.Log("Player " + connectionId + " has connected");
                OnConnection(connectionId);
                break;
			case NetworkEventType.DataEvent:     
				string msg = Encoding.Unicode.GetString (recBuffer, 0, dataSize);
				Debug.Log ("Receiving from Player: " + connectionId + " , the message is " +msg);
				break;
            case NetworkEventType.DisconnectEvent:
                Debug.Log("Player " + connectionId + " has disconnected");
                break;
        }
    }

    private void OnConnection(int cnnId)
	{
		string message = "CONNECTION_SUCCESS";
		byte[] msg = Encoding.Unicode.GetBytes (message);
		NetworkTransport.Send (hostId, cnnId, reliableChannel, msg, msg.Length * sizeof(char), out error);
	}

}
