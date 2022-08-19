using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelController : MonoBehaviour
{

    public Rigidbody playerBody;
    public Rigidbody playerFeet;

    private Vector3 bodyStartPosition;
    private Quaternion bodyStartRotation;
    private Vector3 feetStartPosition;
    private Quaternion feetStartRotation;

    private bool resetRequired = false;
    // Start is called before the first frame update
    void Start()
    {

        bodyStartPosition = playerBody.transform.position;
        bodyStartRotation = playerBody.transform.rotation;
        feetStartPosition = playerFeet.transform.position;
        feetStartRotation = playerFeet.transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            resetRequired = true;

        }
    }

    private void FixedUpdate()
    {
        if (resetRequired)
        {
            resetRequired = false;
            playerBody.transform.position = bodyStartPosition;
            playerBody.transform.rotation = bodyStartRotation;
            playerFeet.transform.position = feetStartPosition;
            playerFeet.transform.rotation = feetStartRotation;

            playerBody.velocity = Vector3.zero;
        }
    }
}
