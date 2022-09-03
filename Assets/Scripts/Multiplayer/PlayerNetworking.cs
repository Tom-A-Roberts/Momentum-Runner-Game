using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class PlayerNetworking : NetworkBehaviour
{
    private NetworkVariable<PlayerNetworkData> _netState = new NetworkVariable<PlayerNetworkData>(writePerm: NetworkVariableWritePermission.Owner);

    private Vector3 _vel;
    private float _rotVelX;
    private float _rotVelY;
    private float _rotVelZ;

    public float wallRunTiltMultiplier = 1.2f;
    public LevelController myLevelController;
    public GameObject myCamera;
    public GameObject grappleGun;
    public GameObject shootingGun;
    public Rigidbody bodyRigidbody;
    public Rigidbody feetRigidbody;
    public Transform multiplayerRepresentation;
    //public AudioSource myAudioSource;

    /// <summary>
    /// How smoothed out a multiplayer player's movement should be. Higher = smoother
    /// </summary>
    [SerializeField] private float _cheapInterpolationTime = 0.05f;

    // Start is called before the first frame update
    void Start()
    {
        
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
                Mathf.SmoothDampAngle(myCamera.transform.rotation.eulerAngles.z, _netState.Value.Rotation.z * wallRunTiltMultiplier, ref _rotVelZ, _cheapInterpolationTime));  
            bodyRigidbody.rotation = Quaternion.Euler(0, myCamera.transform.eulerAngles.y, myCamera.transform.eulerAngles.z);           

            // Keep velocities in sync (Might be a bad idea!)
            bodyRigidbody.velocity = _netState.Value.Velocity;
            feetRigidbody.velocity = _netState.Value.Velocity;
        }
    }
    //[ServerRpc]
    //public void ShootServerRPC()
    //{
    //    ShootClientRPC();
    //}
    //public void ShootStart()
    //{
    //    ShootServerRPC();

    //    LocalShoot();
        
    //}
    //[ClientRpc]
    //public void ShootClientRPC()
    //{
    //    if (!IsOwner)
    //    {

    //        // Do shooting
    //    }
    //}
    //public void LocalShoot()
    //{

    //}


    public override void OnNetworkSpawn()
    {
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

            grappleGun.GetComponent<GrappleGun>().isGrappleOwner = false;
            //myAudioSource.Stop();

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
