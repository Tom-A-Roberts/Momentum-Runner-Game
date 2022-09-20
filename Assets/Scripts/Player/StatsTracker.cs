using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.Netcode;
using UnityEngine;

public class StatsTracker
{
    /// <summary>
    /// How much leeway we give for people skipping checkpoints due to lag. if lapLeeway = 4, then we allow 4 checkpoints to be skipped.
    /// This needs to be high because you can pass many checkpoints very quickly if you travel close to the inner wall around a corner.
    /// </summary>
    private const int lapLeeway = 4;

    public PlayerNetworking myPlayerNetworking;
    public PlayerStateManager myPlayerStateManager;
    public Rigidbody myPlayerRigidbody;

    public int numberOfLaps = 0;

    private int latestLapInt = 0;

    private float fastestSpeed = 0;

    private float latestTimeWhenAlive = 0;
    private Vector3 latestPositionWhenAlive;

    public struct StatsSummary
    {
        public float DistanceTravelled;
        public int LapsCompleted;
        public float AverageSpeed;
        public float FastestSpeed;

        public bool writtenTo;
    }

    public StatsTracker(PlayerNetworking _playerNetworking, PlayerStateManager _playerStateManager, Rigidbody _playerRigidbody)
    {
        myPlayerNetworking = _playerNetworking;
        myPlayerStateManager = _playerStateManager;
        myPlayerRigidbody = _playerRigidbody;
        latestLapInt = 0;
        fastestSpeed = 0;
        latestTimeWhenAlive = 0;
        latestPositionWhenAlive = _playerRigidbody.position;
}

    public void ResetStats()
    {
        latestLapInt = 0;
        fastestSpeed = 0;
        latestTimeWhenAlive = 0;
        latestPositionWhenAlive = myPlayerRigidbody.position;
    }

    public void Update(Vector3 currentVelocity)
    {
        if (!myPlayerStateManager.IsDead)
        {
            Vector2 currentPlanarSpeed = new Vector2(currentVelocity.x, currentVelocity.z);
            float currentSpeed = currentPlanarSpeed.magnitude;

            if (currentSpeed > fastestSpeed)
            {
                fastestSpeed = currentSpeed;
            }

            int LapInt = GetLapIntAroundTrack(myPlayerRigidbody.position);
            if (LapInt > latestLapInt && LapInt < latestLapInt + lapLeeway)
            {
                latestLapInt = LapInt;
            }
            if (LapInt < lapLeeway && latestLapInt > GameStateManager.Singleton.railwayPoints.Length - lapLeeway)
            {
                latestLapInt = LapInt;
                HasCompletedLap();
            }

            latestTimeWhenAlive = GameStateManager.Singleton.gameStateSwitcher.TimeInPlayingState;
            latestPositionWhenAlive = myPlayerRigidbody.position;
        }
    }

    public StatsSummary ProduceLeaderboardStats()
    {
        float _distanceTravelled = GetLapDistanceAroundTrack(latestPositionWhenAlive) + numberOfLaps * GameStateManager.Singleton.RailwayLength;
        if (latestLapInt == 0 && numberOfLaps == 0)
            _distanceTravelled = 0;

        float _averageSpeed = 0;
        if (GameStateManager.Singleton.gameStateSwitcher.TimeInPlayingState > 0)
            _averageSpeed = _distanceTravelled / latestTimeWhenAlive;
        return new StatsSummary()
        {
            DistanceTravelled = _distanceTravelled,
            AverageSpeed = _averageSpeed,
            FastestSpeed = fastestSpeed,
            LapsCompleted = numberOfLaps
        };
    }

    public void HasCompletedLap()
    {
        numberOfLaps += 1;
        PlayerNetworking.localPlayer.myPlayerStateController.HasCompletedLap();
    }

