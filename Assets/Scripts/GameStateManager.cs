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
    private NetworkVariable<GameStateData> _gameState = new NetworkVariable<GameStateData>(writePerm: NetworkVariableWritePermission.Server);

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

    /// <summary>
    /// The current game state within the level
    /// </summary>
    public static GameState CurrentGameState => Singleton.gameStateSwitcher.GameState;
    /// <summary>
    /// The current game state within the level
    /// </summary>
    public GameState currentGameState => gameStateSwitcher.GameState;

    // These three variables get set during populateRailwayPoints(). They store information about
    // how the zone should move around the map
    private float railwayLength = 0;
    private Vector3[] railwayPoints;
    private Quaternion[] railwayDirections;


    
    /// <summary>
    /// Which game states the current level can be in
    /// </summary>
    public enum GameState
    {
        waitingToReadyUp,
        readiedUp,
        playingGame,
        someoneHasWon,
        podium
    }

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
        if (DeveloperMode)
        {
            gameStateSwitcher.SwitchToPlayingGame(false);
        }

        _zoneWidth = zoneStartWidth;
        // Only need to update this once since it doesn't change throughout the game.
        if (!IsHost)
        {
            GetClosingSpeedServerRPC();

            // Add callback function for when the game state is changed:
            _gameState.OnValueChanged += ChangedGameState;
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
    /// Handles switching between different game states such as readied up
    /// </summary>
    public class GameStateSwitcher
    {
        /// <summary>
        /// Maintains the current state of the game (the current level)
        /// </summary>
        public GameState GameState => _gameState;
        private GameState _gameState;

        /// <summary>
        /// The parent who owns this object
        /// </summary>
        public GameStateManager parent;

        /// <summary>
        /// Gets set in NetworkSpawn to the material of the ready up cube
        /// </summary>
        private Material readyUpCubeMaterial;
        /// <summary>
        /// Records the original height of the readyup cube so that it can be pressed down and released using the height
        /// </summary>
        private float readyUpCubeOriginalHeight;

        /// <summary>
        /// Counts down from 5.99 to 0 when everyone is readied up
        /// </summary>
        public float readiedCountdownProgress = 0;

        public GameStateSwitcher(GameStateManager _parent)
        {
            parent = _parent;
            _gameState = GameState.waitingToReadyUp;

            if (parent.ReadyUpCube)
            {
                readyUpCubeMaterial = parent.ReadyUpCube.GetComponent<Renderer>().material;
                readyUpCubeOriginalHeight = parent.ReadyUpCube.transform.position.y;
            }
            else
                Debug.LogWarning("No ready up cube found! please assign it in the GameStateManager inspector");

            SwitchToWaitingToReadyUp(false);
        }

        /// <summary>
        /// Called by GameStateManager.Update()
        /// </summary>
        public void Update()
        {
            if(_gameState == GameState.readiedUp)
            {
                readiedCountdownProgress -= Time.deltaTime;
                UpdateCountdown();
                if(readiedCountdownProgress < 0)
                {
                    readiedCountdownProgress = 0;
                    SwitchToPlayingGame(true);
                }
            }
            else
            {
                readiedCountdownProgress = 0;
            }
        }

        /// <summary>
        /// Updates the countdown visuals and audio, such as the counter on the player's screen
        /// </summary>
        public void UpdateCountdown()
        {
            if (parent.CountdownText)
                parent.CountdownText.text = Mathf.FloorToInt(readiedCountdownProgress).ToString();
            
            parent.localPlayer.myPlayerStateController.playerAudioManager.PlaySoundsDuringCountdown(readiedCountdownProgress);
        }

        /// <param name="useEffects">Whether you want to show effects like animations or sounds</param>
        public void SwitchToWaitingToReadyUp(bool useEffects)
        {
            SwitchFromState(_gameState, useEffects);
            _gameState = GameState.waitingToReadyUp;
            if (parent.waitingToReadyUpPanel)
                parent.waitingToReadyUpPanel.SetActive(true);

            if (parent.ReadyUpBarrier)
                parent.ReadyUpBarrier.SetActive(true);

            if (!parent.DeveloperMode)
                parent.localPlayer.myPlayerStateController.playerAudioManager.SwitchToReadyUpMusic();
        }

        /// <param name="useEffects">Whether you want to show effects like animations or sounds</param>
        public void SwitchToReadiedUp(bool useEffects)
        {
            readiedCountdownProgress = 5.99f;

            SwitchFromState(_gameState, useEffects);
            _gameState = GameState.readiedUp;

            if (parent.CountdownPanel)
                parent.CountdownPanel.SetActive(true);

            if (parent.CountdownText)
                parent.CountdownText.text = Mathf.FloorToInt(readiedCountdownProgress).ToString();

            if (parent.ReadyUpBarrier)
                parent.ReadyUpBarrier.SetActive(true);

            if (!parent.DeveloperMode)
                parent.localPlayer.myPlayerStateController.playerAudioManager.SwitchToCountdown();

            if (parent.ReadyUpFloatingText)
                parent.ReadyUpFloatingText.text = "";
        }

        /// <param name="useEffects">Whether you want to show effects like animations or sounds</param>
        public void SwitchToPlayingGame(bool useEffects)
        {
            SwitchFromState(_gameState, useEffects);
            _gameState = GameState.playingGame;

            if (parent.ReadyUpBarrier)
            {
                parent.ReadyUpBarrier.SetActive(false);
            }

            if (parent.ReadyUpFloatingText)
                parent.ReadyUpFloatingText.text = "";

            if (!parent.DeveloperMode)
                parent.localPlayer.myPlayerStateController.playerAudioManager.SwitchToGameplaySoundtrack();
        }

        /// <param name="useEffects">Whether you want to show effects like animations or sounds</param>
        public void SwitchToSomeoneHasWon(bool localPlayerWon)
        {
            if(_gameState != GameState.someoneHasWon)
            {
                SwitchFromState(_gameState, false);
                _gameState = GameState.someoneHasWon;

                if (parent.ReadyUpBarrier)
                    parent.ReadyUpBarrier.SetActive(false);

                if (localPlayerWon)
                    LocalPlayerWin();
                else
                    LocalPlayerLose();
            }
        }

        public void SwitchToPodium(bool useEffects)
        {
            SwitchFromState(_gameState, useEffects);
            _gameState = GameState.podium;

            if (parent.ReadyUpBarrier)
                parent.ReadyUpBarrier.SetActive(false);
        }

        /// <summary>
        /// When switching out from a state, this function turns off previous state effects
        /// </summary>
        private void SwitchFromState(GameState previousState, bool useEffects)
        {
            if(previousState == GameState.waitingToReadyUp)
            {
                if (parent.waitingToReadyUpPanel)
                    parent.waitingToReadyUpPanel.SetActive(false);

            }
            else if(previousState == GameState.readiedUp)
            {
                if (parent.CountdownPanel)
                    parent.CountdownPanel.SetActive(false);
                
            }
            else if (previousState == GameState.playingGame)
            {

            }
            else if (previousState == GameState.someoneHasWon)
            {
                if (WinLoseEffects.Singleton)
                    WinLoseEffects.Singleton.EndEffects();
            }
            else if(previousState == GameState.podium)
            {

            }
        }


        public void LocalPlayerWin()
        {
            if (WinLoseEffects.Singleton)
            {
                WinLoseEffects.Singleton.StartWinEffects();
            }
        }

        public void LocalPlayerLose()
        {
            if (WinLoseEffects.Singleton)
            {
                WinLoseEffects.Singleton.StartLoseEffects();
            }
        }

        /// <summary>
        /// Initiates local effects when player has begun the ready up phase
        /// </summary>
        public void LocalStartReadyUpEffects()
        {
            if (readyUpCubeMaterial)
            {
                readyUpCubeMaterial.SetColor("_EmissiveColor", new Color(0, 1, 1) * 1);
                readyUpCubeMaterial.SetColor("_BaseColor", new Color(0, 1, 1));
                float newHeight = readyUpCubeOriginalHeight - parent.ReadyUpCube.transform.localScale.y / 2;
                parent.ReadyUpCube.transform.position = new Vector3(parent.ReadyUpCube.transform.position.x, newHeight, parent.ReadyUpCube.transform.position.z);
            }
            if (parent.ReadyUpFloatingText)
                parent.ReadyUpFloatingText.text = "...";
        }
        /// <summary>
        /// Initiates local effects when player has readied up (confirmed by the server)
        /// </summary>
        public void LocalServerReadyUpEffects()
        {
            if (readyUpCubeMaterial)
            {
                readyUpCubeMaterial.SetColor("_EmissiveColor", new Color(0, 1, 1) * 8);
                readyUpCubeMaterial.SetColor("_BaseColor", new Color(0, 1, 1));
                float newHeight = readyUpCubeOriginalHeight;
                parent.ReadyUpCube.transform.position = new Vector3(parent.ReadyUpCube.transform.position.x, newHeight, parent.ReadyUpCube.transform.position.z);
            }
            if (parent.ReadyUpFloatingText)
                parent.ReadyUpFloatingText.text = "Readied up!";
        }
        /// <summary>
        /// Initiates local effects when player has begun the unready up phase
        /// </summary>
        public void LocalStartUnreadyEffects()
        {
            if (readyUpCubeMaterial)
            {
                readyUpCubeMaterial.SetColor("_EmissiveColor", new Color(1, 0, 0) * 1);
                readyUpCubeMaterial.SetColor("_BaseColor", new Color(1, 0, 0));
                float newHeight = readyUpCubeOriginalHeight - parent.ReadyUpCube.transform.localScale.y / 2;
                parent.ReadyUpCube.transform.position = new Vector3(parent.ReadyUpCube.transform.position.x, newHeight, parent.ReadyUpCube.transform.position.z);
            }
            if (parent.ReadyUpFloatingText)
                parent.ReadyUpFloatingText.text = "...";
        }
        /// <summary>
        /// Initiates local effects when player has unreadied (confirmed by the server)
        /// </summary>
        public void LocalServerUnreadyEffects()
        {
            if (readyUpCubeMaterial)
            {
                readyUpCubeMaterial.SetColor("_EmissiveColor", new Color(1, 0, 0) * 4);
                readyUpCubeMaterial.SetColor("_BaseColor", new Color(1, 0, 0));
                float newHeight = readyUpCubeOriginalHeight;
                parent.ReadyUpCube.transform.position = new Vector3(parent.ReadyUpCube.transform.position.x, newHeight, parent.ReadyUpCube.transform.position.z);
            }
            if (parent.ReadyUpFloatingText)
                parent.ReadyUpFloatingText.text = "Shoot this button to ready up";
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
                ZoneWidth = _zoneWidth,
                ZoneSpeed = zoneSpeed
            };
            _gameState.Value = stateData;
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
    private void ChangedGameState(GameStateData oldGameState, GameStateData newGameState)
    {
        zoneProgress = newGameState.ZoneProgress;
        _zoneWidth = newGameState.ZoneWidth;
        zoneSpeed = newGameState.ZoneSpeed;
    }

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
