using UnityEngine;

namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public class CoverData
    {
        public Vector3 Position;
        public float TimeLastUpdated;
        public Vector3 ProtectionDirection;
        public float BotDistance;
        public bool IsBad;
        public CoverStatus PathLengthStatus;
        public CoverStatus StraightLengthStatus;
        public float TimeSinceUpdated => Time.time - TimeLastUpdated;
    }
}