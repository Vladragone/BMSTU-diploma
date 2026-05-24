using UnityEngine;

public class CoopHoldPuzzleController : MonoBehaviour, IFsmPuzzleObject
{
    [Header("FSM Object Settings")]
    public string objectId = "Level1_Controller";

    [Header("Doors")]
    public PuzzleDoor doorA;
    public PuzzleDoor doorB;

    public CoopPuzzleState currentState = CoopPuzzleState.Idle;

    private bool isPlayerAPressing;
    private bool isPlayerBPressing;

    public string ObjectId => objectId;

    private void Start()
    {
        if (EventFsmSyncManager.Instance != null)
        {
            EventFsmSyncManager.Instance.RegisterObject(this);
        }
        else
        {
            Debug.LogError("[Level1] EventFsmSyncManager не найден");
        }
    }

    public bool ApplyPuzzleEvent(PuzzleEvent puzzleEvent)
    {
        if (puzzleEvent.eventType != PuzzleEventType.PressurePlatePressed &&
            puzzleEvent.eventType != PuzzleEventType.PressurePlateReleased)
        {
            Debug.LogWarning("[Level1] Неподходящее событие: " + puzzleEvent.eventType);
            return false;
        }

        CoopPuzzleEvent coopEvent = ConvertToCoopEvent(puzzleEvent);

        ApplyEvent(coopEvent);

        return true;
    }

    private CoopPuzzleEvent ConvertToCoopEvent(PuzzleEvent puzzleEvent)
    {
        bool isPressed = puzzleEvent.eventType == PuzzleEventType.PressurePlatePressed;

        if (puzzleEvent.sourcePlayerId == "PlayerA")
        {
            return isPressed
                ? CoopPuzzleEvent.PlayerAPressed
                : CoopPuzzleEvent.PlayerAReleased;
        }

        if (puzzleEvent.sourcePlayerId == "PlayerB")
        {
            return isPressed
                ? CoopPuzzleEvent.PlayerBPressed
                : CoopPuzzleEvent.PlayerBReleased;
        }

        Debug.LogWarning("[Level1] Неизвестный игрок: " + puzzleEvent.sourcePlayerId);
        return CoopPuzzleEvent.PlayerAReleased;
    }

    public void ApplyEvent(CoopPuzzleEvent puzzleEvent)
    {
        Debug.Log("[LEVEL1 FSM] Event: " + puzzleEvent);

        switch (puzzleEvent)
        {
            case CoopPuzzleEvent.PlayerAPressed:
                isPlayerAPressing = true;
                break;

            case CoopPuzzleEvent.PlayerAReleased:
                isPlayerAPressing = false;
                break;

            case CoopPuzzleEvent.PlayerBPressed:
                isPlayerBPressing = true;
                break;

            case CoopPuzzleEvent.PlayerBReleased:
                isPlayerBPressing = false;
                break;
        }

        RecalculateState();
    }

    private void RecalculateState()
    {
        CoopPuzzleState previousState = currentState;

        if (isPlayerAPressing && isPlayerBPressing)
        {
            currentState = CoopPuzzleState.BothPressed;
        }
        else if (isPlayerAPressing)
        {
            currentState = CoopPuzzleState.OnlyA;
        }
        else if (isPlayerBPressing)
        {
            currentState = CoopPuzzleState.OnlyB;
        }
        else
        {
            currentState = CoopPuzzleState.Idle;
        }

        Debug.Log("[LEVEL1 FSM] State: " + previousState + " -> " + currentState);

        bool shouldOpenDoors = currentState == CoopPuzzleState.BothPressed;

        if (doorA != null)
        {
            doorA.SetDoorOpen(shouldOpenDoors);
        }

        if (doorB != null)
        {
            doorB.SetDoorOpen(shouldOpenDoors);
        }
    }
}