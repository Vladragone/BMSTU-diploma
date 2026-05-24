using UnityEngine;

public class PressurePlateEventSender : MonoBehaviour
{
    [Header("Plate Settings")]
    public string plateId = "Plate_A";

    [Header("Target FSM Object")]
    public string targetObjectId = "Level1_Controller";

    private bool isPressed;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        if (isPressed)
            return;

        isPressed = true;

        Debug.Log("[PressurePlate] Нажата плита: " + plateId);

        SendPlateEvent(PuzzleEventType.PressurePlatePressed);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;

        if (!isPressed)
            return;

        isPressed = false;

        Debug.Log("[PressurePlate] Отпущена плита: " + plateId);

        SendPlateEvent(PuzzleEventType.PressurePlateReleased);
    }

    private bool IsPlayer(Collider other)
    {
        return other.GetComponentInParent<SimpleFpsController>() != null;
    }

    private void SendPlateEvent(PuzzleEventType eventType)
    {
        if (EventFsmSyncManager.Instance == null)
        {
            Debug.LogError("[PressurePlate] EventFsmSyncManager не найден");
            return;
        }

        EventFsmSyncManager.Instance.CreateAndSendEvent(
            plateId,
            targetObjectId,
            eventType
        );
    }
}