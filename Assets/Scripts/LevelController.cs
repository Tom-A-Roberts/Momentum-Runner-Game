using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    public bool SobelEnabled = true;

    public Rigidbody playerBody;
    public Rigidbody playerFeet;

    private Vector3 bodyStartPosition;
    private Quaternion bodyStartRotation;
    private Vector3 feetStartPosition;
    private Quaternion feetStartRotation;

    private bool resetRequired = false;

    void Start()
    {

        bodyStartPosition = playerBody.transform.position;
        bodyStartRotation = playerBody.transform.rotation;
        feetStartPosition = playerFeet.transform.position;
        feetStartRotation = playerFeet.transform.rotation;

        UnityEngine.Rendering.Volume sceneVolume = GameObject.FindObjectOfType<UnityEngine.Rendering.Volume>();
        if(sceneVolume != null && SobelEnabled)
        {
            for (int componentID = 0; componentID < sceneVolume.profile.components.Count; componentID++)
            {
                if (sceneVolume.profile.components[componentID].name.Contains("Sobel"))
                {
                    sceneVolume.profile.components[componentID].active = true;
                }
            }
            
        }
    }

    void Update()
    {
        //if (resetRequired)
        //{
        //    resetRequired = false;
        //    playerBody.transform.position = bodyStartPosition;
        //    playerBody.transform.rotation = bodyStartRotation;
        //    playerFeet.transform.position = feetStartPosition;
        //    playerFeet.transform.rotation = feetStartRotation;

        //    playerBody.velocity = Vector3.zero;
        //}
    }

    public void PlayerDeath()
    {
        resetRequired = true;
        resetRequired = false;
        playerBody.transform.position = bodyStartPosition;
        playerBody.transform.rotation = bodyStartRotation;
        playerFeet.transform.position = feetStartPosition;
        playerFeet.transform.rotation = feetStartRotation;

        playerBody.velocity = Vector3.zero;
    }
}
