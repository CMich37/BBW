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
    [SerializeField] private float crouchSpeed = 5f;
    [SerializeField] private float crouchHeight = 0.5f;
    [SerializeField] private float normalHeight = 2f;
    private Vector2 moveDir;
    private bool isCrouching = false;
    private CapsuleCollider capsuleCollider; // Assuming player has capsule collider

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
        capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider == null)
        {
            capsuleCollider = GetComponentInChildren<CapsuleCollider>();
        }

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
        if (pickupPrompt != null)
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

        HandleCrouch();
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
        if (playerCam == null) return;
        
        Ray ray = new Ray(playerCam.position, playerCam.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer))
        {
            var itemComp = hit.collider.GetComponent<InteractableItem>();
            if (itemComp != null)
            {
                if (pickupPrompt != null)
                {
                    pickupPrompt.gameObject.SetActive(true);
                    pickupPrompt.text = $"Press E to pick up {itemComp.itemName}";
                }

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
                    if (pickupPrompt != null)
                        pickupPrompt.gameObject.SetActive(false);
                }
                return;
            }
        }

        if (pickupPrompt != null)
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
        float currentSpeed = isCrouching ? crouchSpeed : movespeed;
        Vector3 movement = (forward * moveDir.y + right * moveDir.x) * currentSpeed;
        movement.y = rb.linearVelocity.y;
        rb.linearVelocity = movement;
    }
    
    private void HandleCrouch()
    {
        // Check for Ctrl key press (hold to crouch, release to stand)
        bool crouchPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        
        if (crouchPressed)
        {
            // Crouch when Ctrl is held
            if (!isCrouching)
            {
                isCrouching = true;
                if (capsuleCollider != null)
                {
                    capsuleCollider.height = crouchHeight;
                    // Adjust center to keep feet on ground
                    capsuleCollider.center = new Vector3(0, crouchHeight / 2f, 0);
                }
            }
        }
        else
        {
            // Stand up when Ctrl is released
            if (isCrouching)
            {
                isCrouching = false;
                if (capsuleCollider != null)
                {
                    capsuleCollider.height = normalHeight;
                    capsuleCollider.center = new Vector3(0, normalHeight / 2f, 0);
                }
            }
        }
    }
}
