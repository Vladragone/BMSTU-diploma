using UnityEngine;

public class PlayerZoneSelector : MonoBehaviour
{
    [Header("Player Settings")]
    public string localPlayerId = "PlayerA";

    [Header("Zones")]
    public GameObject zoneA;
    public GameObject zoneB;

    private void Start()
    {
        ApplyZoneVisibility();
    }

    private void ApplyZoneVisibility()
    {
        if (localPlayerId == "PlayerA")
        {
            zoneA.SetActive(true);
            zoneB.SetActive(false);

            Debug.Log("[ZoneSelector] Включена зона PlayerA");
        }
        else if (localPlayerId == "PlayerB")
        {
            zoneA.SetActive(false);
            zoneB.SetActive(true);

            Debug.Log("[ZoneSelector] Включена зона PlayerB");
        }
        else
        {
            zoneA.SetActive(true);
            zoneB.SetActive(true);

            Debug.Log("[ZoneSelector] Включены обе зоны");
        }
    }
}