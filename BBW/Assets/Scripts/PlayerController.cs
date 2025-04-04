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
    private PlayerControls inputs;
    private InputAction moveAction;
    private InputAction lookAction;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        moveAction = inputs.FindAction("Movement");
        moveAction = inputs.FindAction("Look");
        //moveAction = inputs.FindAction("Interact");
    }

    // Update is called once per frame
    void Update()
    {
        moveDir = camera.transform.forward;
    }

    void FixedUpdate()
    {
        Look();
        Move();
           
    }

    private void Look()
    {
        throw new NotImplementedException();
    }
    
    private void Move()
    {
        rb.velocity = new Vector2(moveDir.x*movespeed, moveDir.y*movespeed);
    }

}
