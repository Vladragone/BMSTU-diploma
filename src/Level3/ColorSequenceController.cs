using System.Collections;
using UnityEngine;

public class ColorSequenceController : MonoBehaviour, IFsmPuzzleObject
{
    [Header("FSM Object Settings")]
    public string objectId = "Level3_ColorSequence";

    [Header("Correct Sequence")]
    public ColorType firstColor = ColorType.Red;
    public ColorType secondColor = ColorType.Blue;
    public ColorType thirdColor = ColorType.Green;

    [Header("Displays")]
    public SequenceDisplay playerADisplay;
    public SequenceDisplay playerBDisplay;

    [Header("Final Doors")]
    public PuzzleDoor finalDoorA;
    public PuzzleDoor finalDoorB;

    [Header("Error Signals")]
    public ErrorSignal errorSignalA;
    public ErrorSignal errorSignalB;

    public ColorSequenceState currentState = ColorSequenceState.WaitingInput;

    private ColorType[] correctSequence;
    private int currentIndex;

    public string ObjectId => objectId;

    private void Start()
    {
        correctSequence = new ColorType[]
        {
            firstColor,
            secondColor,
            thirdColor
        };

        ResetSequenceVisuals();

        if (EventFsmSyncManager.Instance != null)
        {
            EventFsmSyncManager.Instance.RegisterObject(this);
        }
        else
        {
            Debug.LogError("[Level3] EventFsmSyncManager не найден");
        }
    }

    public bool ApplyPuzzleEvent(PuzzleEvent puzzleEvent)
    {
        if (puzzleEvent.eventType == PuzzleEventType.Level3Solved)
        {
            Debug.Log("[LEVEL3 FSM] Получено событие Level3Solved");
            ApplySolvedEvent();
            return true;
        }

        if (!IsColorEvent(puzzleEvent.eventType))
        {
            Debug.LogWarning("[Level3] Неподходящее событие: " + puzzleEvent.eventType);
            return false;
        }

        ColorType pressedColor = ConvertEventToColor(puzzleEvent.eventType);

        Debug.Log("[LEVEL3 FSM] Получен цвет: " + pressedColor);

        ApplyColorInput(pressedColor);

        return true;
    }

    private void ApplyColorInput(ColorType pressedColor)
    {
        if (currentState == ColorSequenceState.Solved)
        {
            Debug.Log("[LEVEL3 FSM] Последовательность уже решена");
            return;
        }

        ColorType expectedColor = correctSequence[currentIndex];

        if (pressedColor == expectedColor)
        {
            ApplyCorrectColor(pressedColor);
        }
        else
        {
            ApplyWrongColor(pressedColor);
        }
    }

    private void ApplyCorrectColor(ColorType pressedColor)
    {
        Debug.Log("[LEVEL3 FSM] Верный цвет: " + pressedColor);

        SetDisplayColor(currentIndex, pressedColor);

        currentIndex++;

        if (currentIndex >= correctSequence.Length)
        {
            SolvePuzzle();
        }
        else
        {
            currentState = ColorSequenceState.WaitingInput;
            Debug.Log("[LEVEL3 FSM] Ожидаем следующий цвет. Index = " + currentIndex);
        }
    }

    private void ApplyWrongColor(ColorType pressedColor)
    {
        Debug.LogWarning("[LEVEL3 FSM] Неверный цвет: " + pressedColor);

        currentState = ColorSequenceState.Error;

        if (errorSignalA != null)
        {
            errorSignalA.PlayError();
        }

        if (errorSignalB != null)
        {
            errorSignalB.PlayError();
        }

        StartCoroutine(ResetAfterError());
    }

    private IEnumerator ResetAfterError()
    {
        yield return new WaitForSeconds(1.2f);

        currentIndex = 0;
        currentState = ColorSequenceState.WaitingInput;

        ResetSequenceVisuals();

        Debug.Log("[LEVEL3 FSM] Ошибка обработана, последовательность сброшена");
    }

    private void SolvePuzzle()
    {
        currentState = ColorSequenceState.Solved;

        Debug.Log("[LEVEL3 FSM] Последовательность решена");

        ApplySolvedEvent();

        if (EventFsmSyncManager.Instance != null &&
            EventFsmSyncManager.Instance.role == NetworkRole.Server)
        {
            EventFsmSyncManager.Instance.BroadcastServerGeneratedEvent(
                objectId,
                objectId,
                PuzzleEventType.Level3Solved
            );
        }
    }

    private void ApplySolvedEvent()
    {
        currentState = ColorSequenceState.Solved;
        currentIndex = correctSequence.Length;

        SetDisplayColor(0, correctSequence[0]);
        SetDisplayColor(1, correctSequence[1]);
        SetDisplayColor(2, correctSequence[2]);

        OpenFinalDoors();
    }

    private void OpenFinalDoors()
    {
        if (finalDoorA != null)
        {
            finalDoorA.SetDoorOpen(true);
        }

        if (finalDoorB != null)
        {
            finalDoorB.SetDoorOpen(true);
        }

        Debug.Log("[LEVEL3 FSM] Финальные двери открыты событием Level3Solved");
    }

    private void SetDisplayColor(int index, ColorType colorType)
    {
        if (playerADisplay != null)
        {
            playerADisplay.SetSlotColor(index, colorType);
        }

        if (playerBDisplay != null)
        {
            playerBDisplay.SetSlotColor(index, colorType);
        }
    }

    private void ResetSequenceVisuals()
    {
        if (playerADisplay != null)
        {
            playerADisplay.ResetDisplay();
        }

        if (playerBDisplay != null)
        {
            playerBDisplay.ResetDisplay();
        }
    }

    private bool IsColorEvent(PuzzleEventType eventType)
    {
        return eventType == PuzzleEventType.ColorRedPressed ||
               eventType == PuzzleEventType.ColorBluePressed ||
               eventType == PuzzleEventType.ColorGreenPressed;
    }

    private ColorType ConvertEventToColor(PuzzleEventType eventType)
    {
        switch (eventType)
        {
            case PuzzleEventType.ColorRedPressed:
                return ColorType.Red;

            case PuzzleEventType.ColorBluePressed:
                return ColorType.Blue;

            case PuzzleEventType.ColorGreenPressed:
                return ColorType.Green;
        }

        return ColorType.Red;
    }
}