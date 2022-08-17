using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Known Objects")]
    public Transform mainCamera;
    public CollisionDetector GroundDetector;
    private Rigidbody bodyRigidBody;

    [Header("Movement Settings")]
    [Tooltip("The maximum speed the player will accelerate to when on the ground as a result of key-presses.")]
    public float WalkingSpeed = 8f;

    [Tooltip("The power of the acceleration. 1 means instant acceleration to full walking speed. 0 means something slow.")]
    [Range(0f, 1f)]
    public float Acceleration = 0.8f;

    [Tooltip("The power of the deceleration when the player is actively cancelling motion.")]
    public float CancellationPower = 50f;
    [Tooltip("The power of the deceleration that stops the player sliding sideways")]
    public float SidewaysDeceleration = 10f;

    [Tooltip("How much drag the character should have when no keys are pressed (how quick they slow down).")]
    public float DragWhenNoKeysPressed = 10f;

    [Header("Air Related Settings")]
    [Tooltip("Additional gravity is added when falling, in order to increasing the feeling of weight with the character ")]
    public float CharacterFallingWeight = 5f;
    public float JumpForce = 10f;
    //[Tooltip("The acceleration felt when controlling the ball in the air, ONLY CONTROLLING, no increase in speed is possible.")]
    //public float AirAcceleration = 15f;

    Vector2 keyboardInputs;
    bool JumpAlreadyKeyPressed = false;

    void Start()
    {
        bodyRigidBody = GetComponent<Rigidbody>();

    }

    void Update()
    {
        keyboardInputs.x = Input.GetAxisRaw("Horizontal");
        keyboardInputs.y = Input.GetAxisRaw("Vertical");


        if (GroundDetector.IsOnGround)
        {
            if (!JumpAlreadyKeyPressed && Input.GetKeyDown(KeyCode.Space))
            {
                JumpAlreadyKeyPressed = true;
                bodyRigidBody.AddForce(transform.up * JumpForce, ForceMode.Impulse);
            }
        }
        else
        {
            if (JumpAlreadyKeyPressed)
            {
                JumpAlreadyKeyPressed = false;
            }
        }

    }

    private void FixedUpdate()
    {
        transform.localRotation = Quaternion.identity * Quaternion.Euler(0, mainCamera.transform.localEulerAngles.y, 0);


        float xInput = Input.GetAxisRaw("Horizontal");
        float yInput = Input.GetAxisRaw("Vertical");

        processMotion(xInput, yInput);

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
                bodyRigidBody.AddForce(wishDirection * CancellationPower, ForceMode.Acceleration);
            }
            else if (forwardsSpeed < WalkingSpeed && GroundDetector.IsOnGround)
            {
                // How much required acceleration there is to reach the intended speed (walkingspeed).
                float requiredAcc = (WalkingSpeed - forwardsSpeed) / (Time.fixedDeltaTime * ((1 - Acceleration) * 25 + 1));

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
            bodyRigidBody.AddForce(Vector3.down * 20);
        }

    }



}
