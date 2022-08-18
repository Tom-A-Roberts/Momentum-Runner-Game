using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Known Objects")]
    public Transform mainCamera;
    public CollisionDetector GroundDetector;
    private Rigidbody bodyRigidBody;
    private WallRunning wallRunning;
    [Header("----------------")]
    [Header("Movement Settings")]
    [Tooltip("The maximum speed the player will accelerate to when on the ground as a result of key-presses.")]
    public float WalkingSpeed = 8f;
    [Tooltip("How much sprinting increases your speed by")]
    public float SprintMultiplier = 1.5f;
    [Tooltip("The power of the acceleration. 1 means instant acceleration to full walking speed. 0 means something slow.")]
    [Range(0f, 1f)]
    public float Acceleration = 0.8f;
    [Tooltip("The power of the deceleration when the player is actively cancelling motion.")]
    public float CancellationPower = 50f;
    [Tooltip("The power of the deceleration that stops the player sliding sideways")]
    public float SidewaysDeceleration = 10f;
    [Tooltip("How much drag the character should have when no keys are pressed (how quick they slow down).")]
    public float DragWhenNoKeysPressed = 10f;
    [Header("----------------")]
    [Header("Jumping Settings")]
    [Tooltip("Additional gravity is added when falling, in order to increasing the feeling of weight with the character ")]
    public float CharacterFallingWeight = 20f;
    [Tooltip("Force applied when jump key is pressed")]
    public float JumpForce = 12f;
    [Tooltip("Force applied double jumping at the very start of a jump")]
    public float MinJumpForce = 2f; 
    [Tooltip("How many times the character jumps including the first jump before the jump key doesnt work anymore")]
    public int JumpCount = 2;
    [Tooltip("How much we multiply jump force by in the air to account for artificial gravity")]
    public float AirJumpMultiplier = 1.5f;
    [Tooltip("At what part in the jump should the jump power start increasing")]
    public float JumpLerpStart = 10f;
    [Tooltip("How hard we 'kick' away from walls when on them")]
    public float MaxWallKickOffForce = 25f;
    [Header("----------------")]
    [Header("Dash Settings")]
    [Tooltip("How much force is applied when air dashing")]
    public float DashForce = 60f;
    [Tooltip("Amount of seconds the dash force lasts")]
    public float DashForceActiveTime = 0.1f;
    [Tooltip("Amount of seconds for the dash cooldown, measured from time the key was pressed")]
    public float DashCooldown = 1f;

    [Tooltip("Allow the player to accelerate to walkspeed while in the air. This does not effect ability to slow down while in the air")]
    public bool PlayerHasAirControl = true;

    private int remainingJumps;
    private Vector3 wallNormal;
    private float movementSpeed;
    private float currentJumpForce;
    private float yVelocity;
    //private bool canDash;
    private float airDashProgress = 0;
    private float airDashCooldownProgress = 0;

    void Start()
    {
        remainingJumps = JumpCount - 1; //this works off array numbers so its technically 2 :)
        bodyRigidBody = GetComponent<Rigidbody>();
        wallRunning = GetComponent<WallRunning>();
    }

    void Update()
    {

        #region JUMPING LOGIC
        if (remainingJumps != JumpCount)
        {
            if (GroundDetector.IsOnGround)
                remainingJumps = JumpCount;
        }
        else
        {
            remainingJumps = JumpCount - 1;
        }
        if (remainingJumps > 0 && Input.GetKeyDown(KeyCode.Space))
        {
            yVelocity = bodyRigidBody.velocity.y;

            // don't apply as much force if beginning the jump
            float airLerp = Mathf.Lerp(1, 0, Mathf.Clamp01(yVelocity / JumpLerpStart));

            currentJumpForce = (Mathf.Lerp(MinJumpForce, JumpForce, airLerp));

            if(!GroundDetector.IsOnGround) currentJumpForce *= AirJumpMultiplier; //apply air jump multiplier if in air

            if (yVelocity < 0) currentJumpForce -= yVelocity; //if falling cancel out falling velocity

            bodyRigidBody.AddForce(transform.up * currentJumpForce, ForceMode.Impulse);

            // unstick and 'kick off wall
            wallRunning.Unstick();

            // kick hardest when facing into wall
            float wallKickRatio = 1f - Vector3.Dot(transform.forward, wallNormal) / 2f;
            bodyRigidBody.AddForce(wallNormal * MaxWallKickOffForce * wallKickRatio, ForceMode.Impulse);

            remainingJumps--;
        }
        #endregion


        #region SPRINTING LOGIC
        if (Input.GetKey(KeyCode.LeftShift)) movementSpeed = WalkingSpeed * SprintMultiplier;
        else movementSpeed = WalkingSpeed;
        #endregion


        #region AIR DASH LOGIC
        if (Input.GetKeyDown(KeyCode.Q) && airDashCooldownProgress <= 0) //if the player is in the air, hasnt already dashed, and presses q
        {
            // Start air dash
            airDashProgress = 1;
            // Start air dash cooldown
            airDashCooldownProgress = 1;
        }
        #endregion

    }

    private void FixedUpdate()
    {
        transform.localRotation = Quaternion.identity * Quaternion.Euler(0, mainCamera.transform.localEulerAngles.y, 0);

        Tuple<float, float> axisValues = GetMovementAxis();               

        processMotion(axisValues.Item1, axisValues.Item2);

        if(airDashProgress > 0)
        {
            processAirDashMotion();
        }
        if (airDashCooldownProgress > 0)
        {
            airDashCooldownProgress -=  Time.fixedDeltaTime / DashCooldown;
            if (airDashCooldownProgress < 0)
                airDashCooldownProgress = 0;
        }

    }


    void processMotion(float xInput, float yInput)
    {
        // Check if there's motion input
        if (xInput != 0 || yInput != 0)
        {
            Vector2 input = new Vector2(xInput, yInput).normalized;

            // Calculate what directions the inputs mean in worldcoordinate terms
            Vector3 verticalInputWorldDirection = new Vector3(mainCamera.transform.forward.x, 0, mainCamera.transform.forward.z).normalized * input.y;
            Vector3 horizontalInputWorldDirection = new Vector3(mainCamera.transform.right.x, 0, mainCamera.transform.right.z).normalized * input.x;

            // The direction that the player wishes to go in
            Vector3 wishDirection = (verticalInputWorldDirection + horizontalInputWorldDirection).normalized;

            // 90 degrees to the wish velocity
            Vector3 wishVelocitySideways = Quaternion.Euler(0, 90, 0) * wishDirection;

            // Current velocity without the y speed included
            Vector3 currentPlanarVelocity = new Vector3(bodyRigidBody.velocity.x, 0, bodyRigidBody.velocity.z);

            // Unwanted velocity that is sideways to the wish direction
            Vector3 sidewaysVelocity = Vector3.Project(currentPlanarVelocity, wishVelocitySideways);

            float forwardsSpeed = Vector3.Dot(wishDirection, currentPlanarVelocity);

            // Travelling in completely the wrong direction to the user input, so use CancellationDeceleration
            if (forwardsSpeed < 0)
            {
                float activeCancellationPower = CancellationPower;
                if (!PlayerHasAirControl) activeCancellationPower /= 8;

                bodyRigidBody.AddForce(wishDirection * activeCancellationPower, ForceMode.Acceleration);
            }
            else if (forwardsSpeed < movementSpeed && (GroundDetector.IsOnGround || PlayerHasAirControl))
            {
                // How much required acceleration there is to reach the intended speed (walkingspeed).
                float requiredAcc = (movementSpeed - forwardsSpeed) / (Time.fixedDeltaTime * ((1 - Acceleration) * 25 + 1));

                bodyRigidBody.AddForce(wishDirection * requiredAcc, ForceMode.Acceleration);
            }

            bodyRigidBody.AddForce(-sidewaysVelocity * SidewaysDeceleration, ForceMode.Acceleration);

        }

        if (xInput == 0 && yInput == 0 && GroundDetector.IsOnGround)
        {
            bodyRigidBody.drag = DragWhenNoKeysPressed;
        }
        else
        {
            bodyRigidBody.drag = 0.025f;
        }

        if (!GroundDetector.IsOnGround)
        {
            bodyRigidBody.AddForce(Vector3.down * CharacterFallingWeight, ForceMode.Acceleration);
        }

    }

    void processAirDashMotion()
    {
        airDashProgress -= Time.fixedDeltaTime / DashForceActiveTime;
        if (airDashProgress < 0)
            airDashProgress = 0;

        // Increase the dash force over time, as the dash happens
        float currentDashForceAmount = DashForce * (1 - airDashProgress) * 2f;
        currentDashForceAmount = DashForce;
        bodyRigidBody.AddForce(transform.forward * currentDashForceAmount, ForceMode.Force); //add a forward horizontal force
        bodyRigidBody.AddForce(new Vector3(0,bodyRigidBody.velocity.y * -1,0), ForceMode.Impulse);
    }

    public Tuple<float, float> GetMovementAxis()
    {
        float xInput = Input.GetAxisRaw("Horizontal");
        float yInput = Input.GetAxisRaw("Vertical");

        return Tuple.Create(xInput, yInput);
    }

    public void AddForce(float strength, Vector3 direction, ForceMode forceMode)
    {
        bodyRigidBody.AddForce(direction * strength, forceMode);
    }

    public void ResetJumps(int numJumps)
    {
        remainingJumps = numJumps;
    }

    public void SetWallNormal(Vector3 normal)
    {
        wallNormal = normal;
    }
}
