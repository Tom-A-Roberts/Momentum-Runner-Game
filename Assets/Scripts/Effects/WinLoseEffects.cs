using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Unity.Netcode;
using System;

public class WinLoseEffects : MonoBehaviour
{
    public static WinLoseEffects Singleton { get; private set; }

    public GameObject victoryImage;
    public GameObject defeatImage;

    [Header("Prefabs")]
    public GameObject victoryParticles;
    public GameObject defeatParticles;

    /// <summary>
    /// Depth effect to enable when someone has won or someone has lost
    /// </summary>
    [System.NonSerialized]
    public VolumeComponent depthEffect;

    private void Awake()
    {
        if (Singleton != null && Singleton != this)
            Destroy(Singleton);
        Singleton = this;
    }

    public void StartWinEffects()
    {
        victoryImage.SetActive(true);
        if (!depthEffect.active)
            depthEffect.active = true;

        if (GameStateManager.Singleton && GameStateManager.Singleton.localPlayer)
        {
            Quaternion upDirection = Quaternion.FromToRotation(Vector3.forward, GameStateManager.Singleton.localPlayer.myCamera.transform.up); ;
            Instantiate(victoryParticles, GameStateManager.Singleton.localPlayer.bodyRigidbody.position, upDirection);
        }
    }

    public void StartLoseEffects()
    {
        defeatImage.SetActive(true);
        if (!depthEffect.active)
            depthEffect.active = true;

        if (GameStateManager.Singleton && GameStateManager.Singleton.localPlayer)
        {
            Quaternion upDirection = Quaternion.FromToRotation(Vector3.forward, GameStateManager.Singleton.localPlayer.myCamera.transform.up); ;
            Instantiate(defeatParticles, GameStateManager.Singleton.localPlayer.bodyRigidbody.position, upDirection);
        }
    }
    public void EndEffects()
    {
        victoryImage.SetActive(false);
        defeatImage.SetActive(false);
        if (depthEffect.active)
            depthEffect.active = false;

    }


    public void ShowLeaderboard(LeaderboardData leaderboardData)
    {
        LeaderboardEntry[] leaderboardTable = LeaderboardEntry.CreateLeaderboardFromStruct(leaderboardData);
        for (int row = 0; row < leaderboardTable.Length; row++)
        {
            Debug.Log(leaderboardTable[row].DisplayName + " - " + leaderboardTable[row].DistanceTravelled.ToString());
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        UnityEngine.Rendering.Volume[] sceneVolumes = GameObject.FindObjectsOfType<UnityEngine.Rendering.Volume>();
        foreach (var sceneVolume in sceneVolumes)
        {
            if (sceneVolume != null)
            {
                for (int componentID = 0; componentID < sceneVolume.profile.components.Count; componentID++)
                {
                    if (sceneVolume.profile.components[componentID].name.Contains("DepthOfField"))
                    {
                        depthEffect = (DepthOfField)sceneVolume.profile.components[componentID];
                    }
                }
            }
        }

        EndEffects();
        
    }

    // Update is called once per frame
    void Update()
    {

    }
}

public class LeaderboardEntry: IComparer<LeaderboardEntry>
{
    public ulong PlayerID => _playerID;
    private ulong _playerID;

    public string DisplayName => _displayName;
    private string _displayName;

    public float DistanceTravelled => _distanceTravelled;
    private float _distanceTravelled;


    public LeaderboardEntry(ulong playerID, ushort distanceTravelled)
    {
        _playerID = playerID;
        _distanceTravelled = distanceTravelled;
        if(GameStateManager.Singleton && GameStateManager.Singleton.localPlayer)
        {
            _displayName = GameStateManager.Singleton.localPlayer.DisplayName;
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
            dataOut[i] = new LeaderboardEntry(leaderboardData.playerIDs[i], leaderboardData.distancesTravelled[i]);
        }
        Array.Sort(dataOut);
        return dataOut;
    }

    public int Compare(LeaderboardEntry x, LeaderboardEntry y)
    {
        if(x.DistanceTravelled > y.DistanceTravelled)
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

public struct LeaderboardData
{
    public ulong[] playerIDs;
    public ushort[] distancesTravelled;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref playerIDs);
        serializer.SerializeValue(ref distancesTravelled);
    }
}
