using EFT;
using SAIN.Helpers;
using SAIN.Models.Enums;
using SAIN.Preset;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class SAINSteeringClass : BotComponentClassBase
    {
        public SAINSteeringClass(BotComponent sain) : base(sain)
        {
            TickRequirement = ESAINTickState.OnlyNoSleep;
            _randomLook = new RandomLookClass(this);
            _steerPriorityClass = new SteerPriorityClass(this);
            HeardSoundSteering = new HeardSoundSteeringClass(this);
        }

        public ESteerPriority CurrentSteerPriority => _steerPriorityClass.CurrentSteerPriority;
        public ESteerPriority LastSteerPriority => _steerPriorityClass.LastSteerPriority;
        public EEnemySteerDir EnemySteerDir { get; private set; }

        public Vector3 WeaponRootOffset {
            get
            {
                // Lower the position slightly to avoid having bots accidently aim for your head
                return new Vector3(0, BotOwner.WeaponRoot.position.y - Bot.Position.y - 0.1f, 0);
            }
        }

        public bool SteerByPriority(Enemy enemy = null, bool lookRandom = true, bool ignoreRunningPath = false)
        {
            enemy ??= Bot.CurrentTarget.CurrentTargetEnemy;

            switch (_steerPriorityClass.GetCurrentSteerPriority(lookRandom, ignoreRunningPath))
            {
                case ESteerPriority.RunningPath:
                case ESteerPriority.Aiming:
                    return true;

                case ESteerPriority.ManualShooting:
                    LookToPoint(Bot.ManualShoot.ShootPosition + Bot.Info.WeaponInfo.Recoil.CurrentRecoilOffset);
                    return true;

                case ESteerPriority.EnemyVisible:
                    LookToEnemy(enemy);
                    return true;

                case ESteerPriority.UnderFire:
                    LookToUnderFirePos();
                    return true;

                case ESteerPriority.LastHit:
                    LookToLastHitPos();
                    return true;

                case ESteerPriority.EnemyLastKnownLong:
                case ESteerPriority.EnemyLastKnown:
                    if (!LookToLastKnownEnemyPosition(enemy))
                    {
                        LookToRandomPosition();
                    }
                    return true;

                case ESteerPriority.HeardThreat:
                    HeardSoundSteering.LookToHeardPosition();
                    return true;

                case ESteerPriority.Sprinting:
                    //LookToMovingDirection();
                    return true;

                case ESteerPriority.RandomLook:
                    LookToRandomPosition();
                    return true;

                default:
                    return false;
            }
        }

        public bool LookToLastKnownEnemyPosition(Enemy enemy)
        {
            if (FindLastKnownTarget(enemy, out Vector3 Position))
            {
                LookToPoint(Position);
                return true;
            }
            return false;
        }

        public bool LookToMovingDirection(bool sprint = false)
        {
            var Steering = BotOwner.Steering;
            if (Steering == null) return false;
            if (BotOwner?.Mover?.HasPathAndNoComplete == true)
            {
                Vector3 CurrentCorner = BotOwner.Mover.CurrentCornerPoint;
                Vector3 WeaponRoot = BotOwner.WeaponRoot.position;
                Vector3 Dir = CurrentCorner + WeaponRootOffset - WeaponRoot;
                if (Dir.sqrMagnitude > 0.25f * 0.25f)
                {
                    Steering.LookToDirection(Dir);
                    return true;
                }
            }
            return false;
        }

        public void LookToPoint(Vector3 point)
        {
            Vector3 direction = point - BotOwner.WeaponRoot.position;
            if (direction.sqrMagnitude < 1f)
            {
                direction = direction.normalized;
            }
            LookToDirection(direction, false);
        }

        public void LookToDirection(Vector3 direction, bool flat = false)
        {
            if (flat)
            {
                direction.y = 0;
            }
            BotOwner.Steering.LookToDirection(direction, float.MaxValue);
        }

        public void LookToEnemy(Enemy enemy)
        {
            if (enemy != null)
            {
                LookToPoint(enemy.EnemyPosition + WeaponRootOffset);
            }
        }

        public void LookToRandomPosition()
        {
            Vector3? point = _randomLook.UpdateRandomLook();
            if (point != null)
            {
                LookToPoint(point.Value);
            }
        }

        public float AngleToPointFromLookDir(Vector3 point)
        {
            Vector3 direction = (point - BotOwner.WeaponRoot.position).normalized;
            return Vector3.Angle(_lookDirection, direction);
        }

        public float AngleToDirectionFromLookDir(Vector3 direction)
        {
            return Vector3.Angle(_lookDirection, direction);
        }

        public override void Init()
        {
            HeardSoundSteering.Init();
            base.Init();
        }

        public override void ManualUpdate()
        {
            base.ManualUpdate();
            HeardSoundSteering.ManualUpdate();
        }

        public override void Dispose()
        {
            HeardSoundSteering.Dispose();
            base.Dispose();
        }

        public bool FindLastKnownTarget(Enemy enemy, out Vector3 Result)
        {
            if (enemy == null)
            {
                EnemySteerDir = EEnemySteerDir.NullEnemy_ERROR;
                Result = Vector3.zero;
                return false;
            }
            if (enemy.FindLookPoint(out Vector3 Position, out EEnemySteerDir EnumValue))
            {
                EnemySteerDir = EnumValue;
                Result = Position;
                return true;
            }
            EnemySteerDir = EEnemySteerDir.None;
            Result = Vector3.zero;
            return false;
        }

        private void LookToUnderFirePos()
        {
            LookToPoint(Bot.Memory.UnderFireFromPosition + WeaponRootOffset);
        }

        private void LookToLastHitPos()
        {
            var enemyWhoShotMe = _steerPriorityClass.EnemyWhoLastShotMe;
            if (enemyWhoShotMe != null)
            {
                if (FindLastKnownTarget(enemyWhoShotMe, out Vector3 Result))
                {
                    LookToPoint(Result);
                    return;
                }
                var lastShotPos = enemyWhoShotMe.Status.LastShotPosition;
                if (lastShotPos != null)
                {
                    LookToPoint(lastShotPos.Value + WeaponRootOffset);
                    return;
                }
            }
            LookToRandomPosition();
        }

        public HeardSoundSteeringClass HeardSoundSteering { get; }
        private readonly RandomLookClass _randomLook;
        private readonly SteerPriorityClass _steerPriorityClass;

        private Vector3 _lookDirection => Bot.LookDirection;
    }
}