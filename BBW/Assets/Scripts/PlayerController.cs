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
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        moveAction = inputs.FindAction("Movement");
        moveAction = inputs.FindAction("Look");
        //moveAction = inputs.FindAction("Interact");
    }

    private void OnEnable()
    {
        
    }
    private void OnDisable()
    {
        
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
