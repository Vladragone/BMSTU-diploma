[System.Serializable]
public class PuzzleEvent
{
    public int eventId;
    public int sequenceNumber;

    public string sourcePlayerId;
    public string sourceObjectId;
    public string targetObjectId;

    public PuzzleEventType eventType;

    public PuzzleEvent(
        int eventId,
        int sequenceNumber,
        string sourcePlayerId,
        string sourceObjectId,
        string targetObjectId,
        PuzzleEventType eventType)
    {
        this.eventId = eventId;
        this.sequenceNumber = sequenceNumber;
        this.sourcePlayerId = sourcePlayerId;
        this.sourceObjectId = sourceObjectId;
        this.targetObjectId = targetObjectId;
        this.eventType = eventType;
    }
}