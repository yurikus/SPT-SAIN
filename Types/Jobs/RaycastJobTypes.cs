using EFT;
using SAIN.Components;
using SAIN.Helpers;
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
            TotalRaycasts = Points.Count;
            this.Points = Points;
            Mask = InMask;
            Hits = new NativeArray<RaycastHit>(Points.Count, Allocator.TempJob);
            Commands = CreateCommands(Points.Count, Points, ViewPosition, InMask);
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

        public List<Vector3> Points;
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
    
    public struct NavMeshPathRaycastJob(
        Vector3[] PathCorners,
        Vector3[] inOffsets,
        Vector3[] inRaycastPoints,
        Vector3 InViewPosition,
        LayerMask Mask,
        IPlayer inOwner,
        IPlayer inTarget)
        : IBotRaycastJobSingleOwner
        , IBotRaycastJobSingleTarget
    {
        public Vector3 ViewPosition = InViewPosition;
        public int CornerCount = PathCorners.Length;
        public int OffsetCount = inOffsets.Length;
        public RaycastJob RaycastJob = new(inRaycastPoints, InViewPosition, Mask, inOwner, inTarget);
        
        public readonly IPlayer Owner => RaycastJob.Owner;
        public readonly IPlayer Target => RaycastJob.Target;
        public readonly Vector3[] Corners { get; } = PathCorners;
        public readonly Vector3[] Offsets { get; } = inOffsets;
        public readonly Vector3[] RaycastPoints { get; } = inRaycastPoints;

        public int GetCornerFromHitIndex(int hitIndex)
        {
            int TotalCount = RaycastPoints.Length;
            if (hitIndex >= TotalCount)
            {
                Logger.LogError("dumbass 1");
                return -1;
            }
            int ExpectedCornerIndex = hitIndex / OffsetCount;
            if (ExpectedCornerIndex >= CornerCount)
            {
                Logger.LogError("dumbass 2");
                return -1;
            }
            return ExpectedCornerIndex;
        }

        public static NavMeshPathRaycastJob Create(Vector3[] PathCorners, int InOffsetCount, LayerMask Mask, IPlayer inOwner, IPlayer inTarget)
        {
            Vector3 ViewPosition = inOwner.MainParts[BodyPartType.head].Position;
            const float characterHeight = 1.65f;
            Vector3[] inOffsets = CreateOffsets(InOffsetCount, characterHeight);
            Vector3[] inRaycastPoints = CreateVectorArray(PathCorners, inOffsets);
            return new NavMeshPathRaycastJob(PathCorners, inOffsets, inRaycastPoints, ViewPosition, Mask, inOwner, inTarget);
        }

        public static Vector3[] CreateVectorArray(Vector3[] PathCorners, Vector3[] Offsets)
        {
            Vector3[] Result = new Vector3[PathCorners.Length * Offsets.Length];
            int Index = 0;
            for (int i = 0; i < PathCorners.Length; i++)
            {
                for (int j = 0; j < Offsets.Length; j++)
                {
                    Result[Index] = PathCorners[i] + Offsets[j];
                    Index++;
                }
            }
            return Result;
        }

        public static Vector3[] CreateOffsets(int Count, float Extent)
        {
            Vector3 Segment = Vector3.up * (Extent / Count);
            Vector3[] Result = new Vector3[Count];
            Vector3 Offset = Vector3.zero;
            for (int i = 0; i < Count; i++)
            {
                Offset += Segment;
                Result[i] = Offset;
            }
            return Result;
        }
    }
}