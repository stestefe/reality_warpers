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
        // public Quaternion rotation;
        // public int assigned_marker_id = -1;  
    }

    [Serializable]
    public class TransformedMessage
    {
        public List<TransformedAnchor> transformedAnchors = new List<TransformedAnchor>();
    }

    [Serializable]
    public class TransformedAnchor
    {
        public int anchor_id;
        // public int marker_id;
        public Vector3 original_position;
        public Vector3 transformed_position;
        // public Quaternion original_rotation;
    }

    private float timer = 0;
    private static object Lock = new object();  // lock to prevent conflict in main thread and server thread
    private List<TransformedMessage> MessageQueue = new List<TransformedMessage>();

    private Dictionary<int, GameObject> anchorObjects = new Dictionary<int, GameObject>();

    private void Start()
    {
        thread = new Thread(new ThreadStart(SetupServer));
        thread.Start();
    
        UpdateAnchorDictionary();
    }

    private void Update()
    {
        if (Time.time > timer)
        {
            SendAnchorsToClient();
            timer = Time.time + 2f;
        }
        
        lock(Lock)
        {
            foreach (TransformedMessage message in MessageQueue)
            {
                ProcessTransformedAnchors(message);
            }
            MessageQueue.Clear();
        }
    }

    private void UpdateAnchorDictionary()
    {
        anchorObjects.Clear();
        allObjects = FindObjectsOfType<Transform>();
        var matchingObjects = allObjects
            .Where(obj => obj.name == targetName)
            .Select(obj => obj.gameObject)
            .ToList();

        foreach (var obj in matchingObjects)
        {
            var textComponent = obj.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                try
                {
                    var fullIdText = textComponent.text;
                    var anchorId = int.Parse(fullIdText.Split(':')[1]);
                    anchorObjects[anchorId] = obj;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to parse anchor ID from text: {e.Message}");
                }
            }
        }
    }

    private void SendAnchorsToClient()
    {
        Message message = new Message();
        
        // update anchor dictionary in case new anchors were created
        UpdateAnchorDictionary();
        
        // Debug.Log("Found Anchors: " + anchorObjects.Count);

        foreach (var kvp in anchorObjects)
        {
            int anchorId = kvp.Key;
            GameObject anchorObj = kvp.Value;
            
            if (anchorObj == null) continue;

            var currentAnchorPosition = anchorObj.transform.position;
            var currentAnchorRotation = anchorObj.transform.rotation;

            Anchor currentAnchor = new Anchor
            {
                id = anchorId,
                position = currentAnchorPosition,
                // rotation = currentAnchorRotation,
            };

            // Debug.Log($"Anchor {currentAnchor.id} - Pos: {currentAnchor.position}, Rot: {currentAnchor.rotation}");
            Debug.Log($"Anchor {currentAnchor.id} - Pos: {currentAnchor.position}");
            message.listOfAnchors.Add(currentAnchor);
        }
        
        SendMessageToClient(message);
    }

    private void ProcessTransformedAnchors(TransformedMessage transformedMessage)
    {
        foreach (var transformedAnchor in transformedMessage.transformedAnchors){
            Debug.Log("REICEIVED: " + transformedAnchor.anchor_id + " " +  transformedAnchor.transformed_position);
            if (anchorObjects.ContainsKey(transformedAnchor.anchor_id))
            {
                GameObject anchorObj = anchorObjects[transformedAnchor.anchor_id];
                
                if (anchorObj != null)
                {
                    Vector3 newPosition = transformedAnchor.transformed_position;
                    // Quaternion newRotation = transformedAnchor.transformed_rotation;
                    
                    // float lerpSpeed = 5f * Time.deltaTime;
                    // anchorObj.transform.position = Vector3.Lerp(anchorObj.transform.position, new Vector3(0f,0f,0f), lerpSpeed);
                    anchorObj.transform.position = newPosition;
                    // anchorObj.transform.rotation = Quaternion.Lerp(anchorObj.transform.rotation, newRotation, lerpSpeed);
                    
                    Debug.Log($"Updated Anchor {transformedAnchor.anchor_id}:");
                    Debug.Log($"  Position: {newPosition}");
                    // Debug.Log($"  Rotation: {newRotation}");
                }
            }
        }
        
    }

    // private int GetAssignedMarkerId(int anchorId)
    // {
    //     return anchorId;
    // }

    private void SetupServer()
    {
       try
        {
            IPAddress localAddr = IPAddress.Parse(hostIP);
            // Debug.Log(localAddr);
            server = new TcpListener(localAddr, port);
            server.Start();
            Debug.Log("SERVER STARTED");

            byte[] buffer = new byte[2048];
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
                    TransformedMessage message = DecodeTransformed(data);
                    // Add received message to que
                    lock(Lock)
                    {
                        MessageQueue.Add(message);
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

    public void SendMessageToClient(Message message)
    {
        if(stream == null){
            return;
        }
        Debug.Log(Encode(message));
        byte[] msg = Encoding.UTF8.GetBytes(Encode(message));
        stream.Write(msg, 0, msg.Length);
        Debug.Log("Sent: " + message);
    
        // if(stream == null){
        //     return;
        // }
        
        // try
        // {
        //     string jsonData = Encode(message);
        //     Debug.Log("Sending: " + jsonData);
            
        //     byte[] msg = Encoding.UTF8.GetBytes(jsonData + "\n");
        //     stream.Write(msg, 0, msg.Length);
            
        //     Debug.Log($"Sent {message.listOfAnchors.Count} anchors to client");
        // }
        // catch (Exception e)
        // {
        //     Debug.LogError($"Error sending message to client: {e.Message}");
        // }
    }

    // Encode message from struct to Json String
    public string Encode(Message message)
    {
        return JsonUtility.ToJson(message, true);
    }

    public Message Decode(string json_string)
    {
        try
        {
            Message msg = JsonUtility.FromJson<Message>(json_string);
            return msg;
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to decode message: " + e.Message);
            return null;
        }
    }

    public TransformedMessage DecodeTransformed(string json_string)
    {
        try
        {
            TransformedMessage msg = JsonUtility.FromJson<TransformedMessage>(json_string);
            return msg;
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to decode transformed message: " + e.Message);
            return null;
        }
    }

    // public void SetMarkerAssignment(int anchorId, int markerId)
    // {
    //     if (anchorObjects.ContainsKey(anchorId))
    //     {
    //         // You could store this assignment in a dictionary or component
    //         Debug.Log($"Assigned Anchor {anchorId} to Marker {markerId}");
    //     }
    // }
}