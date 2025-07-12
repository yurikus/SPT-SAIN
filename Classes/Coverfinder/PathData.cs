using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public class PathData
    {
        public PathData(NavMeshPath path)
        {
            Path = path;
        }

        public NavMeshPath Path { get; }

        public float PathLength {
            get
            {
                return _pathLength;
            }
            set
            {
                TimeLastUpdated = value;
                RoundedPathLength = Mathf.FloorToInt(value);
                _pathLength = value;
            }
        }

        public float TimeSinceLastUpdated => Time.time - TimeLastUpdated;
        public float TimeLastUpdated { get; private set; }

        public int RoundedPathLength { get; private set; }

        private float _pathLength;
    }
}