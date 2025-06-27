using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Decision
{
    public class DogFightDecisionClass : BotBase, IBotClass
    {
        public DogFightDecisionClass(BotComponent bot) : base(bot) { }

        public void Init()
        {
            base.SubscribeToPreset(null);
            Bot.EnemyController.Events.OnEnemyRemoved += checkClear;
        }

        public void Update()
        {

        }

        public void Dispose()
        {
            Bot.EnemyController.Events.OnEnemyRemoved -= checkClear;
        }

        public bool ShallDogFight()
        {
            if (checkDecisions() &&
                findDogFightTarget())
            {
                return true;
            }
            else
            {
                clearDogFightTarget();
                return false;
            }
        }

        private void clearDogFightTarget()
        {
            if (DogFightTarget != null)
            {
                DogFightTarget = null;
            }
        }

        private bool checkDecisions()
        {
            if (!BotOwner.WeaponManager.HaveBullets ||
                BotOwner.WeaponManager.Reload.Reloading)
            {
                return false;
            }
            ECombatDecision currentDecision = Bot.Decision.CurrentCombatDecision;
            if (currentDecision == ECombatDecision.RushEnemy)
            {
                return false;
            }
            if (currentDecision == ECombatDecision.Retreat || currentDecision == ECombatDecision.RunToCover)
            {
                if (Bot.Decision.SelfActionDecisions.LowOnAmmo(0.2f))
                {
                    return false;
                }
            }
            return true;
        }

        private bool shallClearDogfightTarget(Enemy enemy)
        {
            if (enemy == null ||
                !enemy.EnemyKnown ||
                enemy.Player?.HealthController.IsAlive == false)
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

        private bool findDogFightTarget()
        {
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

                clearDFTargets();
                Enemy newTarget = selectDFTarget();
                if (newTarget != null)
                {
                    DogFightTarget = newTarget;
                    return true;
                }

                getNewDFTargets();
                DogFightTarget = selectDFTarget();

                return DogFightTarget != null;
            }

            return DogFightTarget != null;
        }

        private float _changeDFTargetTime;

        private void clearDFTargets()
        {
            int count = _dogFightTargets.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                if (shallClearDogfightTarget(_dogFightTargets[i]))
                {
                    _dogFightTargets.RemoveAt(i);
                }
            }
        }

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
            return enemy?.CheckValid() == true &&
                enemy.IsVisible &&
                enemy.EnemyKnown &&
                enemy.Path.PathDistance <= _dogFightStartDist;
        }

        private float _dogFightStartDist = 8f;
        private float _dogFightEndDist = 15f;
    }
}
