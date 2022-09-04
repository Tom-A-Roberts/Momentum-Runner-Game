using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleGun : MonoBehaviour
{
    [Header("Known System Objects")]
    [Tooltip("(Local Player Only) This players camera")]
    public Camera PlayerCamera;
    [Tooltip("(Local Player Only) Script responsible for controlling this players movement")]
    public PlayerController PlayerController;
    [Tooltip("(Local and Remote Player) The audio manager of this scripts owning player")]
    public PlayerAudioManager audioManager;

    [Header("Transforms")]
    [Tooltip("(Visual Only) Point that the rope visual is fired from")]
    public Transform GunEndPosition;
    [Tooltip("(Physics Only) Point that the rope joint will start from")]
    public Transform playerCentreOfMass;

    [Header("Visuals")]
    [Tooltip("(Visual Only) Model representing the grappling gun")]
    public GameObject grappleGunModel;
    [Tooltip("(Visual Only) Script responsible for handling the visuals of the rope")]
    public GrapplingRope grapplingRope;

    [Header("Gameplay Settings")]
    [Tooltip("(Local Player Only) Maximum distance a grapple can be initiated from")]
    public float MaxGrappleLength = 20f;
    [Tooltip("(Local Player Only) Force applied to player when grappling")]
    public float GrappleForce = 10f;

    [Header("Gun LookAt Settings")]
    [Tooltip("(Visual Only) How much the grapple gun moves when trying to face the grapple point")]
    public float GrappleLookAtPower = 0.4f; 
    [Tooltip("(Visual Only) How fast the grapple gun looks at the grapple point")]
    public float lookAtSmoothSpeed = 0.05f;

    /// <summary>
    /// Is this GrappleGun owned by this clients player?
    /// </summary>
    [System.NonSerialized] public bool isGrappleOwner = true;

    /// <summary>
    /// Is this GrappleGun currently connected to something?
    /// </summary>
    [System.NonSerialized] public bool grappleConnected = false;
    
    private ConfigurableJoint Rope;    
    private Rigidbody playerRigidbody;

    private Vector3 connectedPoint;
    private float connectedDistance;

    private void Start()
    {
        // OR: what happens if the PlayerController has been destroyed on a remote player already?
        playerRigidbody = PlayerController.gameObject.GetComponent<Rigidbody>();
    }

    void Update()
    {
        ProcessGrappleInput();

        UpdateGunLookAt();
        UpdateGrappleAudio();
    }

    private void FixedUpdate()
    {
        ApplyGrappleForce();
    }

    private void ProcessGrappleInput()
    {
        // Check if grapple gun is being controlled by the owner. If yes, then process local player input
        if (isGrappleOwner)
        {
            if (Input.GetButton("Grapple") && !grappleConnected)
            {
                TryConnectGrapple();
            }
            else if (!Input.GetButton("Grapple") && grappleConnected)
            {
                DisconnectGrapple();
            }
        }
    }

    void TryConnectGrapple()
    {
        // OR: currently not verified by server
        RaycastHit hit;
        if (Physics.Raycast(PlayerCamera.transform.position, PlayerCamera.transform.forward, out hit, MaxGrappleLength, LayerMask.GetMask("Terrain")))
        {
            connectedPoint = hit.point;
            connectedDistance = hit.distance;

            SetupJoint(connectedPoint, connectedDistance);

            // animate extend on server

            // animate extend on client immediately
            AnimateExtend(connectedPoint);
            UpdateGlow(hit);

            grappleConnected = true;
        }
    }

    private void SetupJoint(Vector3 point, float distance)
    {
        Rope = playerCentreOfMass.gameObject.AddComponent<ConfigurableJoint>();
        Rope.autoConfigureConnectedAnchor = false;
        Rope.connectedAnchor = point;

        Rope.xMotion = ConfigurableJointMotion.Limited;
        Rope.yMotion = ConfigurableJointMotion.Limited;
        Rope.zMotion = ConfigurableJointMotion.Limited;

        // make a springy limit
        //SoftJointLimitSpring linearLimitSpring = Rope.linearLimitSpring;
        //linearLimitSpring.spring = 30f;
        //Rope.linearLimitSpring = linearLimitSpring;

        // make a limit
        SoftJointLimit limit = Rope.linearLimit;
        limit.limit = distance;
        limit.contactDistance = 0.05f;
        Rope.linearLimit = limit;

        Rope.enablePreprocessing = true;

        Rope.massScale = 0.5f;
    }

    private void AnimateExtend(Vector3 grapplePosition)
    {
        grapplingRope.Extend(grapplePosition);
        audioManager.GrappleStart();
    }

    void DisconnectGrapple()
    {
        connectedDistance = 0;

        Destroy(Rope);

        // animate retract on server

        // animate retract on client immediately
        AnimateRetract();

        grappleConnected = false;
    }

    private void AnimateRetract()
    {
        grapplingRope.Retract();
        audioManager.GrappleEnd();
    }

    /// <summary>
    /// (Local Only) Applies some force to player when grappling to make it feel better
    /// </summary>
    void ApplyGrappleForce()
    {
        if (grappleConnected && isGrappleOwner)
        {
            Vector3 ropeVec = connectedPoint - playerCentreOfMass.position;

            Vector3 forceDir = Vector3.Cross(playerCentreOfMass.right, ropeVec).normalized;

            PlayerController.AddForce(GrappleForce, forceDir, ForceMode.Force);
        }
    }

    private void UpdateGlow(RaycastHit hit)
    {
        // See if the object has the glow effect enabled. if so, make it glow and let it know
        // that this script is what is grappled to it
        GlowFlashObject connectedGlowObject = hit.collider.gameObject.GetComponent<GlowFlashObject>();
        if (connectedGlowObject != null)
            connectedGlowObject.GrappleStarted(referenceInitiator: this);
    }

    private void UpdateGunLookAt()
    {
        // target direction for gun to look at
        Vector3 targD;

        if (grappleConnected)
        {
            Vector3 lookDirection = connectedPoint - grappleGunModel.transform.position;
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
            Vector3 lookForwards = combinedLook.normalized * GrappleLookAtPower + forwardPlanar * (1 - GrappleLookAtPower);
            lookForwards = -lookForwards.normalized;
            targD = lookForwards;
        }
        else
        {
            targD = -transform.forward;            
        }

        Quaternion rotGoal = Quaternion.LookRotation(targD, Vector3.up);
        grappleGunModel.transform.rotation = Quaternion.Slerp(grappleGunModel.transform.rotation, rotGoal, lookAtSmoothSpeed);
    }

    private void UpdateGrappleAudio()
    {
        if (grappleConnected)
        {
            float ropeDist = (connectedPoint - playerCentreOfMass.position).magnitude;
            if (ropeDist > connectedDistance - 0.1f)
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
            audioManager.UpdateGrappleSwingingIntensity(0);
        }
    }
}
