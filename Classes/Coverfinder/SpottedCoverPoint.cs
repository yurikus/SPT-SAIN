using UnityEngine;

namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public class SpottedCoverPoint
    {
        public const float SPOTTED_PERIOD = 2f;

        public SpottedCoverPoint(CoverPoint coverPoint)
        {
            ExpireTime = SPOTTED_PERIOD;
            CoverPoint = coverPoint;
            TimeCreated = Time.time;
        }

        public bool TooClose(Vector3 coverInfoPosition, Vector3 newPos, float sqrdist = 2f)
        {
            return (coverInfoPosition - newPos).sqrMagnitude > sqrdist;
        }

        public CoverPoint CoverPoint { get; private set; }
        public float TimeCreated { get; private set; }
        public float TimeSinceCreated => Time.time - TimeCreated;

        private readonly float ExpireTime;
        public bool IsValidAgain => TimeSinceCreated > ExpireTime;
    }
}