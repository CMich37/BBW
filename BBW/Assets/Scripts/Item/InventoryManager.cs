using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    [Header("Inventory Slots")]
    [SerializeField] private Image slot1Icon;
    [SerializeField] private Image slot2Icon;
    [SerializeField] private Image slot3Icon;
    [SerializeField] private Image slot1Background; // Background that's always visible
    [SerializeField] private Image slot2Background;
    [SerializeField] private Image slot3Background;
    [SerializeField] private Color selectedSlotColor = Color.yellow; // Color for selected slot highlight
    [SerializeField] private Color normalSlotColor = Color.white; // Normal slot color
    
    [Header("Hand UI")]
    [SerializeField] private Image handIcon; // UI representation of item in hand
    [SerializeField] private Transform handHoldPoint; // Transform where 3D item appears in hand
    
    [Header("Messages")]
    [SerializeField] private TMP_Text inventoryFullMessage;
    [SerializeField] private float messageDisplayTime = 2f;
    
    [Header("Item Prefab")]
    [SerializeField] private GameObject itemDropPrefab; // Prefab to spawn when dropping items
    
    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActions;
    
    // Inventory data
    private InteractableItem[] inventory = new InteractableItem[3]; // 3 slots
    private int currentSlot = 0; // Currently selected slot (0, 1, or 2)
    private InputAction slot1Action;
    private InputAction slot2Action;
    private InputAction slot3Action;
    private InputAction dropAction;
    
    private Coroutine messageCoroutine;
    private GameObject currentHandItem; // 3D item currently shown in hand
    
    private void Awake()
    {
        // Get input actions
        if (inputActions == null)
        {
            PlayerController playerController = FindObjectOfType<PlayerController>();
            if (playerController != null)
            {
                var inputField = typeof(PlayerController).GetField("inputs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (inputField != null)
                {
                    inputActions = inputField.GetValue(playerController) as InputActionAsset;
                }
            }
        }
        
        if (inputActions != null)
        {
            var map = inputActions.FindActionMap("Player");
            if (map != null)
            {
                // Try to find slot actions, or create them if they don't exist
                slot1Action = map.FindAction("Slot1");
                slot2Action = map.FindAction("Slot2");
                slot3Action = map.FindAction("Slot3");
                dropAction = map.FindAction("Drop");
            }
        }
    }
    
    private void Start()
    {
        UpdateHandUI();
        UpdateSlotUI();
        UpdateSlotHighlighting();
        
        // Enable input actions
        if (slot1Action != null) slot1Action.Enable();
        if (slot2Action != null) slot2Action.Enable();
        if (slot3Action != null) slot3Action.Enable();
        if (dropAction != null)
        {
            dropAction.Enable();
            dropAction.performed += ctx => DropCurrentItem();
        }
        
        // Hide message initially
        if (inventoryFullMessage != null)
            inventoryFullMessage.gameObject.SetActive(false);
    }
    
    private void Update()
    {
        // Handle slot switching with number keys
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            SwitchToSlot(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            SwitchToSlot(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            SwitchToSlot(2);
        }
        
        // Handle drop with G key
        if (Input.GetKeyDown(KeyCode.G))
        {
            DropCurrentItem();
        }
    }
    
    private void OnDestroy()
    {
        if (dropAction != null)
            dropAction.performed -= ctx => DropCurrentItem();
    }
    
    public bool PickupItem(InteractableItem item)
    {
        // Find first empty slot
        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i] == null)
            {
                // Add item to slot
                inventory[i] = item;
                
                // Hide item in world
                item.gameObject.SetActive(false);
                
                // If this is the first item, switch to it
                if (i == 0 && currentSlot == 0 && inventory[0] != null)
                {
                    currentSlot = 0;
                }
                
                UpdateSlotUI();
                UpdateHandUI();
                UpdateSlotHighlighting();
                UpdateHandItem3D();
                
                Debug.Log($"Picked up {item.itemName} into slot {i + 1}");
                return true;
            }
        }
        
        // Inventory is full
        ShowInventoryFullMessage();
        return false;
    }
    
    private void SwitchToSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventory.Length)
            return;
            
        currentSlot = slotIndex;
        UpdateHandUI();
        UpdateSlotHighlighting();
        UpdateHandItem3D();
        Debug.Log($"Switched to slot {slotIndex + 1}");
    }
    
    private void DropCurrentItem()
    {
        if (inventory[currentSlot] == null)
            return;
            
        InteractableItem itemToDrop = inventory[currentSlot];
        
        // Get player position and forward direction
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController == null || playerController.playerCam == null)
            return;
            
        Transform playerCam = playerController.playerCam;
        Vector3 dropPosition = playerCam.position + playerCam.forward * 1.5f;
        
        // Reactivate item in world
        itemToDrop.gameObject.SetActive(true);
        itemToDrop.transform.position = dropPosition;
        
        // Add some forward force if item has rigidbody
        Rigidbody rb = itemToDrop.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = playerCam.forward * 3f;
        }
        
        // Remove from inventory
        inventory[currentSlot] = null;
        
        UpdateSlotUI();
        UpdateHandUI();
        UpdateSlotHighlighting();
        UpdateHandItem3D();
        
        Debug.Log($"Dropped {itemToDrop.itemName} from slot {currentSlot + 1}");
    }
    
    private void UpdateSlotUI()
    {
        // Update slot icons (backgrounds stay visible always)
        if (slot1Icon != null)
        {
            slot1Icon.sprite = inventory[0] != null ? (inventory[0].icon != null ? inventory[0].icon : (inventory[0].itemData != null ? inventory[0].itemData.icon : null)) : null;
            slot1Icon.enabled = inventory[0] != null && slot1Icon.sprite != null;
        }
        
        if (slot2Icon != null)
        {
            slot2Icon.sprite = inventory[1] != null ? (inventory[1].icon != null ? inventory[1].icon : (inventory[1].itemData != null ? inventory[1].itemData.icon : null)) : null;
            slot2Icon.enabled = inventory[1] != null && slot2Icon.sprite != null;
        }
        
        if (slot3Icon != null)
        {
            slot3Icon.sprite = inventory[2] != null ? (inventory[2].icon != null ? inventory[2].icon : (inventory[2].itemData != null ? inventory[2].itemData.icon : null)) : null;
            slot3Icon.enabled = inventory[2] != null && slot3Icon.sprite != null;
        }
    }
    
    private void UpdateSlotHighlighting()
    {
        // Highlight selected slot
        if (slot1Background != null)
        {
            slot1Background.color = currentSlot == 0 ? selectedSlotColor : normalSlotColor;
        }
        if (slot2Background != null)
        {
            slot2Background.color = currentSlot == 1 ? selectedSlotColor : normalSlotColor;
        }
        if (slot3Background != null)
        {
            slot3Background.color = currentSlot == 2 ? selectedSlotColor : normalSlotColor;
        }
    }
    
    private void UpdateHandItem3D()
    {
        // Destroy current hand item if exists
        if (currentHandItem != null)
        {
            Destroy(currentHandItem);
            currentHandItem = null;
        }
        
        // Show 3D item in hand if there's an item in selected slot
        InteractableItem currentItem = inventory[currentSlot];
        if (currentItem != null && handHoldPoint != null)
        {
            // Create a copy of the item to show in hand
            currentHandItem = Instantiate(currentItem.gameObject, handHoldPoint);
            currentHandItem.SetActive(true);
            
            // Reset transform
            currentHandItem.transform.localPosition = Vector3.zero;
            currentHandItem.transform.localRotation = Quaternion.identity;
            currentHandItem.transform.localScale = Vector3.one;
            
            // Disable collider and rigidbody if they exist
            Collider col = currentHandItem.GetComponent<Collider>();
            if (col != null) col.enabled = false;
            
            Rigidbody rb = currentHandItem.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            
            // Make sure it's visible (set layer if needed)
            currentHandItem.layer = handHoldPoint.gameObject.layer;
        }
    }
    
    private void UpdateHandUI()
    {
        // Update hand icon based on current slot
        if (handIcon != null)
        {
            InteractableItem currentItem = inventory[currentSlot];
            if (currentItem != null)
            {
                handIcon.sprite = currentItem.icon != null ? currentItem.icon : (currentItem.itemData != null ? currentItem.itemData.icon : null);
                handIcon.enabled = handIcon.sprite != null;
            }
            else
            {
                handIcon.sprite = null;
                handIcon.enabled = false;
            }
        }
    }
    
    private void ShowInventoryFullMessage()
    {
        if (inventoryFullMessage == null)
        {
            Debug.Log("Inventory is Full");
            return;
        }
        
        // Stop existing coroutine if running
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
        }
        
        messageCoroutine = StartCoroutine(ShowMessageCoroutine());
    }
    
    private System.Collections.IEnumerator ShowMessageCoroutine()
    {
        inventoryFullMessage.text = "Inventory is Full";
        inventoryFullMessage.gameObject.SetActive(true);
        
        yield return new WaitForSeconds(messageDisplayTime);
        
        inventoryFullMessage.gameObject.SetActive(false);
        messageCoroutine = null;
    }
    
    // Public getters
    public InteractableItem GetCurrentItem()
    {
        return inventory[currentSlot];
    }
    
    public int GetCurrentSlot()
    {
        return currentSlot;
    }
    
    public bool IsInventoryFull()
    {
        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i] == null)
                return false;
        }
        return true;
    }
}
