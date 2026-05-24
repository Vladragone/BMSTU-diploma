using System.Collections;
using UnityEngine;

public class PuzzleDoor : MonoBehaviour, IFsmPuzzleObject
{
    [Header("FSM Object Settings")]
    public string objectId = "Door_A";

    [Header("Door Movement")]
    public float openHeight = 3f;
    public float moveSpeed = 3f;

    private DoorStateMachine stateMachine;
    private Vector3 closedPosition;
    private Vector3 openPosition;
    private Coroutine moveCoroutine;

    private bool isInitialized;

    public string ObjectId => objectId;

    private void Awake()
    {
        InitializeDoor();
    }

    private void Start()
    {
        if (EventFsmSyncManager.Instance != null)
        {
            EventFsmSyncManager.Instance.RegisterObject(this);
        }
        else
        {
            Debug.LogError("[PuzzleDoor] EventFsmSyncManager не найден на сцене");
        }
    }

    private void InitializeDoor()
    {
        if (isInitialized)
            return;

        closedPosition = transform.position;
        openPosition = closedPosition + Vector3.up * openHeight;

        stateMachine = new DoorStateMachine();

        isInitialized = true;

        Debug.Log("[PuzzleDoor] Инициализирована дверь: " + objectId);
    }

    public bool ApplyPuzzleEvent(PuzzleEvent puzzleEvent)
    {
        InitializeDoor();

        if (puzzleEvent.eventType == PuzzleEventType.ButtonPressed)
        {
            return OpenByFsm();
        }

        Debug.LogWarning("[PuzzleDoor] Неподходящее событие для двери: " + puzzleEvent.eventType);
        return false;
    }

    public void SetDoorOpen(bool isOpen)
    {
        InitializeDoor();

        if (isOpen)
        {
            OpenByFsm();
        }
        else
        {
            CloseByFsm();
        }
    }

    private bool OpenByFsm()
    {
        InitializeDoor();

        bool success = stateMachine.ApplyEvent(DoorEvent.OpenDoor);

        if (success)
        {
            MoveTo(openPosition);
        }

        return success;
    }

    private bool CloseByFsm()
    {
        InitializeDoor();

        bool success = stateMachine.ApplyEvent(DoorEvent.CloseDoor);

        if (success)
        {
            MoveTo(closedPosition);
        }

        return success;
    }

    private void MoveTo(Vector3 targetPosition)
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }

        moveCoroutine = StartCoroutine(MoveDoorCoroutine(targetPosition));
    }

    private IEnumerator MoveDoorCoroutine(Vector3 targetPosition)
    {
        Debug.Log("[PuzzleDoor] Дверь плавно двигается: " + objectId);

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