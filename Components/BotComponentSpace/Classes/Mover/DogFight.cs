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

        private bool swingRight;

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

            if (HasEnemy && backUpFromEnemy(Enemy))
            {
                Status = EDogFightStatus.BackingUp;
                float baseTime = Bot.Enemy?.IsVisible == true ? 0.75f : 1f;
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
                float timeAdd = Mathf.Clamp(0.1f * UnityEngine.Random.Range(0.5f, 1.25f), 0.05f, 0.66f);
                _updateDogFightTimer = Time.time + timeAdd;
                return;
            }
            _updateDogFightTimer = Time.time + 0.2f;
        }

        private bool swing()
        {
            swingRight = EFTMath.RandomBool();
            return false;
        }

        private bool stopMoveToShoot(Enemy Enemy)
        {
            return Status == EDogFightStatus.MovingToEnemy &&
                (Enemy.InLineOfSight || Enemy.IsVisible) &&
                Enemy.CanShoot;
        }

        private bool backUpFromEnemy(Enemy Enemy)
        {
            return
                findStrafePoint2(out Vector3 backupPoint, Enemy) &&
                Bot.Mover.GoToPoint(backupPoint, out _, -1, false, true, false);
        }

        private float _updateDogFightTimer;

        private Vector3? findBackupTarget()
        {
            Enemy enemy = Bot.Enemy;
            if (enemy != null &&
                enemy.Seen &&
                enemy.TimeSinceSeen < _enemyTimeSinceSeenThreshold)
            {
                return Bot.Enemy.EnemyPosition;
            }
            return null;
        }

        private bool canMoveToEnemy(Enemy Enemy)
        {
            return Enemy.LastKnownPosition != null && Enemy.Path.PathToEnemy.status != NavMeshPathStatus.PathInvalid;
        }

        private float _enemyTimeSinceSeenThreshold = 1f;

        private bool findStrafePoint(out Vector3 trgPos)
        {
            Vector3? target = findBackupTarget();
            if (target == null)
            {
                trgPos = Vector3.zero;
                return false;
            }
            Vector3 direction = target.Value - Bot.Position;

            Vector3 a = -Vector.NormalizeFastSelf(direction);
            trgPos = Vector3.zero;
            float num = 0f;
            Vector3 random = Random.onUnitSphere * 1.25f;
            random.y = 0f;
            if (NavMesh.SamplePosition(Bot.Position + a * 2f / 2f + random, out NavMeshHit navMeshHit, 1f, -1))
            {
                trgPos = navMeshHit.position;
                Vector3 a2 = trgPos - Bot.Position;
                float magnitude = a2.magnitude;
                if (magnitude != 0f)
                {
                    Vector3 a3 = a2 / magnitude;
                    num = magnitude;
                    if (NavMesh.SamplePosition(Bot.Position + a3 * 2f, out navMeshHit, 1f, -1))
                    {
                        trgPos = navMeshHit.position;
                        num = (trgPos - Bot.Position).magnitude;
                    }
                }
            }
            if (num != 0f && num > BotOwner.Settings.FileSettings.Move.REACH_DIST)
            {
                dogFightPath.ClearCorners();
                if (NavMesh.CalculatePath(Bot.Position, trgPos, -1, dogFightPath) && dogFightPath.status == NavMeshPathStatus.PathComplete)
                {
                    trgPos = dogFightPath.corners[dogFightPath.corners.Length - 1];
                    return CheckLength(dogFightPath, num);
                }
            }
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