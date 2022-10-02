using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Which game states the current level can be in
/// </summary>
public enum GameState
{
    waitingToReadyUp,
    readiedUp,
    playingGame,
    winState,
    podium
}

/// <summary>
/// Handles switching between different game states such as readied up
/// </summary>
public class GameStateSwitcher
{
    const float readyUpCountdownTime = 5.99f;
    const float victoryScreenTimeBeforeSwitch = 3f;

    /// <summary>
    /// Maintains the current state of the game (the current level)
    /// </summary>
    public GameState GameState => GameStateManager.Singleton.GameState;

    /// <summary>
    /// Sometimes localGameState may go out of sync with the server gamestate. The localGameState is tracked here so it
    /// can resync where necessary.
    /// </summary>
    private GameState localGameState;

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

    private LeaderboardData latestLeaderboardData;

    private bool hasRecievedLeaderboardData = false;

    private bool localWinLoseEffectsActive = false;

    private bool leaderboardShowing = false;

    public float TimeInPlayingState => _timeInPlayingState;
    private float _timeInPlayingState = 0;

    /// <summary>
    /// Counts down from 5.99 to 0 when everyone is readied up
    /// </summary>
    public float readiedCountdownProgress = 0;

    /// <summary>
    /// When a victory or defeat happens, this counter counts down from 5. When it reaches 0, the leaderboard is shown
    /// </summary>
    private float leaderboardShowTimerProgress = 0;

    public GameStateSwitcher(GameStateManager _parent)
    {
        parent = _parent;
        localGameState = GameState.waitingToReadyUp;

        latestLeaderboardData = LeaderboardData.Empty;
        hasRecievedLeaderboardData = false;
        localWinLoseEffectsActive = false;
        if (leaderboardShowing)
            HideLeaderboard();

        if (parent.ReadyUpCube)
        {
            readyUpCubeMaterial = parent.ReadyUpCube.GetComponent<Renderer>().material;
            readyUpCubeOriginalHeight = parent.ReadyUpCube.transform.position.y;
        }
        else
            Debug.LogWarning("No ready up cube found! please assign it in the GameStateManager inspector");

        SwitchToWaitingToReadyUp();
    }

    /// <summary>
    /// Called by GameStateManager.Update()
    /// </summary>
    public void Update()
    {
        //Debug.Log(localGameState.ToString() + "  " + parent._gameState.Value.ToString());

        if (localGameState != GameStateManager.Singleton.GameState)
        {
            LocallySwitchToGameState(GameStateManager.Singleton.GameState);
        }

        if(localGameState == GameState.playingGame)
        {
            _timeInPlayingState += Time.deltaTime;
        }

        if (localGameState == GameState.readiedUp)
        {
            readiedCountdownProgress -= Time.deltaTime;
            UpdateCountdownVisuals();
            if (readiedCountdownProgress < 0)
            {
                readiedCountdownProgress = 0;
                if (NetworkManager.Singleton.IsHost)
                {
                    GameStateManager.Singleton.ServerForceChangeGameState(GameState.playingGame);
                } 
            }
        }
        else
            readiedCountdownProgress = 0;

        if (hasRecievedLeaderboardData && localGameState == GameState.winState && localWinLoseEffectsActive && !leaderboardShowing)
        {
            leaderboardShowTimerProgress -= Time.deltaTime;
            if (leaderboardShowTimerProgress < 0)
            {
                leaderboardShowTimerProgress = 0;
                ShowLeaderboard();
            }
        }

        if (hasRecievedLeaderboardData && localGameState == GameState.winState && !localWinLoseEffectsActive)
        {
            DecideIfLocalPlayerWonOrLost();
        }
    }

    public void RecieveLeaderboardData(LeaderboardData leaderboardData)
    {
        hasRecievedLeaderboardData = true;
        latestLeaderboardData = leaderboardData;
    }


    private void SwitchToWaitingToReadyUp()
    {
        SwitchFromState(localGameState);

        if (localGameState != GameState.readiedUp && NetworkManager.Singleton.IsHost)
            GameStateManager.Singleton.ResetLevelToBeginning();

        localGameState = GameState.waitingToReadyUp;

        if (parent.waitingToReadyUpPanel)
            parent.waitingToReadyUpPanel.SetActive(true);

        if (parent.ReadyUpBarrier)
            parent.ReadyUpBarrier.SetActive(true);

        if (!parent.DeveloperMode)
            parent.localPlayer.myPlayerStateController.playerAudioManager.SwitchToReadyUpMusic();
    }

