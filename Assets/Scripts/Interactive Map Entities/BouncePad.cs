using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BouncePad : MonoBehaviour
{
    public float VerticalForceMultiplier = 1f;
    public float HorizontalForcceApplied = 1f;
    private void OnTriggerEnter(Collider collision)
    {
        PlayerController pc = collision.GetComponent<PlayerController>();
        Rigidbody rb = pc.bodyRigidBody;
        if (pc != null)
        {
            Vector3 bounceYForce = new Vector3 ( 0,(rb.velocity.y * VerticalForceMultiplier * -1f),0);
            Vector3 bounceForwardForce = collision.gameObject.transform.forward * HorizontalForcceApplied;
            Vector3 totalForce = bounceForwardForce + bounceYForce; 
            rb.AddForce(totalForce,ForceMode.Impulse);
            if(pc.JumpCount < 2)
            {
                pc.JumpCount++;
            }
        }
    }
}
