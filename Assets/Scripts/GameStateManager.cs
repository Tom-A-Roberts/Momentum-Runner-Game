using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using UnityEngine.Playables;
using UnityEngine.UI;

/// <summary>
/// This class handles the game state such as:
/// <para>- Player ready state</para>
/// <para>- Walls position and speed</para>
/// <para>- Players winning and losing</para>
/// </summary>
public class GameStateManager : NetworkBehaviour
{
    /// <summary>
    /// The information about the game state that is passed around per tick
    /// </summary>
    private NetworkVariable<GameStateData> _gameState = new NetworkVariable<GameStateData>(writePerm: NetworkVariableWritePermission.Server);

    public static GameStateManager Singleton { get; private set; }

    [Tooltip("Probably called 'PointsList', should be an object containing all the points that the walls will follow.")]
    public GameObject railwayPointsList;
    [Tooltip("The game object to treat as the death wall that chases the players")]
    public GameObject deathWall;
    [Tooltip("The game object to treat as the fog wall that leads the players")]
    public GameObject fogWall;

    [Tooltip("The speed at which the playable zone gets smaller")]
    public float closingSpeed = 0;
    [Tooltip("The zone progress at the start of the game, measured in laps as the unit.")]
    public float zoneProgress = 0;
    [Tooltip("The speed of the zone at the start of the game, measured in meters per second")]
    public float zoneSpeed = 1;
    [Tooltip("How wide the zone starts in meters")]
    public float zoneStartWidth = 350;

    [Header("Effects settings")]
    public Image ScreenRedEdges;

    /// <summary>
    /// The distance in meters between the fog wall and the death wall
    /// </summary>
    [System.NonSerialized]
    public float zoneWidth;

    [System.NonSerialized]
    public PlayerStateManager localPlayer;
    
    // These three variables get set during populateRailwayPoints(). They store information about
    // how the zone should move around the map
    private float railwayLength = 0;
    private Vector3[] railwayPoints;
    private Quaternion[] railwayDirections;

    private BoxCollider deathWallCollider;

    private void Awake()
    {
        if (Singleton != null && Singleton != this)
            Destroy(Singleton);
        Singleton = this;
    }

    void Start()
    {
        
        if (railwayPointsList == null)
        {
            throw new System.Exception("\"railwayPointsList is set to null.\\n Therefore no wall points found to use when moving the deathwall and fogwall!\"");
        }
        populateRailwayPoints();

        if (deathWall)
        {
            deathWallCollider = deathWall.GetComponent<BoxCollider>();

            //SetWallPositionAndRotationToProgress(deathWall.transform, zoneProgress);
        }
        if (fogWall)
        {
            //SetWallPositionAndRotationToProgress(fogWall.transform, zoneProgress);
        }

        
    }


    public override void OnNetworkSpawn()
    {
        zoneWidth = zoneStartWidth;
        // Only need to update this once since it doesn't change throughout the game.
        if (!IsHost)
        {
            GetClosingSpeedServerRPC();

            // Add callback function for when the game state is changed:
            _gameState.OnValueChanged += ChangedGameState;
        }
    }

    #region RPC communications

    /// <summary>
    /// A way that a client can request to know the server's "closingSpeed" variable.
    /// The client requests an update, which the server then broadcasts out to all clients, who then
    /// update their variables.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void GetClosingSpeedServerRPC()
    {
        SetClosingSpeedClientRPC(closingSpeed);
    }
    /// <summary>
    /// Updates all clients with a new closing speed, as determined only by the server
    /// </summary>
    /// <param name="newClosingSpeed">Closing speed to send to clients</param>
    [ClientRpc]
    public void SetClosingSpeedClientRPC(float newClosingSpeed)
    {
        closingSpeed = newClosingSpeed;
    }

    #endregion

    /// <summary>
    /// Update the game state data if host
    /// Then simply move the walls along as expected according to what the game state data says
    /// </summary>
    void Update()
    {
        if (IsHost)
        {
            // Modify variables here according to the progress of the game
            //zoneSpeed = fastestPlayer.Speed * (1 - playerEfficiencyRequirement);
            //zoneWidth -= 1 * Time.deltaTime;

            GameStateData stateData = new GameStateData()
            {
                ZoneProgress = zoneProgress,
                ZoneWidth = zoneWidth,
                ZoneSpeed = zoneSpeed
            };
            _gameState.Value = stateData;
        }
        UpdateWallPositions();

        if (localPlayer && deathWall && deathWallCollider)
        {
            localPlayer.UpdateDeathWallEffects(deathWall.transform, deathWallCollider);
        }
    }

