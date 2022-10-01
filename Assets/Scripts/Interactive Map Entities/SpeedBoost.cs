using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeedBoost : MonoBehaviour
{
    public float DashAcceleration = 1f;
    public float DashForce = 60f;
    public float VerticalForce = 1050f;
    public float SidewaysForce = 1050f;
    private float timer;
    private float DashMultiplier;
    private float boostLength;
    private PlayerController pc;
    private Rigidbody rb;
    private Vector3 boostVector;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<PlayerController>() == null)
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
                if (DashMultiplier < 0) DashMultiplier = 0;
                float currentDashForceAmount = (DashForce * DashMultiplier) * 2f;
                boostVector = transform.forward * currentDashForceAmount;
                float upComponent = Vector3.Dot(transform.up, rb.velocity);
                Vector3 verticalCompensationForce = transform.up * upComponent * -1 * VerticalForce * Time.deltaTime;

                float rightComponent = Vector3.Dot(transform.right, rb.velocity);
                Vector3 sidewaysCompensationForce = transform.right * rightComponent * -1 * SidewaysForce * Time.deltaTime;

                ForceMode boostType = ForceMode.Force;
                pc.BoostForce(boostVector, boostType);
                pc.BoostForce(verticalCompensationForce, ForceMode.Force);
                pc.BoostForce(sidewaysCompensationForce, ForceMode.Force);
            }
        }

    }
    private void OnTriggerExit(Collider other)
    {
        DashMultiplier = 0;
        timer = 0;
        pc = null;
    }
}
