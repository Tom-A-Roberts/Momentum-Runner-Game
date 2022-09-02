using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerNetworkTEMP : NetworkBehaviour
{
    private NetworkVariable<PlayerNetworkData> _netState = new NetworkVariable<PlayerNetworkData>(writePerm: NetworkVariableWritePermission.Owner);

    private Rigidbody myRigidbody;
    private Vector3 _vel;
    private float _rotVel;

    [SerializeField] private float _cheapInterpolationTime = 0.1f;

    // Start is called before the first frame update
    void Start()
    {
        myRigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (IsOwner)
        {
            _netState.Value = new PlayerNetworkData()
            {
                Position = transform.position,
                Rotation = transform.rotation.eulerAngles,
                Velocity = myRigidbody.velocity,
            };
        }
        else
        {
            transform.position = Vector3.SmoothDamp(transform.position, _netState.Value.Position, ref _vel, _cheapInterpolationTime);
            transform.rotation = Quaternion.Euler(
                0,
                Mathf.SmoothDampAngle(transform.rotation.eulerAngles.y, _netState.Value.Rotation.y, ref _rotVel, _cheapInterpolationTime),
                0);
            myRigidbody.velocity = _netState.Value.Velocity;
        }
    }


    struct PlayerNetworkData : INetworkSerializable
    {
        private float _x, _y, _z;
        /// <summary>
        /// Use shorts to save on network bandwidth
        /// </summary>
        private short _xVel, _yVel, _zVel;
        private short _yRot;

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
            get => new Vector3(0, _yRot, 0);
            set => _yRot = (short)value.y;
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
            serializer.SerializeValue(ref _yRot);
        }
    }

   

}
