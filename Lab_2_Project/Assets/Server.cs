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
    const string HOST_IP = "0.0.0.0"; // Select your IP
    // const string hostIP = "127.0.0.1"; // Select your IP
    const int PORT = 13456; // Select your port
    TcpListener server = null;
    TcpClient client = null;
    NetworkStream stream = null;
    Thread thread;

    const int MISSING_THRESHOLD = 5;
    // anchor position
    string targetName = "AnchorPrefab(Clone)";

    Transform[] allObjects;

    bool calibration_done = false;
    
    public GameObject greenObject;
    
    private int messageCounter = 0;

    private Dictionary<int, MarkerData> activeMarkers = new Dictionary<int, MarkerData>();

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
    }

    private float timer = 0;
    private static object Lock = new object();  // lock to prevent conflict in main thread and server thread
    private List<TransformedMessage> MessageQueue = new List<TransformedMessage>();

    private Dictionary<int, GameObject> anchorObjects = new Dictionary<int, GameObject>();

    [Serializable]
    public class MarkerData {
        public int id;
        public int lastSeenInMessage;
        public GameObject markerObject;
        public MarkerData(int id, int lastSeenInMessage, GameObject markerObject){
            this.id = id;
            this.lastSeenInMessage = lastSeenInMessage;
            this.markerObject = markerObject;
        }
    }

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
            timer = Time.time + 0.5f;
        }

        lock (Lock)
        {
            foreach (TransformedMessage message in MessageQueue)
            {
                messageCounter++;
                ProcessTransformedAnchors(message);
            }
            MessageQueue.Clear();
        }
        CleanupInactiveMarkers();
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
            Canvas canvas = anchorObj.GetComponentInChildren<Canvas>();
            TextMeshProUGUI positionText = canvas.gameObject.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            positionText.text = anchorObj.transform.position.ToString();

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
        // CALIBRATION PHASE: delete anchors used for calibration
        if (calibration_done == false)
        {
            Debug.Log("CALIBRATION START");

            List<int> anchorsToRemove = new List<int>();

            foreach (var transformedAnchor in transformedMessage.transformedAnchors)
            {
                if (anchorObjects.ContainsKey(transformedAnchor.anchor_id))
                {
                    GameObject anchorObj = anchorObjects[transformedAnchor.anchor_id];
                    if (anchorObj != null)
                    {
                        Debug.Log($"Destroying calibration anchor with ID: {transformedAnchor.anchor_id}");
                        Destroy(anchorObj);
                        anchorsToRemove.Add(transformedAnchor.anchor_id);
                    }
                }
            }

            foreach (var anchorId in anchorsToRemove)
            {
                anchorObjects.Remove(anchorId);
            }

            calibration_done = true;
            Debug.Log("CALIBRATION END - Anchor objects deleted");
            return; // dont process markers during calibration
        }

        // TRACKING PHASE: Update marker positions
        HashSet<int> seenMarkerIds = new HashSet<int>();

        foreach (var transformedAnchor in transformedMessage.transformedAnchors)
        {
            int markerId = transformedAnchor.anchor_id;
            seenMarkerIds.Add(markerId);

            Debug.Log($"RECEIVED: Marker ID {markerId} at {transformedAnchor.transformed_position}");

            if (activeMarkers.ContainsKey(markerId))
            {
                // update existing marker
                MarkerData markerData = activeMarkers[markerId];
                
                if (markerData.markerObject != null)
                {
                    markerData.markerObject.transform.position = transformedAnchor.transformed_position;
                    markerData.lastSeenInMessage = messageCounter;
                    Debug.Log($"Updated marker {markerId} position");
                }
                else
                {
                    // object was destroyed, recreate it
                    Debug.Log($"Recreating marker {markerId}");
                    GameObject newMarker = Instantiate(greenObject, transformedAnchor.transformed_position, Quaternion.identity);
                    markerData.markerObject = newMarker;
                    markerData.lastSeenInMessage = messageCounter;
                }
            }
            else
            {
                // create new marker
                Debug.Log($"Creating new marker {markerId}");
                Vector3 markerPosition = transformedAnchor.transformed_position;
                GameObject currentGreenObject = Instantiate(greenObject, markerPosition, Quaternion.identity);
                MarkerData currentMarkerData = new MarkerData(markerId, messageCounter, currentGreenObject);
                activeMarkers[markerId] = currentMarkerData;
            }
        }

        // update lastSeenInMessage for markers that weren't in this message
        foreach (var kvp in activeMarkers)
        {
            if (!seenMarkerIds.Contains(kvp.Key))
            {
                Debug.Log($"Marker {kvp.Key} not seen in this message (last seen: {kvp.Value.lastSeenInMessage}, current: {messageCounter})");
            }
        }
    }

    
    private void CleanupInactiveMarkers()
    {
        List<int> markersToRemove = new List<int>();

        foreach (var kvp in activeMarkers)
        {
            int markerId = kvp.Key;
            MarkerData markerData = kvp.Value;

            int messagesSinceLastSeen = messageCounter - markerData.lastSeenInMessage;

            if (messagesSinceLastSeen >= MISSING_THRESHOLD)
            {
                Debug.Log($"Marker {markerId} missing for {messagesSinceLastSeen} messages - destroying");
                
                if (markerData.markerObject != null)
                {
                    Destroy(markerData.markerObject);
                }
                
                markersToRemove.Add(markerId);
            }
        }

        foreach (var markerId in markersToRemove)
        {
            activeMarkers.Remove(markerId);
        }
    }

    private void SetupServer()
    {
        try
        {
            IPAddress localAddr = IPAddress.Parse(HOST_IP);
            // Debug.Log(localAddr);
            server = new TcpListener(localAddr, PORT);
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
                    Debug.Log("RAW DATA RECEIVED: " + data);
                    TransformedMessage message = DecodeTransformed(data);
                    // Add received message to que
                    lock (Lock)
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
}