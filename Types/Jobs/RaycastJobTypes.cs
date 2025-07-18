using EFT;
using System;
using Unity.Collections;
using UnityEngine;

namespace SAIN.Types.Jobs
{
    public struct BotVisiblePathPoint(Vector3 point)
    {
        public Vector3 Point = point;
        public bool IsVisible = false;
        public float TimeLastVisible = 0;
    }

    public struct BotVisiblePathNode : IDisposable
    {
        public Vector3 GroundPosition;
        public NativeArray<BotVisiblePathPoint> PointStack;
        public Vector3 DirectionToCornerNormal;
        public bool IsVisible;
        public float TimeLastVisible = 0;

        public BotVisiblePathNode(Vector3 bottomPoint, Vector3 directionToCornerNormal, float characterHeight, int points)
        {
            GroundPosition = bottomPoint;
            DirectionToCornerNormal = directionToCornerNormal;
            float spacing = characterHeight / points;
            Vector3 step = Vector3.up * spacing;
            PointStack = new NativeArray<BotVisiblePathPoint>(points + 1, Allocator.Persistent);
            for (int i = 0; i <= points; i++)
            {
                PointStack[i] = new(bottomPoint + step * i);
            }
        }

        public void Dispose()
        {
            if (PointStack.IsCreated)
            {
                PointStack.Dispose();
            }
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