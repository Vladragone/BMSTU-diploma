using UnityEngine;

public class FinalTrigger : MonoBehaviour
{
    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered)
            return;

        if (other.GetComponentInParent<SimpleFpsController>() == null)
            return;

        triggered = true;

        Debug.Log("[FINAL] Игрок достиг финиша");

        if (FinalSequenceManager.Instance != null)
        {
            FinalSequenceManager.Instance.PlayFinalSequence();
        }
    }
}
