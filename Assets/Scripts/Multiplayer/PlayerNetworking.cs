using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class PlayerNetworking : NetworkBehaviour
{
    private NetworkVariable<PlayerNetworkData> _netState = new NetworkVariable<PlayerNetworkData>(writePerm: NetworkVariableWritePermission.Owner);

    /// <summary>
    /// A maintained list of all player ID's in the game (the keys), along with their associated prefabs (the values).
    /// </summary>
    public static Dictionary<ulong, GameObject> ConnectedPlayers;

    private Vector3 _vel;
    private float _rotVelX;
    private float _rotVelY;
    private float _rotVelZ;

    public PlayerStateManager myPlayerStateController;
    public GameObject myCamera;
    public GameObject grappleGun;
    public GameObject shootingGun;
    public Rigidbody bodyRigidbody;
    public Rigidbody feetRigidbody;
    public Collider MultiplayerCollider;

    private GunController myGunController;
    private GrappleGun myGrappleGun;
    private PlayerController myPlayerController;
    //public AudioSource myAudioSource;

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

    private List<KeyValuePair<float, PositionData>> positionHistory = new List<KeyValuePair<float, PositionData>>();

    private void HandlePositionData(float serverTime)
    {
        PlayerNetworkData dataReceived = _netState.Value;
        float time = dataReceived.SendTime;

        PositionData positionData = dataReceived.PositionData;

        positionHistory.Add(new KeyValuePair<float, PositionData>(time, positionData));

        if (positionHistory.Count > 0)
            InterpolatePosition(serverTime);
    }

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

    //public void LeverFlicked()
    //{
    //    LevelFlickRequestServerRPC();
    //    AnimateLeverClientRPC();
    //}

    //[ServerRpc]
    //public void LevelFlickRequestServerRPC()
    //{
    //    // Check the player position
    //    AnimateLeverClientRPC();
    //}

    //[ClientRpc]
    //public void AnimateLeverClientRPC()
    //{
    //    if(!IsOwner)
    //    {
    //        // Makes new route appear to user
    //    }
    //}

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
                        playerNetworkingToRollback.MultiplayerCollider.transform.position = posData.Position;
                    }
                }
                else
                {
                    Debug.LogWarning("Trying to shoot at a player who does not exist! PlayerID: " + clientHitID);
                }
            }

            float timeToWait = shootTime - NetworkManager.ServerTime.TimeAsFloat;
            GameObject hitObj = LocalShoot(shootStartPosition, shootDirection, timeToWait);
            serverHitID = CheckForPlayerHit(hitObj);

            if (clientHitID == serverHitID)            
                Debug.Log("No discrepancy found - Hit registered");            
            else
                Debug.LogWarning("Discrepancy found - Hit missed");
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
        MultiplayerCollider.enabled = false;
        myPlayerStateController.EnterSpectatorMode();
    }
    [ClientRpc]
    public void LeaveSpectatorModeClientRPC()
    {
        MultiplayerCollider.enabled = true;
        myPlayerStateController.LeaveSpectatorMode();
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

            gameObject.name = "Player" + OwnerClientId.ToString() + " (Remote)";
        }
        else
        {
            //if (Camera.main != myCamera)
            //{
            //    GameObject spawnCam = Camera.main.gameObject;
            //    Destroy(spawnCam);
            //}    

            myPlayerStateController.HideMultiplayerRepresentation();

            gameObject.name = "Player" + OwnerClientId.ToString() + " (Local)";
        }
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

    public override void OnDestroy()
    {
        // OR: not a huge fan of disconnect being handled here
        // player disconnect should probs be done in a seperate script 
        PlayerDisconnect();

        base.OnDestroy();
    }

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

struct PositionData : INetworkSerializable
{
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
