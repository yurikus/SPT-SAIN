using SAIN.Components;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class CurrentTargetClass : BotComponentClassBase
    {
        public Enemy CurrentTargetEnemy { get; private set; }
        public Vector3? CurrentTargetPosition => _currentTarget;
        public Vector3? CurrentTargetDirection => CurrentTargetEnemy?.EnemyDirection;
        public float CurrentTargetDistance => HasTarget ? CurrentTargetEnemy.RealDistance : float.MaxValue;
        public bool HasTarget => CurrentTargetEnemy != null;
        public string TargetProfileId => CurrentTargetEnemy?.EnemyProfileId;

        public CurrentTargetClass(BotComponent bot) : base(bot)
        {
            TickRequirement = ESAINTickState.OnlyBotActive;
        }

        public override void ManualUpdate()
        {
            updateCurrentTarget();
            updateGoalTarget();
            base.ManualUpdate();
        }

        private void updateCurrentTarget()
        {
            Vector3? target = getTarget(out Enemy newTargetEnemy);
            _currentTarget = target;
            if (target == null)
            {
                if (CurrentTargetEnemy != null)
                {
                    Bot.Cover.CoverFinder.ClearTarget();
                    CurrentTargetEnemy = null;
                }
                return;
            }
            bool hasTarget = CurrentTargetEnemy != null;
            bool sameTarget = CurrentTargetEnemy == newTargetEnemy;
            if (!hasTarget || !sameTarget)
            {
                if (hasTarget)
                {
                    CurrentTargetEnemy.SetIsCurrentEnemy(false);
                    CurrentTargetEnemy = null;
                }
                Bot.Aim.LoseAimTarget();
                newTargetEnemy.SetIsCurrentEnemy(true);
                CurrentTargetEnemy = newTargetEnemy;
                Bot.Cover.CoverFinder.CalcTargetPoint(newTargetEnemy, target.Value);
            }
        }

        private void updateGoalTarget()
        {
            if (_updateGoalTargetTime < Time.time)
            {
                _updateGoalTargetTime = Time.time + 0.5f;

                var goalTarget = BotOwner.Memory.GoalTarget;
                var Target = goalTarget?.Position;
                if (Target != null)
                {
                    if ((Target.Value - Bot.Position).sqrMagnitude < 1f ||
                        goalTarget.CreatedTime > 120f)
                    {
                        goalTarget.Clear();
                        BotOwner.CalcGoal();
                    }
                }
            }
        }

        private Vector3? getTarget(out Enemy targetEnemy)
        {
            Vector3? target =
                getVisibleEnemyPos(out targetEnemy) ??
                getLastHitPosition(out targetEnemy) ??
                getUnderFirePosition(out targetEnemy) ??
                getEnemylastKnownPos(out targetEnemy);

            return target;
        }

        private Vector3? getVisibleEnemyPos(out Enemy targetEnemy)
        {
            Enemy enemy = Bot.Enemy;
            if (enemy != null && enemy.IsVisible)
            {
                targetEnemy = enemy;
                return enemy.EnemyPosition;
            }
            targetEnemy = null;
            return null;
        }

        private Vector3? getLastHitPosition(out Enemy targetEnemy)
        {
            targetEnemy = null;
            if (Bot.Medical.TimeSinceShot > 5f)
            {
                return null;
            }

            Enemy enemy = Bot.Medical.HitByEnemy.EnemyWhoLastShotMe;
            if (enemy == null || !enemy.CheckValid() || enemy.IsCurrentEnemy)
            {
                return null;
            }
            targetEnemy = enemy;
            return enemy.LastKnownPosition ?? enemy.Status.LastShotPosition;
        }

        private Vector3? getUnderFirePosition(out Enemy targetEnemy)
        {
            targetEnemy = null;
            if (!BotOwner.Memory.IsUnderFire)
            {
                return null;
            }

            Enemy enemy = Bot.Memory.LastUnderFireEnemy;
            if (enemy == null ||
                !enemy.CheckValid() ||
                enemy.IsCurrentEnemy)
            {
                return null;
            }
            targetEnemy = enemy;
            return enemy.LastKnownPosition ?? Bot.Memory.UnderFireFromPosition;
        }

        private Vector3? getEnemylastKnownPos(out Enemy targetEnemy)
        {
            Enemy enemy = Bot.EnemyController.GoalEnemy;
            if (enemy != null)
            {
                var lastKnown = getLastKnown(enemy);
                if (lastKnown != null)
                {
                    targetEnemy = enemy;
                    return lastKnown.Value;
                }
            }
            targetEnemy = null;
            return null;
        }

        private Vector3? getLastKnown(Enemy enemy)
        {
            var places = enemy.KnownPlaces;
            return places.LastKnownPlace?.Position ?? places.LastSeenPlace?.Position ?? places.LastHeardPlace?.Position;
        }

        private float _updateGoalTargetTime;
        private Vector3? _currentTarget;
    }
}