using UnityEngine;

public class BridgeLever : MonoBehaviour, IInteractable
{
    [Header("Lever Settings")]
    public string leverId = "Lever_A";

    [Header("Target Bridge")]
    public string targetBridgeId = "Bridge_B";

    public void Interact()
    {
        if (EventFsmSyncManager.Instance == null)
        {
            Debug.LogError("[BridgeLever] EventFsmSyncManager не найден");
            return;
        }

        Debug.Log("[BridgeLever] Рычаг переключён: " + leverId);

        EventFsmSyncManager.Instance.CreateAndSendEvent(
            leverId,
            targetBridgeId,
            PuzzleEventType.BridgeActivated
        );
    }
}