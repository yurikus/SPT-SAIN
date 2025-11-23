using Unity.Collections;
using UnityEngine;

namespace SAIN.Types.Jobs;

public readonly struct RandomDir
{
    public RandomDir(float RandomMin, float RandomMax)
    {
        Magnitude = UnityEngine.Random.Range(RandomMin, RandomMax);
        DirectionNormal = UnityEngine.Random.onUnitSphere;
        Direction = DirectionNormal * Magnitude;
    }

    public RandomDir(float magnitude)
    {
        Magnitude = magnitude;
        DirectionNormal = UnityEngine.Random.onUnitSphere;
        Direction = DirectionNormal * Magnitude;
    }

    public RandomDir(float magnitude, Vector3 directionNormal)
    {
        Magnitude = magnitude;
        DirectionNormal = directionNormal;
        Direction = DirectionNormal * Magnitude;
    }

    public readonly Vector3 Direction;
    public readonly Vector3 DirectionNormal;
    public readonly float Magnitude;
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

    [ReadOnly]
    public NativeArray<Vector3> Directions;

    [WriteOnly]
    public NativeArray<float> Distances;

    public void Execute(int index)
    {
        Distances[index] = Directions[index].magnitude;
    }

    public void Dispose()
    {
        if (Directions.IsCreated)
        {
            Directions.Dispose();
        }

        if (Distances.IsCreated)
        {
            Distances.Dispose();
        }
    }
}
