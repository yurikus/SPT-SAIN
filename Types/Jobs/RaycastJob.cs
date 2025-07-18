using EFT;
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

        public RaycastJob(List<Vector3> Points
        , Vector3 ViewPosition
        , LayerMask InMask
        , IPlayer inOwner
        , IPlayer inTarget
        )
        {
            Owner = inOwner;
            Target = inTarget;
            Mask = InMask;
            TotalRaycasts = Points.Count;
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

        public JobHandle Schedule(int MaxCommandsPerJob = 8)
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
}