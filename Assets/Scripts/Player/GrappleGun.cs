using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GrappleGun : NetworkBehaviour
{
    public Transform GunEndPosition;
    public Transform playerCentreOfMass;
    public Camera PlayerCamera;
    public LineRenderer RopeRenderer;
    public float MaxGrappleLength = 20f;
    public float GrappleForce = 10f;

    public PlayerAudioManager audioManager;

    /// <summary>
    /// How much the grapple gun moves when trying to face the grapple point
    /// </summary>
    public float GrappleLookAtPower = 0.4f;
    public float lookAtSmoothSpeed = 0.05f;
     
    [System.NonSerialized] public bool isGrappleOwner = true;


    public GrapplingRope grapplingRope;
    private ConfigurableJoint Rope;
    public CollisionDetector GroundDetector;
    public PlayerController PlayerController;
    public Rigidbody playerRigidbody;

    public GameObject grappleGunModel;

    private bool grappleConnected = false;
    private Vector3 connectionPoint;
    private float connectedDistance;
    private Vector3 currentRopePosition;

    private Vector3 targD;

    private void Start()
    {
        playerRigidbody = PlayerController.gameObject.GetComponent<Rigidbody>();
    }
    // Update is called once per frame
    void Update()
    {
        // Check if grapple gun is being controlled by the owner. If yes, then engage grapple
        if (isGrappleOwner)
        {
            if (Input.GetButton("Grapple") && !grappleConnected)
            {
                ConnectGrapple();
            }
            else if (!Input.GetButton("Grapple") && grappleConnected)
            {
                DisconnectGrapple();
            }
        }

        if (grappleConnected)
        {
            Vector3 lookDirection = connectionPoint - grappleGunModel.transform.position;
            Vector3 forwardPlanar = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
            Vector3 rightPlanar = Vector3.Cross(forwardPlanar, Vector3.up).normalized;

            float forwardElement = Vector3.Dot(lookDirection, forwardPlanar);
            float upElement = Vector3.Dot(lookDirection, Vector3.up);
            float rightElement = Vector3.Dot(lookDirection, rightPlanar);

            if (forwardElement < 0.1f)
            {
                forwardElement = 0.1f;
            }
            rightElement /= 2;

            Vector3 combinedLook = forwardPlanar * forwardElement + Vector3.up * upElement + rightPlanar * rightElement;
            Vector3 lookForwards = combinedLook.normalized * GrappleLookAtPower + forwardPlanar * (1- GrappleLookAtPower);
            lookForwards = -lookForwards.normalized;
            targD = lookForwards;


            float ropeDist = (connectionPoint - playerCentreOfMass.position).magnitude;
            if(ropeDist > connectedDistance - 0.1f)
            {
                audioManager.UpdateGrappleSwingingIntensity((playerRigidbody.velocity.magnitude / 35f) + 0.4f);
            }
            else
            {
                audioManager.UpdateGrappleSwingingIntensity(0.4f);
            }
        }
        else
        {
            targD = -transform.forward;

            audioManager.UpdateGrappleSwingingIntensity(0);
        }

        Quaternion rotGoal = Quaternion.LookRotation(targD, Vector3.up);
        grappleGunModel.transform.rotation = Quaternion.Slerp(grappleGunModel.transform.rotation, rotGoal, lookAtSmoothSpeed);
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

            // See if the object has the glow effect enabled. if so, make it glow and let it know
            // that this script is what is grappled to it
            GlowFlashObject connectedGlowObject = hit.collider.gameObject.GetComponent<GlowFlashObject>();
            if(connectedGlowObject != null)
                connectedGlowObject.GrappleStarted(referenceInitiator: this);

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

            audioManager.GrappleStart();
        }
    }

    void DisconnectGrapple()
    {
        connectedDistance = 0;

        grapplingRope.Retract();

        Destroy(Rope);
        grappleConnected = false;

        audioManager.GrappleEnd();
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
