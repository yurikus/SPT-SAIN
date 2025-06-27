using SAIN.Models.Structs;
using System;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public struct PlaceData
    {
        public Enemy Enemy;
        public bool IsAI;
        public BotComponent Owner;
        public string OwnerID;
    }

    public enum EEnemyPlaceType
    {
        Vision,
        Hearing,
        Flashlight,
        Injury,
    }

    public class EnemyPlace
    {
        public event Action<EnemyPlace> OnPositionUpdated;
        public event Action<EnemyPlace> OnDispose;

        public PlaceData PlaceData { get; }
        public EEnemyPlaceType PlaceType { get; }
        public SAINSoundType? SoundType { get; set; }

        public bool VisibleSourceOnLastUpdate { get; private set; }
        public bool IsDanger { get; set; }

        public bool ShallClear
        {
            get
            {
                var person = PlaceData.Enemy?.EnemyPerson;
                if (person == null)
                {
                    return true;
                }
                var activeClass = person.ActivationClass;
                if (!activeClass.Active || !activeClass.IsAlive)
                {
                    return true;
                }
                if (playerLeftArea)
                {
                    return true;
                }
                return false;
            }
        }

        private bool playerLeftArea
        {
            get
            {
                if (_nextCheckLeaveTime < Time.time)
                {
                    _nextCheckLeaveTime = Time.time + ENEMY_DIST_TO_PLACE_CHECK_FREQ;
                    // If the person this place was created for is AI and left the area, just forget it and move on.
                    float dist = DistanceToEnemyRealPosition;
                    if (PlaceData.IsAI)
                    {
                        return dist > ENEMY_DIST_TO_PLACE_FOR_LEAVE_AI;
                    }
                    return dist > ENEMY_DIST_TO_PLACE_FOR_LEAVE;
                }
                return false;
            }
        }

        private const float ENEMY_DIST_TO_PLACE_CHECK_FREQ = 10;
        private const float ENEMY_DIST_TO_PLACE_FOR_LEAVE = 150;
        private const float ENEMY_DIST_TO_PLACE_FOR_LEAVE_AI = 100f;
        private const float ENEMY_DIST_UPDATE_FREQ = 0.25f;

        public EnemyPlace(PlaceData placeData, Vector3 position, bool isDanger, EEnemyPlaceType placeType, SAINSoundType? soundType)
        {
            PlaceData = placeData;
            VisibleSourceOnLastUpdate = placeData.Enemy.InLineOfSight;
            IsDanger = isDanger;
            PlaceType = placeType;
            SoundType = soundType;

            _position = position;
            updateDistancesNow(position);
            _timeLastUpdated = Time.time;
        }

        public EnemyPlace(PlaceData placeData, SAINHearingReport report)
        {
            PlaceData = placeData;
            VisibleSourceOnLastUpdate = placeData.Enemy.InLineOfSight;
            IsDanger = report.isDanger;
            PlaceType = report.placeType;
            SoundType = report.soundType;

            _position = report.position;
            updateDistancesNow(report.position);
            _timeLastUpdated = Time.time;
        }

        public void Update()
        {
            checkUpdateDistance();
        }

        public void Dispose()
        {
            OnDispose?.Invoke(this);
        }

        public Vector3 GroundedPosition(float range = 2f)
        {
            Vector3 pos = _position;
            if (Physics.Raycast(pos, Vector3.down, out var hit, range, LayerMaskClass.HighPolyWithTerrainMask))
            {
                return hit.point;
            }
            return pos + (Vector3.down * range);
        }


        public Vector3 Position
        {
            get
            {
                return _position;
            }
            set
            {
                checkNewValue(value, _position);
                _position = value;
                _timeLastUpdated = Time.time;
                VisibleSourceOnLastUpdate = PlaceData.Enemy.InLineOfSight;
                OnPositionUpdated?.Invoke(this);
            }
        }

        private void checkNewValue(Vector3 value, Vector3 oldValue)
        {
            if ((value - oldValue).sqrMagnitude > ENEMY_DIST_RECHECK_MIN_SQRMAG)
                updateDistancesNow(value);
        }

        private const float ENEMY_DIST_RECHECK_MIN_SQRMAG = 0.25f;

        public float TimeSincePositionUpdated => Time.time - _timeLastUpdated;
        public float DistanceToBot { get; private set; }
        public float DistanceToEnemyRealPosition { get; private set; }

        private void checkUpdateDistance()
        {
            if (_nextCheckDistTime <= Time.time)
            {
                updateDistancesNow(_position);
            }
        }

        private void updateDistancesNow(Vector3 position)
        {
            _nextCheckSightTime = 0f;
            _nextCheckDistTime = Time.time + ENEMY_DIST_UPDATE_FREQ;
            DistanceToBot = (position - PlaceData.Owner.Position).magnitude;
            DistanceToEnemyRealPosition = (position - PlaceData.Enemy.EnemyTransform.Position).magnitude;
        }

        public float Distance(Vector3 point)
        {
            return (_position - point).magnitude;
        }

        public float DistanceSqr(Vector3 toPoint)
        {
            return (_position - toPoint).sqrMagnitude;
        }

        public bool HasArrivedPersonal
        {
            get
            {
                return _hasArrivedPers;
            }
            set
            {
                if (value)
                {
                    _timeArrivedPers = Time.time;
                    HasSeenPersonal = true;
                }
                _hasArrivedPers = value;
            }
        }

        public bool HasArrivedSquad
        {
            get
            {
                return _hasArrivedSquad;
            }
            set
            {
                if (value)
                {
                    _timeArrivedSquad = Time.time;
                }
                _hasArrivedSquad = value;
            }
        }

        public bool HasSeenPersonal
        {
            get
            {
                return _hasSeenPers;
            }
            set
            {
                if (value)
                {
                    _timeSeenPers = Time.time;
                }
                _hasSeenPers = value;
            }
        }

        public bool HasSeenSquad
        {
            get
            {
                return _hasSquadSeen;
            }
            set
            {
                if (value)
                {
                    _timeSquadSeen = Time.time;
                }
                _hasSquadSeen = value;
            }
        }

        private float _nextCheckDistTime;
        private Vector3 _position;
        private float _nextCheckLeaveTime;
        public float _timeLastUpdated;
        private bool _hasArrivedPers;
        public float _timeArrivedPers;
        private bool _hasArrivedSquad;
        public float _timeArrivedSquad;
        private bool _hasSeenPers;
        public float _timeSeenPers;
        private bool _hasSquadSeen;
        public float _timeSquadSeen;

        public bool CheckLineOfSight(Vector3 origin, LayerMask mask)
        {
            if (_nextCheckSightTime < Time.time)
            {
                _nextCheckSightTime = Time.time + 0.33f;
                Vector3 pos = Position + Vector3.up;
                Vector3 direction = pos - origin;
                _inSightNow = !Physics.Raycast(pos, direction, out var hit, direction.magnitude, mask);
                if (!_inSightNow)
                    BlockedHit = hit;
                else
                    BlockedHit = null;
            }
            return _inSightNow;
        }

        public RaycastHit? BlockedHit { get; private set; }

        private bool _inSightNow;
        private float _nextCheckSightTime;
    }
}