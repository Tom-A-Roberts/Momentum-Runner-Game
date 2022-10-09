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
    public float zoneStartProgress = 0;
    [Tooltip("How wide the zone starts in meters")]
    public float zoneStartWidth = 350;
    [Tooltip("The zone's 'center' is offset by this bias, where 0.5 is the middle of the available zone")]
    [Range(0, 1)]
    public float zoneCenterBias = 0.25f;
    [Tooltip("The speed of the zone at the start of the game, measured in meters per second")]
    public float zoneBaseSpeed = 1;
    [Tooltip("As you get closer to the fogwall, the zone speed increases faster and faster according to this polynomial power")]
    public int zoneSpeedIncreasePolynomial = 2;
    [Tooltip("As players (average position) gets closer to the fogwall, this is how much faster the wall moves. if 5, then the zoneSpeed would be baseSpeed+5 when players are in fogwall.")]
    public float zoneSpeedIncrease = 1;
    [Tooltip("The zone does not instantly change speed, it does so smoothly according to this smoothing variable. Higher = more smoothing")]
    public float zoneSpeedChangeSmoothing = 1;
    [Tooltip("If true, then when players (average position) are behind the zone centroid, the zone can slow below base speed")]
    public bool zoneSlowsIfPlayersAreSlow = false;

    [Tooltip("0.1 = slow AND sluggish, 1 = fast AND adaptive.")]
    [Range(0, 2)]
    public float zoneDifficultyChanger = 1;

    [Header("Gameplay settings")]
    [Tooltip("How many seconds you are floating upwards for. See PlayerController for more settings")]
    public float respawnDuration = 3;
    [Tooltip("When you get shot, how long the slowing lasts")]
    public float slowdownTime = 1;
    [Tooltip("When leaving slowdown, you get additional speed to counteract the drag during slowing.")]
    public float slowdownAdditionalVelocityMultiplier = 0.3f;
    [Tooltip("When you get shot, how long the shield afterwards lasts")]
    public float slowdownTimeShield = 1;


    [Header("Effects settings")]
    public Image ScreenRedEdges;
    public Image SlowdownBorder;
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
    public float _zoneWidth;

    public float _zoneSpeed = 1;

    public float _zoneProgress = 0;

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

    //private SettingsInterface settings;
    private AdjustSettingsFromPrefs settingsAdjuster;

    private float zoneSpeedTarget = 0;
    private float zoneSpeedDeriv = 0;

    // These three variables get set during populateRailwayPoints(). They store information about
    // how the zone should move around the map
    private float railwayLength = 0;
    public float RailwayLength => railwayLength;

    [System.NonSerialized]
    public Vector3[] railwayPoints;

    [System.NonSerialized]
    public Quaternion[] railwayDirections;

    #region Startup Functions

    private void Awake()
    {
        if (Singleton != null && Singleton != this)
            Destroy(Singleton);
        Singleton = this;

        //settings = new SettingsInterface();
        //int fpsLimit = settings.fpsLimit.Value;
        //if(fpsLimit > 0)
        //{
        //    QualitySettings.vSyncCount = 0;
        //    Application.targetFrameRate = fpsLimit;
        //}


    }

    void Start()
    {
        if (railwayPointsList == null)
        {
            //throw new System.Exception("\"railwayPointsList is set to null.\\n Therefore no wall points found to use when moving the deathwall and fogwall!\"");
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

        settingsAdjuster = new AdjustSettingsFromPrefs();

        settingsAdjuster.UpdateGraphics();

    }

    /// <summary>
    /// Called on PlayerNetwork OnNetworkSpawn() (specifically, when the IsOwner spawns)
    /// </summary>
    public void OnLocalPlayerNetworkSpawn()
    {
        readiedPlayers = new HashSet<ulong>();

        //Debug.Log(localPlayer.myPlayerStateController);

        localPlayer.myPlayerStateController.playerAudioManager.SetupLevelSoundtracks(waitingToReadyUpSong, levelSoundTracks);

        gameStateSwitcher = new GameStateSwitcher(this);

        _zoneWidth = zoneStartWidth;
        _zoneSpeed = zoneBaseSpeed;
        _zoneProgress = zoneStartProgress;
        // Only need to update this once since it doesn't change throughout the game. 
        if (!NetworkManager.Singleton.IsHost)
        {
            closingSpeed = _networkedClosingSpeed.Value;

            // Add callback function for when the game state is changed: 
            _zoneState.OnValueChanged += ChangedZoneState;

            if (DeveloperMode)
            {
                if (fogWall)
                    fogWall.SetActive(false);
                if (deathWall)
                    deathWall.SetActive(false);
            }
        }
        else
        {
            if (DeveloperMode)
            {
                _gameState.Value = GameState.playingGame;
                if(fogWall)
                    fogWall.SetActive(false);
                if(deathWall)
                    deathWall.SetActive(false);
            }
            else
            {
                if (fogWall)
                    fogWall.SetActive(true);
                if (deathWall)
                    deathWall.SetActive(true);

                _gameState.Value = GameState.waitingToReadyUp;
            }
            _networkedClosingSpeed.Value = (ushort)closingSpeed;
        }

        //If nameplate hasn't been initialized by now, ensure it is.
        foreach (var player in PlayerNetworking.ConnectedPlayerNetworkingScripts)
        {
            if (!player.Value.nameplate.Initialized)
            {
                player.Value.InitNameplate();
            }
        }
    }
    #endregion

    public void ResetLevelToBeginning()
    {
        if (NetworkManager.IsHost)
        {
            _zoneWidth = zoneStartWidth;
            _zoneSpeed = zoneBaseSpeed;
            _zoneProgress = zoneStartProgress;

            foreach (PlayerNetworking player in ConnectedPlayerNetworkingScripts.Values)
            {
                player.ResetPlayerServerside();

                //Debug.Log(currentNetworking.myPlayerStateController.bodySpawnPosition);
                player.ServerTeleportPlayer(player.myPlayerStateController.bodySpawnPosition);
            }
        }
    }

    /// <summary>
    /// Update the game state data if host
    /// Then simply move the walls along as expected according to what the game state data says
    /// </summary>
    void Update()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            // Modify variables here according to the progress of the game
            //zoneSpeed = fastestPlayer.Speed * (1 - playerEfficiencyRequirement);

            if(GameState == GameState.playingGame)
                ServerUpdateWallSpeed();

            ZoneStateData stateData = new ZoneStateData()
            {
                ZoneProgress = _zoneProgress,
                ZoneWidth = _zoneWidth,
                ZoneSpeed = _zoneSpeed
            };
            _zoneState.Value = stateData;
        }
        UpdateWallPositions();

        if (localPlayer && deathWall && deathWallCollider)
        {
            localPlayer.myPlayerStateController.UpdateDeathWallEffects(deathWall.transform, deathWallCollider);
        }

        gameStateSwitcher.Update();

        if (Input.GetKeyDown(KeyCode.R))
            GameStateManager.Singleton.TestForWinState();

    }


    /// <summary>
    /// Called whenever the 'non host' recieves new information from the host. 
    /// </summary>
    private void ChangedZoneState(ZoneStateData oldGameState, ZoneStateData newGameState)
    {
        _zoneProgress = newGameState.ZoneProgress;
        _zoneWidth = newGameState.ZoneWidth;
        _zoneSpeed = newGameState.ZoneSpeed;
    }


    #region Winning/Losing Game

    public void ServerForceChangeGameState(GameState newGamestate)
    {
        if (NetworkManager.Singleton.IsHost)
            _gameState.Value = newGamestate;
    }

    public void TestForWinState()
    {
        int winCount = 0;
        int loseCount = 0;

        foreach (var keyValuePair in ConnectedPlayerNetworkingScripts)
        {
            PlayerNetworking playerNetworking = keyValuePair.Value;

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
        if ((winCount <= 1 && loseCount > 0) || DeveloperMode)
        {
            // player has won
            ServerForceChangeGameState(GameState.winState);
            LeaderboardData leaderboardData = GatherWinData();
            localPlayer.SendLeaderboardDataServerRPC(leaderboardData);
        }
    }


    public LeaderboardData GatherWinData()
    {
        ulong[] _playerIDs = new ulong[ConnectedPlayers.Count];
        ushort[] _distancesTravelled = new ushort[ConnectedPlayers.Count];
        ushort[] _averageSpeeds = new ushort[ConnectedPlayers.Count];
        ushort[] _fastestSpeeds = new ushort[ConnectedPlayers.Count];
        int[] _lapsCompleted = new int[ConnectedPlayers.Count];
        bool[] _playersWon = new bool[ConnectedPlayers.Count];

        int i = 0;
        foreach (var keyValuePair in ConnectedPlayerNetworkingScripts)
        {
            PlayerNetworking playerNetworking = keyValuePair.Value;

            StatsTracker.StatsSummary stats = playerNetworking.myStatsTracker.ProduceLeaderboardStats();

            _playerIDs[i] = playerNetworking.OwnerClientId;
            _distancesTravelled[i] = (ushort)stats.DistanceTravelled;
            _averageSpeeds[i] = (ushort)stats.AverageSpeed;
            _fastestSpeeds[i] = (ushort)stats.FastestSpeed;
            _lapsCompleted[i] = (int)stats.LapsCompleted;

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
            averageSpeeds = _averageSpeeds,
            fastestSpeeds = _fastestSpeeds,
            lapsCompleted = _lapsCompleted,
            playersWon = _playersWon,
        };
    }

    #endregion



    #region Wall Helper functions

    /// <summary>
    /// SERVER ONLY
    /// <para>Returns the central (average) point of where the group is in lap units</para>
    /// </summary>
    public float ServerGetPeopleCentroid()
    {
        float averageProgress = 0;
        foreach (var keyValue in ConnectedPlayerNetworkingScripts)
        {
            averageProgress += keyValue.Value.myStatsTracker.LatestPosition;
        }
        if(ConnectedPlayerNetworkingScripts.Count > 0)
        {
            averageProgress /= ConnectedPlayerNetworkingScripts.Count;
        }
        return averageProgress;
    }

    /// <summary>
    /// SERVER ONLY
    /// <para>Changes the speed of the wall according to player progress</para>
    /// </summary>
    void ServerUpdateWallSpeed()
    {
        float playerCentroid = convertMetersToLapsUnits(ServerGetPeopleCentroid());

        float centroidOffset = ((zoneCenterBias - 0.5f) * _zoneWidth);
        float zoneCentroid = _zoneProgress + convertMetersToLapsUnits(centroidOffset);

        float lapDistance = signedDistanceBetweenPoints(playerCentroid, zoneCentroid);
        float metersDistance = convertLapsUnitsToMeters(lapDistance);

        // value = 0 if player centroid is at the zone centroid
        // Value = 1 if player centroid is in the fog wall.
        float playerDistFromCentroidToFogWall01 = metersDistance / (_zoneWidth - (zoneCenterBias * _zoneWidth));
        if (playerDistFromCentroidToFogWall01 < 0 && !zoneSlowsIfPlayersAreSlow)
            playerDistFromCentroidToFogWall01 = 0;

        float forceApplied01 = Mathf.Pow(playerDistFromCentroidToFogWall01, zoneSpeedIncreasePolynomial);
        float forceAppliedSpeed = forceApplied01 * zoneSpeedIncrease;

        zoneSpeedTarget = (zoneBaseSpeed + forceAppliedSpeed) * zoneDifficultyChanger;

        // DEBUG CODE START
        Vector3 playersPos = GetWorldPosFromProgress(playerCentroid);
        Vector3 zonePos = GetWorldPosFromProgress(zoneCentroid);
        playersPos.y = localPlayer.bodyRigidbody.transform.position.y;
        zonePos.y = localPlayer.bodyRigidbody.transform.position.y;
        Color lCol = Color.gray;
        if (lapDistance > 0)
            lCol = Color.green;
        Debug.DrawLine(playersPos, zonePos, lCol, Time.deltaTime);
        // DEBUG CODE END

        _zoneSpeed = Mathf.SmoothDamp(_zoneSpeed, zoneSpeedTarget, ref zoneSpeedDeriv, zoneSpeedChangeSmoothing);
        //Debug.Log(_zoneSpeed.ToString() + "   " + zoneSpeedTarget.ToString());
    }

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
                metersToTravel = _zoneSpeed * Time.deltaTime;
                _zoneWidth -= Time.deltaTime * closingSpeed;
                if (_zoneWidth < 0)
                    _zoneWidth = 0;
            }
        }

        float widthInLapsUnits = convertMetersToLapsUnits(_zoneWidth) * (2 - zoneDifficultyChanger);
        _zoneProgress += convertMetersToLapsUnits(metersToTravel);

        // Move position of death wall along
        if (deathWall)
        {
            Quaternion deathwallRotationOffset = Quaternion.Euler(90, 0, 0);
            SetWallPositionAndRotationToProgress(deathWall.transform, _zoneProgress - (widthInLapsUnits / 2), deathwallRotationOffset);
        }

        // Move position of fog wall along
        if (fogWall)
        {
            Quaternion fogwallRotationOffset = Quaternion.Euler(0, 180, 0);
            SetWallPositionAndRotationToProgress(fogWall.transform, _zoneProgress + (widthInLapsUnits / 2), fogwallRotationOffset);
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
    /// Converts progress in meters into a real-world 3D position
    /// </summary>
    /// <param name="progress">Progress in meters</param>
    /// <returns>The absolute position in world space</returns>
    public static Vector3 GetWorldPosFromProgress(float progress)
    {
        float progressSimplified = convertProgressToBetween01(progress);
        float interpProgress = progressSimplified * Singleton.railwayPoints.Length;
        // Calculate which points to interpolate between
        int lowestPoint = Mathf.FloorToInt(interpProgress);
        int highestPoint = Mathf.CeilToInt(interpProgress);
        // Wrap around to 0
        if (highestPoint == Singleton.railwayPoints.Length)
            highestPoint = 0;
        // Calculate the interpolation amount:
        float interpAmount = interpProgress - Mathf.Floor(interpProgress);
        return Vector3.Lerp(Singleton.railwayPoints[lowestPoint], Singleton.railwayPoints[highestPoint], interpAmount);
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

    public float convertLapsUnitsToMeters(float laps)
    {
        return laps * railwayLength;
    }

    public float convertRailwayPointToDistance(int railwayPoint)
    {
        return ((float)railwayPoint / (float)railwayPoints.Length) * railwayLength;
    }

    public float convertRailwayPointToLaps(int railwayPoint)
    {
        return ((float)railwayPoint / (float)railwayPoints.Length);
    }

    /// <summary>
    /// Returns the signed distance between two points in a lap, given in lap units
    /// </summary>
    public float signedDistanceBetweenPoints(float progressA, float progressB)
    {
        float A_angle = convertProgressToBetween01(progressA) * Mathf.PI * 2;
        float B_angle = convertProgressToBetween01(progressB) * Mathf.PI * 2;
        return Mathf.Atan2(Mathf.Sin(B_angle - A_angle), Mathf.Cos(B_angle - A_angle)) / (MathF.PI * 2);
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
    public ushort[] averageSpeeds;
    public ushort[] fastestSpeeds;
    public int[] lapsCompleted;
    public bool[] playersWon;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref playerIDs);
        serializer.SerializeValue(ref distancesTravelled);
        serializer.SerializeValue(ref averageSpeeds);
        serializer.SerializeValue(ref fastestSpeeds);
        serializer.SerializeValue(ref lapsCompleted);
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
