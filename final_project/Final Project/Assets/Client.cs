using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

#region Message System

public interface IMessage
{
    string type { get; }
    string toJson();
}

[Serializable]
public class MessageBase
{
    public string type;
    public string payload;
}

[Serializable]
public class FrustumTransformMessage : IMessage
{
    public string type = "FrustumTransform"; 
    public Vector3 position;
    public Quaternion rotation;

    string IMessage.type => type;

    public string toJson()
    {
        return JsonUtility.ToJson(this);
    }
}

// Factory for creating messages
public static class MessageFactory
{
    public static IMessage CreateFrustumTransformMessage(Vector3 position, Quaternion rotation)
    {
        return new FrustumTransformMessage
        {
            position = position,
            rotation = rotation
        };
    }
}

#endregion

public class Client : MonoBehaviour
{
    [Header("Server Settings")]
    public string hostIP = "127.0.0.1";
    public int port = 13456;

    [Header("VR Anchors")]
    public Transform centerEyeAnchor;

    public GameObject lightHouseOrigin;

    private TcpClient client;
    private NetworkStream stream;
    private Thread sendThread;
    private Thread receiveThread;
    private bool isConnected = false;

    [Header("Send Settings")]
    public float sendInterval = 0.05f;

    private Vector3 cachedPosition;
    private Quaternion cachedRotation;

    private void Start()
    {
        ConnectToServer();
    }

    private void OnApplicationQuit()
    {
        Disconnect();
    }

    private void Update()
    {
        if (centerEyeAnchor != null)
        {
            cachedPosition = centerEyeAnchor.position;
            cachedRotation = centerEyeAnchor.rotation;
        }
    }

    #region Connection

    private void ConnectToServer()
    {
        try
        {
            client = new TcpClient();
            client.Connect(IPAddress.Parse(hostIP), port);
            stream = client.GetStream();
            isConnected = true;

            Debug.Log($"Connected to server at {hostIP}:{port}");

            sendThread = new Thread(SendLoop) { IsBackground = true };
            sendThread.Start();

            receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            receiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to connect: {e.Message}");
        }
    }

    private void Disconnect()
    {
        isConnected = false;

        if (sendThread != null && sendThread.IsAlive)
            sendThread.Abort();

        if (receiveThread != null && receiveThread.IsAlive)
            receiveThread.Abort();

        if (stream != null) stream.Close();
        if (client != null) client.Close();

        Debug.Log("Disconnected from server");
    }

    #endregion

    #region Sending

    private void SendLoop()
    {
        while (isConnected)
        {
            try
            {
                IMessage message = MessageFactory.CreateFrustumTransformMessage(cachedPosition, cachedRotation);
                SendMessageToServer(message);
            }
            catch (Exception e)
            {
                Debug.LogError($"SendLoop error: {e.Message}");
            }

            Thread.Sleep((int)(sendInterval * 1000));
        }
    }

    private void SendMessageToServer(IMessage message)
    {
        if (stream == null) return;

        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message.toJson() + "\n"); // newline as delimiter
            stream.Write(data, 0, data.Length);
            stream.Flush();
        }
        catch (Exception e)
        {
            Debug.LogError($"SendMessageToServer error: {e.Message}");
        }
    }

    #endregion

    #region Receiving

    private void ReceiveLoop()
    {
        byte[] buffer = new byte[4096];
        while (isConnected)
        {
            try
            {
                if (stream.DataAvailable)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        string[] messages = json.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var msgStr in messages)
                        {
                            if (msgStr != null)
                            {
                                DispatchMessage(msgStr);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"ReceiveLoop error: {e.Message}");
            }

            Thread.Sleep(5);
        }
    }

    private void DispatchMessage(string rawJson)
    {
        MessageBase baseMsg = JsonUtility.FromJson<MessageBase>(rawJson);

        switch (baseMsg.type)
        {
            // case "PlayerTransformUpdate":
            //     HandlePlayerTransform(rawJson);
            //     break;
            // case "GrabRequest":
            //     HandleGrabRequest(rawJson);
            //     break;
            // case "LightRayUpdate":
            //     HandleLightRayUpdate(rawJson);
            //     break;
            case "FrustumTransform":
                var frustum = JsonUtility.FromJson<FrustumTransformMessage>(rawJson);
                HandleFrustumTransform(frustum);
                break;
            default:
                Debug.LogWarning($"Unknown message type: {baseMsg.type}");
                break;
        }
    }

    #endregion

    #region Message Handlers

    // private void HandlePlayerTransform(String rawJson)
    // {
    //     Debug.Log($"Received PlayerTransformUpdate: {msg}");
    // }

    private void HandleFrustumTransform(FrustumTransformMessage message)
    {
        Debug.Log($"Received Frustumtransform: {message.position}, {message.rotation}");
        // lightHouseOrigin.transform.position = cachedPosition;
        // lightHouseOrigin.transform.rotation = cachedRotation; 
    }

    // private void HandleGrabRequest(String rawJson)
    // {
    //     Debug.Log($"Received GrabRequest: {msg}");
    // }

    // private void HandleLightRayUpdate(String rawJson)
    // {
    //     Debug.Log($"Received LightRayUpdate: {msg}");
    // }

    #endregion
}
