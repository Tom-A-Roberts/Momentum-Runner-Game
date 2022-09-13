using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuCameraRotator : MonoBehaviour
{
    public float rotSpeed = 1;

    public float xSpeed = 1;
    public float xMagnitude = 1;
    public float zSpeed = 1;
    public float zMagnitude = 1;

    private Vector3 currentRot;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.rotation = Quaternion.Euler(currentRot);
        currentRot.y += rotSpeed * Time.deltaTime;

        currentRot.x = Mathf.Sin(Time.realtimeSinceStartup * xSpeed) * xMagnitude;
        currentRot.z = Mathf.Sin(Time.realtimeSinceStartup * zSpeed) * zMagnitude;
    }
}
