using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleGun : MonoBehaviour
{
    public Transform GunEndPosition;
    public Transform playerCentreOfMass;
    public Camera PlayerCamera;
    public LineRenderer RopeRenderer;
    public float MaxGrappleLength = 20f;
    public PlayerController playerController;

    private bool grappleConnected = false;
    private Vector3 connectionPoint;
    private float connectedDistance;

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

    private void FixedUpdate()
    {
        ApplyGrappleForce();
    }

    private void OnEnable()
    {
        Application.onBeforeRender += UpdateGrappleVisual;
    }

    private void OnDisable()
    {
        Application.onBeforeRender -= UpdateGrappleVisual;
    }

    void ConnectGrapple()
    {
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

    void UpdateGrappleVisual()
    {
        if (grappleConnected)
        {
            RopeRenderer.SetPositions(new Vector3[] { GunEndPosition.transform.position, connectionPoint });
        }
    }

    void DisconnectGrapple()
    {
        Debug.Log("Grapple Disconnected");

        RopeRenderer.enabled = false;
        grappleConnected = false;
    }

    void ApplyGrappleForce()
    {
        if (grappleConnected)
        {
            Vector3 forceDirection = connectionPoint - playerCentreOfMass.position;
            playerController.AddForce(10f, forceDirection, ForceMode.Acceleration);
        }
    }
}
