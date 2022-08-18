using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallRunning : MonoBehaviour
{
    [Header("Wallrunning")]
    public LayerMask whatIsWall;
    public LayerMask whatIsGround;
    public float wallRunForce;
    public float maxWallRunTime;
    public float wallRunTimer;
    Tuple<float, float> axisValues;

    public float verticalUpFrictionalCoefficient = 1;
    public float verticalDownFrictionalCoefficient = 1;

    [Header("Input")]
    private float horizontalInput;
    private float verticalInput;

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
    private PlayerController pc;
    private Rigidbody rb;

    // Start is called before the first frame update
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pc = GetComponent<PlayerController>();
    }

    // Update is called once per frame
    void Update()
    {
        CheckForWalls();
        StateMachine();
    }

    private void StateMachine()
    {
        axisValues = pc.GetMovementAxis();
        
        if(wallLeft || wallRight && axisValues.Item2 > 0 && !GroundDetector.IsOnGround)
        {
            StartWallRun();
            Debug.Log("WallRunning");
        }
        else
        {
            StopWallRun();
            Debug.Log("no");
        }
    }

    private void FixedUpdate()
    {
        if (isWallRunning == true)
        {
            WallRunningMovement();
        }
    }

    private void StartWallRun()
    {
        isWallRunning = true;
    }

    private void WallRunningMovement()
    {
        //rb.AddForce(transform.up * 29.81f, ForceMode.Acceleration);
        rb.AddForce(transform.up * 24.81f, ForceMode.Acceleration);
        Vector3 wallNormal = wallRight ? rightWallHit.normal: leftWallHit.normal;
        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        if((transform.forward - wallForward).magnitude > (transform.forward + wallForward).magnitude)
        {
            wallForward *= -1;
        }

        //forward force applied to player
        rb.AddForce(wallForward*wallRunForce, ForceMode.Force);

        //nudge player towards wall if they are not actively trying to escape wallrun
        if(!(wallLeft && axisValues.Item1 > 0 ) && !(wallRight && axisValues.Item1 < 0))
        {
            rb.AddForce(-wallNormal * 100, ForceMode.Force);
        }

        //verticalUpFrictionalCoefficient = 1;
        //verticalDownFrictionalCoefficient = 1;

        float y_speed = rb.velocity.y;
        if(y_speed < 0)
        {
            rb.AddForce(transform.up * y_speed * verticalUpFrictionalCoefficient, ForceMode.Acceleration);
        }
        else
        {
            rb.AddForce(transform.up * -y_speed * verticalDownFrictionalCoefficient, ForceMode.Acceleration);
        }

}

    private void StopWallRun()
    {
        isWallRunning = false;
    }

    private void CheckForWalls()
    {
        wallRight = Physics.Raycast(transform.position, transform.right, out rightWallHit, wallCheckDistance, whatIsWall);
        wallLeft = Physics.Raycast(transform.position, -transform.right, out leftWallHit, wallCheckDistance, whatIsWall);
    }
}
