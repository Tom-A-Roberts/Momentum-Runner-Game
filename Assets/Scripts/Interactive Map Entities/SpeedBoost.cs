using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeedBoost : MonoBehaviour
{
    public float DashAcceleration = 1f;
    public float DashForce = 60f;
    public float VerticalForce = 1f;
    private float timer;
    private float DashMultiplier;
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
            DashMultiplier = 1;
            rb = pc.gameObject.GetComponent<Rigidbody>();
        }
        
    }
    private void Update()
    {
        if (DashMultiplier > 0)
        {
            timer += Time.deltaTime / DashAcceleration;
            if (timer > 0)
            {
                DashMultiplier -= Time.deltaTime / timer;
                if(DashMultiplier < 0) DashMultiplier = 0;
                float currentDashForceAmount = (DashForce * DashMultiplier) * 2f;
                boostVector = transform.forward * currentDashForceAmount;
                //verticalCompensationForce = new Vector3(0, rb.velocity.y * -1 * VerticalForce * Time.deltaTime, 0);
                float upComponent = Vector3.Dot(transform.up, rb.velocity);
                verticalCompensationForce = transform.up * upComponent * -1 * VerticalForce * Time.deltaTime;

                ForceMode boostType = ForceMode.Force;
                pc.BoostForce(boostVector, boostType);
                pc.BoostForce(verticalCompensationForce, ForceMode.Force);
            }
        }

    }
    private void OnTriggerExit(Collider other)
    {
        DashMultiplier = 0;
        timer = 0;
        pc = null;
    }

    //new Vector3(0,bodyRigidBody.velocity.y * -1,0), ForceMode.Impulse
    //Rigidbody playerRB = other.gameObject.GetComponent<Rigidbody>();
}
