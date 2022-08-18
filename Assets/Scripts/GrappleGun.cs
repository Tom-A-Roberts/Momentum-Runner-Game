using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleGun : MonoBehaviour
{
    public Transform GunEndPosition;
    public Camera PlayerCamera;
    public LineRenderer RopeRenderer;
    public float MaxGrappleLength = 20f;

    private bool grappleConnected = false;
    private Vector3 connectionPoint;
    private float connectedDistance;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            ConnectGrapple();
        }

        if (Input.GetMouseButtonUp(0))
        {
            DisconnectGrapple();
        }
    }

    private void OnEnable()
    {
        Application.onBeforeRender += UpdateGrapple;
    }

    private void OnDisable()
    {
        Application.onBeforeRender -= UpdateGrapple;
    }

    void ConnectGrapple()
    {
        Debug.Log("Grapple Initiated");

        RaycastHit hit;
        if (Physics.Raycast(PlayerCamera.transform.position, PlayerCamera.transform.forward, out hit, MaxGrappleLength, LayerMask.GetMask("Terrain")))
        {
            Debug.Log("Grapple Connected");

            connectionPoint = hit.point;
            connectedDistance = hit.distance;

            grappleConnected = true;
            RopeRenderer.enabled = true;
        }

    }


    void UpdateGrapple()
    {
        if (grappleConnected)
        {
            RopeRenderer.SetPositions(new Vector3[] { GunEndPosition.transform.position, connectionPoint });
        }
    }

    void DisconnectGrapple()
    {
        RopeRenderer.enabled = false;

        grappleConnected = false;
    }
}
