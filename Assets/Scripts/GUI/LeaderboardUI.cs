using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardUI : MonoBehaviour
{
    public static LeaderboardUI Singleton { get; private set; }

    public GameObject LeaderboardUIObject;
    public GameObject LeaderboardContentObject;
    public Button ReplayLevelButton;
    public GameObject LocalPlayerDataRowTemplate;
    public GameObject RemotePlayerDataRowTemplate;
   
    public bool IsShowing => _isShowing;
    private bool _isShowing = false;

    private List<GameObject> spawnedRows;

    private void Awake()
    {
        if (Singleton != null && Singleton != this)
        {
            Destroy(Singleton);
        }
        Singleton = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        spawnedRows = new List<GameObject>();

        if (!_isShowing)
            HideLeaderboard();
        else
            ShowLeaderboard(LeaderboardData.Empty);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowLeaderboard(LeaderboardData leaderboardData)
    {
        _isShowing = true;
        LeaderboardUIObject.SetActive(true);
        IngameEscMenu.UnlockCursor();

        LeaderboardEntry[] leaderboardTable = LeaderboardEntry.CreateLeaderboardFromStruct(leaderboardData);

        if (!NetworkManager.Singleton.IsHost)
        {
            ReplayLevelButton.interactable = false;
        }

        for (int row = 0; row < leaderboardTable.Length; row++)
        {
            //Debug.Log(leaderboardTable[row].DisplayName + " - " + leaderboardTable[row].PlayerID.ToString() + " - " + PlayerNetworking.localPlayer.OwnerClientId.ToString() + " - " + leaderboardTable[row].DistanceTravelled.ToString());

            GameObject rowPrefabToSpawn;
            if (leaderboardTable[row].PlayerID == PlayerNetworking.localPlayer.OwnerClientId)
                rowPrefabToSpawn = LocalPlayerDataRowTemplate;
            else
                rowPrefabToSpawn = RemotePlayerDataRowTemplate;

            string positionString = "";

            if (row == 0)
                positionString = "1st";
            else if (row == 1)
                positionString = "2nd";
            else if (row == 2)
                positionString = "3rd";

            GameObject spawnedRow = Instantiate(rowPrefabToSpawn, LeaderboardContentObject.transform);
            spawnedRow.transform.Find("PositionText").GetComponent<TMP_Text>().text = positionString;
            spawnedRow.transform.Find("PlayerText").GetComponent<TMP_Text>().text = leaderboardTable[row].DisplayName;
            spawnedRow.transform.Find("DistanceText").GetComponent<TMP_Text>().text = String.Format("{0:n0}", leaderboardTable[row].DistanceTravelled) + " m";
            spawnedRow.transform.Find("LapsCompletedText").GetComponent<TMP_Text>().text = String.Format("{0:n0}", leaderboardTable[row].LapsCompleted);
            spawnedRow.transform.Find("AvSpeedText").GetComponent<TMP_Text>().text = String.Format("{0:n0}", leaderboardTable[row].AverageSpeed) + " m/s";
            spawnedRow.transform.Find("FastestSpeedText").GetComponent<TMP_Text>().text = String.Format("{0:n0}", leaderboardTable[row].FastestSpeed) + " m/s";
            spawnedRow.SetActive(true);
            spawnedRows.Add(spawnedRow);
        }
    }
    public void HideLeaderboard()
    {
        for (int row = 0; row < spawnedRows.Count; row++)
        {
            Destroy(spawnedRows[row]);
        }

        _isShowing = false;
        if (!IngameEscMenu.Singleton.isEscMenuShowing)
            IngameEscMenu.LockCursor();
        LeaderboardUIObject.SetActive(false);
    }
}


public class LeaderboardEntry : IComparer<LeaderboardEntry>, IComparable<LeaderboardEntry>
{
    public ulong PlayerID => _playerID;
    private ulong _playerID;

    public string DisplayName => _displayName;
    private string _displayName;

    public float DistanceTravelled => _distanceTravelled;
    private float _distanceTravelled;

    public float AverageSpeed => _averageSpeed;
    private float _averageSpeed;
    public float FastestSpeed => _fastestSpeed;
    private float _fastestSpeed;

    public int LapsCompleted => _lapsCompleted;
    private int _lapsCompleted;

    public bool PlayerWon => _playerWon;
    private bool _playerWon;

    public LeaderboardEntry(ulong playerID, ushort distanceTravelled, ushort averageSpeed, ushort fastestSpeed, int lapsCompleted, bool playerWon)
    {
        _playerID = playerID;
        _distanceTravelled = distanceTravelled;
        _playerWon = playerWon;
        _averageSpeed = averageSpeed;
        _fastestSpeed = fastestSpeed;
        _lapsCompleted = lapsCompleted;
        if (GameStateManager.Singleton && GameStateManager.Singleton.localPlayer)
        {
            _displayName = PlayerNetworking.ConnectedPlayers[playerID].GetComponent<PlayerNetworking>().DisplayName;
        }
        else
        {
            _displayName = "(Unknown Name)";
        }
    }

    public static LeaderboardEntry[] CreateLeaderboardFromStruct(LeaderboardData leaderboardData)
    {
        LeaderboardEntry[] dataOut = new LeaderboardEntry[leaderboardData.playerIDs.Length];

        for (int i = 0; i < leaderboardData.playerIDs.Length; i++)
        {
            dataOut[i] = new LeaderboardEntry(leaderboardData.playerIDs[i], 
                leaderboardData.distancesTravelled[i], 
                leaderboardData.averageSpeeds[i], 
                leaderboardData.fastestSpeeds[i], 
                leaderboardData.lapsCompleted[i], 
                leaderboardData.playersWon[i]);
        }
        Array.Sort(dataOut);
        Array.Reverse(dataOut);
        return dataOut;
    }

    public int Compare(LeaderboardEntry x, LeaderboardEntry y)
    {
        if (x.PlayerWon && !y.PlayerWon)
        {
            return 1;
        }
        else if (!x.PlayerWon && y.PlayerWon)
        {
            return -1;
        }
        else
        {
            if (x.DistanceTravelled > y.DistanceTravelled)
            {
                return 1;
            }
            else if (x.DistanceTravelled == y.DistanceTravelled)
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }
    }

    public int CompareTo(LeaderboardEntry obj)
    {
        if (PlayerWon && !obj.PlayerWon)
        {
            return 1;
        }
        else if (!PlayerWon && obj.PlayerWon)
        {
            return -1;
        }
        else
        {
            if (DistanceTravelled > obj.DistanceTravelled)
            {
                return 1;
            }
            else if (DistanceTravelled == obj.DistanceTravelled)
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }
    }
}