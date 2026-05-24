using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class UdpEventTransport : MonoBehaviour
{
    public static UdpEventTransport Instance { get; private set; }

    [Header("Role")]
    public NetworkRole role = NetworkRole.Server;

    [Header("Network Settings")]
    public string serverIp = "127.0.0.1";
    public int serverPort = 7777;
    public int clientPort = 7778;

    [Header("Bad Internet Test")]
    [Range(0f, 1f)]
    public float outgoingPacketLossChance = 0f;

    [Tooltip("Средняя искусственная задержка отправки в миллисекундах")]
    public float artificialLatencyMs = 0f;

    [Tooltip("Разброс задержки в миллисекундах")]
    public float jitterMs = 0f;

    [Range(0f, 1f)]
    public float duplicatePacketChance = 0f;

    private UdpClient udpClient;
    private IPEndPoint serverEndPoint;

    private Thread receiveThread;
    private bool isRunning;

    private readonly Queue<NetworkMessage> receivedMessages = new Queue<NetworkMessage>();
    private readonly object queueLock = new object();

    private readonly Dictionary<string, IPEndPoint> clientEndPoints = new Dictionary<string, IPEndPoint>();
    private readonly object clientsLock = new object();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        StartTransport();

        if (role == NetworkRole.Client)
        {
            SendClientHello();
        }
    }

    private void Update()
    {
        ProcessReceivedMessagesOnMainThread();
    }

    private void OnDestroy()
    {
        StopTransport();
    }

    public void StartTransport()
    {
        if (isRunning)
            return;

        if (role == NetworkRole.Server)
        {
            udpClient = new UdpClient(serverPort);
            Debug.Log("[UDP SERVER] Started on port " + serverPort);
        }
        else
        {
            udpClient = new UdpClient(clientPort);
            serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
            Debug.Log("[UDP CLIENT] Started on port " + clientPort);
        }

        isRunning = true;

        receiveThread = new Thread(ReceiveLoop);
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    public void StopTransport()
    {
        isRunning = false;

        try
        {
            udpClient?.Close();
        }
        catch
        {
        }

        try
        {
            if (receiveThread != null && receiveThread.IsAlive)
            {
                receiveThread.Join(100);
            }
        }
        catch
        {
        }
    }

    private void SendClientHello()
    {
        string playerId = "Unknown";

        if (EventFsmSyncManager.Instance != null)
        {
            playerId = EventFsmSyncManager.Instance.localPlayerId;
        }

        NetworkMessage hello = NetworkMessage.CreateClientHelloMessage(playerId);
        SendNetworkMessage(hello);

        Debug.Log("[UDP CLIENT] Sent ClientHello: " + playerId);
    }

    public void SendNetworkMessage(NetworkMessage message)
    {
        if (message == null)
        {
            Debug.LogError("[UDP] Нельзя отправить пустое сообщение");
            return;
        }

        string json = JsonUtility.ToJson(message);
        byte[] data = Encoding.UTF8.GetBytes(json);

        StartCoroutine(SendWithNetworkConditions(data, json, message.messageType));
    }

    private IEnumerator SendWithNetworkConditions(byte[] data, string json, NetworkMessageType messageType)
    {
        if (Random.value < outgoingPacketLossChance)
        {
            Debug.LogWarning("[UDP " + role + "] Имитируем потерю исходящего пакета: " + messageType);

            if (ResearchMetrics.Instance != null)
            {
                ResearchMetrics.Instance.RegisterPacketLoss();
            }

            yield break;
        }

        float finalDelayMs = artificialLatencyMs;

        if (jitterMs > 0f)
        {
            finalDelayMs += Random.Range(-jitterMs, jitterMs);
        }

        if (finalDelayMs < 0f)
        {
            finalDelayMs = 0f;
        }

        if (finalDelayMs > 0f)
        {
            yield return new WaitForSeconds(finalDelayMs / 1000f);
        }

        SendRaw(data, json);

        if (ResearchMetrics.Instance != null)
        {
            ResearchMetrics.Instance.RegisterSentMessage();
        }

        if (Random.value < duplicatePacketChance)
        {
            Debug.LogWarning("[UDP " + role + "] Имитируем дубликат пакета: " + messageType);

            yield return new WaitForSeconds(0.05f);

            SendRaw(data, json);

            if (ResearchMetrics.Instance != null)
            {
                ResearchMetrics.Instance.RegisterSentMessage();
            }
        }
    }

    private void SendRaw(byte[] data, string json)
    {
        if (role == NetworkRole.Client)
        {
            SendToServer(data, json);
        }
        else
        {
            BroadcastToClients(data, json);
        }
    }

    private void SendToServer(byte[] data, string json)
    {
        if (udpClient == null || serverEndPoint == null)
        {
            Debug.LogError("[UDP CLIENT] UDP не запущен или адрес сервера не задан");
            return;
        }

        udpClient.Send(data, data.Length, serverEndPoint);
        Debug.Log("[UDP CLIENT] Sent message: " + json);
    }

    private void BroadcastToClients(byte[] data, string json)
    {
        if (udpClient == null)
        {
            Debug.LogError("[UDP SERVER] UDP не запущен");
            return;
        }

        lock (clientsLock)
        {
            if (clientEndPoints.Count == 0)
            {
                Debug.LogWarning("[UDP SERVER] Нет зарегистрированных клиентов для рассылки");
                return;
            }

            foreach (KeyValuePair<string, IPEndPoint> client in clientEndPoints)
            {
                udpClient.Send(data, data.Length, client.Value);
                Debug.Log("[UDP SERVER] Sent to " + client.Key + ": " + json);
            }
        }
    }

    private void ReceiveLoop()
    {
        while (isRunning)
        {
            try
            {
                IPEndPoint senderEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpClient.Receive(ref senderEndPoint);

                string json = Encoding.UTF8.GetString(data);
                NetworkMessage message = JsonUtility.FromJson<NetworkMessage>(json);

                if (role == NetworkRole.Server)
                {
                    RegisterClientEndPoint(message, senderEndPoint);
                }

                lock (queueLock)
                {
                    receivedMessages.Enqueue(message);
                }
            }
            catch
            {
            }
        }
    }

    private void RegisterClientEndPoint(NetworkMessage message, IPEndPoint senderEndPoint)
    {
        if (message == null)
            return;

        string playerId = "";

        if (message.messageType == NetworkMessageType.ClientHello)
        {
            playerId = message.senderPlayerId;
        }
        else if (message.puzzleEvent != null)
        {
            playerId = message.puzzleEvent.sourcePlayerId;
        }

        if (string.IsNullOrWhiteSpace(playerId))
            return;

        lock (clientsLock)
        {
            clientEndPoints[playerId] = senderEndPoint;
        }
    }

    private void ProcessReceivedMessagesOnMainThread()
    {
        while (true)
        {
            NetworkMessage message = null;

            lock (queueLock)
            {
                if (receivedMessages.Count > 0)
                {
                    message = receivedMessages.Dequeue();
                }
            }

            if (message == null)
                break;

            Debug.Log("[UDP " + role + "] Received message type: " + message.messageType);

            if (EventFsmSyncManager.Instance != null)
            {
                EventFsmSyncManager.Instance.OnNetworkMessageReceived(message);
            }
        }
    }
}