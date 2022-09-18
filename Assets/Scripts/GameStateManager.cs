using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using UnityEngine.Playables;
using UnityEngine.UI;
using TMPro;
using static PlayerNetworking;
using UnityEngine.Rendering.PostProcessing;

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
    private NetworkVariable<ZoneStateData> _zoneState = new NetworkVariable<ZoneStateData>(writePerm: NetworkVariableWritePermission.Server);
    private NetworkVariable<GameState> _gameState = new NetworkVariable<GameState>(writePerm: NetworkVariableWritePermission.Server);

    private NetworkVariable<ushort> _networkedClosingSpeed = new NetworkVariable<ushort>(writePerm: NetworkVariableWritePermission.Server);

    public static GameStateManager Singleton { get; private set; }

    [Header("Known Objects")]
    [Tooltip("Probably called 'PointsList', should be an object containing all the points that the walls will follow.")]
    public GameObject railwayPointsList;
    [Tooltip("The game object to treat as the death wall that chases the players")]
    public GameObject deathWall;
    [Tooltip("The game object to treat as the fog wall that leads the players")]
    public GameObject fogWall;

    [Header("Zone settings")]
    [Tooltip("The speed at which the playable zone gets smaller")]
    public float closingSpeed = 0;
    [Tooltip("The zone progress at the start of the game, measured in laps as the unit.")]
    public float zoneProgress = 0;
    [Tooltip("The speed of the zone at the start of the game, measured in meters per second")]
    public float zoneSpeed = 1;
    [Tooltip("How wide the zone starts in meters")]
    public float zoneStartWidth = 350;

    [Header("Gameplay settings")]
    [Tooltip("How many seconds you are floating upwards for. See PlayerController for more settings")]
    public float respawnDuration = 3;

    [Header("Effects settings")]
    public Image ScreenRedEdges;
    public GameObject waitingToReadyUpPanel;
    public GameObject ReadyUpCube;
    public TMP_Text ReadyUpFloatingText;
    public GameObject ReadyUpBarrier;
    public GameObject CountdownPanel;
    public TMP_Text CountdownText;

    [Header("Level Music Settings")]
    public AudioClip waitingToReadyUpSong;
    public AudioClip levelSoundTracks;

    [Header("Dev settings")]
    [Tooltip("When true: disables death, disables ready up sequence, disables music")]
    public bool DeveloperMode = true;

    /// <summary>
    /// The distance in meters between the fog wall and the death wall
    /// </summary>
    public float ZoneWidth => _zoneWidth;
    /// <summary>
    /// The distance in meters between the fog wall and the death wall
    /// </summary>
    private float _zoneWidth;

    /// <summary>
    /// The local player who is playing this game (IsOwner = true).
    /// <para>This is NOT necessarily the host</para>
    /// </summary>
    [System.NonSerialized]
    public PlayerNetworking localPlayer;

    /// <summary>
    /// Holds the deathwall collider, used by gameobjects to find out how close they are to it
    /// </summary>
    [System.NonSerialized]
    public BoxCollider deathWallCollider;

    /// <summary>
    /// This class can be told to switch between any game state and it will handle it for you
    /// </summary>
    [System.NonSerialized]
    public GameStateSwitcher gameStateSwitcher;

    /// <summary>
    /// A maintained set that contains the ID's of all players that are readied up.
    /// Only reliably maintained during the waiting to ready up gamestate
    /// </summary>
    public HashSet<ulong> readiedPlayers;

    public GameState GameState => _gameState.Value;

    // These three variables get set during populateRailwayPoints(). They store information about
    // how the zone should move around the map
    private float railwayLength = 0;
    private Vector3[] railwayPoints;
    private Quaternion[] railwayDirections;

    #region Startup Functions

    private void Awake()
    {
        if (Singleton != null && Singleton != this)
            Destroy(Singleton);
        Singleton = this;
    }

    void Start()
    {
        _zoneWidth = zoneStartWidth;
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

    /// <summary>
    /// Called on PlayerNetwork OnNetworkSpawn() (specifically, when the IsOwner spawns)
    /// </summary>
    public void OnLocalPlayerNetworkSpawn()
    {
        readiedPlayers = new HashSet<ulong>();

        localPlayer.myPlayerStateController.playerAudioManager.SetupLevelSoundtracks(waitingToReadyUpSong, levelSoundTracks);

        gameStateSwitcher = new GameStateSwitcher(this);

        _zoneWidth = zoneStartWidth;
        // Only need to update this once since it doesn't change throughout the game.
        if (!NetworkManager.Singleton.IsHost)
        {
            closingSpeed = _networkedClosingSpeed.Value;

            // Add callback function for when the game state is changed:
            _zoneState.OnValueChanged += ChangedZoneState;
        }
        else
        {
            if (DeveloperMode)
                _gameState.Value = GameState.playingGame;
            else
                _gameState.Value = GameState.waitingToReadyUp;

            _networkedClosingSpeed.Value = (ushort)closingSpeed;
        }

        if (GameStateManager.Singleton.gameStateSwitcher.GameState != GameState.waitingToReadyUp)
        {
            if (localPlayer.PlayerReadyUpState == ReadyUpState.unready || localPlayer.PlayerReadyUpState == ReadyUpState.waitingToUnready)
            {
                localPlayer.ReadyUpStateChange();
            }
        }
    }
    #endregion


    /// <summary>
    /// Update the game state data if host
    /// Then simply move the walls along as expected according to what the game state data says
    /// </summary>
    void Update()
    {
        if (localPlayer.IsOwnedByServer)
        {
            // Modify variables here according to the progress of the game
            //zoneSpeed = fastestPlayer.Speed * (1 - playerEfficiencyRequirement);
            //zoneWidth -= 1 * Time.deltaTime;

            ZoneStateData stateData = new ZoneStateData()
            {
                ZoneProgress = zoneProgress,
                ZoneWidth = _zoneWidth,
                ZoneSpeed = zoneSpeed
            };
            _zoneState.Value = stateData;
        }
        UpdateWallPositions();

        if (localPlayer && deathWall && deathWallCollider)
        {
            localPlayer.myPlayerStateController.UpdateDeathWallEffects(deathWall.transform, deathWallCollider);
        }

        gameStateSwitcher.Update();
    }

    /// <summary>
    /// Called whenever the 'non host' recieves new information from the host.
    /// </summary>
    private void ChangedZoneState(ZoneStateData oldGameState, ZoneStateData newGameState)
    {
        zoneProgress = newGameState.ZoneProgress;
        _zoneWidth = newGameState.ZoneWidth;
        zoneSpeed = newGameState.ZoneSpeed;
    }


    #region ServerRPCs

    public void HostForceChangeGameState(GameState newGamestate)
    {
        if (NetworkManager.Singleton.IsHost)
            _gameState.Value = newGamestate;
    }

    public void TestForWinState()
    {
        int winCount = 0;
        int loseCount = 0;

        foreach (var keyValuePair in PlayerNetworking.ConnectedPlayers)
        {
            PlayerNetworking playerNetworking = keyValuePair.Value.GetComponent<PlayerNetworking>();

            if (playerNetworking._isDead.Value)
            {
                loseCount += 1;
            }
            else
            {
                winCount += 1;
            }
        }
        if (winCount == 0 && loseCount > 0)
        {
            Debug.LogWarning("Inconsistent State! nobody seems to have won somehow");
        }
        if (winCount <= 1 && loseCount > 0)
        {
            // player has won
            LeaderboardData leaderboardData = GatherWinData();
            
        }
    }


    public LeaderboardData GatherWinData()
    {
        ulong[] _playerIDs = new ulong[PlayerNetworking.ConnectedPlayers.Count];
        ushort[] _distancesTravelled = new ushort[PlayerNetworking.ConnectedPlayers.Count];
        bool[] _playersWon = new bool[PlayerNetworking.ConnectedPlayers.Count];

        int i = 0;
        foreach (var keyValuePair in PlayerNetworking.ConnectedPlayers)
        {
            PlayerNetworking playerNetworking = keyValuePair.Value.GetComponent<PlayerNetworking>();
            _playerIDs[i] = playerNetworking.OwnerClientId;
            _distancesTravelled[i] = (ushort)playerNetworking.bodyRigidbody.position.x;

            if (playerNetworking._isDead.Value)
            {
                _playersWon[i] = false;
            }
            else
            {
                _playersWon[i] = true;
            }
            i += 1;
        }

        return new LeaderboardData()
        {
            playerIDs = _playerIDs,
            distancesTravelled = _distancesTravelled,
            playersWon = _playersWon,
        };
    }

    [ServerRpc]
    public void SendLeaderboardDataServerRPC(LeaderboardData leaderboardData)
    {
        SendLeaderboardDataClientRPC(leaderboardData);
    }
    [ClientRpc]
    public void SendLeaderboardDataClientRPC(LeaderboardData leaderboardData)
    {
        gameStateSwitcher.latestLeaderboardData = leaderboardData;
    }

    #endregion

    #region Wall Helper functions

    /// <summary>
    /// Moves zoneProgress forward according to zoneSpeed.
    /// Updates the physical positions of the walls according to zoneProgress.
    /// </summary>
    void UpdateWallPositions()
    {
        float metersToTravel = 0;

        if (gameStateSwitcher != null)
        {
            if (gameStateSwitcher.GameState == GameState.playingGame)
            {
                metersToTravel = zoneSpeed * Time.deltaTime;
                _zoneWidth -= Time.deltaTime * closingSpeed;
            }
        }

        float widthInLapsUnits = convertMetersToLapsUnits(_zoneWidth);
        zoneProgress += convertMetersToLapsUnits(metersToTravel);

        // Move position of death wall along
        if (deathWall)
        {
            Quaternion deathwallRotationOffset = Quaternion.Euler(90, 0, 0);
            SetWallPositionAndRotationToProgress(deathWall.transform, zoneProgress - (widthInLapsUnits / 2), deathwallRotationOffset);
        }

        // Move position of fog wall along
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
    /// Given a progress in laps, e.g -3.5, this function converts it to single lap progress, e.g 0.5
    /// </summary>
    /// <param name="progress">progress in laps units</param>
    /// <returns>progress in laps units clamped to 01</returns>
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

    /// <summary>
    /// Using the length of the track, given something like 50 meters, it works out how far that is in laps units
    /// </summary>
    /// <param name="meters">Length of distance in meters</param>
    /// <returns>Length in laps units</returns>
    public float convertMetersToLapsUnits(float meters)
    {
        return meters / railwayLength;
    }

    /// <summary>
    /// The walls run along rails, which is called the railway. This function creates the list of railway points
    /// and directions
    ///  - And works out the railway length
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
/// Holds all continuous game state datas such as zone progress, zone width, and zone speed
/// </summary>
struct ZoneStateData : INetworkSerializable
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
public struct LeaderboardData: INetworkSerializable
{
    public ulong[] playerIDs;
    public ushort[] distancesTravelled;
    public bool[] playersWon;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref playerIDs);
        serializer.SerializeValue(ref distancesTravelled);
        serializer.SerializeValue(ref playersWon);
    }

    public static LeaderboardData Empty
    {
        get
        {
            return new LeaderboardData()
            {
                playerIDs = null,
                distancesTravelled = null,
                playersWon = null
            };
        }
    }
}
