using UnityEngine;

public class PuzzleButton : MonoBehaviour, IInteractable
{
    [Header("Button Settings")]
    public string buttonId = "Button_A";

    [Header("Event Target")]
    public string targetObjectId = "Door_B";

    public void Interact()
    {
        EventFsmSyncManager.Instance.CreateAndSendEvent(
            buttonId,
            targetObjectId,
            PuzzleEventType.ButtonPressed
        );
    }
}