using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleGun : MonoBehaviour
{
    [Header("Known Player Objects")]
    [Tooltip("(Local Player Only) This players camera")]
    public GameObject PlayerCamera;
    [Tooltip("(Local Player Only) Script responsible for controlling this players movement")]
    public PlayerController PlayerController;
    [Tooltip("(Local and Remote Player) The audio manager of this scripts owning player")]
    public PlayerAudioManager AudioManager;
    [Tooltip("(Local and Remote Player) This players networking script")]
    public PlayerNetworking playerNetworking;

    [Header("Transforms")]
    [Tooltip("(Visual Only) Point that the rope visual is fired from")]
    public Transform GunEndPosition;
    [Tooltip("(Physics Only) Point that the rope joint will start from")]
    public Transform PlayerCentreOfMass;

    [Header("Visuals")]
    [Tooltip("(Visual Only) Model representing the grappling gun")]
    public GameObject GrappleGunModel;
    [Tooltip("(Visual Only) Script responsible for handling the visuals of the rope")]
    public GrapplingRope GrapplingRope;

    [Header("Gameplay Settings")]
    [Tooltip("(Local Player Only) Maximum distance a grapple can be initiated from")]
    public float MaxGrappleLength = 20f;
    [Tooltip("(Local Player Only) Force applied to player when grappling")]
    public float GrappleForce = 10f;
    [Tooltip("(Local Player Only) The spherecast radius to check, when the player misses a grapple target")]
    public float AimAssistRadius = 1f;

    [Header("Gun LookAt Settings")]
    [Tooltip("(Visual Only) How much the grapple gun moves when trying to face the grapple point")]
    public float GrappleLookAtPower = 0.4f; 
    [Tooltip("(Visual Only) How fast the grapple gun looks at the grapple point")]
    public float LookAtSmoothSpeed = 0.05f;


    /// <summary>
    /// Is this GrappleGun owned by this clients player?
    /// </summary>
    [System.NonSerialized] public bool isGrappleOwner = true;

    /// <summary>
    /// Is this GrappleGun currently connected to something?
    /// </summary>
    [System.NonSerialized] public bool grappleConnected = false;
    
    private ConfigurableJoint ropeJoing;    
    private Rigidbody playerRigidbody;
    private GrappleCrosshair onScreenGrappleCrosshair;

    private Vector3 connectedPoint;
    private float connectedDistance;

    /// <summary>
    /// Can add to this in the future, e.g which gameobject was hit.
    /// This saves having to send the rayhit about which is not good practice
    /// </summary>
    public struct GrappleablePointInfo
    {
        /// <summary>
        /// Whether the grapple ray test found a target
        /// </summary>
        public bool targetFound;
        /// <summary>
        /// Where the grapple ray test point is
        /// </summary>
        public Vector3 grapplePoint;
        /// <summary>
        /// Distance from raygun end to the grapplePoint
        /// </summary>
        public float grappleDistance;

        /// <summary>
        /// Empty info collection for when you don't grapple to anything
        /// </summary>
        public static GrappleablePointInfo Empty
        {
            get => new GrappleablePointInfo()
            {
                targetFound = false,
                grapplePoint = Vector3.zero,
                grappleDistance = 0,
            };
        }
    }

    private void Start()
    {
        // OR: what happens if the PlayerController has been destroyed on a remote player already?
        playerRigidbody = PlayerController.gameObject.GetComponent<Rigidbody>();

        if (playerNetworking.IsOwner)
            onScreenGrappleCrosshair = GrappleCrosshair.Instance;
    }

    private void Update()
    {
        GrappleablePointInfo raypointInfo;
        if (!grappleConnected)
            raypointInfo = TestForGrappleablePoint();
        else
            raypointInfo = GrappleablePointInfo.Empty;
        ProcessGrappleInput(raypointInfo);

        // Show grapplepoint on UI
        if (onScreenGrappleCrosshair != null)
            onScreenGrappleCrosshair.UpdateGrappleLocation(raypointInfo);

        UpdateGunLookAt();
        UpdateGrappleAudio();
    }

    private void FixedUpdate()
    {
        ApplyGrappleForce();
    }

    private void ProcessGrappleInput(GrappleablePointInfo raypointInfo)
    {
        // Check if grapple gun is being controlled by the owner. If yes, then process local player input
        if (isGrappleOwner)
        {
            if (Input.GetButton("Grapple") && !grappleConnected && raypointInfo.targetFound)
            {
                ConnectGrapple(raypointInfo);
            }
            else if (!Input.GetButton("Grapple") && grappleConnected)
            {
                DisconnectGrapple();
            }
        }
    }

    private void ConnectGrapple(GrappleablePointInfo raypointInfo)
    {
        // OR: currently not verified by server

        connectedPoint = raypointInfo.grapplePoint;
        connectedDistance = raypointInfo.grappleDistance;

        SetupJoint(connectedPoint, connectedDistance);

        // animate extend on server

        playerNetworking.UpdateGrappleState(true, connectedPoint);

        // animate extend on client immediately
        //AnimateExtend(connectedPoint);

        grappleConnected = true;
    }

    private GrappleablePointInfo TestForGrappleablePoint()
    {
        RaycastHit hit;
        // Don't spherecast immediately in front of the player
        const float initialSpherecastDeadzone = 2;
        if (Physics.Raycast(PlayerCamera.transform.position, PlayerCamera.transform.forward, out hit, MaxGrappleLength, LayerMask.GetMask("Terrain")))
        {
            return new GrappleablePointInfo()
            {
                targetFound = true,
                grapplePoint = hit.point,
                grappleDistance = hit.distance,
            };
        }
        else 
        {
            Vector3 spherecastStart = PlayerCamera.transform.position + playerRigidbody.transform.forward * initialSpherecastDeadzone;
            float spherecastLength = MaxGrappleLength - (AimAssistRadius/1) - initialSpherecastDeadzone;
            if (Physics.SphereCast(spherecastStart, (AimAssistRadius/1), PlayerCamera.transform.forward, out hit, spherecastLength, LayerMask.GetMask("Terrain")))
            {
                return new GrappleablePointInfo()
                {
                    targetFound = true,
                    grapplePoint = hit.point,
                    grappleDistance = Vector3.Distance(hit.point, PlayerCamera.transform.position),
                };
            }
            //else
            //{
            //    float spherecastLength2 = MaxGrappleLength - AimAssistRadius - initialSpherecastDeadzone;
            //    if (Physics.SphereCast(spherecastStart, AimAssistRadius, PlayerCamera.transform.forward, out hit, spherecastLength2, LayerMask.GetMask("Terrain")))
            //    {
            //        return new GrappleablePointInfo()
            //        {
            //            targetFound = true,
            //            grapplePoint = hit.point,
            //            grappleDistance = Vector3.Distance(hit.point, PlayerCamera.transform.position),
            //        };
            //    }
            //}
        }
        return GrappleablePointInfo.Empty;
    }

    private void SetupJoint(Vector3 point, float distance)
    {
        ropeJoing = PlayerCentreOfMass.gameObject.AddComponent<ConfigurableJoint>();
        ropeJoing.autoConfigureConnectedAnchor = false;
        ropeJoing.connectedAnchor = point;

        ropeJoing.xMotion = ConfigurableJointMotion.Limited;
        ropeJoing.yMotion = ConfigurableJointMotion.Limited;
        ropeJoing.zMotion = ConfigurableJointMotion.Limited;

        // make a springy limit
        //SoftJointLimitSpring linearLimitSpring = Rope.linearLimitSpring;
        //linearLimitSpring.spring = 30f;
        //Rope.linearLimitSpring = linearLimitSpring;

        // make a limit
        SoftJointLimit limit = ropeJoing.linearLimit;
        limit.limit = distance;
        limit.contactDistance = 0.05f;
        ropeJoing.linearLimit = limit;

        ropeJoing.enablePreprocessing = true;

        ropeJoing.massScale = 0.5f;
    }

    public void AnimateExtend(Vector3 grapplePosition)
    {
        GrapplingRope.Extend(grapplePosition);

        UpdateGlow(grapplePosition);

        AudioManager.GrappleStart();
    }

    private void DisconnectGrapple()
    {
        connectedDistance = 0;

        Destroy(ropeJoing);

        // animate retract on server
        playerNetworking.UpdateGrappleState(false, Vector3.zero);

        // animate retract on client immediately

        grappleConnected = false;
    }

    public void AnimateRetract()
    {
        GrapplingRope.Retract();
        AudioManager.GrappleEnd();
    }

    /// <summary>
    /// (Local Only) Applies some force to player when grappling to make it feel better
    /// </summary>
    private void ApplyGrappleForce()
    {
        if (grappleConnected && isGrappleOwner)
        {
            Vector3 ropeVec = connectedPoint - PlayerCentreOfMass.position;

            Vector3 forceDir = Vector3.Cross(PlayerCentreOfMass.right, ropeVec).normalized;

            PlayerController.AddForce(GrappleForce, forceDir, ForceMode.Force);
        }
    }

    private void UpdateGlow(Vector3 point)
    {
        Collider[] colliders = Physics.OverlapSphere(point, 0.1f);

        // See if the objects have the glow effect enabled. if so, make them glow and let them know
        // that this script has grappled to them
        GlowFlashObject connectedGlowObject;
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject.TryGetComponent(out connectedGlowObject))
                connectedGlowObject.GrappleStarted(referenceInitiator: this);
        }
    }

    private void UpdateGunLookAt()
    {
        // target direction for gun to look at
        Vector3 targD;

        if (grappleConnected)
        {
            Vector3 lookDirection = connectedPoint - GrappleGunModel.transform.position;
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
        GrappleGunModel.transform.rotation = Quaternion.Slerp(GrappleGunModel.transform.rotation, rotGoal, LookAtSmoothSpeed);
    }

    private void UpdateGrappleAudio()
    {
        if (grappleConnected)
        {
            float ropeDist = (connectedPoint - PlayerCentreOfMass.position).magnitude;
            if (ropeDist > connectedDistance - 0.1f)
            {
                AudioManager.UpdateGrappleSwingingIntensity((playerRigidbody.velocity.magnitude / 35f) + 0.4f);
            }
            else
            {
                AudioManager.UpdateGrappleSwingingIntensity(0.4f);
            }
        }
        else
        {
            AudioManager.UpdateGrappleSwingingIntensity(0);
        }
    }
}