    public static int GetLapIntAroundTrack(Vector3 pos)
    {
        if (GameStateManager.Singleton.railwayPoints != null && GameStateManager.Singleton.railwayPoints.Length > 1)
        {
            Vector3 closestPointPosition = GameStateManager.Singleton.railwayPoints[0];
            int closestPoint = 0;
            float closestDistance = Vector3.Distance(pos, closestPointPosition);

            for (int i = 1; i < GameStateManager.Singleton.railwayPoints.Length; i++)
            {
                float dist = Vector3.Distance(pos, GameStateManager.Singleton.railwayPoints[i]);
                if (dist < closestDistance)
                {
                    closestPoint = i;
                    closestDistance = dist;
                    closestPointPosition = GameStateManager.Singleton.railwayPoints[i];
                }
            }
            int pointA;

            int pointAfter = closestPoint + 1;
            int pointBefore = closestPoint - 1;
            if (pointAfter >= GameStateManager.Singleton.railwayPoints.Length)
                pointAfter -= GameStateManager.Singleton.railwayPoints.Length;
            if (pointBefore <= -1)
                pointBefore += GameStateManager.Singleton.railwayPoints.Length;

            float afterPointDistance = Vector3.Distance(pos, GameStateManager.Singleton.railwayPoints[pointAfter]);
            float beforePointDistance = Vector3.Distance(pos, GameStateManager.Singleton.railwayPoints[pointBefore]);
            if (afterPointDistance < beforePointDistance)
            {
                pointA = closestPoint;
            }
            else
            {
                pointA = pointBefore;
            }
            // Flip result:
            int result = GameStateManager.Singleton.railwayPoints.Length - pointA;
            //if (result == GameStateManager.Singleton.railwayPoints.Length)
            //    result = 0;



            return result - 1;
        }
        return 0;
    }

    public static float GetLapDistanceAroundTrack(Vector3 pos)
    {
        if(GameStateManager.Singleton.railwayPoints != null && GameStateManager.Singleton.railwayPoints.Length > 1)
        {
            Vector3 closestPointPosition = GameStateManager.Singleton.railwayPoints[0];
            int closestPoint = 0;
            float closestDistance = Vector3.Distance(pos, closestPointPosition);

            for (int i = 1; i < GameStateManager.Singleton.railwayPoints.Length; i++)
            {
                float dist = Vector3.Distance(pos, GameStateManager.Singleton.railwayPoints[i]);
                if (dist < closestDistance)
                {
                    closestPoint = i;
                    closestDistance = dist;
                    closestPointPosition = GameStateManager.Singleton.railwayPoints[i];
                }
            }
            int pointA;
            int pointB;

            int pointAfter = closestPoint + 1;
            int pointBefore = closestPoint - 1;
            if (pointAfter >= GameStateManager.Singleton.railwayPoints.Length)
                pointAfter -= GameStateManager.Singleton.railwayPoints.Length;
            if (pointBefore <= -1)
                pointBefore += GameStateManager.Singleton.railwayPoints.Length;

            float afterPointDistance = Vector3.Distance(pos, GameStateManager.Singleton.railwayPoints[pointAfter]);
            float beforePointDistance = Vector3.Distance(pos, GameStateManager.Singleton.railwayPoints[pointBefore]);
            if(afterPointDistance < beforePointDistance)
            {
                pointA = closestPoint;
                pointB = pointAfter;
            }
            else
            {
                pointA = pointBefore;
                pointB = closestPoint;
            }
            //pointA = pointBefore;
            //pointB = pointAfter;

            Vector3 pointAPos = GameStateManager.Singleton.railwayPoints[pointA];
            //Vector3 pointBPos = GameStateManager.Singleton.railwayPoints[pointB];
            Vector3 pointAForwards = GameStateManager.Singleton.railwayDirections[pointA] * Vector3.forward;
            //Vector3 pointAForwards = (pointBPos - pointAPos).normalized;

            //Debug.DrawLine(pointAPos, pointAPos + (pointBPos - pointAPos), Color.red, Time.deltaTime);
            //Debug.DrawLine(pointAPos, pos, Color.green, Time.deltaTime);
            //Debug.DrawLine(pointBPos, pos, Color.blue, Time.deltaTime);

            float distanceAlongPointAForwards = Vector3.Dot(pos - pointAPos, pointAForwards);
            if (distanceAlongPointAForwards < 0)
                distanceAlongPointAForwards = 0;

            float finalDistanceAlong = GameStateManager.Singleton.convertRailwayPointToDistance(pointA) + distanceAlongPointAForwards;
            //Debug.Log(finalDistanceAlong);
            finalDistanceAlong = GameStateManager.Singleton.RailwayLength - finalDistanceAlong;
            if (finalDistanceAlong <= 0)
                finalDistanceAlong = 0;
            // Flip result:
            return finalDistanceAlong;
        }
        return 0;
    }



}
