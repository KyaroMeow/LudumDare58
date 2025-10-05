using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Conveyor : MonoBehaviour
{
    [Header("Conveyor Settings")]
    public float conveyorSpeed = -2f;
    public bool canMove = true;
    Rigidbody rb;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (canMove)
        {
            Move();
        }
    }
    private void Move()
    {
        Vector3 pos = rb.position;
        rb.position += Vector3.back * conveyorSpeed * Time.fixedDeltaTime;
        rb.MovePosition(pos);
    }
}
