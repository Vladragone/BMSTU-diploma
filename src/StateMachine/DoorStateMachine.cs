using UnityEngine;

public class DoorStateMachine
{
    public DoorState CurrentState { get; private set; }

    public DoorStateMachine()
    {
        CurrentState = DoorState.Closed;
    }

    public bool ApplyEvent(DoorEvent doorEvent)
    {
        Debug.Log($"[DOOR FSM] State: {CurrentState}, Event: {doorEvent}");

        if (CurrentState == DoorState.Closed && doorEvent == DoorEvent.OpenDoor)
        {
            CurrentState = DoorState.Open;
            Debug.Log("[DOOR FSM] Closed -> Open");
            return true;
        }

        if (CurrentState == DoorState.Open && doorEvent == DoorEvent.CloseDoor)
        {
            CurrentState = DoorState.Closed;
            Debug.Log("[DOOR FSM] Open -> Closed");
            return true;
        }

        Debug.LogWarning("[DOOR FSM] Событие отклонено");
        return false;
    }
}