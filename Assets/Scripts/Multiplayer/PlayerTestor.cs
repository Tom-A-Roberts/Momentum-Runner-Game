using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerTestor : NetworkBehaviour
{
    public float Speed = 1;
    public float CancellationPower = 1;
    public float Acceleration = 0.8f;
    public float SidewaysDeceleration = 1;
    public float sprintMultiplier = 2f;
    // Start is called before the first frame update

    private float speedTracker = 1;

    private Rigidbody myRigidbody;
    private Vector2 input;
    void Start()
    {
        myRigidbody = GetComponent<Rigidbody>();
        speedTracker = Speed;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            Destroy(this);
        }
    }

    // Update is called once per frame
    void Update()
    {

        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;



    }
    void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            speedTracker = Speed * sprintMultiplier;
        }
        else
        {
            speedTracker = Speed;
        }

        if (input != Vector2.zero)
        {
            // Calculate what directions the inputs mean in worldcoordinate terms
            Vector3 verticalInputWorldDirection = (Vector3.forward - Vector3.right).normalized * input.y;
            Vector3 horizontalInputWorldDirection = (Vector3.forward + Vector3.right).normalized * input.x;

            // The direction that the player wishes to go in
            Vector3 wishDirection = (verticalInputWorldDirection + horizontalInputWorldDirection).normalized;

            // 90 degrees to the wish velocity
            Vector3 wishVelocitySideways = Quaternion.Euler(0, 90, 0) * wishDirection;

            // Current velocity without the y speed included
            Vector3 currentPlanarVelocity = new Vector3(myRigidbody.velocity.x, 0, myRigidbody.velocity.z);

            // Unwanted velocity that is sideways to the wish direction
            Vector3 sidewaysVelocity = Vector3.Project(currentPlanarVelocity, wishVelocitySideways);

            float forwardsSpeed = Vector3.Dot(wishDirection, currentPlanarVelocity);

            // Travelling in completely the wrong direction to the user input, so use CancellationDeceleration
            if (forwardsSpeed < 0)
            {
                float activeCancellationPower = CancellationPower;

                myRigidbody.AddForce(wishDirection * activeCancellationPower, ForceMode.Acceleration);
            }
            else if (forwardsSpeed < speedTracker)
            {
                // How much required acceleration there is to reach the intended speed (walkingspeed).
                float requiredAcc = (speedTracker - forwardsSpeed) / (Time.fixedDeltaTime * ((1 - Acceleration) * 25 + 1));

                myRigidbody.AddForce(wishDirection * requiredAcc, ForceMode.Acceleration);
            }

            myRigidbody.AddForce(-sidewaysVelocity * SidewaysDeceleration, ForceMode.Acceleration);

        }
    }




}
