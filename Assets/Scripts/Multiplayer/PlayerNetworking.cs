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

    public LevelLogicManager myLevelController;
    public GameObject myCamera;
    public GameObject grappleGun;
    public GameObject shootingGun;
    public Rigidbody bodyRigidbody;
    public Rigidbody feetRigidbody;
    public Transform multiplayerRepresentation;

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
        if (IsOwner)
        {
            _netState.Value = new PlayerNetworkData()
            {
                Position = bodyRigidbody.position,
                // OR: camera rotation used because locally it has the wallrunning rotation applied
                // ...definitely not a janky way of animating this differently locally and remotely
                Rotation = myCamera.transform.rotation.eulerAngles,
                Velocity = bodyRigidbody.velocity,
            };
        }
        else
        {
            // Some suuuper basic interpolation:

            bodyRigidbody.position = Vector3.SmoothDamp(bodyRigidbody.position, _netState.Value.Position, ref _vel, _cheapInterpolationTime);
            feetRigidbody.position = bodyRigidbody.position - myLevelController.bodyFeetOffset;

            myCamera.transform.rotation = Quaternion.Euler(Mathf.SmoothDampAngle(myCamera.transform.rotation.eulerAngles.x, _netState.Value.Rotation.x, ref _rotVelX, _cheapInterpolationTime),
                Mathf.SmoothDampAngle(bodyRigidbody.rotation.eulerAngles.y, _netState.Value.Rotation.y, ref _rotVelY, _cheapInterpolationTime),
                Mathf.SmoothDampAngle(myCamera.transform.rotation.eulerAngles.z, _netState.Value.Rotation.z, ref _rotVelZ, _cheapInterpolationTime));  
            bodyRigidbody.rotation = Quaternion.Euler(0, myCamera.transform.eulerAngles.y, myCamera.transform.eulerAngles.z);           

            // Keep velocities in sync (Might be a bad idea!)
            bodyRigidbody.velocity = _netState.Value.Velocity;
            feetRigidbody.velocity = _netState.Value.Velocity;
        }
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
            ShootServerRPC(shootStartPosition, shootDirection);

            if (!IsServer)
                LocalShoot(shootStartPosition, shootDirection);
        }
    }

    [ServerRpc]
    public void ShootServerRPC(Vector3 shootStartPosition, Vector3 shootDirection)
    {
        GameObject hitObj = LocalShoot(shootStartPosition, shootDirection);
        int hitID = CheckForShotHit(hitObj);

        ShootClientRPC(shootStartPosition, shootDirection, hitID);
    }

    [ClientRpc]
    public void ShootClientRPC(Vector3 shootStartPosition, Vector3 shootDirection, int playerHitID)
    {
        if (!IsOwner)
        {
            LocalShoot(shootStartPosition, shootDirection);
        }
        myLevelController.ProcessPotentialHit(playerHitID);
    }

    public GameObject LocalShoot(Vector3 shootStartPosition, Vector3 shootDirection)
    {
        GameObject hitObj = myGunController.TryShoot(shootStartPosition, shootDirection);
        return hitObj;
    }

    /// <summary>
    /// </summary>
    /// <param name="rayHitObject">The gameobject that was hit by the shoot raycast</param>
    /// <returns>IF the ray hit a network player, returns OwnerClientID. ELSE, return -1</returns>
    public int CheckForShotHit(GameObject rayHitObject)
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

        Debug.Log(OwnerClientId);

        ConnectedPlayers[OwnerClientId] = gameObject;
        Debug.Log("Player " + OwnerClientId.ToString() + " joined. There are now " + ConnectedPlayers.Keys.Count.ToString() + " players.");
    }

    private void PlayerDisconnect()
    {
        ConnectedPlayers.Remove(OwnerClientId);
        Debug.Log("Player " + playerID.ToString() + " left. There are now " + ConnectedPlayers.Keys.Count.ToString() + " players.");

        if (IsOwner)
        {
            // OR: if I am disconnected then send my player to main menu
            IngameEscMenu.Instance.LoadMainMenu();
        }
    }

    public override void OnNetworkSpawn()
    {
        PlayerConnect();

        myGrappleGun = grappleGun.GetComponent<GrappleGun>();
        myGrappleGun.isGrappleOwner = IsOwner;

        myPlayerController = GetComponentInChildren<PlayerController>();
        myPlayerController.NetworkInitialize(IsOwner);

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
            for (int childID = 0; childID < multiplayerRepresentation.childCount; childID++)
            {
                multiplayerRepresentation.GetChild(childID).GetComponent<MeshRenderer>().enabled = true;
            }
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

        /// <summary>
        /// Throw all values into serializer so they can be sent around the network
        /// </summary>
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
}
