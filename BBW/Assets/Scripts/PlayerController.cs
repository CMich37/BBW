using System;
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
    [SerializeField]
    public Camera camera;
    [SerializeField]
    private InputActionAsset inputs;
    private InputAction moveAction;
    private InputAction lookAction;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        moveAction = inputs.FindActionMap("Player").FindAction("Movement");
        moveAction = inputs.FindActionMap("Player").FindAction("Look");
        //moveAction = inputs.FindAction("Interact");

    }

    void Start()
    {
        
    }

    private void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
    }
    private void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void FixedUpdate()
    {
        Look();
        Move();
           
    }

    private void Look()
    {
        
    }
    
    private void Move()
    {
        Debug.Log(moveAction.ReadValue<Vector2>());
        moveDir = moveAction.ReadValue<Vector2>();
        rb.linearVelocity = new Vector2(moveDir.x*movespeed, moveDir.y*movespeed);
    }

}
