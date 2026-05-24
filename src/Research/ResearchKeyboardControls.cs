using UnityEngine;

public class ResearchKeyboardControls : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            if (ResearchMetrics.Instance != null)
            {
                ResearchMetrics.Instance.PrintStatistics();
            }
        }

        if (Input.GetKeyDown(KeyCode.F6))
        {
            if (ResearchMetrics.Instance != null)
            {
                ResearchMetrics.Instance.ResetStatistics();
            }
        }
    }
}