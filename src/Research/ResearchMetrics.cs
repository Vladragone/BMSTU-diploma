using System.Collections.Generic;
using UnityEngine;

public class ResearchMetrics : MonoBehaviour
{
    public static ResearchMetrics Instance { get; private set; }

    [Header("Runtime Metrics")]
    public int createdEvents;
    public int sentMessages;
    public int lostPackets;
    public int resendCount;
    public int duplicateRejected;
    public int wrongOrderRejected;
    public int appliedEvents;

    private readonly Dictionary<int, float> eventCreateTimes =
        new Dictionary<int, float>();

    private readonly List<float> ackTimes =
        new List<float>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RegisterSentMessage()
    {
        sentMessages++;
    }

    public void RegisterPacketLoss()
    {
        lostPackets++;
    }

    public void RegisterResend()
    {
        resendCount++;
    }

    public void RegisterDuplicateRejected()
    {
        duplicateRejected++;
    }

    public void RegisterWrongOrderRejected()
    {
        wrongOrderRejected++;
    }

    public void RegisterAppliedEvent()
    {
        appliedEvents++;
    }

    public void RegisterCreatedEvent(int eventId)
    {
        createdEvents++;
        eventCreateTimes[eventId] = Time.time;
    }

    public void RegisterAck(int eventId)
    {
        if (!eventCreateTimes.ContainsKey(eventId))
            return;

        float delay = Time.time - eventCreateTimes[eventId];

        ackTimes.Add(delay);

        Debug.Log("[RESEARCH] RTT for Event " +
                  eventId +
                  " = " +
                  delay.ToString("F3") +
                  " sec");
    }

    public float GetAverageAckTime()
    {
        if (ackTimes.Count == 0)
            return 0f;

        float sum = 0f;

        foreach (float time in ackTimes)
        {
            sum += time;
        }

        return sum / ackTimes.Count;
    }

    public void PrintStatistics()
    {
        string mode = "Unknown";

        if (EventFsmSyncManager.Instance != null)
        {
            mode = EventFsmSyncManager.Instance.synchronizationMode.ToString();
        }

        Debug.Log("========== NETWORK RESEARCH ==========");
        Debug.Log("Synchronization Mode: " + mode);
        Debug.Log("Created Events: " + createdEvents);
        Debug.Log("Sent Messages: " + sentMessages);
        Debug.Log("Lost Packets: " + lostPackets);
        Debug.Log("Resends: " + resendCount);
        Debug.Log("Duplicate Rejected: " + duplicateRejected);
        Debug.Log("Wrong Order Rejected: " + wrongOrderRejected);
        Debug.Log("Applied Events: " + appliedEvents);
        Debug.Log("Applied Ratio: " + GetAppliedRatio().ToString("F3"));
        Debug.Log("Average RTT: " + GetAverageAckTime().ToString("F3") + " sec");
        Debug.Log("======================================");
    }

    private float GetAppliedRatio()
    {
        if (createdEvents == 0)
            return 0f;

        return (float)appliedEvents / createdEvents;
    }

    public void ResetStatistics()
    {
        createdEvents = 0;
        sentMessages = 0;
        lostPackets = 0;
        resendCount = 0;
        duplicateRejected = 0;
        wrongOrderRejected = 0;
        appliedEvents = 0;

        eventCreateTimes.Clear();
        ackTimes.Clear();

        Debug.Log("[RESEARCH] Statistics reset");
    }
}
