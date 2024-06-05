using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class ColliderEvent : MonoBehaviour
{

    private GameManager gameManager;

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void OnTriggerEnter(Collider other)
    {
        gameManager.onTriggerEnter_Ball.Invoke(rb, other, "trigger");
    }

    public void OnCollisionEnter(Collision collision)
    {
        gameManager.onCollisionEnter_Ball.Invoke(rb, collision.collider, "collision");
    }

    public void setGameManager(GameManager gm)
    {
        gameManager = gm;
    }
}