using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceTest : MonoBehaviour
{
    public float minAccelerationForce, maxAccelerationForce, rotationForce;

    private Vector3 direction, rotation;
    private float accelerationForce;

    public void Initialize(Vector3 targetDirection)
    {
        direction = targetDirection;
        rotation = Random.onUnitSphere;
        accelerationForce = Random.Range(minAccelerationForce, maxAccelerationForce);
    }

    public void AddForce()
    {
        var rb = GetComponent<Rigidbody>();
        rb.AddForce(direction * accelerationForce, ForceMode.Acceleration);
        rb.AddTorque(rotation * rotationForce, ForceMode.Acceleration);
    }
}
