using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Known Objects")]
    public Transform mainCamera;
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
    [Tooltip("The acceleration felt when controlling the ball in the air, ONLY CONTROLLING, no increase in speed is possible.")]
    public float AirAcceleration = 15f;
    [Tooltip("How much drag the character should have when no keys are pressed (how quick they slow down).")]
    public float DragWhenNoKeysPressed = 19f;
    [Tooltip("Force applied to player when jump button is pressed.")]
    public float jumpHeight = 10000f;

    Vector2 keyboardInputs;
    bool JumpKeyPressed = false;
    bool isOnFloor = true;
    int jumpNumber = 2;

    void Start()
    {
        bodyRigidBody = GetComponent<Rigidbody>();

    }

    void Update()
    {
        if (isOnFloor)
        {
            jumpNumber = 2;
        }
        keyboardInputs.x = Input.GetAxisRaw("Horizontal");
        keyboardInputs.y = Input.GetAxisRaw("Vertical");
        if(Input.GetKey(KeyCode.Space) && jumpNumber > 0)
        {
            jumpNumber--;
            Jump();
        }

    }

    private void FixedUpdate()
    {
        transform.localRotation = Quaternion.identity * Quaternion.Euler(0, mainCamera.transform.localEulerAngles.y, 0);


        float xInput = Input.GetAxisRaw("Horizontal");
        float yInput = Input.GetAxisRaw("Vertical");

        processMotion(xInput, yInput);

    }
    void Jump()
    {
        bodyRigidBody.AddForce(new Vector3 (0,jumpHeight,0));
    }

    void processMotion(float xInput, float yInput)
    {

        if (xInput == 0 && yInput == 0 && isOnFloor)
        {
            bodyRigidBody.drag = DragWhenNoKeysPressed;
        }
        else
        {
            bodyRigidBody.drag = 0.025f;

        }

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

            float requiredAcc = 0;

            // Travelling in completely the wrong direction to the user input, so use CancellationDeceleration
            if (forwardsSpeed < 0)
            {
                bodyRigidBody.AddForce(wishDirection * CancellationPower, ForceMode.Acceleration);
            }
            else if (forwardsSpeed < WalkingSpeed)
            {
                // How much required acceleration there is to reach the intended speed (walkingspeed).
                requiredAcc = (WalkingSpeed - forwardsSpeed) / (Time.fixedDeltaTime * ((1 - Acceleration)*25 + 1));

                bodyRigidBody.AddForce(wishDirection * requiredAcc, ForceMode.Acceleration);

            }

            bodyRigidBody.AddForce(-sidewaysVelocity * SidewaysDeceleration, ForceMode.Acceleration);


            //if (isOnFloor)
            //{

            //    MultiplierV = (MultiplierV * (MaxAcceleration - MinAcceleration)) + MinAcceleration;
            //    MultiplierH = (MultiplierH * (MaxAcceleration - MinAcceleration)) + MinAcceleration;

            //    bodyRigidBody.AddForce(targetForwardVector * MultiplierV);
            //    bodyRigidBody.AddForce(targetHorizontalVector * MultiplierH);
            //}
            //else
            //{
            //    MultiplierV = MultiplierV * AirAcceleration;
            //    MultiplierH = MultiplierH * AirAcceleration;

            //    bodyRigidBody.AddForce(targetForwardVector * MultiplierV);
            //    bodyRigidBody.AddForce(targetHorizontalVector * MultiplierH);

            //    bodyRigidBody.AddForce(Vector3.down * 20);
            //}
        }
    }

}
