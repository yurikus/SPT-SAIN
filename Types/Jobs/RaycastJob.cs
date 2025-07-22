using EFT;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace SAIN.Types.Jobs
{
    public struct RaycastJob
        : IRaycastJob
        , IBotRaycastJobSingleOwner
        , IBotRaycastJobSingleTarget
    {
        public RaycastJob(Vector3[] Points
        , Vector3 ViewPosition
        , LayerMask InMask
        , IPlayer inOwner
        , IPlayer inTarget
        )
        {
            Owner = inOwner;
            Target = inTarget;
            TotalRaycasts = Points.Length;
            Mask = InMask;
            Hits = new NativeArray<RaycastHit>(Points.Length, Allocator.TempJob);
            Commands = CreateCommands(Points.Length, Points, ViewPosition, InMask);
        }

        public RaycastJob(List<Vector3> points
        , Vector3 ViewPosition
        , LayerMask InMask
        , IPlayer inOwner
        , IPlayer inTarget
        )
        {
            Owner = inOwner;
            Target = inTarget;
            Mask = InMask;
            TotalRaycasts = points.Count;
            Points = points;
            Hits = new NativeArray<RaycastHit>(TotalRaycasts, Allocator.TempJob);
            Commands = CreateCommands(TotalRaycasts, Points, ViewPosition, InMask);
        }

        public RaycastJob(RandomDir[] Directions
        , Vector3 OriginPoint
        , LayerMask InMask
        , IPlayer inOwner
        , IPlayer inTarget
        )
        {
            Owner = inOwner;
            Target = inTarget;
            Mask = InMask;
            TotalRaycasts = Directions.Length;
            Hits = new NativeArray<RaycastHit>(TotalRaycasts, Allocator.TempJob);
            Commands = CreateCommands(Directions, OriginPoint, InMask);
        }

        public RaycastJob(List<RandomDir> Directions
        , Vector3 OriginPoint
        , LayerMask InMask
        , IPlayer inOwner
        , IPlayer inTarget
        )
        {
            Owner = inOwner;
            Target = inTarget;
            Mask = InMask;
            TotalRaycasts = Directions.Count;
            Hits = new NativeArray<RaycastHit>(TotalRaycasts, Allocator.TempJob);
            Commands = CreateCommands(Directions, OriginPoint, InMask);
        }

        public JobHandle Schedule(int MaxCommandsPerJob = 32)
        {
            Handle = RaycastCommand.ScheduleBatch(Commands, Hits, MaxCommandsPerJob);
            _IsScheduled = true;
            return Handle;
        }

        public readonly void Complete() => Handle.Complete();

        public readonly bool IsCompleted => Handle.IsCompleted;
        public readonly IPlayer Owner { get; }
        public readonly IPlayer Target { get; }
        public readonly bool IsCreated => Hits.IsCreated || Commands.IsCreated;
        public readonly int TotalRaycasts { get; }
        public readonly bool IsScheduled => !IsCompleted && _IsScheduled;
        public LayerMask Mask { get; }
        public NativeArray<RaycastHit> Hits { get; }
        public NativeArray<RaycastCommand> Commands { get; }
        public JobHandle Handle { get; private set; }

        public readonly void Dispose()
        {
            if (!IsCompleted) Complete();
            if (Hits.IsCreated) Hits.Dispose();
            if (Commands.IsCreated) Commands.Dispose();
        }

        public int OffsetCount;
        private bool _IsScheduled;
        internal List<Vector3> Points;

        private static NativeArray<RaycastCommand> CreateCommands(int Count, Vector3[] Points, Vector3 ViewPosition, LayerMask Mask)
        {
            var Result = new NativeArray<RaycastCommand>(Count, Allocator.TempJob);
            for (int i = 0; i < Count; i++)
            {
                Vector3 Direction = Points[i] - ViewPosition;
                Result[i] = new RaycastCommand(ViewPosition, Direction.normalized, new QueryParameters {
                    layerMask = Mask
                }, Direction.magnitude);
            }
            return Result;
        }

        private static NativeArray<RaycastCommand> CreateCommands(int Count, List<Vector3> Points, Vector3 ViewPosition, LayerMask Mask)
        {
            var Result = new NativeArray<RaycastCommand>(Count, Allocator.TempJob);
            for (int i = 0; i < Count; i++)
            {
                Vector3 Direction = Points[i] - ViewPosition;
                Result[i] = new RaycastCommand(ViewPosition, Direction, new QueryParameters {
                    layerMask = Mask
                }, 1f);
            }
            return Result;
        }

        private static NativeArray<RaycastCommand> CreateCommands(RandomDir[] Points, Vector3 ViewPosition, LayerMask Mask)
        {
            int Count = Points.Length;
            var Result = new NativeArray<RaycastCommand>(Count, Allocator.TempJob);
            for (int i = 0; i < Count; i++)
            {
                RandomDir Direction = Points[i];
                Result[i] = new RaycastCommand(ViewPosition, Direction.DirectionNormal, new QueryParameters {
                    layerMask = Mask
                }, Direction.Magnitude);
            }
            return Result;
        }

        private static NativeArray<RaycastCommand> CreateCommands(List<RandomDir> Points, Vector3 ViewPosition, LayerMask Mask)
        {
            int Count = Points.Count;
            var Result = new NativeArray<RaycastCommand>(Count, Allocator.TempJob);
            for (int i = 0; i < Count; i++)
            {
                RandomDir Direction = Points[i];
                Result[i] = new RaycastCommand(ViewPosition, Direction.DirectionNormal, new QueryParameters {
                    layerMask = Mask
                }, Direction.Magnitude);
            }
            return Result;
        }
    }

    public struct PathVisionJob
    {
        public PathVisionJob(List<Vector3> points, Vector3 origin, Enemy enemy, QueryParameters queryParameters)
        {
            Enemy = enemy;
            int pointCount = points.Count;
            NativeArray<RaycastCommand> commands = new(pointCount, Allocator.TempJob);
            for (int i = 0; i < pointCount; i++)
            {
                commands[i] = new RaycastCommand(origin, (points[i] - origin), queryParameters, 1f);
            }
            Commands = commands;
            Hits = new NativeArray<RaycastHit>(pointCount, Allocator.TempJob);
        }

        public void Schedule(int minCommandsPerJob = -1)
        {
            if (minCommandsPerJob < 0) minCommandsPerJob = Commands.Length;
            Handle = RaycastCommand.ScheduleBatch(Commands, Hits, minCommandsPerJob);
        }

        public void Complete()
        {
            JobHandle handle = Handle;
            if (!handle.IsCompleted)
            {
                Logger.LogDebug("job was not complete yet, finished anyways.");
                handle.Complete();
            }
            Handle = handle;
        }

        public readonly Enemy Enemy { get; }

        public JobHandle Handle { get; private set; }
        public NativeArray<RaycastHit> Hits { get; private set; }
        public NativeArray<RaycastCommand> Commands { get; private set; }

        public void Dispose()
        {
            Complete();
            if (Hits.IsCreated) Hits.Dispose();
            if (Commands.IsCreated) Commands.Dispose();
        }
    }
}