﻿using System.Collections;
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

    public List<Player> players;

    private string playerName;
    public GameObject playerPrefab;

    public void Connect()
    {
        string pName = GameObject.Find("NameInput").GetComponent<InputField>().text;
        if (pName == "")
        {
            Debug.Log("You must enter the name");
            return;
        }

        playerName = pName;

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
            case NetworkEventType.DataEvent:       //3
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                Debug.Log("Receiving : " + msg);
                string[] splitData = msg.Split('|');
                switch (splitData[0])
                {
                    case "ASKNAME":
                        OnAskName(splitData);
                        break;
                    case "CNN":
                        SpawnPlayer(splitData[1], int.Parse(splitData[2]));
                        break;
                    case "DC":
                        break;
                    default:
                        Debug.Log("Invalid message : " + msg);
                        break;
                }
                break;
        }
    }

    private void OnAskName(string[] data){
        ourClientId = int.Parse(data[1]);

        // send our name to server to the server
        Send("NAMEIS|" + playerName, reliableChannel);

        // create all the other player
        for (int i = 2; i < data.Length; i++){
            string[] d = data[i].Split('%');
            SpawnPlayer(d[0], int.Parse(d[1]));
        }
    }

    private void SpawnPlayer(string playerName, int cnnId){
        GameObject go = Instantiate(playerPrefab) as GameObject;

        // Is this ours 
        if(cnnId == ourClientId){
            // TODO: add mobility
            GameObject.Find("Canvas").SetActive(false);
            //remove canvas
            //start
            isStarted = true;
        }

        Player p = new Player();
        p.avatar = go;
        p.playername = playerName;
        p.connectionId = cnnId;
       players.Add(p);
    }

     private void Send(string message, int channelId) {
        Debug.Log("Sending : " + message);
        byte[] msg = Encoding.Unicode.GetBytes(message);
        NetworkTransport.Send(hostId, connectionId, channelId, msg, message.Length*sizeof(char), out error);
        
    }
}