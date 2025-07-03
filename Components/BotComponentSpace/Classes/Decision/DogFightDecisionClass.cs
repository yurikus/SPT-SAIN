using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Decision
{
    public class DogFightDecisionClass : BotBase
    {
        public DogFightDecisionClass(BotComponent bot) : base(bot)
        {
            CanEverTick = false;
        }

        public override void Init()
        {
            Bot.EnemyController.Events.OnEnemyRemoved += checkClear;
            base.Init();
        }

        public override void Dispose()
        {
            Bot.EnemyController.Events.OnEnemyRemoved -= checkClear;
            base.Dispose();
        }

        public bool ShallDogFight()
        {
            if (!BotOwner.WeaponManager.HaveBullets ||
                BotOwner.WeaponManager.Reload.Reloading)
            {
                return false;
            }
            switch (Bot.Decision.CurrentCombatDecision)
            {
                case ECombatDecision.RushEnemy:
                    return false;

                case ECombatDecision.Retreat:
                case ECombatDecision.RunToCover:
                    if (Bot.Decision.SelfActionDecisions.LowOnAmmo(0.2f))
                    {
                        return false;
                    }
                    break;

                default:
                    break;
            }
            if (DogFightTarget != null)
            {
                if (shallDogFightEnemy(DogFightTarget))
                {
                    return true;
                }
                if (shallClearDogfightTarget(DogFightTarget))
                {
                    DogFightTarget = null;
                }
            }

            if (_changeDFTargetTime < Time.time)
            {
                _changeDFTargetTime = Time.time + 0.5f;

                int count = _dogFightTargets.Count;
                for (int i = count - 1; i >= 0; i--)
                {
                    if (shallClearDogfightTarget(_dogFightTargets[i]))
                    {
                        _dogFightTargets.RemoveAt(i);
                    }
                }
                Enemy newTarget = selectDFTarget();
                if (newTarget != null)
                {
                    DogFightTarget = newTarget;
                    return true;
                }

                getNewDFTargets();
                DogFightTarget = selectDFTarget();
            }
            return DogFightTarget != null;
        }

        private void clearDogFightTarget()
        {
            if (DogFightTarget != null)
            {
                DogFightTarget = null;
            }
        }

        private bool shallClearDogfightTarget(Enemy enemy)
        {
            if (enemy == null ||
                !enemy.Seen ||
                !enemy.EnemyKnown ||
                enemy.Player?.HealthController.IsAlive == false)
            {
                return true;
            }
            if (!Bot.EnemyController.Enemies.ContainsValue(enemy) || !enemy.CheckValid())
            {
                return true;
            }
            float pathDist = enemy.Path.PathDistance;
            if (pathDist > _dogFightEndDist)
            {
                return true;
            }
            return !enemy.IsVisible && enemy.TimeSinceSeen > 6f;
        }

        private float _changeDFTargetTime;

        private void getNewDFTargets()
        {
            _dogFightTargets.Clear();

            var enemies = Bot.EnemyController.Enemies;
            foreach (var enemy in enemies.Values)
            {
                if (shallDogFightEnemy(enemy))
                {
                    _dogFightTargets.Add(enemy);
                }
            }
        }

        private Enemy selectDFTarget()
        {
            int count = _dogFightTargets.Count;
            if (count > 0)
            {
                if (count > 1)
                {
                    _dogFightTargets.Sort((x, y) => x.RealDistance.CompareTo(y.RealDistance));
                }
                return _dogFightTargets[0];
            }
            return null;
        }

        private readonly List<Enemy> _dogFightTargets = new();

        public Enemy DogFightTarget { get; set; }

        private void checkClear(string profileID, Enemy enemy)
        {
            if (DogFightTarget != null &&
                DogFightTarget.EnemyProfileId == profileID)
            {
                DogFightTarget = null;
            }
        }

        private bool shallDogFightEnemy(Enemy enemy)
        {
            return enemy != null && 
                Bot.EnemyController.Enemies.ContainsValue(enemy) &&
                enemy?.CheckValid() == true &&
                enemy.IsVisible &&
                enemy.EnemyKnown &&
                enemy.Path.PathDistance <= _dogFightStartDist;
        }

        private float _dogFightStartDist = 8f;
        private float _dogFightEndDist = 15f;
    }
}