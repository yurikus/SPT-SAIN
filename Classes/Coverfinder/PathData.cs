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

        public float PathLength
        {
            get
            {
                return _pathLength;
            }
            set
            {
                RoundedPathLength = Mathf.RoundToInt(value);
                _pathLength = value;
            }
        }

        public int RoundedPathLength { get; private set; }

        private float _pathLength;
    }
}