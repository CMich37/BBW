using System;
using Akila.FPSFramework;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField]
    private Rigidbody rb;
    [SerializeField]
    private float movespeed = 10;
    [SerializeField]
    private Vector2 moveDir;
    [Header("Camera")]
    [SerializeField]
    public Transform playerCam;
    [SerializeField]
    public float sensitivity = 200;
    [SerializeField]
    public float maximumX = 90f;
    [SerializeField]
    public float minimumX = -90f;
    public float xRotation = 0f;
    [SerializeField]
    public Vector3 camOffset = new Vector3(0, -0.2f, 0);
    [SerializeField]
    public bool lockCursor = true;
    [SerializeField]
    private InputActionAsset inputs;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction interactAction;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        var playerActionMap = inputs.FindActionMap("Player");
        interactAction = playerActionMap.FindAction("Interact");
        moveAction = playerActionMap.FindAction("Move");
        lookAction = playerActionMap.FindAction("Look");
        //moveAction = inputs.FindAction("Interact");

    }

    void Start()
    {
        // Debug.Log("Move action: " + (moveAction != null));
        // Debug.Log("Look action: " + (lookAction != null));
        // Debug.Log("Input Action Asset: " + inputs.name);
        
    }

    private void OnEnable()
    {
        interactAction.Enable();
        moveAction.Enable();
        lookAction.Enable();
    }
    private void OnDisable()
    {
        interactAction.Disable();
        moveAction.Disable();
        lookAction.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked; // Locks cursor to center
            Cursor.visible = false; // Hides the cursor
        }
        Interact();
    }

    void FixedUpdate()
    {
        Look();
        Move();
           
    }

    private void Interact()
    {
        // Debug.Log("press "+interactAction.IsPressed()); // for holding
        // Debug.Log("triger" + interactAction.triggered); // use this
        // if (interactAction.triggered)
        // {
        //     Debug.Break();
        // }
        
    }

    private void Look()
    {
        Debug.Log("look delta: " + lookAction.ReadValue<Vector2>());
        // Get mouse/joystick input
        Vector2 lookDelta = lookAction.ReadValue<Vector2>() * sensitivity * Time.deltaTime;

        // Horizontal (Yaw) rotation (rotates the player left/right)
        float yaw = lookDelta.x;
        transform.Rotate(Vector3.up * yaw);

        // Vertical (Pitch) rotation (rotates the camera up/down)
        xRotation -= lookDelta.y; // Subtract for inverted controls (like most FPS games)
        xRotation = Mathf.Clamp(xRotation, minimumX, maximumX); // Clamp to prevent over-rotation

        // Apply rotation to the camera
        playerCam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        
    }
    
    private void Move()
    {
        // moveDir = moveAction.ReadValue<Vector2>();
        // //Debug.Log("Movement Input: " + moveAction.ReadValue<Vector2>());
        
        // // Convert 2D input to 3D movement (ignoring vertical movement)
        // Vector3 movement = new Vector3(moveDir.y, 0, moveDir.x) * movespeed;
        // rb.linearVelocity = movement;

        moveDir = moveAction.ReadValue<Vector2>();
    
        // Get forward and right vectors relative to player's rotation
        Vector3 forward = transform.forward; // Z-axis (where player is looking)
        Vector3 right = transform.right;     // X-axis (player's right side)
        
        // Combine movement direction with orientation
        Vector3 movement = (forward * moveDir.y + right * moveDir.x) * movespeed;
        
        // Keep y-velocity from physics (like gravity)
        movement.y = rb.linearVelocity.y;
        
        rb.linearVelocity = movement;
    }

}
