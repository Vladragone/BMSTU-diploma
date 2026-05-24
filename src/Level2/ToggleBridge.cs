using System.Collections;
using UnityEngine;

public class ToggleBridge : MonoBehaviour, IFsmPuzzleObject
{
    [Header("FSM Object Settings")]
    public string objectId = "Bridge_A";

    [Header("Bridge Movement")]
    public Vector3 extendedOffset = new Vector3(0, 0, 6);

    public float moveSpeed = 3f;

    private Vector3 retractedPosition;
    private Vector3 extendedPosition;

    private Coroutine moveCoroutine;

    private BridgeStateMachine stateMachine;

    public string ObjectId => objectId;

    private void Start()
    {
        retractedPosition = transform.position;
        extendedPosition = retractedPosition + extendedOffset;

        stateMachine = new BridgeStateMachine();

        if (EventFsmSyncManager.Instance != null)
        {
            EventFsmSyncManager.Instance.RegisterObject(this);
        }
        else
        {
            Debug.LogError("[ToggleBridge] EventFsmSyncManager не найден");
        }
    }

    public bool ApplyPuzzleEvent(PuzzleEvent puzzleEvent)
    {
        if (puzzleEvent.eventType != PuzzleEventType.BridgeActivated)
        {
            Debug.LogWarning("[ToggleBridge] Неподходящее событие: "
                             + puzzleEvent.eventType);

            return false;
        }

        bool success = stateMachine.ApplyEvent(BridgeEvent.ToggleBridge);

        if (!success)
        {
            return false;
        }

        UpdateBridgeVisual();

        return true;
    }

    private void UpdateBridgeVisual()
    {
        if (stateMachine.CurrentState == BridgeState.Extended)
        {
            MoveTo(extendedPosition);
        }
        else
        {
            MoveTo(retractedPosition);
        }
    }

    private void MoveTo(Vector3 targetPosition)
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }

        moveCoroutine = StartCoroutine(MoveBridgeCoroutine(targetPosition));
    }

    private IEnumerator MoveBridgeCoroutine(Vector3 targetPosition)
    {
        Debug.Log("[ToggleBridge] Движение моста: " + objectId);

        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.position = targetPosition;
    }
}