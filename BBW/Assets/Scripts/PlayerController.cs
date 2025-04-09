using System;
using Akila.FPSFramework;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float movespeed = 10;
    private Vector2 moveDir;

    [Header("Camera")]
    [SerializeField] public Transform playerCam;
    [SerializeField] public float sensitivity = 200;
    [SerializeField] public float maximumX = 90f;
    [SerializeField] public float minimumX = -90f;
    private float xRotation = 0f;
    [SerializeField] public bool lockCursor = true;

    [Header("Interaction & Inventory")]
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private GameObject inventoryUI;

    // Inventory slots
    private InteractableItem[] inventorySlots = new InteractableItem[4];
    // 0 = right hand, 1 = left hand, 2 = pocket1, 3 = pocket2

    [Header("UI Slots")]
    [SerializeField] private Image uiRightHand;
    [SerializeField] private Image uiLeftHand;
    [SerializeField] private Image uiPocket1;
    [SerializeField] private Image uiPocket2;

    [Header("Prompt UI")]
    [SerializeField] private TMP_Text pickupPrompt;

    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputs;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction interactAction;
    private InputAction inventoryAction;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        var map = inputs.FindActionMap("Player");
        moveAction      = map.FindAction("Move");
        lookAction      = map.FindAction("Look");
        interactAction  = map.FindAction("Interact");
        inventoryAction = map.FindAction("Inventory");

        moveAction.Enable();
        lookAction.Enable();
        interactAction.Enable();
        inventoryAction.Enable();

        inventoryAction.performed += ctx => ToggleInventoryUI();
    }

    private void Start()
    {
        inventoryUI.SetActive(false);
        pickupPrompt.gameObject.SetActive(false);
        RefreshUI();
    }

    private void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        interactAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        interactAction.Disable();
    }

    private void Update()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        HandleInteract();
    }

    private void FixedUpdate()
    {
        Look();
        Move();
    }

    // ——— Interaction & Pickup ———
    private void HandleInteract()
    {
        Ray ray = new Ray(playerCam.position, playerCam.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer))
        {
            var itemComp = hit.collider.GetComponent<InteractableItem>();
            if (itemComp != null)
            {
                pickupPrompt.gameObject.SetActive(true);
                pickupPrompt.text = $"Press E to pick up {itemComp.itemName}";

                if (interactAction.triggered)
                {
                    AddToInventory(itemComp);
                    pickupPrompt.gameObject.SetActive(false);
                }
                return;
            }
        }

        pickupPrompt.gameObject.SetActive(false);
    }

    // ——— Inventory Methods ———
    private void ToggleInventoryUI()
    {
        inventoryUI.SetActive(!inventoryUI.activeSelf);
    }

    private void AddToInventory(InteractableItem item)
    {
        // Find first empty slot
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i] == null)
            {
                inventorySlots[i] = item;
                item.gameObject.SetActive(false); // hide in world
                RefreshUI();
                return;
            }
        }
        // Inventory full: you could show a “full” message here
    }

    private void RefreshUI()
    {
        // Right Hand (slot 0)
        uiRightHand.sprite = inventorySlots[0] != null ? inventorySlots[0].icon : null;
        uiRightHand.enabled = inventorySlots[0] != null;

        // Left Hand (slot 1)
        uiLeftHand.sprite = inventorySlots[1] != null ? inventorySlots[1].icon : null;
        uiLeftHand.enabled = inventorySlots[1] != null;

        // Pocket1 (slot 2)
        uiPocket1.sprite = inventorySlots[2] != null ? inventorySlots[2].icon : null;
        uiPocket1.enabled = inventorySlots[2] != null;

        // Pocket2 (slot 3)
        uiPocket2.sprite = inventorySlots[3] != null ? inventorySlots[3].icon : null;
        uiPocket2.enabled = inventorySlots[3] != null;
    }

    private void Look()
    {
        Vector2 lookDelta = lookAction.ReadValue<Vector2>() * sensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * lookDelta.x);

        xRotation -= lookDelta.y;
        xRotation = Mathf.Clamp(xRotation, minimumX, maximumX);
        playerCam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    private void Move()
    {
        moveDir = moveAction.ReadValue<Vector2>();
        Vector3 forward = transform.forward;
        Vector3 right   = transform.right;
        Vector3 movement = (forward * moveDir.y + right * moveDir.x) * movespeed;
        movement.y = rb.linearVelocity.y;
        rb.linearVelocity = movement;
    }
}
