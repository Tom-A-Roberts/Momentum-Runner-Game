using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeedBoost : MonoBehaviour
{
    public float ActiveDashTimeAdjustment = 1f;
    public float DashForce = 60f;
    private float timer;
    private float speedBoostProgress;
    private float boostLength;
    private PlayerController pc;
    private Rigidbody rb;
    private Vector3 boostVector;
    private Vector3 verticalCompensationForce;
    
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.GetComponent<PlayerController>() == null)
        {
            return;
        }
        else
        {
            pc = other.gameObject.GetComponent<PlayerController>();
            speedBoostProgress = 1;
            rb = pc.gameObject.GetComponent<Rigidbody>();
        }
        
    }
    private void OnTriggerStay(Collider other)
    {
        if (speedBoostProgress > 0)
        {
            timer += Time.fixedDeltaTime * ActiveDashTimeAdjustment;
            if (timer > 0)
            {
                speedBoostProgress -= Time.fixedDeltaTime / timer;
                if(speedBoostProgress < 0) speedBoostProgress = 0;
                float currentDashForceAmount = DashForce * (1 - speedBoostProgress) * 2f;
                boostVector = transform.forward * currentDashForceAmount;
                verticalCompensationForce = new Vector3(0, rb.velocity.y * -1, 0);
                ForceMode boostType = ForceMode.Force;
                pc.BoostForce(boostVector, boostType);
                pc.BoostForce(verticalCompensationForce, ForceMode.Acceleration);
            }
        }

    }
    private void OnTriggerExit(Collider other)
    {
        speedBoostProgress = 0;
        timer = 0;
        pc = null;
    }

    //new Vector3(0,bodyRigidBody.velocity.y * -1,0), ForceMode.Impulse
    //Rigidbody playerRB = other.gameObject.GetComponent<Rigidbody>();
}
