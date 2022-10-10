using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlowZone : MonoBehaviour
{
    [Tooltip("The drag coefficient of this zone")]
    public float dragCoefficient = 1;
    [Tooltip("Drag is applied until the player is below this speed")]
    public float MinimumSpeed = 10f;

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

    private void FixedUpdate()
    {
        if (playerIsInZone == true)
        {
            // Formula for drag is: drag_coefficient * v^2
            if(rb.velocity.magnitude > MinimumSpeed)
            {
                Vector3 dragDirection = -rb.velocity.normalized;
                float v_squared = Mathf.Pow(rb.velocity.magnitude, 2);
                Vector3 dragForce = dragDirection * v_squared * dragCoefficient * Time.fixedDeltaTime;

                pc.BoostForce(dragForce, ForceMode.Acceleration);
            }

            // Remove gravity:
            // extra 1.1 seems to be needed to counter the feet
            Vector3 GravityForce = Physics.gravity * 1.1f;// + pc.CharacterFallingWeight * Vector3.down;
            pc.BoostForce(-GravityForce, ForceMode.Force);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        playerIsInZone = false;
        rb = null;
        pc = null;
    }
}
