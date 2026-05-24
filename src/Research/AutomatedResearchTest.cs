using System.Collections;
using UnityEngine;

public class AutomatedResearchTest : MonoBehaviour
{
    [Header("Test Settings")]
    public bool autoStart = false;

    public int eventCount = 100;

    public float interval = 0.2f;

    public float resultWaitTime = 10f;

    [Header("Target")]
    public bool usePressurePlateScenario = true;

    public string sourceObjectId = "Button_A";
    public string targetObjectId = "Door_B";
    public PuzzleEventType eventType = PuzzleEventType.ButtonPressed;

    private bool isRunning;

    private void Start()
    {
        if (autoStart)
        {
            StartCoroutine(RunTest());
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F7))
        {
            if (!isRunning)
            {
                StartCoroutine(RunTest());
            }
        }
    }

    private IEnumerator RunTest()
    {
        isRunning = true;

        Debug.Log("========== AUTO TEST START ==========");

        if (ResearchMetrics.Instance != null)
        {
            ResearchMetrics.Instance.ResetStatistics();
        }

        yield return new WaitForSeconds(1f);

        for (int i = 0; i < eventCount; i++)
        {
            if (EventFsmSyncManager.Instance != null)
            {
                EventFsmSyncManager.Instance.CreateAndSendEvent(
                    GetSourceObjectId(),
                    GetTargetObjectId(),
                    GetEventType(i)
                );
            }

            yield return new WaitForSeconds(interval);
        }

        Debug.Log("========== AUTO TEST FINISHED ==========");

        yield return new WaitForSeconds(resultWaitTime);

        if (ResearchMetrics.Instance != null)
        {
            ResearchMetrics.Instance.PrintStatistics();
        }

        isRunning = false;
    }

    private string GetSourceObjectId()
    {
        if (!usePressurePlateScenario)
            return sourceObjectId;

        if (EventFsmSyncManager.Instance != null &&
            EventFsmSyncManager.Instance.localPlayerId == "PlayerB")
        {
            return "Level1_Plate_B";
        }

        return "Level1_Plate_A";
    }

    private string GetTargetObjectId()
    {
        if (usePressurePlateScenario)
            return "Level1_Controller";

        return targetObjectId;
    }

    private PuzzleEventType GetEventType(int index)
    {
        if (!usePressurePlateScenario)
            return eventType;

        return index % 2 == 0
            ? PuzzleEventType.PressurePlatePressed
            : PuzzleEventType.PressurePlateReleased;
    }
}