    /// <summary>
    /// Called whenever the 'non host' recieves new information from the host.
    /// </summary>
    private void ChangedGameState(GameStateData oldGameState, GameStateData newGameState)
    {
        zoneProgress = newGameState.ZoneProgress;
        zoneWidth = newGameState.ZoneWidth;
        zoneSpeed = newGameState.ZoneSpeed;
    }

    #region Wall Helper functions

    /// <summary>
    /// Moves zoneProgress forward according to zoneSpeed.
    /// Updates the physical positions of the walls according to zoneProgress
    /// </summary>
    void UpdateWallPositions()
    {
        float metersToTravel = zoneSpeed * Time.deltaTime;
        float widthInLapsUnits = convertMetersToLapsUnits(zoneWidth);

        zoneProgress += convertMetersToLapsUnits(metersToTravel);


        if (deathWall)
        {
            Quaternion deathwallRotationOffset = Quaternion.Euler(90, 0, 0);
            SetWallPositionAndRotationToProgress(deathWall.transform, zoneProgress - (widthInLapsUnits / 2), deathwallRotationOffset);
        }

        if (fogWall)
        {
            Quaternion fogwallRotationOffset = Quaternion.Euler(0, 0, 0);
            SetWallPositionAndRotationToProgress(fogWall.transform, zoneProgress + (widthInLapsUnits / 2), fogwallRotationOffset);
        }
    }

    /// <summary>
    /// Using the progress value measured in laps, set the positions and rotations of the
    /// fog wall and death wall (if they exist) using interpolation.
    /// </summary>
    /// <param name="progress">progress value measured in laps</param>
    void SetWallPositionAndRotationToProgress(Transform wall, float progress, Quaternion rotationOffset)
    {
        float progressSimplified = convertProgressToBetween01(progress);

        //Quaternion deathwallRotationOffset = Quaternion.Euler(90, 0, 0);

        float interpProgress = progressSimplified * railwayPoints.Length;
        // Calculate which points to interpolate between
        int lowestPoint = Mathf.FloorToInt(interpProgress);
        int highestPoint = Mathf.CeilToInt(interpProgress);
        // Wrap around to 0
        if (highestPoint == railwayPoints.Length)
            highestPoint = 0;
        // Calculate the interpolation amount:
        float interpAmount = interpProgress - Mathf.Floor(interpProgress);

        wall.position = Vector3.Lerp(railwayPoints[lowestPoint], railwayPoints[highestPoint], interpAmount);

        Quaternion targetQuaternion = Quaternion.Lerp(railwayDirections[lowestPoint], railwayDirections[highestPoint], interpAmount);

        wall.rotation = targetQuaternion * rotationOffset;
        
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="progress"></param>
    /// <returns></returns>
    public static float convertProgressToBetween01(float progress)
    {
        float progressSimplified = progress - Mathf.Floor(progress);
        //if (progress < 0)
        //    progressSimplified = 1 - progressSimplified;
        progressSimplified = 1 - progressSimplified;
        if (progressSimplified == 1)
            progressSimplified = 0;
        return progressSimplified;
    }

    public float convertMetersToLapsUnits(float meters)
    {
        return meters / railwayLength;
    }

    /// <summary>
    /// The walls run along rails, which is called the railway.
    /// </summary>
    private void populateRailwayPoints()
    {
        List<Vector3> pointsList = new List<Vector3>();
        List<Quaternion> directionsList = new List<Quaternion>();
        float distAccumulator = 0;
        Vector3 previousPos = Vector3.zero;
        for (int pointID = 0; pointID < railwayPointsList.transform.childCount; pointID++)
        {
            Transform currentChild = railwayPointsList.transform.GetChild(pointID);
            pointsList.Add(currentChild.position);
            directionsList.Add(currentChild.rotation);
            if (pointID > 0)
            {
                distAccumulator += Vector3.Distance(previousPos, currentChild.position);
            }
            previousPos = currentChild.position;
        }
        railwayPoints = pointsList.ToArray();
        railwayDirections = directionsList.ToArray();
        railwayLength = distAccumulator;
    }

    #endregion
}

/// <summary>
/// Holds all continuous game state datas
/// </summary>
struct GameStateData : INetworkSerializable
{
    private short _zoneSpeed;
    private float _zoneProgress, _zoneWidth;

    internal float ZoneProgress
    {
        get => _zoneProgress;
        set => _zoneProgress = value;
    }
    internal float ZoneWidth
    {
        get => _zoneWidth;
        set => _zoneWidth = value;
    }
    internal float ZoneSpeed
    {
        get => _zoneSpeed;
        set => _zoneSpeed = (short)value;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref _zoneSpeed);
        serializer.SerializeValue(ref _zoneWidth);
        serializer.SerializeValue(ref _zoneProgress);
    }
}
