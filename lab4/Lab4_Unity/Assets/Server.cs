/*
Reference
Implementing a Basic TCP Server in Unity: A Step-by-Step Guide
By RabeeQiblawi Nov 20, 2023
https://medium.com/@rabeeqiblawi/implementing-a-basic-tcp-server-in-unity-a-step-by-step-guide-449d8504d1c5
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class TCP : MonoBehaviour
{
    const string hostIP = "0.0.0.0";
    const int port = 13456;
    TcpListener server = null;
    TcpClient client = null;
    NetworkStream stream = null;
    Thread thread;

    Animator playerAnimation;

    public Transform LHand;
    public Transform RHand;
    public Transform Head;
    // public Transform LFoot;
    // public Transform RFoot;

    public GameObject yBot;

    // public GameObject boneRenderer;

    // public GameObject RigBuilder;
    public GameObject VrRig;

    public BoneRenderer boneRenderer;

    public RigBuilder rigBuilder;

    public Dictionary<string, int> bodyDict = new Dictionary<string, int>{
        { "head", 0 },
        { "leftHand", 1 },
        { "rightHand", 2 },
        { "leftFoot", 3 },
        { "rightFoot", 4 }
    };

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

    [Serializable]
    public class TransformedMessage
    {
        public List<TransformedAnchor> transformedAnchors = new List<TransformedAnchor>();
    }

    [Serializable]
    public class TransformedAnchor
    {
        public int anchor_id;
        public Vector3 original_position;
        public Vector3 transformed_position;
    }

    private float timer = 0;
    private static object Lock = new object();
    private List<TransformedMessage> MessageQue = new List<TransformedMessage>();

    private void Start()
    {
        thread = new Thread(new ThreadStart(SetupServer));
        playerAnimation = yBot.GetComponent<Animator>();
        rigBuilder = yBot.GetComponent<RigBuilder>();
        boneRenderer = yBot.GetComponent<BoneRenderer>();

        thread.Start();
    }

    private float waitTime = 2.0f;
    private float jumpingTimer = 0.0f;
    public bool currentlyJumpingJack = false;

    private void Update()
    {
        if (currentlyJumpingJack)
        {
            jumpingTimer += Time.deltaTime;
            if (jumpingTimer > waitTime)
            {
                playerAnimation.SetBool("JumpingJack", false);
                boneRenderer.enabled = true;
                rigBuilder.enabled = true;
                VrRig.SetActive(true);
                jumpingTimer = 0.0f;
                currentlyJumpingJack = false;
            }
        }

        // TODO
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log("--------------------- PRESS KEY Q----------------------");
            boneRenderer.enabled = false;
            rigBuilder.enabled = false;
            VrRig.SetActive(false);
            playerAnimation.SetBool("JumpingJack", true);
            currentlyJumpingJack = true;
        }

        if (Time.time > timer)
        {
            // SendAnchorsToClient();
            timer = Time.time + 0.5f;
        }

        lock (Lock)
        {
            foreach (TransformedMessage msg in MessageQue)
            {
                Move(msg);
            }
            MessageQue.Clear();
        }
    }

    private void SendAnchorsToClient()
    {
        if (VrRig == null)
        {
            return;
        }
        Message message = new Message();
        
        message.listOfAnchors.Add(new Anchor
        {
            id = bodyDict["head"],
            position = Head.transform.position,
        });

        message.listOfAnchors.Add(new Anchor
        {
            id = bodyDict["leftHand"],
            position = LHand.transform.position,
        });

        message.listOfAnchors.Add(new Anchor
        {
            id = bodyDict["rightHand"],
            position = RHand.transform.position,
        });

        // message.listOfAnchors.Add(new Anchor
        // {
        //     id = bodyDict["leftFoot"],
        //     position = LFoot.transform.position,
        // });

        // message.listOfAnchors.Add(new Anchor
        // {
        //     id = bodyDict["rightFoot"],
        //     position = RFoot.transform.position,
        // });

        SendMessageToClient(message);
    }

    private void SetupServer()
    {
        try
        {
            IPAddress localAddr = IPAddress.Parse(hostIP);
            server = new TcpListener(localAddr, port);
            server.Start();

            byte[] buffer = new byte[4096];
            string data = null;

            while (true)
            {
                Debug.Log("Waiting for connection...");
                client = server.AcceptTcpClient();
                Debug.Log("Connected!");

                data = null;
                stream = client.GetStream();

                int i;
                while ((i = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    data = Encoding.UTF8.GetString(buffer, 0, i);
                    TransformedMessage message = DecodeTransformed(data);
                    Debug.Log(message.ToString());
                    
                    if (message != null)
                    {
                        lock (Lock)
                        {
                            MessageQue.Add(message);
                        }
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
        if (stream == null)
        {
            return;
        }
        byte[] msg = Encoding.UTF8.GetBytes(Encode(message));
        stream.Write(msg, 0, msg.Length);
        Debug.Log("Sent: " + message);
    }

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

    public void Move(TransformedMessage message)
    {
        float DISTANCE_THRESHHOLD = 1f;
        Vector3 leftHandPosition = Vector3.zero;
        Vector3 rightHandPosition = Vector3.zero;

        foreach (TransformedAnchor anchor in message.transformedAnchors)
        {
            switch (anchor.anchor_id)
            {
                case 0: // head
                    Head.position = anchor.transformed_position;
                    Debug.Log("Head: " + Head.position.ToString());
                    break;
                case 1: // leftHand
                    LHand.position = anchor.transformed_position;
                    leftHandPosition = LHand.position;
                    Debug.Log("Left Hand: " + LHand.position.ToString());
                    break;
                case 2: // rightHand
                    RHand.position = anchor.transformed_position;
                    rightHandPosition = RHand.position;
                    Debug.Log("Right Hand: " + RHand.position.ToString());
                    break;
                // case 3: // leftFoot
                //     LFoot.position = anchor.transformed_position;
                //     Debug.Log("Left Foot: " + LFoot.position.ToString());
                //     break;
                // case 4: // rightFoot
                //     RFoot.position = anchor.transformed_position;
                //     Debug.Log("Right Foot: " + RFoot.position.ToString());
                //     break;
            }

            if (leftHandPosition != Vector3.zero && rightHandPosition != Vector3.zero)
            {
                float dist = Vector3.Distance(leftHandPosition, rightHandPosition);
                Debug.Log("---------- THIS IS MY DISTANCE ------------" + dist);
                if (dist <= DISTANCE_THRESHHOLD)
                {
                    Debug.Log("---------- I AM JUMPINGGGGGGGGG ------------" + dist);
                    boneRenderer.enabled = false;
                    rigBuilder.enabled = false;
                    VrRig.SetActive(false);
                    playerAnimation.SetBool("JumpingJack", true);
                    currentlyJumpingJack = true;
                }
            }
        }
    }
}
