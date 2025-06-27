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

        public void DogFightMove(bool aggressive)
        {
            if (Player.IsInPronePose &&
                Bot.Enemy?.IsVisible == true &&
                Bot.Enemy.CanShoot)
            {
                Bot.Mover.Lean.HoldLean(1f);
                return;
            }

            if (stopMoveToShoot())
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

            if (backUpFromEnemy())
            {
                Status = EDogFightStatus.BackingUp;
                float baseTime = Bot.Enemy?.IsVisible == true ? 0.5f : 0.75f;
                _updateDogFightTimer = Time.time + baseTime * UnityEngine.Random.Range(0.66f, 1.33f);
                return;
            }

            if (!aggressive)
            {
                _updateDogFightTimer = Time.time + 0.5f;
                return;
            }

            if (canMoveToEnemy() &&
                Bot.Mover.GoToEnemy(Bot.Enemy, -1, false, false))
            {
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

        private bool stopMoveToShoot()
        {
            Enemy enemy = Bot.Enemy;
            if (enemy == null)
            {
                return false;
            }
            return Status == EDogFightStatus.MovingToEnemy &&
                (enemy.InLineOfSight || enemy.IsVisible) &&
                enemy.CanShoot;
        }

        private bool backUpFromEnemy()
        {
            return
                findStrafePoint(out Vector3 backupPoint) &&
                Bot.Mover.GoToPoint(backupPoint, out _, -1, false, false);
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

        private bool canMoveToEnemy()
        {
            Enemy enemy = Bot.Enemy;
            if (enemy != null &&
                //enemy.Seen &&
                //enemy.TimeSinceSeen >= _enemyTimeSinceSeenThreshold &&
                enemy.Path.PathToEnemy.status != NavMeshPathStatus.PathInvalid)
            {
                return true;
            }
            return false;
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

        private bool CheckLength(NavMeshPath path, float straighDist)
        {
            return path.CalculatePathLength() < straighDist * 1.5f;
        }

        private readonly NavMeshPath dogFightPath = new();
    }
}