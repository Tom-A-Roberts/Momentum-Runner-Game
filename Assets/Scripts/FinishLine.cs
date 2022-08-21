using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FinishLine : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Rigidbody playerRB = other.gameObject.GetComponent<Rigidbody>();
        if (playerRB != null)
        {
            if (playerRB.velocity.magnitude > 999) Debug.Log("999");
            else Debug.Log(Mathf.RoundToInt(playerRB.velocity.magnitude));

        }
    }

}
