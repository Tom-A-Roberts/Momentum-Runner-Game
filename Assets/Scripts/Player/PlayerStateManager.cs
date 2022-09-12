using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Unity.Netcode;
using UnityEngine.UI;

public class PlayerStateManager : MonoBehaviour
{
    public bool SobelEnabled = true;

    [Header("Known Objects")]
    public PlayerNetworking playerNetworking;
    public Rigidbody playerBody;
    public Rigidbody playerFeet;
    public Camera playerCamera;
    public PlayerAudioManager playerAudioManager;
    public PlayerController playerController;
    public WallRunning wallRunningScript;
    public GunController gunController;
    public GrappleGun grappleGunScript;
    public CameraController cameraController;
    public Transform multiplayerRepresentation;
    public MeshRenderer[] firstPersonViewMeshes;
    public MeshRenderer spectatorRepresentation;
    public CapsuleCollider bodyCollider;
    public SphereCollider feetCollider;


    [Header("Effects settings")]
    public float deathwallRedStartDistance = 5;

    [Header("Game States")]
    [SerializeField]
    private bool _spectatorMode = false;
    public bool SpectatorMode => _spectatorMode;

    [System.NonSerialized]
    public Vector3 bodySpawnPosition;
    [System.NonSerialized]
    public Vector3 feetSpawnPosition;
    [System.NonSerialized]
    public Vector3 bodyFeetOffset;

    // Privates
    
    private Vector3 bodyStartPosition;
    private Quaternion bodyStartRotation;
    private Vector3 feetStartPosition;
    private Quaternion feetStartRotation;

    void Start()
    {
        GameObject spawnPointG = GameObject.FindGameObjectWithTag("SpawnPoint");
        Vector3 spawnPosition = Vector3.zero;
        if (spawnPointG == null)
            Debug.LogWarning("No spawnpoint found! spawning player at 0,0,0");
        else
        {
            spawnPosition = spawnPointG.transform.position;
        }
        bodyFeetOffset = playerBody.transform.position - playerFeet.transform.position;

        Vector3 spawnOffset = new Vector3(Random.value * 5 - 2.5f, 0, Random.value * 5 - 2.5f);
        playerBody.transform.position = spawnPosition + spawnOffset;
        playerFeet.transform.position = spawnPosition - bodyFeetOffset + spawnOffset;

        bodyStartPosition = playerBody.transform.position;
        bodyStartRotation = playerBody.transform.rotation;
        feetStartPosition = playerFeet.transform.position;
        feetStartRotation = playerFeet.transform.rotation;

        bodySpawnPosition = bodyStartPosition;
        feetSpawnPosition = feetStartPosition;

        //Sobel sobelController = null;
        UnityEngine.Rendering.Volume sceneVolume = GameObject.FindObjectOfType<UnityEngine.Rendering.Volume>();
        if(sceneVolume != null && SobelEnabled)
        {
            for (int componentID = 0; componentID < sceneVolume.profile.components.Count; componentID++)
            {
                if (sceneVolume.profile.components[componentID].name.Contains("Sobel"))
                {
                    sceneVolume.profile.components[componentID].active = true;
                    //FogManager.Instance.sobelRenderer = sceneVolume.profile.components[componentID];
                    //sobelController = (Sobel)sceneVolume.profile.components[componentID];
                }
            }
        }

        if (playerNetworking.IsOwner)
        {
            // Let GameStateManager know who is the local player
            GameStateManager.Singleton.localPlayer = this;

            // Update the fog manager to know who is the local player
            if (FogManager.Instance)
            {
                FogManager.Instance.ResetFog();
            }
            else
            {
                FogManager fogger = FindObjectOfType<FogManager>();

                FogManager.Instance = fogger;

                if (!fogger)
                    Debug.LogWarning("No FogManager found!");
            }
            // if we get to this point and no FogManager then big sad 
            if (FogManager.Instance)
            {
                FogManager.Instance.playerBody = playerBody.gameObject;
                FogManager.Instance.playerCamera = playerCamera;
                FogManager.Instance.Initialize();
            }
        }

        if(GameStateManager.Singleton && GameStateManager.Singleton.ScreenRedEdges)
        {
            Color newcol = GameStateManager.Singleton.ScreenRedEdges.color;
            newcol.a = 0;
            GameStateManager.Singleton.ScreenRedEdges.color = newcol;
        }
        else
        {
            Debug.LogWarning("No red edges image found in UI, the effect won't work. Please set GameStateManager.ScreenRedEdges");
        }

        playerBody.position = bodyStartPosition;
        playerFeet.position = feetStartPosition;

        if (SpectatorMode)
        {
            EnterSpectatorMode();
        }
        else
        {
            LeaveSpectatorMode();
        }
    }

