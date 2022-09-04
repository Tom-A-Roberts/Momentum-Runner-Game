using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Unity.Netcode;

public class LevelLogicManager : MonoBehaviour
{
    public bool SobelEnabled = true;

    public Rigidbody playerBody;
    public Rigidbody playerFeet;

    private Transform spawnPoint;

    private Vector3 bodyStartPosition;
    private Quaternion bodyStartRotation;
    private Vector3 feetStartPosition;
    private Quaternion feetStartRotation;

    public Vector3 bodySpawnPosition;
    public Vector3 feetSpawnPosition;
    public Vector3 bodyFeetOffset;

    void Start()
    {
        spawnPoint = GameObject.FindGameObjectWithTag("SpawnPoint").transform;

        bodyFeetOffset = playerBody.transform.position - playerFeet.transform.position;

        Vector3 spawnOffset = new Vector3(Random.value * 5 - 2.5f, 0, Random.value * 5 - 2.5f);
        playerBody.transform.position = spawnPoint.transform.position + spawnOffset;
        playerFeet.transform.position = spawnPoint.transform.position - bodyFeetOffset + spawnOffset;

        bodyStartPosition = playerBody.transform.position;
        bodyStartRotation = playerBody.transform.rotation;
        feetStartPosition = playerFeet.transform.position;
        feetStartRotation = playerFeet.transform.rotation;

        bodySpawnPosition = bodyStartPosition;
        feetSpawnPosition = feetStartPosition;
        

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
        playerBody.position = bodyStartPosition;
        playerFeet.position = feetStartPosition;
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

    //public void BeenShotByOwner()
    //{
    //    Debug.Log("I've (" + gameObject.name + ") been shot by the owner player");
    //}
    ///// <summary>
    ///// Sent by server on CORRECT hit
    ///// </summary>
    //public void BeenShotByRemote()
    //{
    //    Debug.Log("I've (" + gameObject.name + ") been shot by a remote player");
    //}

    public void ProcessPotentialHit(int playerHitID)
    {
        if (playerHitID != -1)
            Debug.Log("I think I ('" + this.gameObject.name + "') just shot player ID: " + playerHitID.ToString());
    }


}
