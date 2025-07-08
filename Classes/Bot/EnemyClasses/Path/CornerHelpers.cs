using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public static class CornerHelpers
    {
        public static Vector3 EyeLevelCorner(Vector3 eyePos, Vector3 botPosition, Vector3 groundPosition)
        {
            groundPosition.y += (eyePos - botPosition).y;
            return groundPosition;
        }

        public static Vector3 PointPastEyeLevelCorner(Vector3 eyePos, Vector3 botPosition, Vector3 groundPosition)
        {
            Vector3 corner = EyeLevelCorner(eyePos, botPosition, groundPosition);
            return BlindCornerFinder.RaycastPastCorner(corner, eyePos, 0f, 2f);
        }
    }
}