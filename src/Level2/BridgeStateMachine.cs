using UnityEngine;

public class BridgeStateMachine
{
    public BridgeState CurrentState { get; private set; }

    public BridgeStateMachine()
    {
        CurrentState = BridgeState.Retracted;
    }

    public bool ApplyEvent(BridgeEvent bridgeEvent)
    {
        Debug.Log("[BRIDGE FSM] State: " + CurrentState +
                  " Event: " + bridgeEvent);

        if (bridgeEvent != BridgeEvent.ToggleBridge)
        {
            return false;
        }

        if (CurrentState == BridgeState.Retracted)
        {
            CurrentState = BridgeState.Extended;

            Debug.Log("[BRIDGE FSM] Retracted -> Extended");

            return true;
        }

        if (CurrentState == BridgeState.Extended)
        {
            CurrentState = BridgeState.Retracted;

            Debug.Log("[BRIDGE FSM] Extended -> Retracted");

            return true;
        }

        return false;
    }
}