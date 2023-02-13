//Sanchay Ravindiran 2020

/*
    This class represents the main network gateway throughout
    the server project. It implements the functionality needed
    to manage the connectivity of clients connected to the server
    and to read and write messages between those clients. By
    abstracting message delivery, other classes can use this
    class to perform network operations via messages.

    Every fixed update the server processes packets from all
    of its clients, disconnecting, connecting and handling
    each client accordingly. Packets containing user defined
    data are unpacked, and their deserialized messages are
    handed off to a child class for further individual processing.
*/

using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Networking;

[System.Obsolete]
public class Server : MonoBehaviour
{
    private const byte version = 9;

    private int host;
    private bool hosted;
    private byte error;

    private Queue<Message> receivedMessages = new Queue<Message>();
    private Queue<Message> processedMessages = new Queue<Message>();

    protected byte Reliable;
    protected byte Unreliable;
    protected byte ReliableOrdered;
    protected byte UnreliableFragmented;
    protected bool Debugging;

    protected List<int> Users = new List<int>();
    private byte[] messageBuffer = new byte[512];
    private BinaryFormatter binaryFormatter = new BinaryFormatter();

    private const ushort port = 4777;

    WaitForSeconds delay = new WaitForSeconds(0.1f);

    [SerializeField] protected UI SID; //S.D. = Special Information Display
    [SerializeField] protected string ProjectName;

    [Space]
    [SerializeField] private int hostPort;

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.runInBackground = true;

        SID.Display("please do not close this window :) ~sanchay productions", false);
        SID.Display("camping trip server v" + version, false);
    }

    protected void On()
    {
        if (hosted) return;

        GlobalConfig globalConfig = new GlobalConfig();
        ConnectionConfig connectionConfig = new ConnectionConfig();
        globalConfig.ReactorModel = ReactorModel.SelectReactor;

        NetworkTransport.Init(globalConfig);

        connectionConfig.SendDelay = 0;
        connectionConfig.MinUpdateTimeout = 1;

        Reliable = connectionConfig.AddChannel(QosType.Reliable);
        ReliableOrdered = connectionConfig.AddChannel(QosType.ReliableSequenced);
        Unreliable = connectionConfig.AddChannel(QosType.Unreliable);
        UnreliableFragmented = connectionConfig.AddChannel(QosType.UnreliableFragmented);

        HostTopology hostTopology = new HostTopology(connectionConfig, 100);

        host = NetworkTransport.AddHost(hostTopology, hostPort, IP.Local());

        if ((NetworkError)error != NetworkError.Ok)
        {
            SID.Display(string.Format("setup error - {0}", (NetworkError)error), true);
            Off();
            return;
        }

        hosted = true;

        SID.Display("local network available", true);
        SID.Display("server information: ", false);
        SID.Display(string.Format("port:{0}, address:{1}, maxconnections:{2}", NetworkTransport.GetHostPort(host), IP.Local(), hostTopology.MaxDefaultConnections), false);
        SID.Display("use the w and s keys to navigate console", true);
    }

    private void Say(string information)
    {
        if (Debugging)
        {
            Debug.Log("<b>server:</b>" + information);
        }
    }

    protected void Off()
    {
        if (!hosted) return;

        hosted = false;

        for (int i = 0; i < Users.Count; i++)
        {
            NetworkTransport.Disconnect(host, Users[i], out error);
        }

        Users.Clear();
        receivedMessages.Clear();
        processedMessages.Clear();

        NetworkTransport.RemoveHost(host);
        NetworkTransport.Shutdown();

        host = -1;

        SID.Display("off", true);
    }

    protected void Send(int user, Message[] messages, byte channel)
    {
        byte[] serializedMessage = Convert(messages);
        NetworkTransport.Send(host, user, channel, serializedMessage, serializedMessage.Length, out error);
    }

    protected void Broadcast(Message[] messages, byte channel)
    {
        byte[] serializedMessage = Convert(messages);

        for (int i = 0; i < Users.Count; i++)
        {
            NetworkTransport.Send(host, Users[i], channel, serializedMessage, serializedMessage.Length, out error);
        }
    }

    protected void Enqueue(Message message)
    {
        processedMessages.Enqueue(message);
    }

    private byte[] Convert(Message[] messages)
    {
        Container container = new Container
        {
            Messages = messages
        };

        MemoryStream memoryStream = new MemoryStream();
        binaryFormatter.Serialize(memoryStream, container);

        return memoryStream.ToArray();
    }

    protected virtual void UserMessage(Message message)
    {

    }

    protected virtual void UserLeft(int user)
    {

    }

    private void FixedUpdate()
    {
        if (!hosted) return;

        ProcessMessages();
    }

    private void ProcessMessages()
    {
        int unusedValue;
        int recievingUser;

        NetworkEventType message = NetworkEventType.Nothing;
        do
        {
            message = NetworkTransport.Receive(out unusedValue, out recievingUser, out unusedValue, messageBuffer, 512, out unusedValue, out error);
            switch (message)
            {
                case NetworkEventType.BroadcastEvent:
                    goto case NetworkEventType.Nothing;
                case NetworkEventType.DataEvent:

                    MemoryStream memoryStream = new MemoryStream(messageBuffer);
                    receivedMessages.Enqueue(binaryFormatter.Deserialize(memoryStream) as Message);

                    goto case NetworkEventType.Nothing;
                case NetworkEventType.ConnectEvent:

                    Users.Add(recievingUser);

                    Assign assign = new Assign
                    {
                        User = recievingUser
                    };

                    Message[] assignContainer = { assign };
                    Send(recievingUser, assignContainer, Reliable);
                    SID.Display("#" + recievingUser + " connected", true);

                    goto case NetworkEventType.Nothing;
                case NetworkEventType.DisconnectEvent:

                    Users.Remove(recievingUser);
                    Destroy destroy = new Destroy
                    {
                        User = recievingUser
                    };

                    Message[] destroyContainer = { destroy };
                    Send(recievingUser, destroyContainer, Unreliable);
                    UserLeft(recievingUser);

                    SID.Display("#" + recievingUser + " disconnected", true);

                    goto case NetworkEventType.Nothing;
                case NetworkEventType.Nothing:
                    break;
            }
        }
        while (message != NetworkEventType.Nothing);

        for (int i = 0; i < receivedMessages.Count; i++)
        {
            UserMessage(receivedMessages.Dequeue());
        }

        if (processedMessages.Count > 0)
        {
            Broadcast(processedMessages.ToArray(), Reliable);
            processedMessages.Clear();
        }
    }

    private void OnApplicationQuit()
    {
        Off();
    }
}
