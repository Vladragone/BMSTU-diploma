[System.Serializable]
public class NetworkMessage
{
    public NetworkMessageType messageType;

    public PuzzleEvent puzzleEvent;

    public int ackForEventId;
    public string senderPlayerId;
    public string targetPlayerId;

    public NetworkMessage(NetworkMessageType messageType)
    {
        this.messageType = messageType;
        this.puzzleEvent = null;
        this.ackForEventId = -1;
        this.senderPlayerId = "";
        this.targetPlayerId = "";
    }

    public static NetworkMessage CreateClientHelloMessage(string playerId)
    {
        NetworkMessage message = new NetworkMessage(NetworkMessageType.ClientHello);
        message.senderPlayerId = playerId;
        return message;
    }

    public static NetworkMessage CreatePuzzleEventMessage(PuzzleEvent puzzleEvent)
    {
        NetworkMessage message = new NetworkMessage(NetworkMessageType.PuzzleEvent);
        message.puzzleEvent = puzzleEvent;
        message.senderPlayerId = puzzleEvent.sourcePlayerId;
        return message;
    }

    public static NetworkMessage CreateAcknowledgementMessage(int ackForEventId, string targetPlayerId)
    {
        NetworkMessage message = new NetworkMessage(NetworkMessageType.Acknowledgement);
        message.ackForEventId = ackForEventId;
        message.targetPlayerId = targetPlayerId;
        return message;
    }
}