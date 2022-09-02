using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class LevelController : MonoBehaviour
{
    public bool SobelEnabled = true;

    public Rigidbody playerBody;
    public Rigidbody playerFeet;

    private Vector3 bodyStartPosition;
    private Quaternion bodyStartRotation;
    private Vector3 feetStartPosition;
    private Quaternion feetStartRotation;

    public Vector3 bodySpawnPosition;
    public Vector3 feetSpawnPosition;
    public Vector3 bodyFeetOffset;

    void Start()
    {

        bodyStartPosition = playerBody.transform.position;
        bodyStartRotation = playerBody.transform.rotation;
        feetStartPosition = playerFeet.transform.position;
        feetStartRotation = playerFeet.transform.rotation;

        bodySpawnPosition = bodyStartPosition;
        feetSpawnPosition = feetStartPosition;
        bodyFeetOffset = bodyStartPosition - feetStartPosition;

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
    }

    public void RespawnPlayer()
    {
        playerBody.transform.position = bodySpawnPosition;
        playerBody.transform.rotation = bodyStartRotation;
        playerFeet.transform.position = feetSpawnPosition;
        playerFeet.transform.rotation = feetStartRotation;
    }

    public void RestartLevel()
    {
        bodySpawnPosition = bodyStartPosition;
        feetSpawnPosition = feetStartPosition;
        RespawnPlayer();
        //reset any other variables changes during the level
    }
    public void PlayerDeath()
    {
        playerBody.transform.position = bodyStartPosition;
        playerBody.transform.rotation = bodyStartRotation;
        playerFeet.transform.position = feetStartPosition;
        playerFeet.transform.rotation = feetStartRotation;

        playerBody.velocity = Vector3.zero;
    }
}
