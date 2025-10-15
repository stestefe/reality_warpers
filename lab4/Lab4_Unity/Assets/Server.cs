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
using System.Linq;
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
    public GameObject VrRig;

    public BoneRenderer boneRenderer;

    public RigBuilder rigBuilder;

    public GameObject cart;

    private bool returnBasketState;

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
        public List<TransformedAnchor> transformedSkeletonAnchors = new List<TransformedAnchor>();
        public List<TransformedAnchor> transformedArcuoAnchors = new List<TransformedAnchor>();
    }

    [Serializable]
    public class TransformedAnchor
    {
        public int anchor_id;
        public Vector3 original_position;
        public Vector3 transformed_position;
    }

    [Serializable]
    public class MarkerData
    {
        public int id;
        public int lastSeenInMessage;
        public MarkerData(int id, int lastSeenInMessage)
        {
            this.id = id;
            this.lastSeenInMessage = lastSeenInMessage;
        }
    }


    private float timer = 0;
    private static object Lock = new object();
    private List<TransformedMessage> MessageQue = new List<TransformedMessage>();

    private Dictionary<int, MarkerData> activeMarkers = new Dictionary<int, MarkerData>();

    const int MISSING_THRESHOLD = 5;

    private int messageCounter = 0;

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
            // CALIBRATION PHASE
            SendAnchorsToClient();
            timer = Time.time + 0.5f;
        }

        lock (Lock)
        {
            foreach (TransformedMessage msg in MessageQue)
            {
                messageCounter++;
                MoveCart(msg);
                if (returnBasketState == true)
                {
                    MoveMediapipe(msg);
                }

            }
            MessageQue.Clear();
        }
        CleanupInactiveIdMarkers();
    }

    private void SendAnchorsToClient()
    {
        if (VrRig == null)
        {
            return;
        }
        Message message = new Message();

        // head
        // message.listOfAnchors.Add(new Anchor
        // {
        //     id = bodyDict["head"],
        //     position = Head.transform.position,
        // });

        // leftHand
        message.listOfAnchors.Add(new Anchor
        {
            id = bodyDict["leftHand"],
            position = LHand.transform.position,
        });

        // rightHand
        message.listOfAnchors.Add(new Anchor
        {
            id = bodyDict["rightHand"],
            position = RHand.transform.position,
        });

        message.listOfAnchors.Add(new Anchor
        {
            id = 3,
            position = cart.transform.position
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

    public void MoveMediapipe(TransformedMessage message)
    {


        foreach (TransformedAnchor skeletonAnchor in message.transformedSkeletonAnchors)
        {
            switch (skeletonAnchor.anchor_id)
            {
                // case 0: // head
                //     Head.position = skeletonAnchor.transformed_position;
                //     Debug.Log("Head: " + Head.position.ToString());
                //     break;
                case 0: // leftHand
                    LHand.position = skeletonAnchor.transformed_position;
                    // leftHandPosition = LHand.position;
                    Debug.Log("Left Hand: " + LHand.position.ToString());
                    break;
                case 1: // rightHand
                    RHand.position = skeletonAnchor.transformed_position;
                    // rightHandPosition = RHand.position;
                    Debug.Log("Right Hand: " + RHand.position.ToString());
                    break;
                    // case 3: // leftFoot
                    //     LFoot.position = skeletonAnchor.transformed_position;
                    //     Debug.Log("Left Foot: " + LFoot.position.ToString());
                    //     break;
                    // case 4: // rightFoot
                    //     RFoot.position = skeletonAnchor.transformed_position;
                    //     Debug.Log("Right Foot: " + RFoot.position.ToString());
                    //     break;
            }

            //         Debug.Log("---------- I AM JUMPINGGGGGGGGG ------------" + dist);
            //         boneRenderer.enabled = false;
            //         rigBuilder.enabled = false;
            //         VrRig.SetActive(false);
            //         playerAnimation.SetBool("JumpingJack", true);
            //         currentlyJumpingJack = true;
        }
    }

    private void MoveCart(TransformedMessage transformedMessage)
    {
        HashSet<int> seenMarkerIds = new HashSet<int>();

        foreach (var arcuoMarker in transformedMessage.transformedArcuoAnchors)
        {
            int markerId = arcuoMarker.anchor_id;
            seenMarkerIds.Add(markerId);

            // update cart position skip for reload marker ID 2
            if (markerId != 2)
            {
                Vector3 newPosition = arcuoMarker.transformed_position;
                newPosition.y = 0;
                cart.transform.position = newPosition;
            }

            // update or add marker to tracking
            if (activeMarkers.ContainsKey(markerId))
            {
                // pdate existing marker
                activeMarkers[markerId].lastSeenInMessage = messageCounter;
                Debug.Log($"updated marker {markerId} : {messageCounter}");
            }
            else
            {
                // new marker detected
                Debug.Log($"creating new marker {markerId} : messagecounter {messageCounter}");

                if (markerId == 2 && !returnBasketState)
                {
                    returnBasketState = true;
                    ToggleFlowerSearch();
                    ToggleMediapipeTracking();
                    Debug.Log("ENTERING BASKET MODE");
                }

                activeMarkers[markerId] = new MarkerData(markerId, messageCounter);
            }
        }

        // update lastSeenInMessage for markers that weren't in this message
        foreach (var kvp in activeMarkers)
        {
            if (!seenMarkerIds.Contains(kvp.Key))
            {
                int messagesSinceSeen = messageCounter - kvp.Value.lastSeenInMessage;
                Debug.Log($"Marker {kvp.Key} not seen in this message (last seen: {kvp.Value.lastSeenInMessage}, current: {messageCounter})");
            }
        }
    }

    private void CleanupInactiveIdMarkers()
    {
        List<int> markersToRemove = new List<int>();

        foreach (var kvp in activeMarkers)
        {
            int markerId = kvp.Key;
            MarkerData markerData = kvp.Value;

            int messagesSinceLastSeen = messageCounter - markerData.lastSeenInMessage;

            if (messagesSinceLastSeen >= MISSING_THRESHOLD)
            {
                Debug.Log($"marker {markerId} exceeded threshold ({messagesSinceLastSeen} messages). Removing.");

                // check if basket marker is being removed
                if (markerId == 2 && returnBasketState)
                {
                    returnBasketState = false;
                    ToggleFlowerSearch();
                    ToggleMediapipeTracking();
                    Debug.Log("NOT IN BASKET MODE ANYMORE");
                }
                markersToRemove.Add(markerId);
            }
        }

        foreach (var markerId in markersToRemove)
        {
            activeMarkers.Remove(markerId);
        }
    }




    private void ToggleMediapipeTracking()
    {
        if (returnBasketState)
        {

            boneRenderer.enabled = true;
            rigBuilder.enabled = true;
            VrRig.SetActive(true);
            // playerAnimation.SetBool("JumpingJack", true);
            // currentlyJumpingJack = true;
            return;
        }
        boneRenderer.enabled = false;
        rigBuilder.enabled = false;
        VrRig.SetActive(false);
        // playerAnimation.SetBo
    }

    private void ToggleFlowerSearch()
    {
        // String targetName = "Hydrangea_Collider(Clone)";
        // Transform[] allObjects = FindObjectsOfType<Transform>();
        // var matchingObjects = allObjects
        //     .Where(obj => obj.name == targetName)
        //     .Select(obj => obj.gameObject)
        //     .ToList();

        // foreach (var obj in matchingObjects)
        // {
        //     if (returnBasketState == true)
        //     {
        //         obj.SetActive(false);
        //         continue;
        //     }
        //     obj.SetActive(true);
        // }
        GameObject spawner = GameObject.FindObjectOfType<SpawnGameObject>()?.gameObject;
        if (returnBasketState == true)
        {

            Debug.Log("Deactivate Flowers");
            if (spawner != null)
            {
                spawner.GetComponent<SpawnGameObject>().DeactivateFlowers();
            }
            return;
        }

        if (spawner != null)
        {
            Debug.Log("Activate Flowers");
            spawner.GetComponent<SpawnGameObject>().ActivateFlowers();
        }
    }

}
    