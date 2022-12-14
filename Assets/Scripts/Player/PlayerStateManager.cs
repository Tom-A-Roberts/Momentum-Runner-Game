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
    public float hitmarkerDuration = 0.1f;

    public bool IsSpectating => playerNetworking._isSpectating.Value;

    public bool IsRespawning => playerNetworking._isRespawning.Value;

    public bool IsDead => playerNetworking._isDead.Value;

    // Set to true if level is singleplayer and it's been completed (finish line reached)
    public bool completedSingleplayerLevel = false;

    //[SerializeField]
    //private bool _isRespawningAndIsHost = false;
    //public bool IsRespawningAndIsHost => _isRespawningAndIsHost;

    [System.NonSerialized]
    public Vector3 bodySpawnPosition;
    [System.NonSerialized]
    public Vector3 feetSpawnPosition;
    [System.NonSerialized]
    public Vector3 bodyFeetOffset;

    /// <summary>
    /// Tracks what is locally happening. If this value differs from the network variable, then they are resynced within Update()
    /// </summary>
    [System.NonSerialized]
    public bool isRespawningLocally = false;

    /// <summary>
    /// Tracks what is locally happening. If this value differs from the network variable, then they are resynced within Update()
    /// </summary>
    [System.NonSerialized]
    public bool isDeadLocally = false;

    /// <summary>
    /// Tracks what is locally happening. If this value differs from the network variable, then they are resynced within Update() 
    /// </summary>
    [System.NonSerialized]
    public bool isSpectatingLocally = false;

    // Privates
    private int fogInitDelayCounter = 0;
    private bool fogInitialized = false;
    private Vector3 bodyStartPosition;
    private Quaternion bodyStartRotation;
    private Vector3 feetStartPosition;
    private Quaternion feetStartRotation;

    private float hitmarkerTimer = 0;

    private float slowdownTimer = 0;

    public float SlowdownShieldTimer => slowdownShieldTimer;
    private float slowdownShieldTimer = 0;

    private float respawningTimer = 0;

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

        UnityEngine.Rendering.Volume[] sceneVolumes = GameObject.FindObjectsOfType<UnityEngine.Rendering.Volume>();
        foreach (var sceneVolume in sceneVolumes)
        {
            if (sceneVolume != null && SobelEnabled)
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

        if (GameStateManager.Singleton && GameStateManager.Singleton.ScreenRedEdges)
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

    }

    void InitializeFogwall()
    {


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

    void Update()
    {
        if (!fogInitialized && playerNetworking.IsOwner)
        {
            fogInitDelayCounter += 1;// Time.deltaTime;
            if (fogInitDelayCounter > 5)
            {
                fogInitialized = true;
                InitializeFogwall();
            }
        }
        if(GameStateManager.Singleton && (!GameStateManager.Singleton.DeveloperMode && !playerNetworking._isDead.Value && NetworkManager.Singleton.IsHost))
            CheckForDeath();

        if (IsRespawning && NetworkManager.Singleton.IsHost && GameStateManager.Singleton)
        {
            respawningTimer += Time.deltaTime;
            if(respawningTimer > GameStateManager.Singleton.respawnDuration)
            {
                playerNetworking._isRespawning.Value = false;
            }
        }

        if(hitmarkerTimer > 0)
        {
            hitmarkerTimer -= Time.deltaTime;
            if(hitmarkerTimer <= 0)
            {
                hitmarkerTimer = 0;
                EndHitmarker();
            }
        }

        if(slowdownTimer > 0 && GameStateManager.Singleton)
        {
            slowdownTimer -= Time.deltaTime / GameStateManager.Singleton.slowdownTime;

            if(slowdownTimer <=0){
                slowdownTimer = 0;
                EndSlowdown();
            }
            else
            {
                ProcessSlowdown(1 - slowdownTimer);
            }
        }

        if(slowdownShieldTimer > 0 && GameStateManager.Singleton)
        {
            slowdownShieldTimer -= Time.deltaTime / GameStateManager.Singleton.slowdownTimeShield;

            if(slowdownShieldTimer <= 0)
            {
                slowdownShieldTimer = 0;
            }

            ProcessSlowdownShield(1 - slowdownShieldTimer);
        }

        //if (Input.GetKeyDown(KeyCode.T))
        //{
        //    ProcessHit();
        //}

        CheckForServerStateChanges();
    }

    private void FixedUpdate()
    {
        if (slowdownTimer > 0)
        {
            ProcessSlowdownFixedUpdate(1 - slowdownTimer);
        }
    }

    public void CheckForServerStateChanges()
    {
        if (isDeadLocally != playerNetworking._isDead.Value)
        {
            if (playerNetworking._isDead.Value)
                PlayerDeathLocally();
            else
                PlayerExitDeathLocally();
        }

        if(isSpectatingLocally != playerNetworking._isSpectating.Value)
        {
            if (playerNetworking._isSpectating.Value)
                EnterSpectatorModeLocally();
            else
                LeaveSpectatorModeLocally();
        }

        if (isRespawningLocally != playerNetworking._isRespawning.Value)
        {
            if (playerNetworking._isRespawning.Value)
                EnterRespawningModeLocally();
            else
                LeaveRespawningModeLocally();
        }
    }

    /// <summary>
    /// Called by collision detector
    /// </summary>
    public void CheckForRespawn()
    {
        // Server checks for respawning state:
        //if ((NetworkManager.Singleton.IsHost || playerNetworking.IsOwner) && !IsRespawning)
        if ((NetworkManager.Singleton.IsHost) && !IsRespawning)
        {
            // Respawn player and teleport them to location:
            if(!playerNetworking._isRespawning.Value)
                playerNetworking.ServerTeleportPlayer(playerBody.position);
            
            playerNetworking._isRespawning.Value = true;
        }
    }
   

    /// <summary>
    /// (Death by deathwall)
    /// </summary>
    void CheckForDeath()
    {
        if(GameStateManager.Singleton && (GameStateManager.Singleton.deathWallCollider && GameStateManager.Singleton.deathWall))
        {
            Vector3 ClosestPoint = Physics.ClosestPoint(playerBody.position, GameStateManager.Singleton.deathWallCollider, GameStateManager.Singleton.deathWall.transform.position, GameStateManager.Singleton.deathWall.transform.rotation);

            float signedDistance = Vector3.Dot(playerBody.position - ClosestPoint, -GameStateManager.Singleton.deathWall.transform.up);

            float sidewaysDistance = Vector3.Dot(playerBody.position - GameStateManager.Singleton.deathWall.transform.position, GameStateManager.Singleton.deathWall.transform.right);

            //Debug.Log(sidewaysDistance.ToString() + "   " + GameStateManager.Singleton.deathWall.transform.localScale.x.ToString());

            //Debug.DrawLine(playerBody.position, ClosestPoint, Color.red);
            //Debug.DrawLine(playerBody.position, playerBody.position + sidewaysDistance * GameStateManager.Singleton.deathWall.transform.right, Color.blue);

            if(signedDistance < -0.2 && Mathf.Abs(sidewaysDistance) < GameStateManager.Singleton.deathWall.transform.localScale.x / 2)
            {
                playerNetworking._isDead.Value = true;
                if (NetworkManager.Singleton.IsHost)
                {
                    GameStateManager.Singleton.TestForWinState();
                }
            }
        }
    }

    /// <summary>
    /// As the deathwall gets closer to the player, the effects show up on the screen more
    /// </summary>
    public void UpdateDeathWallEffects(Transform deathwallTransform, BoxCollider deathWallCollider)
    {
        if (GameStateManager.Singleton)
        {
            if (deathwallTransform.gameObject.activeSelf)
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
            else
            {
                Color newcol = GameStateManager.Singleton.ScreenRedEdges.color;
                newcol.a = 0;
                GameStateManager.Singleton.ScreenRedEdges.color = newcol;
            }
        }
    }




    public void StartHitmarker()
    {
        if (playerNetworking.IsOwner)
        {
            playerAudioManager.HitmarkerSound();
            if (IngameEscMenu.Singleton.hitmarkerUIElement)
            {
                hitmarkerTimer = hitmarkerDuration;
                IngameEscMenu.Singleton.hitmarkerUIElement.SetActive(true);
            }
            else
            {
                Debug.LogWarning("No hitmarker UI element found");
            }
        }
    }
    public void EndHitmarker()
    {
        if (playerNetworking.IsOwner)
        {
            if (IngameEscMenu.Singleton.hitmarkerUIElement)
            {
                IngameEscMenu.Singleton.hitmarkerUIElement.SetActive(false);
            }
            else
            {
                Debug.LogWarning("No hitmarker UI element found");
            }
        }
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

    public void RespawnPlayerToBeginning()
    {
        TeleportPlayer(bodyStartPosition);
        playerBody.transform.rotation = bodyStartRotation;
        playerFeet.transform.rotation = feetStartRotation;
    }

    public void RestartLevel()
    {
        bodySpawnPosition = bodyStartPosition;
        feetSpawnPosition = feetStartPosition;

        RespawnPlayerToBeginning();
        //reset any other variables changes during the level
    }


    public void TeleportPlayer(Vector3 newBodyPosition)
    {
        playerBody.position = newBodyPosition;
        playerFeet.position = newBodyPosition - bodyFeetOffset;
        playerBody.transform.position = newBodyPosition;
        playerFeet.transform.position = newBodyPosition - bodyFeetOffset;
        playerFeet.transform.rotation = feetStartRotation;

        playerBody.velocity = Vector3.zero;
        playerFeet.velocity = Vector3.zero;
    }

    /// <summary>
    /// Activated every time this player completes a lap
    /// </summary>
    public void HasCompletedLap()
    {
        Debug.Log("Lap complete!");
        // Not implemented yet
    }
    public void HasCompletedSingleplayerLevel()
    {
        //Debug.Log("Here!!");

        if (!completedSingleplayerLevel)
        {
            completedSingleplayerLevel = true;
            IEnumerator winWaitCoroutine = WaitAfterSingleplayerWin(6f);

            WinLoseEffects winLoseEffects = FindObjectOfType<WinLoseEffects>();
            if (winLoseEffects != null)
            {
                winLoseEffects.StartWinEffects();
            }
            StartCoroutine(winWaitCoroutine);
        }
    }

    private IEnumerator WaitAfterSingleplayerWin(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        IngameEscMenu.Singleton.LoadMainMenu();
    }


    #region Locally Entering and Leaving States

    public void PlayerDeathLocally()
    {
        if (!isDeadLocally)
        {
            EnterSpectatorModeLocally();
        }
        isDeadLocally = true;
    }

    public void PlayerExitDeathLocally()
    {
        if (isDeadLocally)
        {
            LeaveSpectatorModeLocally();
        }
        isDeadLocally = false;
    }

    /// <summary>
    /// Plays out local animation for entering spectator mode locally. If is host, it also ensures the message is sent to the other players too
    /// </summary>
    public void EnterSpectatorModeLocally()
    {
        if (NetworkManager.Singleton.IsHost && !IsSpectating)
        {
            playerNetworking._isSpectating.Value = true;
        }

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
            //bodyCollider.enabled = false;
            //feetCollider.enabled = false;
            spectatorRepresentation.enabled = true;
            HideMultiplayerRepresentation();
        }

        isSpectatingLocally = true;
    }

    /// <summary>
    /// Plays out local animation for leaving spectator mode locally. If is host, it also ensures the message is sent to the other players too
    /// </summary>
    public void LeaveSpectatorModeLocally()
    {
        if (NetworkManager.Singleton.IsHost && IsSpectating && !IsDead && !IsRespawning)
        {
            playerNetworking._isSpectating.Value = false;
        }

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
            //bodyCollider.enabled = true;
            //feetCollider.enabled = true;
            spectatorRepresentation.enabled = false;
            ShowMultiplayerRepresentation();
        }

        isSpectatingLocally = false;
    }

    /// <summary>
    /// Plays out local animation for entering respawning mode locally. If is host, it also ensures the message is sent to the other players too
    /// </summary>
    public void EnterRespawningModeLocally()
    {
        if(NetworkManager.Singleton.IsHost)
        {
            respawningTimer = 0;
            playerNetworking._isSpectating.Value = true;
            playerNetworking._isRespawning.Value = true;
        }
        if (playerNetworking.IsOwner && IngameEscMenu.Singleton)
            IngameEscMenu.Singleton.ShowRecoveringInfo();

        isRespawningLocally = true;
        playerController.respawningMode = true;

        EnterSpectatorModeLocally();
    }

    /// <summary>
    /// Plays out local animation for leaving respawning mode locally. If is host, it also ensures the message is sent to the other players too
    /// </summary>
    public void LeaveRespawningModeLocally()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            if (!playerNetworking._isDead.Value)
            {
                playerNetworking._isSpectating.Value = false;
                playerNetworking.ServerTeleportPlayer(playerBody.position);
            }
            respawningTimer = 0;
            playerNetworking._isRespawning.Value = false;
        }

        if (isRespawningLocally)
        {
            playerBody.velocity = Vector3.zero;
            playerController.AddForce(10, playerBody.transform.forward, ForceMode.VelocityChange);
            playerController.AddForce(10, Vector3.up, ForceMode.VelocityChange);
        }

        if (playerNetworking.IsOwner && IngameEscMenu.Singleton)
            IngameEscMenu.Singleton.HideRecoveringInfo();

        isRespawningLocally = false;
        playerController.respawningMode = false;

        LeaveSpectatorModeLocally();
    }
    #endregion

    public void ProcessHit()
    {
        Debug.Log("Ow I just got shot");

        if(slowdownShieldTimer == 0 && GameStateManager.Singleton && GameStateManager.Singleton.GameState != GameState.readiedUp)
        {
            playerAudioManager.HitSound();
            slowdownShieldTimer = 1;
            StartSlowdown();
        }
    }

    [System.NonSerialized]
    public Vector3 slowdownStartVelocity;

    public void StartSlowdown()
    {
        slowdownTimer = 1;

        slowdownStartVelocity = playerBody.velocity;

        playerController.BoostForce(-slowdownStartVelocity, ForceMode.VelocityChange);


    }

    public void GameStarted()
    {

        slowdownShieldTimer = 2;
    }

    /// <summary>
    /// Changes the slowdown state for this player. Called even if it's a remote player
    /// </summary>
    /// <param name="t">Percentage through the slowdown, from 0 to 1</param>
    public void ProcessSlowdown(float t)
    {

        if (playerNetworking.IsOwner && GameStateManager.Singleton)
        {
            if (GameStateManager.Singleton.SlowdownBorder)
            {
                Color newcol = GameStateManager.Singleton.SlowdownBorder.color;
                newcol.a = 1 - t;
                GameStateManager.Singleton.SlowdownBorder.color = newcol;
            }

        }
    }

    public void ProcessSlowdownShield(float t)
    {
        if (!playerNetworking.IsOwner)
        {
            playerNetworking.nameplate.SetHealthBar(1-t);
        }
    }

    public void ProcessSlowdownFixedUpdate(float t)
    {
        const float percentageOfSlowdown = 1f;

        float newT = Mathf.Clamp01(t / percentageOfSlowdown);

        if(newT <= 1 && GameStateManager.Singleton)
        {
            Vector3 GravityForce = Physics.gravity * 1f * (1 - newT);
            playerController.BoostForce(-GravityForce, ForceMode.Acceleration);
            playerController.playerControlFactor = newT;

            playerController.BoostForce((slowdownStartVelocity * (1 + GameStateManager.Singleton.slowdownAdditionalVelocityMultiplier)) / GameStateManager.Singleton.slowdownTime, ForceMode.Acceleration);
        }
    }

    public void EndSlowdown()
    {

        if (playerNetworking.IsOwner && GameStateManager.Singleton)
        {
            if (GameStateManager.Singleton.SlowdownBorder)
            {
                Color newcol = GameStateManager.Singleton.SlowdownBorder.color;
                newcol.a = 0;
                GameStateManager.Singleton.SlowdownBorder.color = newcol;
            }
        }
    }
}
