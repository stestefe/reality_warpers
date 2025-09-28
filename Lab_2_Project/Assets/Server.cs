/*
Reference
Implementing a Basic TCP Server in Unity: A Step-by-Step Guide
By RabeeQiblawi Nov 20, 2023
https://medium.com/@rabeeqiblawi/implementing-a-basic-tcp-server-in-unity-a-step-by-step-guide-449d8504d1c5
*/

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class TCP : MonoBehaviour
{
    const string hostIP = "0.0.0.0"; // Select your IP
    // const string hostIP = "127.0.0.1"; // Select your IP
    const int port = 13456; // Select your port
    TcpListener server = null;
    TcpClient client = null;
    NetworkStream stream = null;
    Thread thread;

    // anchor position
    string targetName = "AnchorPrefab(Clone)";
    List<GameObject> allAnchors = new List<GameObject>();

    Transform[] allObjects;

    // Define your own message
    [Serializable]
    public class Message
    {
        public List<Anchor> listOfAnchors = new List<Anchor>();
    }
    
    [Serializable]
    public class Anchor
    {
        public int id;
        public Vector3 position;
    }

    private float timer = 0;
    private static object Lock = new object();  // lock to prevent conflict in main thread and server thread
    private List<Message> MessageQue = new List<Message>();


    private void Start()
    {
        thread = new Thread(new ThreadStart(SetupServer));
        thread.Start();
    }

    private void Update()
    {
        // Debug.Log("Hallo")
        // Send message to client every 2 second
        if (Time.time > timer)
        {

            Message message = new Message();
            
            
            // TODO: set to values from RealSense
            // message.id = 1;
            // message.position = [0,0,0];
            // Vector3 position = new Vector3(1f, 2f, 3f);
            // message.position = position;

            allObjects = FindObjectsOfType<Transform>();
            var matchingObjects = allObjects
                .Where(obj => obj.name == targetName)
                .Select(obj => obj.gameObject)
                .ToList();

            Debug.Log("Found Anchors: " + matchingObjects.Count);

            foreach (var obj in matchingObjects)
            {
                var fullIdText = obj.GetComponentInChildren<TextMeshProUGUI>().text;
                var currentAnchorId = int.Parse(fullIdText.Split(":")[1]);

                var currentAnchorPosition = obj.transform.position;

                Anchor currentAnchor = new Anchor
                {
                    id = currentAnchorId,
                    position = currentAnchorPosition
                };

                Debug.Log("Anchor" + currentAnchor.id + " " + currentAnchor.position);

                message.listOfAnchors.Add(currentAnchor);
            }
            SendMessageToClient(message);
            timer = Time.time + 2f;
        }
        // Process message que
        lock(Lock)
        {
            foreach (Message message in MessageQue)
            {
                // Unity only allow main thread to modify GameObjects.
                // Spawn, Move, Rotate GameObjects here. 
                // int id = message.id;

                // float x = message.position.x;
                // float y = message.position.y;
                // float z = message.position.z;

                // Debug.Log("Received from client: ---------" + "ID:" + id + " | x: " + x + " | y: " + y +  " | z: " + z);
            }
            MessageQue.Clear();
        }
    }

    private void SetupServer()
    {
        try
        {
            IPAddress localAddr = IPAddress.Parse(hostIP);
            // Debug.Log(localAddr);
            server = new TcpListener(localAddr, port);
            server.Start();
            Debug.Log("SERVER STARTED");

            byte[] buffer = new byte[1024];
            string data = null;

            while (true)
            {
                Debug.Log("Waiting for connection...");
                client = server.AcceptTcpClient();
                Debug.Log("Connected!");

                data = null;
                stream = client.GetStream();

                // Receive message from client    
                int i;
                while ((i = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    data = Encoding.UTF8.GetString(buffer, 0, i);
                    Message message = Decode(data);
                    // Add received message to que
                    lock(Lock)
                    {
                        MessageQue.Add(message);
                    }
                }
                client.Close();
            }
        }
        catch (SocketException e)
        {
            Debug.Log("SocketException: " + e);
        }
        finally
        {
            server.Stop();
        }
    }

    private void OnApplicationQuit()
    {
        stream.Close();
        client.Close();
        server.Stop();
        thread.Abort();
    }

    // public void SendMessageToClient(Message message)
    // {
    //     Debug.Log("HalloHalli");
    //     Debug.Log(message.some_string);

    //     string json = JsonUtility.ToJson(message);
    //     byte[] msg = Encoding.UTF8.GetBytes(json);


    //     // byte[] msg = Encoding.UTF8.GetBytes(Encode(message));
    //     Debug.Log(msg.Length);
    //     stream.Write(msg, 0, msg.Length);
    //     Debug.Log("Sent: " + message);
    // }

    public void SendMessageToClient(Message message)
    {
        if(stream == null){
            return;
        }
        Debug.Log(Encode(message));
        byte[] msg = Encoding.UTF8.GetBytes(Encode(message));
        stream.Write(msg, 0, msg.Length);
        Debug.Log("Sent: " + message);
    }

    // Encode message from struct to Json String
    public string Encode(Message message)
    {
        // Debug.Log(message.listOfAnchors[0].);
        return JsonUtility.ToJson(message, true);
    }

    // Decode messaage from Json String to struct
    public Message Decode(string json_string)
    {
        try{
            Message msg = JsonUtility.FromJson<Message>(json_string);
            return msg;
        }
        catch (Exception e){
            Debug.LogError("Failed to decode message: " + e.Message);
            return null;
        }
    }
}