    void Update()
    {

    }

    public void ShowMultiplayerRepresentation()
    {
        for (int childID = 0; childID < multiplayerRepresentation.childCount; childID++)
        {
            multiplayerRepresentation.GetChild(childID).GetComponent<MeshRenderer>().enabled = true;
        }
    }
    public void HideMultiplayerRepresentation()
    {
        for (int childID = 0; childID < multiplayerRepresentation.childCount; childID++)
        {
            multiplayerRepresentation.GetChild(childID).GetComponent<MeshRenderer>().enabled = false;
        }
    }
    public void ShowFirstPersonRepresentation()
    {
        foreach (MeshRenderer mesh in firstPersonViewMeshes)
        {
            mesh.enabled = true;
        }
    }
    public void HideFirstPersonRepresentation()
    {
        foreach (MeshRenderer mesh in firstPersonViewMeshes)
        {
            mesh.enabled = false;
        }
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

        playerNetworking.EnterSpectatorModeServerRPC();
    }

    /// <summary>
    /// As the deathwall gets closer to the player, the effects show up on the screen more
    /// </summary>
    public void UpdateDeathWallEffects(Transform deathwallTransform, BoxCollider deathWallCollider)
    {
        if (GameStateManager.Singleton && GameStateManager.Singleton.ScreenRedEdges)
        {
            Vector3 ClosestPoint = Physics.ClosestPoint(playerBody.position, deathWallCollider, deathwallTransform.position, deathwallTransform.rotation);

            float distance = Vector3.Distance(ClosestPoint, playerBody.position);

            float power = Mathf.Clamp01((deathwallRedStartDistance - distance) / deathwallRedStartDistance);

            playerAudioManager.UpdateDeathwallIntensity(power);

            Color newcol = GameStateManager.Singleton.ScreenRedEdges.color;
            newcol.a = power;
            GameStateManager.Singleton.ScreenRedEdges.color = newcol;
        }
    }

    //public MeshRenderer spectatorRepresentation;
    //public CapsuleCollider bodyCollider;
    //public SphereCollider feetCollider;

    public void EnterSpectatorMode()
    {
        _spectatorMode = true;

        wallRunningScript.spectatorMode = true;
        grappleGunScript.spectatorMode = true;
        gunController.spectatorMode = true;
        playerController.spectatorMode = true;
        playerAudioManager.spectatorMode = true;
        cameraController.spectatorMode = true;


        HideFirstPersonRepresentation();

        if (playerNetworking.IsOwner)
        {
            if (GrappleCrosshair.Instance)
                GrappleCrosshair.Instance.spectatorMode = true;
        }
        else
        {
            bodyCollider.enabled = false;
            feetCollider.enabled = false;
            spectatorRepresentation.enabled = true;
            HideMultiplayerRepresentation();
        }
    }
    public void LeaveSpectatorMode()
    {
        _spectatorMode = false;

        wallRunningScript.spectatorMode = false;
        grappleGunScript.spectatorMode = false;
        gunController.spectatorMode = false;
        playerController.spectatorMode = false;
        playerAudioManager.spectatorMode = false;
        cameraController.spectatorMode = false;

        ShowFirstPersonRepresentation();

        if (playerNetworking.IsOwner)
        {
            if(GrappleCrosshair.Instance)
                GrappleCrosshair.Instance.spectatorMode =false;
        }
        else
        {
            bodyCollider.enabled = true;
            feetCollider.enabled = true;
            spectatorRepresentation.enabled = false;
            ShowMultiplayerRepresentation();
        }
    }

    public void ProcessPotentialHit(int playerHitID)
    {
        if (playerHitID != -1)
            Debug.Log("I think I ('" + this.gameObject.name + "') just shot player ID: " + playerHitID.ToString());

        //playerNetworking.LeverFlicked();
    }


}
