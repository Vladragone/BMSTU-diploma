using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventFsmSyncManager : MonoBehaviour
{
    public static EventFsmSyncManager Instance { get; private set; }

    [Header("Network Role")]
    public NetworkRole role = NetworkRole.Client;

    [Header("Player Settings")]
    public string localPlayerId = "PlayerA";

    [Header("Reliable UDP Settings")]
    public float resendDelay = 1.5f;

    [Header("Research Mode")]
    public SynchronizationResearchMode synchronizationMode =
        SynchronizationResearchMode.ReliableFsmMethod;

    private readonly Dictionary<string, IFsmPuzzleObject> registeredObjects = new Dictionary<string, IFsmPuzzleObject>();
    private readonly HashSet<string> appliedEventKeys = new HashSet<string>();
    private readonly Dictionary<int, PuzzleEvent> pendingEvents = new Dictionary<int, PuzzleEvent>();
    private readonly Dictionary<string, int> expectedSequenceByPlayer = new Dictionary<string, int>();

    private int nextEventId = 1;
    private int nextSequenceNumber = 1;

    private bool UseReliableMethod
    {
        get
        {
            return synchronizationMode == SynchronizationResearchMode.ReliableFsmMethod;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ApplyCommandLineModeOverride();
    }

    private void ApplyCommandLineModeOverride()
    {
        string[] args = System.Environment.GetCommandLineArgs();

        foreach (string arg in args)
        {
            if (arg == "-basicSync")
            {
                synchronizationMode = SynchronizationResearchMode.BasicUdpEvents;
            }
            else if (arg == "-reliableSync")
            {
                synchronizationMode = SynchronizationResearchMode.ReliableFsmMethod;
            }
        }

        Debug.Log("[EventFsmSyncManager] Synchronization research mode: " + synchronizationMode);
    }

    private string GetEventKey(PuzzleEvent puzzleEvent)
    {
        return puzzleEvent.sourcePlayerId + ":" + puzzleEvent.eventId;
    }

    public void RegisterObject(IFsmPuzzleObject puzzleObject)
    {
        if (puzzleObject == null)
            return;

        if (string.IsNullOrWhiteSpace(puzzleObject.ObjectId))
            return;

        if (registeredObjects.ContainsKey(puzzleObject.ObjectId))
            return;

        registeredObjects.Add(puzzleObject.ObjectId, puzzleObject);

        Debug.Log("[EventFsmSyncManager] Зарегистрирован FSM-объект: " + puzzleObject.ObjectId);
    }

    public void CreateAndSendEvent(string sourceObjectId, string targetObjectId, PuzzleEventType eventType)
    {
        if (role != NetworkRole.Client)
        {
            Debug.LogWarning("[EventFsmSyncManager] Игровые события должен создавать клиент. Текущая роль: " + role);
            return;
        }

        PuzzleEvent puzzleEvent = new PuzzleEvent(
            nextEventId++,
            nextSequenceNumber++,
            localPlayerId,
            sourceObjectId,
            targetObjectId,
            eventType
        );

        Debug.Log("[CLIENT " + localPlayerId + "] Создано событие: " +
                  puzzleEvent.eventType + " | ID=" + puzzleEvent.eventId +
                  " | Seq=" + puzzleEvent.sequenceNumber);

        if (ResearchMetrics.Instance != null)
        {
            ResearchMetrics.Instance.RegisterCreatedEvent(puzzleEvent.eventId);
        }

        if (UseReliableMethod)
        {
            pendingEvents[puzzleEvent.eventId] = puzzleEvent;
        }

        SendPuzzleEventMessage(puzzleEvent);

        if (UseReliableMethod)
        {
            StartCoroutine(ResendUntilAcknowledged(puzzleEvent.eventId));
        }
    }

    private IEnumerator ResendUntilAcknowledged(int eventId)
    {
        while (pendingEvents.ContainsKey(eventId))
        {
            yield return new WaitForSeconds(resendDelay);

            if (!pendingEvents.ContainsKey(eventId))
                yield break;

            Debug.LogWarning("[CLIENT " + localPlayerId + "] ACK не получен. Повторная отправка события ID=" + eventId);

            if (ResearchMetrics.Instance != null)
            {
                ResearchMetrics.Instance.RegisterResend();
            }

            SendPuzzleEventMessage(pendingEvents[eventId]);
        }
    }

    public void OnNetworkMessageReceived(NetworkMessage message)
    {
        if (message == null)
        {
            Debug.LogError("[EventFsmSyncManager] Получено пустое сетевое сообщение");
            return;
        }

        if (message.messageType == NetworkMessageType.ClientHello)
        {
            Debug.Log("[EventFsmSyncManager] Получен ClientHello от " + message.senderPlayerId);
            return;
        }

        if (message.messageType == NetworkMessageType.Acknowledgement)
        {
            if (!UseReliableMethod)
                return;

            HandleAcknowledgement(message);
            return;
        }

        if (message.messageType == NetworkMessageType.PuzzleEvent)
        {
            if (message.puzzleEvent == null)
            {
                Debug.LogError("[EventFsmSyncManager] Получен PuzzleEvent, но он пустой");
                return;
            }

            if (role == NetworkRole.Server)
            {
                if (UseReliableMethod)
                {
                    HandlePuzzleEventOnServer(message.puzzleEvent);
                }
                else
                {
                    HandleBasicPuzzleEventOnServer(message.puzzleEvent);
                }
            }
            else
            {
                if (UseReliableMethod)
                {
                    HandleApprovedPuzzleEventOnClient(message.puzzleEvent);
                }
                else
                {
                    HandleBasicPuzzleEventOnClient(message.puzzleEvent);
                }
            }

            return;
        }

        Debug.LogWarning("[EventFsmSyncManager] Неизвестный тип сообщения: " + message.messageType);
    }

    private void HandleAcknowledgement(NetworkMessage message)
    {
        if (role != NetworkRole.Client)
            return;

        if (message.targetPlayerId != localPlayerId)
            return;

        if (pendingEvents.TryGetValue(message.ackForEventId, out PuzzleEvent acknowledgedEvent))
        {
            pendingEvents.Remove(message.ackForEventId);

            Debug.Log("[CLIENT " + localPlayerId + "] Получен ACK. Событие удалено из очереди ожидания: " + message.ackForEventId);

            if (ResearchMetrics.Instance != null)
            {
                ResearchMetrics.Instance.RegisterAck(message.ackForEventId);
            }

            string eventKey = GetEventKey(acknowledgedEvent);

            if (!appliedEventKeys.Contains(eventKey))
            {
                bool success = ApplyEventToFsm(acknowledgedEvent);

                if (success)
                {
                    appliedEventKeys.Add(eventKey);

                    if (ResearchMetrics.Instance != null)
                    {
                        ResearchMetrics.Instance.RegisterAppliedEvent();
                    }

                    Debug.Log("[CLIENT " + localPlayerId + "] Собственное событие применено по ACK: " + eventKey);
                }
                else
                {
                    Debug.LogWarning("[CLIENT " + localPlayerId + "] ACK получен, но собственное событие не применено локально: " + eventKey);
                }
            }
        }
        else
        {
            Debug.LogWarning("[CLIENT " + localPlayerId + "] ACK получен, но событие уже не ожидалось: " + message.ackForEventId);
        }
    }

    private void HandlePuzzleEventOnServer(PuzzleEvent puzzleEvent)
    {
        string eventKey = GetEventKey(puzzleEvent);

        Debug.Log("[SERVER] Получено событие: " + puzzleEvent.eventType +
                  " | Key=" + eventKey +
                  " | Seq=" + puzzleEvent.sequenceNumber +
                  " | Player=" + puzzleEvent.sourcePlayerId);

        if (appliedEventKeys.Contains(eventKey))
        {
            Debug.LogWarning("[SERVER] Повтор события. FSM не меняем, отправляем только ACK: " + eventKey);

            if (ResearchMetrics.Instance != null)
            {
                ResearchMetrics.Instance.RegisterDuplicateRejected();
            }

            SendAcknowledgement(puzzleEvent.eventId, puzzleEvent.sourcePlayerId);
            return;
        }

        if (!expectedSequenceByPlayer.ContainsKey(puzzleEvent.sourcePlayerId))
        {
            expectedSequenceByPlayer[puzzleEvent.sourcePlayerId] = 1;
        }

        int expectedSequence = expectedSequenceByPlayer[puzzleEvent.sourcePlayerId];

        if (puzzleEvent.sequenceNumber < expectedSequence)
        {
            Debug.LogWarning("[SERVER] Старое событие. Ожидали Seq=" +
                             expectedSequence + ", получили Seq=" + puzzleEvent.sequenceNumber);

            if (ResearchMetrics.Instance != null)
            {
                ResearchMetrics.Instance.RegisterWrongOrderRejected();
            }

            SendAcknowledgement(puzzleEvent.eventId, puzzleEvent.sourcePlayerId);
            return;
        }

        if (puzzleEvent.sequenceNumber > expectedSequence)
        {
            Debug.LogWarning("[SERVER] Событие пришло слишком рано. Ожидали Seq=" +
                             expectedSequence + ", получили Seq=" + puzzleEvent.sequenceNumber);

            if (ResearchMetrics.Instance != null)
            {
                ResearchMetrics.Instance.RegisterWrongOrderRejected();
            }

            return;
        }

        bool success = ApplyEventToFsm(puzzleEvent);

        if (!success)
        {
            Debug.LogWarning("[SERVER] Событие не прошло проверку FSM и не будет подтверждено");
            return;
        }

        appliedEventKeys.Add(eventKey);
        expectedSequenceByPlayer[puzzleEvent.sourcePlayerId] = expectedSequence + 1;

        if (ResearchMetrics.Instance != null)
        {
            ResearchMetrics.Instance.RegisterAppliedEvent();
        }

        Debug.Log("[SERVER] Событие применено. Следующий ожидаемый Seq для " +
                  puzzleEvent.sourcePlayerId + " = " +
                  expectedSequenceByPlayer[puzzleEvent.sourcePlayerId]);

        SendAcknowledgement(puzzleEvent.eventId, puzzleEvent.sourcePlayerId);
        SendPuzzleEventMessage(puzzleEvent);
    }

    private void HandleBasicPuzzleEventOnServer(PuzzleEvent puzzleEvent)
    {
        string eventKey = GetEventKey(puzzleEvent);

        Debug.Log("[BASE SERVER] Received event: " + puzzleEvent.eventType +
                  " | Key=" + eventKey +
                  " | Seq=" + puzzleEvent.sequenceNumber +
                  " | Player=" + puzzleEvent.sourcePlayerId);

        bool success = ApplyEventToFsm(puzzleEvent);

        if (!success)
        {
            Debug.LogWarning("[BASE SERVER] Event was not applied to FSM: " + eventKey);
            return;
        }

        if (ResearchMetrics.Instance != null)
        {
            ResearchMetrics.Instance.RegisterAppliedEvent();
        }

        SendPuzzleEventMessage(puzzleEvent);
    }

    private void HandleApprovedPuzzleEventOnClient(PuzzleEvent puzzleEvent)
    {
        string eventKey = GetEventKey(puzzleEvent);

        Debug.Log("[CLIENT " + localPlayerId + "] Получено подтверждённое сервером событие: " +
                  puzzleEvent.eventType + " | Key=" + eventKey);

        if (puzzleEvent.sourcePlayerId == localPlayerId)
        {
            pendingEvents.Remove(puzzleEvent.eventId);
        }

        if (appliedEventKeys.Contains(eventKey))
        {
            Debug.LogWarning("[CLIENT " + localPlayerId + "] Повтор подтверждённого события отклонён: " + eventKey);

            if (ResearchMetrics.Instance != null)
            {
                ResearchMetrics.Instance.RegisterDuplicateRejected();
            }

            return;
        }

        bool success = ApplyEventToFsm(puzzleEvent);

        if (success)
        {
            appliedEventKeys.Add(eventKey);

            if (ResearchMetrics.Instance != null)
            {
                ResearchMetrics.Instance.RegisterAppliedEvent();
            }

            Debug.Log("[CLIENT " + localPlayerId + "] Событие применено локально: " + eventKey);
        }
        else
        {
            Debug.Log("[CLIENT " + localPlayerId + "] На этом клиенте нет целевого объекта, событие пропущено: " + puzzleEvent.targetObjectId);
        }
    }

    private void HandleBasicPuzzleEventOnClient(PuzzleEvent puzzleEvent)
    {
        string eventKey = GetEventKey(puzzleEvent);

        Debug.Log("[BASE CLIENT " + localPlayerId + "] Received server event: " +
                  puzzleEvent.eventType + " | Key=" + eventKey);

        bool success = ApplyEventToFsm(puzzleEvent);

        if (success)
        {
            if (ResearchMetrics.Instance != null)
            {
                ResearchMetrics.Instance.RegisterAppliedEvent();

                if (puzzleEvent.sourcePlayerId == localPlayerId)
                {
                    ResearchMetrics.Instance.RegisterAck(puzzleEvent.eventId);
                }
            }

            Debug.Log("[BASE CLIENT " + localPlayerId + "] Event applied locally: " + eventKey);
        }
        else
        {
            Debug.Log("[BASE CLIENT " + localPlayerId + "] Event was not applied locally: " + eventKey);
        }
    }

    private bool ApplyEventToFsm(PuzzleEvent puzzleEvent)
    {
        Debug.Log("[FSM] Применение события: " +
                  puzzleEvent.eventType +
                  " | SourceObject=" + puzzleEvent.sourceObjectId +
                  " | TargetObject=" + puzzleEvent.targetObjectId);

        if (!registeredObjects.TryGetValue(puzzleEvent.targetObjectId, out IFsmPuzzleObject targetObject))
        {
            Debug.LogError("[FSM] Целевой объект не найден: " + puzzleEvent.targetObjectId);
            return false;
        }

        return targetObject.ApplyPuzzleEvent(puzzleEvent);
    }

    private void SendPuzzleEventMessage(PuzzleEvent puzzleEvent)
    {
        NetworkMessage message = NetworkMessage.CreatePuzzleEventMessage(puzzleEvent);
        SendMessageToTransport(message);
    }

    private void SendAcknowledgement(int eventId, string targetPlayerId)
    {
        NetworkMessage ackMessage = NetworkMessage.CreateAcknowledgementMessage(eventId, targetPlayerId);
        SendMessageToTransport(ackMessage);

        Debug.Log("[SERVER] Отправлен ACK для события: " + eventId + " игроку " + targetPlayerId);
    }

    private void SendMessageToTransport(NetworkMessage message)
    {
        if (UdpEventTransport.Instance == null)
        {
            Debug.LogError("[EventFsmSyncManager] UdpEventTransport не найден. Сообщение не отправлено.");
            return;
        }

        UdpEventTransport.Instance.SendNetworkMessage(message);
    }

    public void BroadcastServerGeneratedEvent(string sourceObjectId, string targetObjectId, PuzzleEventType eventType)
    {
        if (role != NetworkRole.Server)
        {
            Debug.LogWarning("[EventFsmSyncManager] Только сервер может создавать серверные события");
            return;
        }

        PuzzleEvent puzzleEvent = new PuzzleEvent(
            nextEventId++,
            nextSequenceNumber++,
            "Server",
            sourceObjectId,
            targetObjectId,
            eventType
        );

        Debug.Log("[SERVER] Создано серверное событие: " +
                  eventType + " | Target=" + targetObjectId);

        SendPuzzleEventMessage(puzzleEvent);
    }
}
