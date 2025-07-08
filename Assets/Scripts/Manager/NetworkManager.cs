using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.UI.Image;

public class NetworkManager : Singleton<NetworkManager>
{
    #region ENUM
    enum MsgType : byte
    {
        Test = 0,
        StartSim = 1,
        InputData = 2,
        ClientAckTick = 3
    }
    #endregion

    #region Private Field
    private UdpClient udpClient;
    private IPEndPoint udpRemoteEndPoint;
    private Thread udpReceiveThread;

    private bool isConnected = false;
    private bool isStart = false;
    #endregion


    #region Public Field
    [Header("IP/Port")]
    public string serverIP = "127.0.0.1";
    public int serverPort = 5000;
    public string clientIP = "127.0.0.1";
    public int clientPort = 6000;

    [Header("Info")]
    public bool isHost;
    public int packetLossRate;
    #endregion

    [NonSerialized] public Action<byte[]> MessageReceived;

    #region Override Method
    public override void AwakeFunc()
    {
        
    }
    #endregion

    #region Public Method
    public void StartSim()
    {
        if (!isConnected || isStart || !isHost) return;

        if(isHost)
        {
            SendStartSim();
        }
        Singleton<SimManager>.Instance.StartSim(isHost);
        isStart = true;
    }
    public void Connect()
    {
        Debug.Log("call connect");

        if (isConnected) {
            Debug.Log("already connected");
            return;
        }

        if (isHost)
            MessageReceived += data => OnReceiveClientData(data);
        else
            MessageReceived += data => OnReceiveHostData(data);

        ConnectUDP();
    }
    #endregion 


    #region UDP
    private void ConnectUDP()
    {
        Debug.Log("Start connect.");
        try
        {
            string targetIP = isHost ? clientIP : serverIP;
            int targetPort = isHost ? clientPort : serverPort;
            int myPort = isHost ? serverPort : clientPort;

            udpClient = new UdpClient(myPort);
            udpRemoteEndPoint = new IPEndPoint(IPAddress.Parse(targetIP), targetPort);
            udpClient.Connect(udpRemoteEndPoint);

            udpReceiveThread = new Thread(UDPReceiveLoop);
            udpReceiveThread.Start();
            Debug.Log("UDP connected.");
            isConnected = true;
        }
        catch (Exception e)
        {
            Debug.LogError("UDP Connect Error: " + e.Message);
        }
    }

    private void UDPReceiveLoop()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        try
        {
            while (true)
            {
                byte[] data = udpClient.Receive(ref remoteEP);
                MessageReceived?.Invoke(data);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("UDP Receive Error: " + e.Message);
        }
    }

    private void SendData(byte[] bytes, bool usePacketLoss = true)
    {
        if (usePacketLoss && (isHost && packetLossRate > 0))
        {
            if (UnityEngine.Random.Range(1, 100) <= packetLossRate) return;
        }
        udpClient.Send(bytes, bytes.Length);
    }

    public void SendTestPacket()
    {
        Debug.Log("SendTestPacket");
        var buffer = new byte[] { (byte)MsgType.Test };
        SendData(buffer, false);
    }

    public void Close()
    {
        udpClient?.Close();
        udpReceiveThread?.Abort();

        isConnected = false;
    }
    #endregion


    #region Host
    private void SendStartSim()
    {
        var buffer = new byte[] { (byte)MsgType.StartSim };
        SendData(buffer, false);
    }
    public void SendInputData(Queue<InputData> datas)
    {
        InputData[] array = datas.ToArray();
        Queue<InputData> copy = new Queue<InputData>(array);
        Debug.Log($"SendTestPacket - Count: {datas.Count}");
        var buffer = new List<byte>();
        buffer.Add((byte)MsgType.InputData);
        buffer.AddRange(SerializeStructUtil.SerializeQueue(copy));
        SendData(buffer.ToArray());
    }
    private void OnReceiveClientData(byte[] bytes)
    {
        MsgType type = (MsgType)bytes[0];
        switch (type)
        {
            case MsgType.Test:
                Debug.Log("Receive Test Packet");
                break;

            case MsgType.ClientAckTick:
                Singleton<SimManager>.Instance.ClearLog(BitConverter.ToInt32(bytes, 1));
                break;
        }
    }
    #endregion

    #region Client
    public void SendClientLastTick(int n)
    {
        var buffer = new List<byte>();
        buffer.Add((byte)MsgType.ClientAckTick);
        buffer.AddRange(BitConverter.GetBytes(n));

        SendData(buffer.ToArray());
    }
    private void OnReceiveHostData(byte[] bytes)
    {
        MsgType type = (MsgType)bytes[0];

        switch (type)
        {
            case MsgType.Test:
                Debug.Log("Receive Test Packet");
                break;

            case MsgType.StartSim:
                Debug.Log("Receive StartSim Packet");
                Singleton<SimManager>.Instance.StartSim(false);
                break;

            case MsgType.InputData:
                Debug.Log("Receive InputData Packet");
                Queue<InputData> receivedQueue = SerializeStructUtil.DeserializeQueue<InputData>(bytes[1..]);
                int cnt = 0;
                foreach (InputData data in receivedQueue)
                {
                    Debug.Log($"{cnt++} : {data.tick} / {data.forward} / {data.backward} / {data.left} / {data.right}");
                }
                SimManager.Instance.inputQueue = receivedQueue;
                break;
        }
    }
    #endregion

    #region Unity
    void OnDestroy()
    {
        Close();
    }
    #endregion
}
