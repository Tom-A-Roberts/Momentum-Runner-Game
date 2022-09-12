using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlowZone : MonoBehaviour
{
    public float MinimumSpeed;
    public float SlowDownForce;
    public float SidewaysForce = 1050f;
    public float VerticalForce = 1050f;


    private PlayerController pc;
    private Rigidbody rb;
    private bool playerIsInZone;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<PlayerController>() == null)
        {
            return;
        }
        else
        {
            playerIsInZone = true;
            pc = other.gameObject.GetComponent<PlayerController>();
            rb = pc.gameObject.GetComponent<Rigidbody>();
        }

    }

    private void Update()
    {
            float upComponent;
            float rightComponent;
            Vector3 sidewaysCompensationForce;
            Vector3 verticalCompensationForce;
        if (playerIsInZone == true)
        {
            upComponent = Vector3.Dot(transform.up, rb.velocity);
            rightComponent = Vector3.Dot(transform.right, rb.velocity);
            verticalCompensationForce = transform.up * upComponent * -1 * VerticalForce * Time.deltaTime;
            sidewaysCompensationForce = transform.right * rightComponent * -1 * SidewaysForce * Time.deltaTime;

            if (Vector3.Dot(rb.velocity, transform.forward) < MinimumSpeed) { }
            else
            {
                Vector3 slowZoneVelocity = transform.forward * SlowDownForce * -1;
                pc.BoostForce(slowZoneVelocity, ForceMode.Force);
            }
            pc.BoostForce(verticalCompensationForce, ForceMode.Force);
            pc.BoostForce(sidewaysCompensationForce, ForceMode.Force);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        playerIsInZone = false;
        rb = null;
        pc = null;
    }
}
