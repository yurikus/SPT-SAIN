using Unity.Collections;
using UnityEngine;

namespace SAIN.Types.Jobs
{
    public struct CalcDistanceAndNormalJob : IDisposableJobFor
    {
        [ReadOnly] public NativeArray<Vector3> Directions;

        [WriteOnly] public NativeArray<float> Distances;
        [WriteOnly] public NativeArray<Vector3> Normals;

        public void Execute(int index)
        {
            Vector3 direction = Directions[index];
            Distances[index] = direction.magnitude;
            Normals[index] = direction.normalized;
        }

        public void Dispose()
        {
            if (Directions.IsCreated) Directions.Dispose();
            if (Distances.IsCreated) Distances.Dispose();
            if (Normals.IsCreated) Normals.Dispose();
        }
    }

    public struct CalcDistanceJob : IDisposableJobFor
    {
        public static CalcDistanceJob Create(Vector3 Origin, Vector3[] Points)
        {
            CalcDistanceJob Result = new();
            int Count = Points.Length;
            Result.Directions = new NativeArray<Vector3>(Count, Allocator.TempJob);
            for (int i = 0; i < Count; i++)
            {
                Result.Directions[i] = Points[i] - Origin;
            }
            Result.Distances = new NativeArray<float>(Count, Allocator.TempJob);
            return Result;
        }

        public static CalcDistanceJob Create(Vector3[] Origins, Vector3[] Points)
        {
            CalcDistanceJob Result = new();
            int Count = Points.Length;
            Result.Directions = new NativeArray<Vector3>(Count, Allocator.TempJob);
            for (int i = 0; i < Count; i++)
            {
                Result.Directions[i] = Points[i] - Origins[i];
            }
            Result.Distances = new NativeArray<float>(Count, Allocator.TempJob);
            return Result;
        }

        public static CalcDistanceJob Create(Vector3[] Origins, Vector3 Point)
        {
            CalcDistanceJob Result = new();
            int Count = Origins.Length;
            Result.Directions = new NativeArray<Vector3>(Count, Allocator.TempJob);
            for (int i = 0; i < Count; i++)
            {
                Result.Directions[i] = Point - Origins[i];
            }
            Result.Distances = new NativeArray<float>(Count, Allocator.TempJob);
            return Result;
        }

        [ReadOnly] public NativeArray<Vector3> Directions;
        [WriteOnly] public NativeArray<float> Distances;

        public void Execute(int index)
        {
            Distances[index] = Directions[index].magnitude;
        }

        public void Dispose()
        {
            if (Directions.IsCreated) Directions.Dispose();
            if (Distances.IsCreated) Distances.Dispose();
        }
    }

    public struct CalcEnemyPlaceJob : IDisposableJobFor
    {
        [ReadOnly] public NativeArray<Vector3> PlacePositions;
        [ReadOnly] public NativeArray<Vector3> BotPositions;
        [ReadOnly] public NativeArray<Vector3> EnemyPositions;
        [WriteOnly] public NativeArray<float> PlaceDistancesToBot;
        [WriteOnly] public NativeArray<float> PlaceDistancesToEnemy;

        public void Execute(int index)
        {
            Vector3 EnemyPlace = PlacePositions[index];
            PlaceDistancesToBot[index] = (BotPositions[index] - EnemyPlace).magnitude;
            PlaceDistancesToEnemy[index] = (EnemyPositions[index] - EnemyPlace).magnitude;
        }

        public void Dispose()
        {
            if (PlacePositions.IsCreated) PlacePositions.Dispose();
            if (BotPositions.IsCreated) BotPositions.Dispose();
            if (EnemyPositions.IsCreated) EnemyPositions.Dispose();
            if (PlaceDistancesToBot.IsCreated) PlaceDistancesToBot.Dispose();
            if (PlaceDistancesToEnemy.IsCreated) PlaceDistancesToEnemy.Dispose();
        }
    }
}