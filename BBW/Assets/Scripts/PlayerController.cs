using System;
using Akila.FPSFramework;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;
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
    [SerializeField] private InventoryManager inventoryManager;

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
    }

    private void Start()
    {
        pickupPrompt.gameObject.SetActive(false);
        
        // Find InventoryManager if not assigned
        if (inventoryManager == null)
        {
            inventoryManager = FindObjectOfType<InventoryManager>();
        }
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
        // Lock cursor (no inventory UI to toggle)
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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
                    if (inventoryManager != null)
                    {
                        bool success = inventoryManager.PickupItem(itemComp);
                        if (!success)
                        {
                            // Item pickup failed (inventory full) - message is handled by InventoryManager
                        }
                    }
                    else
                    {
                        Debug.LogWarning("InventoryManager not found! Cannot pick up item.");
                    }
                    pickupPrompt.gameObject.SetActive(false);
                }
                return;
            }
        }

        pickupPrompt.gameObject.SetActive(false);
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
