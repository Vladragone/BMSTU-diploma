using UnityEngine;

public class ColorButton : MonoBehaviour, IInteractable
{
    [Header("Button Settings")]
    public string buttonId = "ColorButton_Red";
    public ColorType colorType = ColorType.Red;

    [Header("Target FSM")]
    public string targetObjectId = "Level3_ColorSequence";

    public void Interact()
    {
        if (EventFsmSyncManager.Instance == null)
        {
            Debug.LogError("[ColorButton] EventFsmSyncManager не найден");
            return;
        }

        PuzzleEventType eventType = GetEventType();

        Debug.Log("[ColorButton] Нажата цветная кнопка: " + colorType);

        EventFsmSyncManager.Instance.CreateAndSendEvent(
            buttonId,
            targetObjectId,
            eventType
        );
    }

    private PuzzleEventType GetEventType()
    {
        switch (colorType)
        {
            case ColorType.Red:
                return PuzzleEventType.ColorRedPressed;

            case ColorType.Blue:
                return PuzzleEventType.ColorBluePressed;

            case ColorType.Green:
                return PuzzleEventType.ColorGreenPressed;
        }

        return PuzzleEventType.ColorRedPressed;
    }
}