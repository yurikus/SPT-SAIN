using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Mover
{
    public enum EDogFightStatus
    {
        None = 0,
        BackingUp = 1,
        MovingToEnemy = 2,
        Shooting = 3,
    }

    public class DogFight : BotBase
    {
        public EDogFightStatus Status { get; private set; }

        public DogFight(BotComponent sain) : base(sain)
        {
        }

        public void ResetDogFightStatus()
        {
            if (Status != EDogFightStatus.None)
                Status = EDogFightStatus.None;
        }

        public void DogFightMove(bool aggressive, Enemy Enemy)
        {
            bool HasEnemy = Enemy != null;
            if (HasEnemy &&
                Enemy.IsVisible &&
                Enemy.CanShoot &&
                Player.IsInPronePose)
            {
                Bot.Mover.Lean.HoldLean(1f);
                return;
            }

            if (Player.IsInPronePose)
            {
                Bot.Mover.Prone.SetProne(false);
            }

            Bot.Mover.SetTargetMoveSpeed(Bot.Info.FileSettings.Move.STRAFE_SPEED);

            if (HasEnemy && stopMoveToShoot(Enemy))
            {
                Status = EDogFightStatus.Shooting;
                Bot.Mover.StopMove(0f);
                float timeAdd = 0.5f * UnityEngine.Random.Range(0.5f, 1.33f);
                Bot.Mover.Lean.HoldLean(timeAdd);
                _updateDogFightTimer = Time.time + timeAdd;
                return;
            }

            if (_updateDogFightTimer > Time.time)
            {
                return;
            }

            if (HasEnemy && !Enemy.IsVisible)
            {
                Bot.Suppression.TrySuppressEnemy(Enemy);
            }

            if (HasEnemy && backUpFromEnemy(Enemy))
            {
                Status = EDogFightStatus.BackingUp;
                float baseTime = Enemy.IsVisible ? 0.75f : 1f;
                _updateDogFightTimer = Time.time + baseTime * UnityEngine.Random.Range(0.66f, 1.33f);
                return;
            }

            if (!aggressive)
            {
                _updateDogFightTimer = Time.time + 0.5f;
                return;
            }

            if (HasEnemy &&
                canMoveToEnemy(Enemy) &&
                Bot.Mover.GoToEnemy(Enemy, -1, false, false))
            {
                Bot.Mover.SetTargetMoveSpeed(0.9f);
                Status = EDogFightStatus.MovingToEnemy;
                float timeAdd = Mathf.Clamp(0.25f * UnityEngine.Random.Range(0.5f, 1.25f), 0.1f, 0.66f);
                _updateDogFightTimer = Time.time + timeAdd;
                return;
            }
            _updateDogFightTimer = Time.time + 0.33f;
        }

        private bool stopMoveToShoot(Enemy Enemy)
        {
            return Status == EDogFightStatus.MovingToEnemy &&
                Enemy.IsVisible && Enemy.CanShoot;
            //&&
            //(Enemy.InLineOfSight || Enemy.IsVisible) &&
            //Enemy.CanShoot;
        }

        private bool backUpFromEnemy(Enemy Enemy)
        {
            if (findStrafePoint(out Vector3 backupPoint, Enemy))
            {
                return true;
            }
            if (findStrafePoint2(out backupPoint, Enemy))
            {
                return Bot.Mover.GoToPoint(backupPoint, out _, -1, false, true, false);
            }
            return false;
        }

        private float _updateDogFightTimer;

        private Vector3? findBackupTarget(Enemy Enemy)
        {
            if (Enemy != null && (
                Enemy.Bot.BotOwner.WeaponManager.Reload.Reloading || 
                !Enemy.Bot.BotOwner.WeaponManager.HaveBullets || 
                (Enemy.Seen && Enemy.TimeSinceSeen < _enemyTimeSinceSeenThreshold) || 
                (!Enemy.Seen && Enemy.LastKnownPosition != null && Enemy.TimeSinceLastKnownUpdated < _enemyTimeSinceSeenThreshold)
                ))
            {
                return Enemy.VisiblePathPoint ?? Enemy.LastKnownPosition ?? Enemy.EnemyTransform.Position;
            }
            return null;
        }

        private bool canMoveToEnemy(Enemy Enemy)
        {
            return Enemy.LastKnownPosition != null && Enemy.Path.PathToEnemy.status != NavMeshPathStatus.PathInvalid;
        }

        private float _enemyTimeSinceSeenThreshold = 1f;

        private bool findStrafePoint(out Vector3 movePosition, Enemy Enemy)
        {
            Vector3? target = findBackupTarget(Enemy);
            if (target != null)
            {
                Vector3 BotPosition = Bot.Position;
                Vector3 targetDirection = target.Value - BotPosition;
                targetDirection.y = 0;
                Vector3 directionAwayFromTargetNormal = -Vector.NormalizeFastSelf(targetDirection);
                Vector3 positionAwayFromTarget = BotPosition + directionAwayFromTargetNormal * 3f;

                const int MaxIterations = 5;
                for (int i = 0; i < MaxIterations; i++)
                {
                    Vector3 random = Random.onUnitSphere * 2f;
                    random.y = Mathf.Clamp(random.y, -0.5f, 0.5f);
                    Vector3 RandomBackupPoint = positionAwayFromTarget + random;
                    if (NavMesh.SamplePosition(RandomBackupPoint, out NavMeshHit navMeshHit, 2f, -1) && (navMeshHit.position - BotPosition).sqrMagnitude > 1)
                    {
                        movePosition = navMeshHit.position;
                        if (Bot.Mover.GoToPoint(movePosition, out _, -1, false, true, false))
                        {
                            return true;
                        }
                    }
                }
            }
            movePosition = Vector3.zero;
            return false;
        }

        private bool findStrafePoint2(out Vector3 MovePoint, Enemy Enemy)
        {
            if (Enemy.Seen && Enemy.TimeSinceSeen < _enemyTimeSinceSeenThreshold * Random.Range(0.66f, 1.33f))
            {
                Vector3? LastKnown = Enemy.LastKnownPosition;
                if (LastKnown != null && NavMesh.SamplePosition(Bot.Position, out NavMeshHit Hit, 0.5f, -1))
                {
                    Vector3 Origin = Hit.position;
                    Vector3 direction = (Origin - LastKnown.Value).normalized;
                    Vector3 random = Random.onUnitSphere * Random.Range(1.25f, 2f);
                    random.y = 0f;
                    Vector3 Point = Origin + (direction * Random.Range(1f, 2f)) + random;
                    if (NavMesh.Raycast(Origin, Point, out NavMeshHit RaycastHit, -1))
                    {
                        if (RaycastHit.distance <= 0.5f)
                        {
                            dogFightPath.ClearCorners();
                            if (NavMesh.CalculatePath(Bot.Position, Point, -1, dogFightPath))
                            {
                                MovePoint = dogFightPath.corners[dogFightPath.corners.Length - 1];
                                return true;
                            }
                        }
                        MovePoint = RaycastHit.position;
                        return true;
                    }
                    MovePoint = Point;
                    return true;
                }
            }
            MovePoint = Bot.Position;
            return false;
        }

        private bool CheckLength(NavMeshPath path, float straighDist)
        {
            return path.CalculatePathLength() < straighDist * 1.5f;
        }

        private readonly NavMeshPath dogFightPath = new();
    }
}