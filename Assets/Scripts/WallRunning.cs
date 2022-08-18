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
        rb.AddForce(transform.up * 29.81f, ForceMode.Acceleration);
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
