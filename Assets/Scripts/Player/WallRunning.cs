using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class WallRunning : NetworkBehaviour
{
    [Header("Wallrunning")]
    public PlayerAudioManager playerAudioManager;
    public LayerMask whatIsWall;
    public LayerMask whatIsGround;
    public float wallRunForce;
    public float maxWallRunTime;
    public float wallRunTimer;
    Tuple<float, float> axisValues;
    public float stickingForce = 100f;
    private float targetAngle;
    private float originalAngle = 0f;
    public float WallRunCameraAngle = 25f;
    public float attachAngleTimer = 0.5f;
    public float detachAngleTimer = 0.7f;
    private float currentTargetTime;
    private float angleTimer;

    [System.NonSerialized]
    public bool spectatorMode = false;

    [Tooltip("0= weightless, 1= as weighty as normal")]
    [Range(0f, 1f)]
    public float effectOfGravityDuringWallrun = 0.6f;

    [Tooltip("Amount of friction added to slow you moving down a wall during wallrunning")]
    public float verticalUpFrictionalCoefficient = 1;
    [Tooltip("Amount of friction added to slow you moving up a wall during wallrunning")]
    public float verticalDownFrictionalCoefficient = 1;

    [Header("Detection")]
    public float wallCheckDistance;
    public float minJumpHeight;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    private bool wallLeft;
    private bool wallRight;
    private bool isWallRunning;

    [Header("References")]
    public CollisionDetector GroundDetector;
    public CameraController cc;
    private PlayerController pc;
    private Rigidbody rb;

    public GameObject CurrentWallrunWall => currentWallrunWall;
    private GameObject currentWallrunWall;

    public bool IsWallRunning => isWallRunning;

    private float currentStickingForce;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            Destroy(this);
        }
    }

    // Start is called before the first frame update
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pc = GetComponent<PlayerController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (currentTargetTime - angleTimer >= 0f)
        {
            angleTimer += Time.deltaTime;

            if (currentTargetTime != 0f)
            {
                cc.zRotation = Mathf.Lerp(originalAngle, targetAngle, angleTimer / currentTargetTime);
            }
            else
            {
                cc.zRotation = targetAngle;
            }
        }

        CheckForWalls();
        StateMachine();
    }

    private void StateMachine()
    {
        axisValues = pc.GetMovementAxis();
        
        if((wallLeft || wallRight) && axisValues.Item2 > 0 && !GroundDetector.IsOnGround && !spectatorMode)
        {
            StartWallRun();
            
        }
        else
        {
            StopWallRun();
        }
    }

    private void FixedUpdate()
    {
        if (isWallRunning == true)
        {
            WallRunningMovement();
        }
        else
        {
            playerAudioManager.UpdateWallRunningIntensity(0);
        }
    }

    private void StartWallRun()
    {
        originalAngle = cc.zRotation;
        if (wallRight) targetAngle = WallRunCameraAngle;
        else if (wallLeft) targetAngle = -WallRunCameraAngle;
        currentTargetTime = attachAngleTimer;
        angleTimer = 0f;
        
        pc.ResetJumps(pc.JumpCount);

        Stick();

        isWallRunning = true;

        if(currentWallrunWall != null)
        {
            GlowFlashObject glowFlashObject = currentWallrunWall.GetComponent<GlowFlashObject>();
            if (glowFlashObject != null)
            {
                glowFlashObject.WallRunStarted(referenceInitiator: this);
            }
        }
    }

    private void WallRunningMovement()
    {
        // Remove effect of gravity, according to "effectOfGravityDuringWallrun"
        rb.AddForce(transform.up * (pc.CharacterFallingWeight + Physics.gravity.magnitude) * effectOfGravityDuringWallrun, ForceMode.Acceleration);

        Vector3 wallNormal = wallRight ? rightWallHit.normal: leftWallHit.normal;
        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        pc.SetWallNormal(wallNormal);

        if((transform.forward - wallForward).magnitude > (transform.forward + wallForward).magnitude)
        {
            wallForward *= -1;
        }

        //forward force applied to player
        rb.AddForce(wallForward * wallRunForce, ForceMode.Force);

        //nudge player towards wall if they are not actively trying to escape wallrun
        if(!(wallLeft && axisValues.Item1 > 0 ) && !(wallRight && axisValues.Item1 < 0))
        {
            rb.AddForce(-wallNormal * currentStickingForce, ForceMode.Force);
        }

        float y_speed = rb.velocity.y;
        if(y_speed < 0)
        {
            rb.AddForce(transform.up * -y_speed * verticalUpFrictionalCoefficient, ForceMode.Acceleration);
        }
        else
        {
            rb.AddForce(transform.up * -y_speed * verticalDownFrictionalCoefficient, ForceMode.Acceleration);
        }

        Vector3 wallVelocity = Vector3.ProjectOnPlane(rb.velocity, wallNormal);
        playerAudioManager.UpdateWallRunningIntensity(wallVelocity.magnitude / 20f);
    }

    private void StopWallRun()
    {
        targetAngle = 0;
        originalAngle = cc.zRotation;
        currentTargetTime = detachAngleTimer; //lower than attach feels nicer
        angleTimer = 0f;

        pc.SetWallNormal(Vector3.zero);
        isWallRunning = false;
    }

    private void CheckForWalls()
    {
        wallRight = Physics.Raycast(transform.position, transform.right, out rightWallHit, wallCheckDistance, whatIsWall);
        wallLeft = Physics.Raycast(transform.position, -transform.right, out leftWallHit, wallCheckDistance, whatIsWall);

        if(wallRight)
            currentWallrunWall = rightWallHit.collider.gameObject;
        else if (wallLeft)
            currentWallrunWall = leftWallHit.collider.gameObject;
    }

    public void Unstick()
    {
        currentStickingForce = 0f;
    }

    public void Stick()
    {
        currentStickingForce = stickingForce;
    }
}
