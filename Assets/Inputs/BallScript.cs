using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BallScript : MonoBehaviour
{

    private Camera mainCamera;
    private Rigidbody rb;
    //private BallClick _ballClick;

    [SerializeField] private float impulseforce = 1f;

    void Start()
    {
        mainCamera = Camera.main;
        rb = GetComponent<Rigidbody>();

        //_ballClick = new BallClick();
        //_ballClick.ball.click.performed += ctx => OnClick(ctx);
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        // Raycast to detect where the mouse click hits in the world
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Vector3 forceDirection = transform.position - hit.point;
            forceDirection.Normalize();

            // Apply force at the hit point
            rb.AddForceAtPosition(forceDirection * impulseforce, hit.point, ForceMode.Impulse);
        }
    }




    /*private void OnEnable()
    {
        _ballClick.Enable();
    }

    private void OnDisable()
    {
        _ballClick.Disable();
    }*/

}
