using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System;

class ServerClient
{
    public int connectionId;
    public string playerName;
    public Vector3 position;
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
    private Dictionary<int, ServerClient> clients = new Dictionary<int, ServerClient>();

    private float lastMovementUpdate;
    private float movementUpdateRate = 0.05f;

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
        Debug.Log("Server Started!");
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
                    case "MYPOSITION":
                        OnMyPosition(connectionId, float.Parse(splitData[1]), float.Parse(splitData[2]));
                        break;
                    default:
                        Debug.Log("Invalid message : " + msg);
                        break;
                }
                break;
            case NetworkEventType.DisconnectEvent: //4
                msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                splitData = msg.Split('|');
                Debug.Log("Player " + connectionId + " has disconnected");
                Ondisconnection(connectionId);
                break;
        }

        // ask player for position 
        if (Time.time - lastMovementUpdate > movementUpdateRate)
        {
            lastMovementUpdate = Time.time;
            string message = "ASKPOSITION|";
            foreach(int i in clients.Keys)
            {
                message += clients[i].connectionId.ToString() + "%" + clients[i].position.x.ToString() + "%" + clients[i].position.y.ToString() + "|";
            }
            message.Trim('|');
            Send(message, unreliableChannel, clients);
        }
    }

    private void OnMyPosition(int connectionId, float x, float y)
    {
        clients[connectionId].position = new Vector3(x, y, 0);
    }

    private void OnConnection(int cnnId)
    {
        // add all existing clients to the list
        ServerClient c = new ServerClient();
        c.playerName = "TEMP";
        c.connectionId = cnnId;
        clients.Add(cnnId, c);
        // When the player joins the server, tell hims his id
        //request his name and send the name of all the other players
        string msg = "ASKNAME|" + cnnId + "|";
        foreach (KeyValuePair<int, ServerClient> sc in clients)
        {
            msg += sc.Value.playerName + '%' + sc.Key + '|';
        }
        msg = msg.Trim('|');
        // ASKNAME|1|DAVE%1|MICHEAL%2|TEMP%3
        Send(msg, reliableChannel, cnnId);
    }

    private void Send(string message, int channelId, int cnnId)
    {
        Dictionary<int, ServerClient> c = new Dictionary<int, ServerClient>();
        c.Add(cnnId, clients[cnnId]);
        Send(message, reliableChannel, c);
    }

    private void Send(string message, int channelId, Dictionary<int, ServerClient> clients)
    {
        Debug.Log("Sending : " + message);
        byte[] msg = Encoding.Unicode.GetBytes(message);
        foreach (KeyValuePair<int, ServerClient> sc in clients)
        {
            NetworkTransport.Send(hostId, sc.Key, channelId, msg, message.Length * sizeof(char), out error);
        }
    }

    private void OnNameIs(int cnnId, string playerName)
    {
        // Link the name to the connection id
        clients[cnnId].playerName = playerName;

        // tell everybody that a new player has connected
        Send("CNN|" + playerName + "|" + cnnId, reliableChannel, clients);
    }

    private void Ondisconnection(int cnnId)
    {
        //remove this player from connection ID
        clients.Remove(cnnId);
        //tell everyone that someone has disconnected
        Send("DC|" + cnnId, reliableChannel, clients);
    }
}
