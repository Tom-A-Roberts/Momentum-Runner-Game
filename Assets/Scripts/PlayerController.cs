using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Known Objects")]
    public Transform mainCamera;
    //[Tooltip("The acceleration felt when moving forwards on the ground at higher speeds.")]
    private Rigidbody bodyRigidBody;

    [Header("Movement Settings")]
    [Tooltip("The maximum speed the player will accelerate to when on the ground as a result of key-presses.")]
    public float WalkingSpeed = 80f;
    [Tooltip("The power of the acceleration in-line with the player's desired direction.")]
    public float Acceleration = 10f;
    [Tooltip("The power of the deceleration when the player is actively cancelling .")]
    public float CancellationDeceleration = 15f;
    [Tooltip("The power of the deceleration that stops the player sliding sideways")]
    public float SidewaysDeceleration = 15f;
    [Tooltip("The acceleration felt when controlling the ball in the air, ONLY CONTROLLING, no increase in speed is possible.")]
    public float AirAcceleration = 15f;

    Vector2 keyboardInputs;
    bool JumpKeyPressed = false;
    bool isOnFloor = true;

    void Start()
    {
        bodyRigidBody = GetComponent<Rigidbody>();

    }

    void Update()
    {
        keyboardInputs.x = Input.GetAxisRaw("Horizontal");
        keyboardInputs.y = Input.GetAxisRaw("Vertical");


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

        if (xInput == 0 && yInput == 0 && isOnFloor)
        {
            bodyRigidBody.drag = 4;
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

            // velocity that is in-line with the wish direction
            //Vector3 forwardsVelocity = Vector3.Project(wishVelocity, currentPlanarVelocity);

            float forwardsSpeed = Vector3.Dot(wishDirection, currentPlanarVelocity);

            print(forwardsSpeed.ToString() + "  " + sidewaysVelocity.magnitude.ToString());

            // Travelling in completely the wrong direction to the user input, so use CancellationDeceleration
            if (forwardsSpeed < 0)
            {
                bodyRigidBody.AddForce(wishDirection * CancellationDeceleration, ForceMode.Acceleration);
            }
            else if (forwardsSpeed < WalkingSpeed)
            {
                // We are accelerating towards the target speed
                bodyRigidBody.AddForce(wishDirection * Acceleration, ForceMode.Acceleration);
            }

            bodyRigidBody.AddForce(-sidewaysVelocity * SidewaysDeceleration, ForceMode.Acceleration);


            //float MultiplierV = 1;
            //float MultiplierH = 1;

            //float DotV = Vector3.Dot(targetForwardVector, currentPlanarVelocity.normalized);
            //float DotH = Vector3.Dot(targetHorizontalVector, currentPlanarVelocity.normalized);


            //if (DotV < 0)
            //{
            //    DotV = 0;
            //}
            //if (DotH < 0)
            //{
            //    DotH = 0;
            //}
            //MultiplierV = 0.5f - DotV;
            //MultiplierH = 0.5f - DotH;

            //if (MultiplierV < 0)
            //{
            //    MultiplierV = 0;
            //}
            //if (MultiplierH < 0)
            //{
            //    MultiplierH = 0;
            //}

            //MultiplierV *= 2;
            //MultiplierH *= 2;


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
