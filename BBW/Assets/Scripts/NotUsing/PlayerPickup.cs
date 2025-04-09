using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPickup : MonoBehaviour
{
    public float pickupRange = 3f;
    public LayerMask interactableLayer;
    public Transform rightHandHoldPoint;
    public InventoryManager inventory;

    private PlayerControls inputActions;

    private void Awake()
    {
        inputActions = new PlayerControls();

    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Interact.performed += OnInteract;

    }

    private void OnDisable()
    {
        inputActions.Player.Interact.performed -= OnInteract;
        inputActions.Player.Disable();
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        TryPickup();
    }

    void TryPickup()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, interactableLayer))
        {
            InteractableItem item = hit.collider.GetComponent<InteractableItem>();
            if (item != null)
            {
                inventory.PickupToRightHand(item.gameObject);
            }
        }
    }
}
