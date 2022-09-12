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
        if (playerIsInZone == true)
        {
            if (Vector3.Dot(rb.velocity, transform.forward) < MinimumSpeed) return;
            else
            {
                Vector3 slowZoneVelocity = transform.forward * SlowDownForce * -1;

                float upComponent = Vector3.Dot(transform.up, rb.velocity);
                Vector3 verticalCompensationForce = transform.up * upComponent * -1 * VerticalForce * Time.deltaTime;

                float rightComponent = Vector3.Dot(transform.right, rb.velocity);
                Vector3 sidewaysCompensationForce = transform.right * rightComponent * -1 * SidewaysForce * Time.deltaTime;
                pc.BoostForce(slowZoneVelocity, ForceMode.Force);
                pc.BoostForce(verticalCompensationForce, ForceMode.Force);
                pc.BoostForce(sidewaysCompensationForce, ForceMode.Force);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        playerIsInZone = false;
        rb = null;
        pc = null;
    }
}
