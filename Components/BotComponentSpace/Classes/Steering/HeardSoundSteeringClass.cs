using EFT;
using SAIN.Helpers;
using SAIN.Models.Enums;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Mover
{
    public struct SoundStruct
    {
        private const float TIME_TO_LOOK = 3f;
        private const float TIME_TO_CLEAR = 10f;

        public SoundStruct(Enemy enemy, EnemyPlace place)
        {
            Enemy = enemy;
            Place = place;
        }

        public readonly Enemy Enemy;

        public readonly EnemyPlace Place;

        public float TimeSinceHeard => Place.TimeSincePositionUpdated;

        public Vector3 Position => Place.Position;

        public bool ShallLook =>
            clearForLook &&
            TimeSinceHeard < TIME_TO_LOOK;

        private bool clearForLook => Place != null && Enemy != null && Enemy.WasValid;

        public bool ShallClear => !clearForLook || TimeSinceHeard >= TIME_TO_CLEAR;
    }

    public class HeardSoundSteeringClass : BotSubClass<SAINSteeringClass>, IBotClass
    {
        public bool HasDangerToLookAt => LastHeardVisibleDanger?.ShallLook == true || LastHeardDanger?.ShallLook == true;
        public SoundStruct? LastHeardDanger { get; private set; }
        public SoundStruct? LastHeardVisibleDanger { get; private set; }

        public HeardSoundSteeringClass(SAINSteeringClass steering) : base(steering)
        {
            _hearingPath = new NavMeshPath();
        }

        public void Init()
        {
            Bot.EnemyController.Events.OnEnemyHeard += enemyHeard;
        }

        public void Update()
        {
            checkClearPlaces();
        }

        private void checkClearPlaces()
        {
            if (_nextCheckClearTime < Time.time)
            {
                _nextCheckClearTime = Time.time + CHECK_CLEAR_FREQ;
                if (LastHeardDanger?.ShallClear == true)
                {
                    clearPlace(LastHeardDanger.Value.Place);
                }
                if (LastHeardVisibleDanger?.ShallClear == true)
                {
                    clearPlace(LastHeardVisibleDanger.Value.Place);
                }
            }
        }

        private float _nextCheckClearTime;
        private const float CHECK_CLEAR_FREQ = 1f;

        private void clearPlace(EnemyPlace place)
        {
            if (place == null)
            {
                return;
            }
            if (LastHeardDanger?.Place == place)
            {
                LastHeardDanger.Value.Place.OnDispose -= clearPlace;
                LastHeardDanger = null;
            }
            if (LastHeardVisibleDanger?.Place == place)
            {
                LastHeardVisibleDanger.Value.Place.OnDispose -= clearPlace;
                LastHeardVisibleDanger = null;
            }
        }

        public void Dispose()
        {
            Bot.EnemyController.Events.OnEnemyHeard -= enemyHeard;
        }

        public void LookToHeardPosition()
        {
            var visibleDanger = LastHeardVisibleDanger;
            if (visibleDanger?.ShallLook == true)
            {
                BaseClass.LookToPoint(visibleDanger.Value.Position + BaseClass.WeaponRootOffset);
                return;
            }
            var heardSound = LastHeardDanger;
            if (heardSound?.ShallLook == true)
            {
                if (heardSound.Value.Enemy.InLineOfSight)
                {
                    BaseClass.LookToPoint(heardSound.Value.Position + BaseClass.WeaponRootOffset);
                    return;
                }
                heardSound.Value.Enemy.Path.EnemyCorners.TryGetValue(ECornerType.First, out EnemyCorner corner);
                if (corner != null)
                {
                    BaseClass.LookToPoint(corner.EyeLevelCorner(Bot.Transform.WeaponRoot, Bot.Position));
                    return;
                }
                BaseClass.LookToPoint(heardSound.Value.Position + BaseClass.WeaponRootOffset);
                return;
            }

            BaseClass.LookToRandomPosition();
        }

        private void enemyHeard(Enemy enemy, SAINSoundType soundType, bool isDanger, EnemyPlace place)
        {
            if (place == null)
            {
                return;
            }
            if (!place.VisibleSourceOnLastUpdate)
            {
                return;
            }
            if (!isDanger)
            {
                return;
            }
            if (place.PlaceData.OwnerID == Bot.ProfileId)
            {
                setLastVisSound(place, enemy);
                return;
            }
            var myEnemy = Bot.EnemyController.GetEnemy(enemy.EnemyProfileId, true);
            if (myEnemy == null)
            {
                return;
            }
            if (myEnemy.InLineOfSight)
            {
                setLastVisSound(place, myEnemy);
                return;
            }
        }

        private void setLastVisSound(EnemyPlace place, Enemy enemy)
        {
            if (place.VisibleSourceOnLastUpdate && place.PlaceData.OwnerID == Bot.ProfileId)
            {
                LastHeardVisibleDanger = new SoundStruct(enemy, place);
                return;
            }
            var activeEnemy = Bot.Enemy;
            if (activeEnemy == null || enemy.IsDifferent(activeEnemy))
            {
                LastHeardDanger = new SoundStruct(enemy, place);
            }
        }

        public void LookToHeardPosition(Vector3 soundPos, bool visionCheck = false)
        {
            if ((soundPos - Bot.Position).sqrMagnitude > 125f.Sqr())
            {
                BaseClass.LookToPoint(soundPos);
                return;
            }

            findCorner(soundPos);

            if (_lastHeardSoundCorner != null)
            {
                BaseClass.LookToPoint(_lastHeardSoundCorner.Value);
            }
            else
            {
                BaseClass.LookToPoint(soundPos);
            }
        }

        private void findCorner(Vector3 soundPos)
        {
            if (_lastHeardSoundTimer < Time.time || (_lastHeardSoundCheckedPos - soundPos).magnitude > 1f)
            {
                _lastHeardSoundTimer = Time.time + 1f;
                _lastHeardSoundCheckedPos = soundPos;
                _hearingPath.ClearCorners();
                if (NavMesh.CalculatePath(Bot.Position, soundPos, -1, _hearingPath))
                {
                    if (_hearingPath.corners.Length > 2)
                    {
                        Vector3 headPos = BotOwner.LookSensor._headPoint;
                        for (int i = _hearingPath.corners.Length - 1; i >= 0; i--)
                        {
                            Vector3 corner = _hearingPath.corners[i] + Vector3.up;
                            Vector3 cornerDir = corner - headPos;
                            if (!Physics.Raycast(headPos, cornerDir.normalized, cornerDir.magnitude, LayerMaskClass.HighPolyWithTerrainMask))
                            {
                                _lastHeardSoundCorner = corner;
                                return;
                            }
                        }
                    }
                }
                _lastHeardSoundCorner = null;
            }
        }

        private float _lastHeardSoundTimer;
        private Vector3 _lastHeardSoundCheckedPos;
        private Vector3? _lastHeardSoundCorner;
        private NavMeshPath _hearingPath;
    }
}