    /// <param name="useEffects">Whether you want to show effects like animations or sounds</param>
    private void SwitchToReadiedUp()
    {
        SwitchFromState(localGameState);

        if (localGameState != GameState.waitingToReadyUp && NetworkManager.Singleton.IsHost)
            GameStateManager.Singleton.ResetLevelToBeginning();

        localGameState = GameState.readiedUp;

        readiedCountdownProgress = readyUpCountdownTime;

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
    private void SwitchToPlayingGame()
    {
        SwitchFromState(localGameState);
        localGameState = GameState.playingGame;
        _timeInPlayingState = 0;

        if (parent.ReadyUpBarrier)
        {
            parent.ReadyUpBarrier.SetActive(false);
        }

        if (parent.ReadyUpFloatingText)
            parent.ReadyUpFloatingText.text = "";

        if (!parent.DeveloperMode)
            parent.localPlayer.myPlayerStateController.playerAudioManager.SwitchToGameplaySoundtrack();

        foreach (var playerKeyValue in PlayerNetworking.ConnectedPlayerNetworkingScripts)
        {
            playerKeyValue.Value.myPlayerStateController.GameStarted();
        }
    }

    /// <param name="useEffects">Whether you want to show effects like animations or sounds</param>
    private void SwitchToSomeoneHasWon()
    {
        SwitchFromState(localGameState);
        localGameState = GameState.winState;

        if (parent.ReadyUpBarrier)
            parent.ReadyUpBarrier.SetActive(false);
    }

    private void SwitchToPodium()
    {
        SwitchFromState(localGameState);
        localGameState = GameState.podium;

        if (parent.ReadyUpBarrier)
            parent.ReadyUpBarrier.SetActive(false);
    }

    /// <summary>
    /// When switching out from a state, this function turns off previous state effects
    /// </summary>
    private void SwitchFromState(GameState previousState)
    {
        if (previousState == GameState.waitingToReadyUp)
        {
            if (parent.waitingToReadyUpPanel)
                parent.waitingToReadyUpPanel.SetActive(false);

        }
        else if (previousState == GameState.readiedUp)
        {
            if (parent.CountdownPanel)
                parent.CountdownPanel.SetActive(false);

        }
        else if (previousState == GameState.playingGame)
        {
            
        }
        else if (previousState == GameState.winState)
        {
            if (localWinLoseEffectsActive && WinLoseEffects.Singleton)
                WinLoseEffects.Singleton.EndEffects();
            hasRecievedLeaderboardData = false;
            localWinLoseEffectsActive = false;
            readiedCountdownProgress = 0;
            if (leaderboardShowing)
                HideLeaderboard();
        }
        else if (previousState == GameState.podium)
        {
            if (localWinLoseEffectsActive && WinLoseEffects.Singleton)
                WinLoseEffects.Singleton.EndEffects();
            hasRecievedLeaderboardData = false;
            localWinLoseEffectsActive = false;
            readiedCountdownProgress = 0;
            if (leaderboardShowing)
                HideLeaderboard();
        }
    }

    private void LocallySwitchToGameState(GameState newGameState)
    {
        if (newGameState == GameState.waitingToReadyUp && localGameState != GameState.waitingToReadyUp)
            SwitchToWaitingToReadyUp();
        else if (newGameState == GameState.readiedUp && localGameState != GameState.readiedUp)
            SwitchToReadiedUp();
        else if (newGameState == GameState.playingGame && localGameState != GameState.playingGame)
            SwitchToPlayingGame();
        else if (newGameState == GameState.winState && localGameState != GameState.winState)
            SwitchToSomeoneHasWon();
        else if (newGameState == GameState.podium && localGameState != GameState.podium)
            SwitchToPodium();
    }

    /// <summary>
    /// Updates the countdown visuals and audio, such as the counter on the player's screen
    /// </summary>
    private void UpdateCountdownVisuals()
    {
        if (parent.CountdownText)
        {
            int value = Mathf.FloorToInt(readiedCountdownProgress);
            if(value >= 0)
                parent.CountdownText.text = value.ToString();
            else
                parent.CountdownText.text = "...";
        }
        parent.localPlayer.myPlayerStateController.playerAudioManager.PlaySoundsDuringCountdown(readiedCountdownProgress);
    }

    private void DecideIfLocalPlayerWonOrLost()
    {
        leaderboardShowTimerProgress = victoryScreenTimeBeforeSwitch;
        bool playerFound = false;
        for (int i = 0; i < latestLeaderboardData.playerIDs.Length; i++)
        {
            if (latestLeaderboardData.playerIDs[i] == parent.localPlayer.OwnerClientId)
            {
                if (latestLeaderboardData.playersWon[i])
                {
                    LocalPlayerWin();
                }
                else
                {
                    LocalPlayerLose();
                }
                playerFound = true;
                break;
            }
        }
        // If player wasn't in the leaderboard information, still make them lose
        if (!playerFound)
        {
            LocalPlayerLose();
        }
    }

    private void ShowLeaderboard()
    {
        leaderboardShowing = true;
        if (LeaderboardUI.Singleton)
            LeaderboardUI.Singleton.ShowLeaderboard(latestLeaderboardData);
    }
    private void HideLeaderboard()
    {
        leaderboardShowing = false;
        if (LeaderboardUI.Singleton)
            LeaderboardUI.Singleton.HideLeaderboard();
    }

    private void LocalPlayerWin()
    {
        localWinLoseEffectsActive = true;
        if (WinLoseEffects.Singleton)
        {
            WinLoseEffects.Singleton.StartWinEffects();
        }
    }

    private void LocalPlayerLose()
    {
        localWinLoseEffectsActive = true;
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

