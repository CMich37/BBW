using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour
{
    public Transform leftHandPoint, rightHandPoint;
    public GameObject leftHandItem, rightHandItem;
    public GameObject pocket1Item, pocket2Item;

    public GameObject inventoryUI;

    private PlayerControls inputActions;

    private void Awake()
    {
        inputActions = new PlayerControls();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Inventory.performed += OnInventory;
    }

    private void OnDisable()
    {
        inputActions.Player.Inventory.performed -= OnInventory;
        inputActions.Player.Disable();
    }

    private void OnInventory(InputAction.CallbackContext context)
    {
        inventoryUI.SetActive(!inventoryUI.activeSelf);
    }

    public void PickupToRightHand(GameObject item)
    {
        if (rightHandItem != null)
        {
            Debug.Log("Right hand is full!");
            return;
        }

        rightHandItem = item;
        AttachItemToHand(item, rightHandPoint);
    }

    public void MoveToPocket(int pocketIndex)
    {
        if (rightHandItem == null) return;

        if (pocketIndex == 1 && pocket1Item == null)
        {
            pocket1Item = rightHandItem;
            DetachItem(rightHandItem);
            rightHandItem = null;
        }
        else if (pocketIndex == 2 && pocket2Item == null)
        {
            pocket2Item = rightHandItem;
            DetachItem(rightHandItem);
            rightHandItem = null;
        }
    }

    void AttachItemToHand(GameObject item, Transform handPoint)
    {
        item.transform.SetParent(handPoint);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;
    }

    void DetachItem(GameObject item)
    {
        item.transform.SetParent(null);

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = false;
    }
}
