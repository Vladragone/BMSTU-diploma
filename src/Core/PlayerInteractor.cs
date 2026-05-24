using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction Settings")]
    public Camera playerCamera;
    public float interactionDistance = 3f;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                interactable.Interact();
            }
            else
            {
                Debug.Log("Объект найден, но он не интерактивный: " + hit.collider.gameObject.name);
            }
        }
        else
        {
            Debug.Log("Перед игроком нет объекта");
        }
    }
}