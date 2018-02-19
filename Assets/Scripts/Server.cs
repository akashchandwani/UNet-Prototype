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
        webHostId = NetworkTransport.AddWebsocketHost(topo, port, null);

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
            case NetworkEventType.Nothing:         //1
                break;
            case NetworkEventType.ConnectEvent:    //2
                Debug.Log("Player " + connectionId + " has connected");
                OnConnection(connectionId);
                break;
            case NetworkEventType.DataEvent:       //3
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                Debug.Log("Receiving from : " + connectionId + msg);
                string[] splitData = msg.Split('|');
                switch (splitData[0])
                {
                    case "NAMEIS":
                        OnNameIs(connectionId, splitData[1]);
                        break;
                    case "CNN":
                        break;
                    case "DC":
                        break;
                    default:
                        Debug.Log("Invalid message : " + msg);
                        break;
                }
                break;
            case NetworkEventType.DisconnectEvent: //4
                Debug.Log("Player " + connectionId + " has disconnected");
                break;
        }
    }

    private void OnConnection(int cnnId)
    {
        // Add him to a list
        ServerClient c = new ServerClient();
        c.playerName = "TEMP";
        c.connectionId = cnnId;
        clients.Add(c);


        // When the player joins the server, tell hims his id
        //request his name and send the name of all the other players
        string msg = "ASKNAME|" + cnnId + "|";
        foreach (ServerClient sc in clients)
        {
            msg += sc.playerName + '%' + sc.connectionId + '|';
        }
        msg = msg.Trim('|');

        // ASKNAME|1|DAVE%1|MICHEAL%2|TEMP%3
        Send(msg, reliableChannel, cnnId);
    }

    private void Send(string message, int channelId, int cnnId)
    {
        List<ServerClient> c = new List<ServerClient>();
        c.Add(clients.Find(x => x.connectionId == cnnId));
        Send(message, reliableChannel, c);
    }

    private void Send(string message, int channelId, List<ServerClient> c)
    {
        Debug.Log("Sending : " + message);
        byte[] msg = Encoding.Unicode.GetBytes(message);
        foreach (ServerClient sc in c)
        {
            NetworkTransport.Send(hostId, sc.connectionId, channelId, msg, message.Length * sizeof(char), out error);
        }
    }

    private void OnNameIs(int cnnId, string playerName)
    {
        // Link the name to the connection id
        clients.Find(x => x.connectionId == cnnId).playerName = playerName;

        // tell everybody that a new player has connected
        Send("CNN|" + playerName + "|" + cnnId, reliableChannel, clients);
    }

    
}
