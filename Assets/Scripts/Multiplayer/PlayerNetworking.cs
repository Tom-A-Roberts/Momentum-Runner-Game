using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Rendering;
using System;
using System.Linq;
using System.Xml;
using Unity.Collections;
using static GameStateManager;
using Unity.VisualScripting;

public class PlayerNetworking : NetworkBehaviour
{
    /// <summary>
    /// All continuously updated player state data, specifically for this one player
    /// </summary>
    private NetworkVariable<PlayerNetworkData> _netState = new NetworkVariable<PlayerNetworkData>(writePerm: NetworkVariableWritePermission.Owner);

    private NetworkVariable<FixedString64Bytes> displayName = new NetworkVariable<FixedString64Bytes>(writePerm: NetworkVariableWritePermission.Owner);

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
    /// A maintained list of all player ID's in the game (the keys), along with their associated prefabs (the values).
    /// </summary>
    public static Dictionary<ulong, GameObject> ConnectedPlayers;

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
            // who do we think we hit?
            GameObject hitObj = LocalShoot(shootStartPosition, shootDirection, 0f);
            int hitID = CheckForPlayerHit(hitObj);

            if (hitID == -1)
            {
                Debug.Log("I missed");
            }            

            float shootTime = NetworkManager.Singleton.LocalTime.TimeAsFloat;            
            ShootServerRPC(shootTime, shootStartPosition, shootDirection, hitID);
        }
    }

    [ServerRpc]
    public void ShootServerRPC(float shootTime, Vector3 shootStartPosition, Vector3 shootDirection, int clientHitID)
    {
        int serverHitID = clientHitID;

        if (!IsOwnedByServer)
        {
            ulong rtt = NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(OwnerClientId);            
            float timeToCheck = shootTime - rtt / 1000f;

            // should probably rollback all players 
            GameObject playerIThinkIHit;
            if (clientHitID != -1)
            {
                if (ConnectedPlayers.TryGetValue((ulong)clientHitID, out playerIThinkIHit))
                {
                    PlayerNetworking playerNetworkingToRollback;
                    if (playerIThinkIHit.TryGetComponent(out playerNetworkingToRollback))
                    {
                        PositionData posData = playerNetworkingToRollback.GetPositionDataAtTime(timeToCheck, _cheapInterpolationTime);

                        Vector3 originalColliderPosition = playerNetworkingToRollback.MultiplayerCollider.transform.localPosition;

                        playerNetworkingToRollback.MultiplayerCollider.transform.position = posData.Position;

                        float timeToWait = shootTime - NetworkManager.ServerTime.TimeAsFloat;
                        GameObject hitObj = LocalShoot(shootStartPosition, shootDirection, timeToWait);
                        serverHitID = CheckForPlayerHit(hitObj);

                        playerNetworkingToRollback.MultiplayerCollider.transform.localPosition = originalColliderPosition;

                        if (clientHitID == serverHitID)
                            Debug.Log("No discrepancy found - Hit registered");
                        else
                            Debug.LogWarning("Discrepancy found - Hit missed");
                    }
                }
                else
                {
                    Debug.LogWarning("Trying to shoot at a player who does not exist! PlayerID: " + clientHitID);
                }
            }
            else
            {
                float timeToWait = shootTime - NetworkManager.ServerTime.TimeAsFloat;
                LocalShoot(shootStartPosition, shootDirection, timeToWait);
            }
        }

        ShootClientRPC(shootTime, shootStartPosition, shootDirection, serverHitID);
    }

    [ClientRpc]
    public void ShootClientRPC(float shootTime, Vector3 shootStartPosition, Vector3 shootDirection, int playerHitID)
    {
        if (!IsOwner)
        {
            float timeToWait = shootTime - NetworkManager.ServerTime.TimeAsFloat;

            LocalShoot(shootStartPosition, shootDirection, timeToWait);
        }

        myPlayerStateController.ProcessPotentialHit(playerHitID);
    }

    public GameObject LocalShoot(Vector3 shootStartPosition, Vector3 shootDirection, float timeToWait)
    {
        GameObject hitObj = myGunController.TryShoot(shootStartPosition, shootDirection);

        // OR: fire instantly if client shooting
        if (timeToWait > 0)
        {
            StartCoroutine(SyncedShoot(shootStartPosition, shootDirection, timeToWait));
        }
        else
        {
            myGunController.DoShoot(shootStartPosition, shootDirection);
        }

        return hitObj;
    }

    // TODO: sync shooting result too!
    IEnumerator SyncedShoot(Vector3 shootStartPosition, Vector3 shootDirection, float timeToWait)
    {
        if (timeToWait > 0)        
            yield return new WaitForSeconds(timeToWait);

        myGunController.DoShoot(shootStartPosition, shootDirection);
    }

    /// <summary>
    /// </summary>
    /// <param name="rayHitObject">The gameobject that was hit by the shoot raycast</param>
    /// <returns>IF the ray hit a network player, returns OwnerClientID. ELSE, return -1</returns>
    public int CheckForPlayerHit(GameObject rayHitObject)
    {
        int playerHitID = -1;
        if (rayHitObject != null)
        {
            Transform prefabParent = rayHitObject.transform.root;
            if (prefabParent != null)
            {
                PlayerNetworking shotPlayerNetworkingScript;
                if (prefabParent.TryGetComponent<PlayerNetworking>(out shotPlayerNetworkingScript))
                {
                    playerHitID = (int)shotPlayerNetworkingScript.OwnerClientId;

                }
            }
        }
        return playerHitID;
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
        EnterSpectatorModeClientRPC();
    }
    [ClientRpc]
    public void EnterSpectatorModeClientRPC()
    {
        // TR: this collider line should be moved to inside the EnterSpectatorMode function. The EnterSpectatorMode function is called
        // from other places too.
        MultiplayerCollider.enabled = false;
        myPlayerStateController.EnterSpectatorMode();
    }
    [ClientRpc]
    public void LeaveSpectatorModeClientRPC()
    {
        // TR: this collider line should be moved to inside the LeaveSpectatorMode function. The LeaveSpectatorMode function is called
        // from other places too.
        MultiplayerCollider.enabled = true;
        myPlayerStateController.LeaveSpectatorMode();
    }

    /// <summary>
    /// Asks the server to produce a list of who are spectators and who aren't
    /// </summary>
    [ServerRpc]
    public void GetListOfSpectatorsServerRPC()
    {
        List<ulong> playerIDs = new List<ulong>(ConnectedPlayers.Count);
        List<bool> spectatorStatus = new List<bool>(ConnectedPlayers.Count);

        foreach (KeyValuePair<ulong, GameObject> entry in ConnectedPlayers)
        {
            PlayerStateManager currentPlayerState;
            if(entry.Value.TryGetComponent(out currentPlayerState))
            {
                playerIDs.Add(currentPlayerState.playerNetworking.OwnerClientId);
                spectatorStatus.Add(currentPlayerState.SpectatorMode);
            }
        }
        ReturnListOfSpectatorsClientRPC(playerIDs.ToArray(), spectatorStatus.ToArray());
    }
    /// <summary>
    /// Sends a list of who's spectators and who isn't to the connected clients, updating their states
    /// </summary>
    [ClientRpc]
    public void ReturnListOfSpectatorsClientRPC(ulong[] playerIDs, bool[] spectatorStatus)
    {
        if (IsOwner && !IsHost)
        {
            Dictionary<ulong, bool> spectatorStatusDict = new Dictionary<ulong, bool>();
            for (int index = 0; index < playerIDs.Length; index++)
            {
                spectatorStatusDict.Add(playerIDs[index], spectatorStatus[index]);
            }

            foreach (KeyValuePair<ulong, GameObject> entry in ConnectedPlayers)
            {
                PlayerStateManager currentPlayerState;
                if (spectatorStatusDict.ContainsKey(entry.Key) && entry.Value.TryGetComponent(out currentPlayerState))
                {
                    // If status is set to spectator = true:
                    if (spectatorStatusDict[entry.Key] == true)
                    {
                        if (!currentPlayerState.SpectatorMode)
                            currentPlayerState.EnterSpectatorMode();
                    }
                    else
                    {
                        if (currentPlayerState.SpectatorMode)
                            currentPlayerState.LeaveSpectatorMode();
                    }
                }
            }
        }
    }

    #endregion

    #region Respawning
    // respawning is completely handled by the server.
    // The server tells clients when they should begin respawning AND end respawning
    public void EnterRespawningModeServer(Vector3 respawnLocation)
    {
        EnterRespawningModeClientRPC(respawnLocation);
    }

    public void LeaveRespawningModeServer()
    {
        LeaveRespawningModeClientRPC();
    }

    [ClientRpc]
    public void EnterRespawningModeClientRPC(Vector3 respawnLocation)
    {
        myPlayerStateController.EnterRespawningMode(respawnLocation);
    }
    [ClientRpc]
    public void LeaveRespawningModeClientRPC()
    {
        myPlayerStateController.LeaveRespawningMode();
    }

    #endregion

    #region ReadyingUp

    /// <summary>
    /// Toggles the current ready up state, sending a request to change state to the server. In the meantime, the waiting effects are applied, such as the button
    /// being half pressed.
    /// </summary>
    public void ReadyUpStateChange()
    {
        if (IsOwner && GameStateManager.Singleton.gameStateSwitcher.GameState == GameState.waitingToReadyUp)
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

    /// <summary>
    /// When a client (the server, really) thinks everyone has readied up, they can prompt the server to do a check. Usually the server
    /// itself does this. If the server agrees everyone is ready, then it calls the client RPC to say everyone is ready. At this point
    /// it cannot be undone
    /// </summary>
    [ServerRpc(RequireOwnership =false)]
    public void EveryoneHasReadiedUpServerRPC()
    {
        if(GameStateManager.Singleton.readiedPlayers.Count == ConnectedPlayers.Count)
        {
            GameStateManager.Singleton.gameStateSwitcher.SwitchToReadiedUp(true);
        }
    }

    #endregion

    #region Death

    public void StartDeath()
    {
        if(IsOwner || NetworkManager.Singleton.IsHost)
        {
            PlayerDeathServerRPC();
            myPlayerStateController.PlayerDeath();
        }
    }

    [ServerRpc]
    public void PlayerDeathServerRPC()
    {
        PlayerDeathClientRPC();
        
        GameStateManager.Singleton.TestForWinState();
    }
    [ClientRpc]
    public void PlayerDeathClientRPC()
    {
        if(!(IsOwner || NetworkManager.Singleton.IsHost))
        {
            myPlayerStateController.PlayerDeath();
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
        }

        if (ConnectedPlayers.ContainsKey(OwnerClientId))
        {
            Debug.LogWarning("I am player " + OwnerClientId.ToString() + " but there is already such a player in the ConnectedPlayers dictionary!");
        }

        ConnectedPlayers[OwnerClientId] = gameObject;
        Debug.Log("Player " + OwnerClientId.ToString() + " joined. There are now " + ConnectedPlayers.Keys.Count.ToString() + " players.");
    }

    /// <summary>
    /// Called by the player who disconnects, they modify the static list that persists across all playerNetworking instances locally.
    /// </summary>
    private void PlayerDisconnect()
    {
        ConnectedPlayers.Remove(OwnerClientId);
        Debug.Log("Player " + OwnerClientId.ToString() + " left. There are now " + ConnectedPlayers.Keys.Count.ToString() + " players.");

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

            // Activate the game state manager initialization:
            // Sadly has to be done here as the GameStateManager OnNetworkSpawn is unreliable
            GameStateManager.Singleton.OnLocalPlayerNetworkSpawn();

            // Update who in the game is currently spectators or not
            GetListOfSpectatorsServerRPC();

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
