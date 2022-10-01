using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using static GameStateManager;

public class PlayerNetworking : NetworkBehaviour
{
    /// <summary>
    /// All continuously updated player state data, specifically for this one player.
    /// </summary>
    private NetworkVariable<PlayerNetworkData> _netState = new NetworkVariable<PlayerNetworkData>(writePerm: NetworkVariableWritePermission.Owner);

    private NetworkVariable<FixedString64Bytes> displayName = new NetworkVariable<FixedString64Bytes>(writePerm: NetworkVariableWritePermission.Owner);

    public NetworkVariable<bool> _isDead = new NetworkVariable<bool>(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> _isSpectating = new NetworkVariable<bool>(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> _isRespawning = new NetworkVariable<bool>(writePerm: NetworkVariableWritePermission.Server);

    [Header("Known Objects")]
    public PlayerStateManager myPlayerStateController;
    public GameObject myCamera;
    public GameObject grappleGun;
    public GameObject shootingGun;
    public Rigidbody bodyRigidbody;
    public Rigidbody feetRigidbody;
    public Collider MultiplayerCollider;

    // These variables are set in OnNetworkSpawn
    private GunController myGunController;
    private GrappleGun myGrappleGun;
    private PlayerController myPlayerController;

    /// <summary>
    /// Stats are ONLY tracked by the host
    /// </summary>
    public StatsTracker myStatsTracker;

    /// <summary>
    /// A maintained list of all player ID's in the game (the keys), along with their associated prefabs (the values).
    /// </summary>
    public static Dictionary<ulong, GameObject> ConnectedPlayers;
    public static Dictionary<ulong, PlayerNetworking> ConnectedPlayerNetworkingScripts;

    /// <summary>
    /// Gets set to the local (IsOwner) PlayerNetworking instance
    /// </summary>
    public static PlayerNetworking localPlayer;

    /// <summary>
    /// Holds past player states. If IsOwner, then time is local time. If data is recieved from remote, then time is recieved from the remote owner.
    /// </summary>
    private List<KeyValuePair<float, PositionData>> positionHistory = new List<KeyValuePair<float, PositionData>>();

    /// <summary>
    /// Which ready up state this player is in. May not be accurate in all cases when the player
    /// is not the owner
    /// </summary>
    public ReadyUpState PlayerReadyUpState => _playerReadyUpState;

    /// <summary>
    /// Which ready up state this player is in. May not be accurate in all cases when the player
    /// is not the owner
    /// </summary>
    private ReadyUpState _playerReadyUpState = ReadyUpState.unready;

    /// <summary>
    /// My display name, as decided by the owner playerNetworking
    /// </summary>
    public string DisplayName => displayName.Value.ToSafeString();

    /// <summary>
    /// My display name, as decided by the owner playerNetworking
    /// </summary>

    /// <summary>
    /// <para>unready and ready are what they say on the tin</para>
    /// <para>waitingToReady and waitingToUnready are when you have shot and are waiting for
    /// the server to confirm your new state.</para>
    /// </summary>
    public enum ReadyUpState
    {
        unready,
        waitingToReady,
        ready,
        waitingToUnready
    }

    /// <summary>
    /// How smoothed out a multiplayer player's movement should be. Higher = smoother
    /// </summary>
    [SerializeField] private float _cheapInterpolationTime = 0.05f;


    // Start is called before the first frame update
    void Start()
    {
        myGunController = shootingGun.GetComponent<GunController>();
    }

    // Update is called once per frame
    void Update()
    {
        float localTime = NetworkManager.LocalTime.TimeAsFloat;
        float serverTime = NetworkManager.ServerTime.TimeAsFloat;

        if (IsSpawned)
        {
            if (IsOwner)
            {
                UpdatePositionData(localTime);
            }
            else
            {
                HandlePositionData(serverTime);
            }
        }

        if (NetworkManager.Singleton.IsHost && myStatsTracker != null)
        {
            myStatsTracker.Update(_netState.Value.PositionData.Velocity);
        }
    }

    /// <summary>
    /// Using the local time, this function writes the local player's state along with the time stamp into a struct
    /// and sends this over the network to the other clients. The state is added to a position history list, ordered by
    /// timestamp. Local time is calculated by NetworkManager.LocalTime.TimeAsFloat
    /// </summary>
    /// <param name="localTime">Calculated by: NetworkManager.LocalTime.TimeAsFloat</param>
    private void UpdatePositionData(float localTime)
    {
        PositionData posData = new PositionData()
        {
            Position = bodyRigidbody.position,
            // OR: camera rotation used because locally it has the wallrunning rotation applied
            // ...definitely not a janky way of animating this differently locally and remotely
            Rotation = myCamera.transform.rotation.eulerAngles,
            Velocity = bodyRigidbody.velocity,
        };

        _netState.Value = new PlayerNetworkData()
        {
            SendTime = localTime,
            PositionData = posData,
        };
        
        // OR: needs to be stored to allow players to shoot at server host properly
        positionHistory.Add(new KeyValuePair<float, PositionData>(localTime, posData));
    }

    /// <summary>
    /// Reads the latest player state from the network variable, and updates the position history list with this state .
    /// </summary>
    /// <param name="serverTime">NetworkManager.ServerTime.TimeAsFloat</param>
    private void HandlePositionData(float serverTime)
    {
        PlayerNetworkData dataReceived = _netState.Value;
        float time = dataReceived.SendTime;

        PositionData positionData = dataReceived.PositionData;

        positionHistory.Add(new KeyValuePair<float, PositionData>(time, positionData));

        if (positionHistory.Count > 0)
            InterpolatePosition(serverTime);
    }

    /// <summary>
    /// Finds what position the player was likely to be in at a given point in time.
    /// The function creates a new set of position data, interpolated from the existing data.
    /// </summary>
    /// <param name="localTime">The time at which we wish to observe the state of</param>
    /// <param name="interpolationTime">How much we reverse the time, giving an allowance for lag and packet jitter</param>
    /// <returns>INTERPOLATED position data</returns>
    private PositionData GetPositionDataAtTime(float localTime, float interpolationTime)
    {
        float timeToFind = localTime - interpolationTime;

        int foundDataIndex = positionHistory.FindLastIndex(value => value.Key < timeToFind);

        PositionData foundData;

        float interpRatio;
        PositionData foundData2;
        Vector3 position;
        Vector3 velocity;
        float xRot;
        float yRot;
        float zRot;

        if (foundDataIndex < positionHistory.Count - 1)
        {
            foundData = positionHistory[foundDataIndex].Value;
            interpRatio = (timeToFind - positionHistory[foundDataIndex].Key) / (positionHistory[foundDataIndex + 1].Key - positionHistory[foundDataIndex].Key);           
            foundData2 = positionHistory[foundDataIndex + 1].Value;
            position = Vector3.Lerp(foundData.Position, foundData2.Position, interpRatio);
            velocity = Vector3.Lerp(foundData.Velocity, foundData2.Velocity, interpRatio);
            xRot = Mathf.LerpAngle(foundData.Rotation.x, foundData2.Rotation.x, interpRatio);
            yRot = Mathf.LerpAngle(foundData.Rotation.y, foundData2.Rotation.y, interpRatio);
            zRot = Mathf.LerpAngle(foundData.Rotation.z, foundData2.Rotation.z, interpRatio);
        }
        else
        {
            Debug.LogWarning("Cannot Interp!");
            foundData = positionHistory[positionHistory.Count - 1].Value;
            position = foundData.Position;
            velocity = foundData.Velocity;
            xRot = foundData.Rotation.x;
            yRot = foundData.Rotation.y;
            zRot = foundData.Rotation.z;
        }

        PositionData interpolatedData = new PositionData
        {
            Position = position,
            Velocity = velocity,
            Rotation = new Vector3(xRot, yRot, zRot)
        };

        return interpolatedData;
    }

    /// <summary>
    /// Finds the state of a player from back in time (a particular localTime) and SETS the player's state back
    /// to the old state.
    /// </summary>
    /// <param name="localTime">The time to revert to, as defined by the owner who created the state</param>
    private void InterpolatePosition(float localTime)
    {
        PositionData interpolatedPosition = GetPositionDataAtTime(localTime, _cheapInterpolationTime);

        Vector3 position = interpolatedPosition.Position;
        Vector3 velocity = interpolatedPosition.Velocity;
        float xRot = interpolatedPosition.Rotation.x; 
        float yRot = interpolatedPosition.Rotation.y;
        float zRot = interpolatedPosition.Rotation.z;

        bodyRigidbody.position = position;
        
        feetRigidbody.position = bodyRigidbody.position - myPlayerStateController.bodyFeetOffset;

        myCamera.transform.rotation = Quaternion.Euler(xRot, yRot, zRot);
        bodyRigidbody.rotation = Quaternion.Euler(0, myCamera.transform.eulerAngles.y, myCamera.transform.eulerAngles.z);

        // Keep velocities in sync (Might be a bad idea!)
        bodyRigidbody.velocity = velocity;
        feetRigidbody.velocity = velocity;
    }


    #region SHOOTING
    public void ShootStart(Vector3 shootStartPosition, Vector3 shootDirection)
    {
        if (IsOwner)
        {
            // animate the shot client side immediately
            AnimateShot(shootStartPosition, shootDirection, 0f);

            float shootTime = NetworkManager.Singleton.LocalTime.TimeAsFloat;            
            ShootServerRPC(shootTime, shootStartPosition, shootDirection);
        }
    }

    [ServerRpc]
    public void ShootServerRPC(float shootTime, Vector3 shootStartPosition, Vector3 shootDirection)
    {
        int serverHitHash;

        // OR: not 100% sure that we need rtt here
        ulong rtt = NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(OwnerClientId);            
        float timeToCheck = shootTime - rtt / 1000f;

        // Reset player colliders then fire the shot serverside
        RollbackPlayerColliders(timeToCheck, true);

        GameObject hitObj = myGunController.TryShoot(shootStartPosition, shootDirection);
        serverHitHash = GetHashOfHit(hitObj);

        ResetPlayerColliders();

        //float timeToWait = shootTime - NetworkManager.ServerTime.TimeAsFloat;

        ShootClientRPC(shootTime, shootStartPosition, shootDirection, serverHitHash);
    }

    [ClientRpc]
    public void ShootClientRPC(float shootTime, Vector3 shootStartPosition, Vector3 shootDirection, int serverHitHash)
    {
        float timeToWait = shootTime - NetworkManager.ServerTime.TimeAsFloat;

        if (!IsOwner)
        {            
            AnimateShot(shootStartPosition, shootDirection, timeToWait);
        }

        if (timeToWait > 0)
        {
            StartCoroutine(SyncedShotResult(serverHitHash, timeToWait));
        }
        else
        {
            PerformShotResult(serverHitHash);
        }
    }

    void AnimateShot(Vector3 shootStartPosition, Vector3 shootDirection, float timeToWait)
    {
        // OR: fire instantly if client shooting
        if (timeToWait > 0)
        {
            StartCoroutine(SyncedShotAnimation(shootStartPosition, shootDirection, timeToWait));
        }
        else
        {
            myGunController.DoShoot(shootStartPosition, shootDirection);
        }
    }

    IEnumerator SyncedShotAnimation(Vector3 shootStartPosition, Vector3 shootDirection, float timeToWait)
    {
        if (timeToWait > 0)
            yield return new WaitForSeconds(timeToWait);

        myGunController.DoShoot(shootStartPosition, shootDirection);
    }

    IEnumerator SyncedShotResult(int hitHash, float timeToWait)
    {
        if (timeToWait > 0)
            yield return new WaitForSeconds(timeToWait);

        PerformShotResult(hitHash);
    }

    void PerformShotResult(int hitHash)
    {
        if (hitHash != -1)
        {
            NetworkObject hitNetworkObject = GetNetworkObject((ulong)hitHash);

            if (hitNetworkObject != null)
            {
                GameObject hitGameObject = hitNetworkObject.gameObject;

                // run ALL hit checks in here please
                if (hitGameObject)
                {
                    
                    if (IsOwner)// && (hitGameObject.tag == "Player" || hitGameObject.tag == "Target"))
                    {
                        myPlayerStateController.StartHitmarker();
                    }

                    // first check player hit
                    PlayerNetworking shotPlayerNetworking = CheckForPlayerHit(hitGameObject);
                    if (shotPlayerNetworking != null)
                    {

                        shotPlayerNetworking.myPlayerStateController.ProcessHit();
                        return;
                    }

                    // then switch
                    Target hitTarget = hitGameObject.transform.gameObject.GetComponent<Target>();
                    if (hitTarget != null)
                    {
                        hitTarget.OnHitByLaser();
                        return;
                    }

                    // finally ready up - only if owner
                    if (IsOwner)
                    {                        
                        if (hitGameObject.transform.tag == "Readyup" && GameStateManager.Singleton.GameState == GameState.waitingToReadyUp)
                        {
                            ReadyUpStateChange();
                            return;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// </summary>
    /// <param name="rayHitObject">The gameobject that was hit by the shoot raycast</param>
    /// <returns>IF the ray hit a network object, returns NetworkObjectID. ELSE, return -1</returns>
    int GetHashOfHit(GameObject rayHitObject)
    {
        if (rayHitObject != null)
        {
            // first check whether the object we hit has a NetworkObject
            NetworkObject hitNetworkObject;
            if (rayHitObject.TryGetComponent<NetworkObject>(out hitNetworkObject))
            {
                return (int)hitNetworkObject.NetworkObjectId;
            }
            else
            {
                // if not then try the parent? (currently only necessary for players)
                // searching in children would be an extremely bad idea
                Transform prefabParent = rayHitObject.transform.root;
                if (prefabParent != null)
                {
                    if (prefabParent.TryGetComponent<NetworkObject>(out hitNetworkObject))
                    {
                        return (int)hitNetworkObject.NetworkObjectId;
                    }
                }
            }
        }

        return -1;
    }

    /// <summary>
    /// </summary>
    /// <param name="rayHitObject">The gameobject that was hit by the shoot raycast</param>
    /// <returns>IF the ray hit a network player, returns its PlayerNetworking. ELSE, returns null</returns>
    public PlayerNetworking CheckForPlayerHit(GameObject rayHitObject)
    {
        if (rayHitObject != null)
        {
            Transform prefabParent = rayHitObject.transform.root;
            if (prefabParent != null)
            {
                PlayerNetworking shotPlayerNetworkingScript;
                if (prefabParent.TryGetComponent<PlayerNetworking>(out shotPlayerNetworkingScript))
                {
                    return shotPlayerNetworkingScript;
                }
                else
                {
                    return null;
                }
            }
        }
        
        return null;
    }
    #endregion

    #region GRAPPLING
    public void UpdateGrappleState(bool grappleConnecting, Vector3 connectedPosition)
    {
        GrappleServerRPC(grappleConnecting, connectedPosition);

        LocalGrapple(grappleConnecting, connectedPosition);
    }

    [ServerRpc]
    public void GrappleServerRPC(bool grappleConnecting, Vector3 connectedPosition)
    {
        GrappleClientRPC(grappleConnecting, connectedPosition);
    }

    [ClientRpc]
    public void GrappleClientRPC(bool grappleConnecting, Vector3 connectedPosition)
    {
        if (!IsOwner)
        {
            LocalGrapple(grappleConnecting, connectedPosition);
        }
    }

    public void LocalGrapple(bool grappleConnecting, Vector3 connectedPosition)
    {
        myGrappleGun.grappleConnected = grappleConnecting;

        if (grappleConnecting)
        {
            myGrappleGun.AnimateExtend(connectedPosition);
        }
        else
        {
            myGrappleGun.AnimateRetract();
        }

    }
    #endregion

    #region SpectatorMode
    /// <summary>
    /// Allow clients to turn themselves into spectators when they like.
    /// No need to prevent cheaters here lol, let them do it
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void EnterSpectatorModeServerRPC()
    {
        _isSpectating.Value = true;
    }
    public void LeaveSpectatorMode()
    {
        _isSpectating.Value = false;
    }

    #endregion

    #region ForceTeleporting
    // Whenever the server wants to teleport a player, this is a useful command. Such as respawning etc
    public void ServerTeleportPlayer(Vector3 teleportLocation)
    {
        TeleportPlayerClientRPC(teleportLocation);
    }

    [ClientRpc]
    public void TeleportPlayerClientRPC(Vector3 teleportLocation)
    {
        myPlayerStateController.TeleportPlayer(teleportLocation);
    }

    #endregion

    #region ReadyingUp


    /// <summary>
    /// When a client (the server, really) thinks everyone has readied up, they can prompt the server to do a check. Usually the server
    /// itself does this. If the server agrees everyone is ready, then it calls the client RPC to say everyone is ready. At this point
    /// it cannot be undone
    /// </summary>
    [ServerRpc(RequireOwnership =false)]
    public void EveryoneHasReadiedUpServerRPC()
    {
        if (GameStateManager.Singleton.readiedPlayers.Count == ConnectedPlayers.Count)
        {
            GameStateManager.Singleton.ServerForceChangeGameState(GameState.readiedUp);
        }
    }

    /// <summary>
    /// Toggles the current ready up state, sending a request to change state to the server. In the meantime, the waiting effects are applied, such as the button
    /// being half pressed.
    /// </summary>
    public void ReadyUpStateChange()
    {
        if (IsOwner)
        {
            if (_playerReadyUpState == ReadyUpState.ready)
            {
                _playerReadyUpState = ReadyUpState.waitingToUnready;
                GameStateManager.Singleton.gameStateSwitcher.LocalStartUnreadyEffects();
                UnReadyUpServerRPC();
            }
            else if (_playerReadyUpState == ReadyUpState.unready)
            {
                _playerReadyUpState = ReadyUpState.waitingToReady;
                GameStateManager.Singleton.gameStateSwitcher.LocalStartReadyUpEffects();
                ReadyUpServerRPC();
            }
        }
    }

    /// <summary>
    /// Activated when the client recieves a message from the server saying you are now fully readied up
    /// </summary>
    public void ServerReadyUp()
    {
        if (IsOwner)
        {
            _playerReadyUpState = ReadyUpState.ready;
            GameStateManager.Singleton.gameStateSwitcher.LocalServerReadyUpEffects();
        }
        else
        {
            // Display unready state above myPlayerStateManager's head
        }

        // Track who's ready and who isn't:
        if (!GameStateManager.Singleton.readiedPlayers.Contains(OwnerClientId))
        {
            GameStateManager.Singleton.readiedPlayers.Add(OwnerClientId);
        }

        // If everyone is ready and you are the host:
        if (IsHost && GameStateManager.Singleton.readiedPlayers.Count == ConnectedPlayers.Count)
        {
            //Debug.Log("here");
            EveryoneHasReadiedUpServerRPC();
        }
    }

    /// <summary>
    /// Activated when the client recieves a message from the server saying you are now fully unreadied
    /// </summary>
    public void ServerUnready()
    {
        if (IsOwner)
        {
            _playerReadyUpState = ReadyUpState.unready;
            GameStateManager.Singleton.gameStateSwitcher.LocalServerUnreadyEffects();
        }
        else
        {
            // Display unready state above myPlayerStateManager's head
        }

        // Track who's ready and who isn't:
        if (GameStateManager.Singleton.readiedPlayers.Contains(OwnerClientId))
        {
            GameStateManager.Singleton.readiedPlayers.Remove(OwnerClientId);
        }
    }

    [ServerRpc]
    public void ReadyUpServerRPC()
    {
        ReadyUpClientRPC();
    }
    [ServerRpc]
    public void UnReadyUpServerRPC()
    {
        UnReadyUpClientRPC();
    }
    [ClientRpc]
    public void ReadyUpClientRPC()
    {
        ServerReadyUp();
    }
    [ClientRpc]
    public void UnReadyUpClientRPC()
    {
        ServerUnready();
    }

    #endregion

    #region RollbackCode

    // should maybe be in seperate manager that rolls back entire server state
    /// <summary>
    /// Rolls back all multiplayer colliders to be in a past game state
    /// </summary>
    /// <param name="time"></param> The time to rollback to 
    /// <param name="ignoreMe"></param> Whether to disable the collider of the player calling this as well
    void RollbackPlayerColliders(float time, bool ignoreMe)
    {
        // disable my own collider if requested
        MultiplayerCollider.enabled = !ignoreMe;

        foreach (ulong playerID in ConnectedPlayers.Keys)
        {
            GameObject playerObject;
            if (ConnectedPlayers.TryGetValue(playerID, out playerObject))
            {
                PlayerNetworking playerNetworkingToRollback;
                if (playerObject.TryGetComponent(out playerNetworkingToRollback))
                {
                    PositionData posData = playerNetworkingToRollback.GetPositionDataAtTime(time, _cheapInterpolationTime);

                    playerNetworkingToRollback.MultiplayerCollider.transform.position = posData.Position;
                }
            }
            else
            {
                Debug.LogWarning("ConnectedPlayers contains players that don't exist!");
            }
        }
    }

    void ResetPlayerColliders()
    {
        MultiplayerCollider.enabled = true;

        foreach (ulong playerID in ConnectedPlayers.Keys)
        {
            GameObject playerObject;
            if (ConnectedPlayers.TryGetValue(playerID, out playerObject))
            {
                PlayerNetworking playerNetworkingToRollback;
                if (playerObject.TryGetComponent(out playerNetworkingToRollback))
                {
                    playerNetworkingToRollback.MultiplayerCollider.transform.localPosition = Vector3.zero;
                }
            }
            else
            {
                Debug.LogWarning("ConnectedPlayers contains players that don't exist!");
            }
        }
    }
    #endregion

    #region Winning/Losing Game

    [ServerRpc]
    public void SendLeaderboardDataServerRPC(LeaderboardData leaderboardData)
    {
        
        SendLeaderboardDataClientRPC(leaderboardData);
    }
    [ClientRpc]
    public void SendLeaderboardDataClientRPC(LeaderboardData leaderboardData)
    {
        GameStateManager.Singleton.gameStateSwitcher.RecieveLeaderboardData(leaderboardData);
    }

    public void ResetPlayerServerside()
    {
        ResetPlayerClientRPC();
    }

    [ClientRpc]
    public void ResetPlayerClientRPC()
    {
        ResetPlayerLocally();

    }

    /// <summary>
    /// Resets them to the beginning of the map, and wipes their distance and speed stats
    /// </summary>
    public void ResetPlayerLocally()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            GameStateManager.Singleton.ServerForceChangeGameState(GameState.waitingToReadyUp);

            myStatsTracker.ResetStats();
            _isDead.Value = false;
            _isRespawning.Value = false;
            _isSpectating.Value = false;

            //ServerTeleportPlayer(myPlayerStateController.bodySpawnPosition);
        }

        if (IsOwner)
        {
            myPlayerStateController.TeleportPlayer(myPlayerStateController.bodySpawnPosition);

            if (_playerReadyUpState == ReadyUpState.ready)
            {
                ReadyUpStateChange();
            }
        }
    }

    #endregion

    /// <summary>
    /// Called by the player who joins, they modify the static list that persists across all playerNetworking instances locally.
    /// If the static instance doesn't exist, it is created
    /// </summary>
    private void PlayerConnect()
    {
        if (ConnectedPlayers == null || (IsOwner && IsHost))
        {
            ConnectedPlayers = new Dictionary<ulong, GameObject>();
            ConnectedPlayerNetworkingScripts = new Dictionary<ulong, PlayerNetworking>();
        }

        if (ConnectedPlayers.ContainsKey(OwnerClientId))
        {
            Debug.LogWarning("I am player " + OwnerClientId.ToString() + " but there is already such a player in the ConnectedPlayers dictionary!");
        }

        ConnectedPlayers[OwnerClientId] = gameObject;
        ConnectedPlayerNetworkingScripts[OwnerClientId] = gameObject.GetComponent<PlayerNetworking>();
        Debug.Log("Player " + OwnerClientId.ToString() + " joined. There are now " + ConnectedPlayerNetworkingScripts.Keys.Count.ToString() + " players.");
    }

    /// <summary>
    /// Called by the player who disconnects, they modify the static list that persists across all playerNetworking instances locally.
    /// </summary>
    private void PlayerDisconnect()
    {
        ConnectedPlayers.Remove(OwnerClientId);
        ConnectedPlayerNetworkingScripts.Remove(OwnerClientId);
        Debug.Log("Player " + OwnerClientId.ToString() + " left. There are now " + ConnectedPlayerNetworkingScripts.Keys.Count.ToString() + " players.");

        if (IsOwner)
        {
            // OR: if I am disconnected then send my player to main menu
            IngameEscMenu.Singleton.LoadMainMenu();
        }
    }

    public override void OnNetworkSpawn()
    {
        PlayerConnect();

        myGrappleGun = grappleGun.GetComponent<GrappleGun>();
        myGrappleGun.isGrappleOwner = IsOwner;

        myPlayerController = GetComponentInChildren<PlayerController>();
        myPlayerController.NetworkInitialize(IsOwner);

        // OR: not sure it matters if this is always on?
        MultiplayerCollider.enabled = true; //!IsOwner;

        if (NetworkManager.Singleton.IsHost)
            myStatsTracker = new StatsTracker(this, myPlayerStateController, bodyRigidbody);

        // Check if this object has been spawned as an OTHER player (aka it's not controlled by the current client)
        if (!IsOwner)
        {
            //bodyRigidbody.isKinematic = true;
            //feetRigidbody.isKinematic = true;
            
            // Remove camera
            Destroy(myCamera.GetComponent<CameraController>());
            Destroy(myCamera.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>());
            Destroy(myCamera.GetComponent<AudioListener>());
            Destroy(myCamera.GetComponent<Camera>());

            // Make the body grapplable...
            bodyRigidbody.gameObject.layer = 6;

            // enable the multiplayer 3rd person representation of this prefab

            myPlayerStateController.ShowMultiplayerRepresentation();

            bodyRigidbody.gameObject.GetComponent<MeshRenderer>().enabled = false;
            feetRigidbody.gameObject.GetComponent<MeshRenderer>().enabled = false;

            gameObject.name = displayName.Value.ToSafeString() + " (Remote)";
        }
        else
        {
            GameStateManager.Singleton.localPlayer = this;
            localPlayer = this;

            // Activate the game state manager initialization:
            // Sadly has to be done here as the GameStateManager OnNetworkSpawn is unreliable
            GameStateManager.Singleton.OnLocalPlayerNetworkSpawn();

            // Update who in the game is currently spectators or not
            //GetListOfSpectatorsServerRPC();

            myPlayerStateController.HideMultiplayerRepresentation();

            displayName.Value = GetMyDisplayName();

            gameObject.name = displayName.Value.ToSafeString() + " (Local)";
        }
    }

    public FixedString64Bytes GetMyDisplayName()
    {
        // If using steam, this is where we'd connect to the steam API
        string name = "Player" + OwnerClientId.ToString();
        FixedString64Bytes bytesName = new FixedString64Bytes(name);
        return bytesName;
    }

    /// <summary>
    /// A helper function to debug any issues with the ConnectedPlayers list/dict.
    /// </summary>
    public static void PrintPlayerList()
    {
        string outStr = "";
        foreach (ulong playerID in ConnectedPlayers.Keys)
        {
            if(ConnectedPlayers[playerID])
                outStr += playerID.ToString() + "=" + ConnectedPlayers[playerID].name + ", ";
            else
                outStr += playerID.ToString() + "=NULL, ";
        }
        Debug.Log(outStr);
    }

    /// <summary>
    /// Called whenever the object is destroyed, such as when a remote client disconnects, their prefab is destroyed.
    /// </summary>
    public override void OnDestroy()
    {
        // OR: not a huge fan of disconnect being handled here
        // player disconnect should probs be done in a seperate script 
        PlayerDisconnect();

        base.OnDestroy();
    }

    /// <summary>
    /// The continuous data that is synced across the network. A time key along with a state is syncronized
    /// </summary>
    struct PlayerNetworkData : INetworkSerializable
    {
        private float _sendTime;
        private PositionData _posData;
        
        internal float SendTime
        {
            get => _sendTime;
            set { _sendTime = value; }
        }
        
        internal PositionData PositionData
        {
            get => _posData;
            set { _posData = value; }
        }

        /// <summary>
        /// Throw all values into serializer so they can be sent around the network
        /// </summary>
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _sendTime);
            _posData.NetworkSerialize(serializer);
        }
    }
}

/// <summary>
/// Represents a single tick of player state information, to be synced in PlayerNetworkData across the network
/// </summary>
struct PositionData : INetworkSerializable
{
    /// <summary>
    /// The position of the body
    /// </summary>
    private float _x, _y, _z;

    /// <summary>
    /// Use shorts to save on network bandwidth
    /// </summary>
    private short _xVel, _yVel, _zVel;
    private short _xRot, _yRot, _zRot;

    /// <summary>
    /// Gets/Sets position
    /// </summary>
    internal Vector3 Position
    {
        get => new Vector3(_x, _y, _z);
        set
        {
            _x = value.x;
            _y = value.y;
            _z = value.z;
        }
    }

    /// <summary>
    /// Also upload velocity to improve interpolation accuracy
    /// </summary>
    internal Vector3 Velocity
    {
        get => new Vector3(_xVel, _yVel, _zVel);
        set
        {
            _xVel = (short)value.x;
            _yVel = (short)value.y;
            _zVel = (short)value.z;
        }
    }

    /// <summary>
    /// Gets/Sets rotation
    /// </summary>
    internal Vector3 Rotation
    {
        get => new Vector3(_xRot, _yRot, _zRot);
        set
        {
            _xRot = (short)value.x;
            _yRot = (short)value.y;
            _zRot = (short)value.z;
        }
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref _x);
        serializer.SerializeValue(ref _y);
        serializer.SerializeValue(ref _z);
        serializer.SerializeValue(ref _xVel);
        serializer.SerializeValue(ref _yVel);
        serializer.SerializeValue(ref _zVel);
        serializer.SerializeValue(ref _xRot);
        serializer.SerializeValue(ref _yRot);
        serializer.SerializeValue(ref _zRot);
    }
}
