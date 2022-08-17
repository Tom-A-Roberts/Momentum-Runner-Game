using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleGun : MonoBehaviour
{
    public Transform GunEndPosition;
    public LineRenderer RopeRenderer;
    public float MaxGrappleLength = 20f;

    private bool grappleConnected = false;
    private Vector3 connectionPoint;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Grapple Initiated");

            RaycastHit hit;
            if (Physics.Raycast(GunEndPosition.position, GunEndPosition.transform.forward, out hit, MaxGrappleLength))
            {
                Debug.Log("Grapple Connected");

                grappleConnected = true;
                connectionPoint = hit.point;
            }
        }

        if (grappleConnected)
            ConnectGrapple();
    }

    void ConnectGrapple()
    {
        RopeRenderer.SetPositions(new Vector3[] { GunEndPosition.transform.position, connectionPoint });
    }
}
