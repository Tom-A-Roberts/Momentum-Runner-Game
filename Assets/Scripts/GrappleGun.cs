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
    public float GrappleForce = 10f;

    public GrapplingRope grapplingRope;
    private ConfigurableJoint Rope;
    public CollisionDetector GroundDetector;
    public PlayerController PlayerController;

    private bool grappleConnected = false;
    private Vector3 connectionPoint;
    private float connectedDistance;
    private Vector3 currentRopePosition;


    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Grapple"))
        {
            ConnectGrapple();
        }

        if (Input.GetButtonUp("Grapple"))
        {
            DisconnectGrapple();
        }
    }

    private void FixedUpdate()
    {
        ApplyGrappleForce();
    }

    void ConnectGrapple()
    {
        RaycastHit hit;
        if (Physics.Raycast(PlayerCamera.transform.position, PlayerCamera.transform.forward, out hit, MaxGrappleLength, LayerMask.GetMask("Terrain")))
        {
            connectionPoint = hit.point;
            connectedDistance = hit.distance;

            Rope = playerCentreOfMass.gameObject.AddComponent<ConfigurableJoint>();
            Rope.autoConfigureConnectedAnchor = false;
            Rope.connectedAnchor = connectionPoint;

            Rope.xMotion = ConfigurableJointMotion.Limited;
            Rope.yMotion = ConfigurableJointMotion.Limited;
            Rope.zMotion = ConfigurableJointMotion.Limited;

            // make a springy limit
            //SoftJointLimitSpring linearLimitSpring = Rope.linearLimitSpring;
            //linearLimitSpring.spring = 30f;
            //Rope.linearLimitSpring = linearLimitSpring;

            // make a limit
            SoftJointLimit limit = Rope.linearLimit;
            limit.limit = connectedDistance;
            limit.contactDistance = 0.05f;         
            Rope.linearLimit = limit;

            Rope.enablePreprocessing = true;

            Rope.massScale = 0.5f;

            currentRopePosition = GunEndPosition.position;

            grappleConnected = true;

            grapplingRope.Extend(connectionPoint);
        }
    }

    void DisconnectGrapple()
    {
        connectedDistance = 0;

        grapplingRope.Retract();

        Destroy(Rope);
        grappleConnected = false;
    }

    void ApplyGrappleForce()
    {
        if (grappleConnected)
        {
            Vector3 ropeVec = connectionPoint - playerCentreOfMass.position;

            Vector3 forceDir = Vector3.Cross(playerCentreOfMass.right, ropeVec).normalized;

            PlayerController.AddForce(GrappleForce, forceDir, ForceMode.Force);
        }
    }

    public bool IsGrappling()
    {
        return grappleConnected;
    }

    public Vector3 GetPoint()
    {
        return connectionPoint;
    }

    public float GetSqrDistance()
    {
        return connectedDistance * connectedDistance;
    }
    public float GetDistance()
    {
        return connectedDistance;
    }
}
