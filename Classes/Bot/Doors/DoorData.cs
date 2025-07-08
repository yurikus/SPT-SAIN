using EFT.Interactive;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class DoorData
    {
        private const float DOOR_SINGLE_INTERACTION_FREQ = 1f;
        private const float UPDATE_SQRMAG_FREQ = 0.5f;

        public DoorData(NavMeshDoorLink link)
        {
            Link = link;
            LinkPosition = link.transform.position;
            Door = link.Door;
        }

        public bool CanInteractByTime()
        {
            return LastInteractTime + DOOR_SINGLE_INTERACTION_FREQ < Time.time;
        }

        public float LastInteractTime { get; set; }
        public float LastOpenTime { get; set; }
        public float LastCloseTime { get; set; }

        public NavMeshDoorLink Link { get; }
        public Vector3 LinkPosition { get; }
        public Door Door { get; }

        public float CurrentSqrMagnitude
        {
            get
            {
                if (_nextCheckSqrMagTime < Time.time)
                {
                    _nextCheckSqrMagTime = Time.time + UPDATE_SQRMAG_FREQ;
                    LastSqrMagnitude = Direction.sqrMagnitude;
                }
                return LastSqrMagnitude;
            }
        }

        public void CalcDirection(Vector3 from)
        {
            if (Time.frameCount != _lastCalcFrame)
            {
                _lastCalcFrame = Time.frameCount;
                Direction = CenterPoint - from;
                DirectionNormal = Direction.normalized;
            }
        }

        private int _lastCalcFrame;

        public Vector3 CenterPoint
        {
            get
            {
                switch (Door.DoorState)
                {
                    case EDoorState.Open:
                        return Link.MidOpen;
                    default:
                        return Link.MidClose;
                }
            }
        }

        public float LastSqrMagnitude { get; private set; }

        private float _nextCheckSqrMagTime;

        public Vector3 Direction { get; private set; }
        public Vector3 DirectionNormal { get; private set; }

        public float DotProduct { get; set; }
        public bool DoorInFront => DotProduct > 0;
    }
}