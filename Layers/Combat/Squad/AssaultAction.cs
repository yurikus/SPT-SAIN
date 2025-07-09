using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.Classes.Coverfinder;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.Layers.Combat.Squad
{
    internal class AssaultAction : CombatAction
    {
        public AssaultAction(BotOwner bot) : base(bot, nameof(AssaultAction))
        {
        }

        public override void Update(CustomLayer.ActionData data)
        {
            Enemy enemy = Bot.Enemy;
            Shoot.ShootAnyVisibleEnemies(enemy);
            if (!Bot.Steering.SteerByPriority(enemy, false) && enemy != null)
            {
                Bot.Steering.LookToEnemy(enemy);
            }

            if (enemy != null)
            {
                if (PointDestination == null)
                {
                    PointDestination = Bot.Cover.FindPointInDirection(enemy.EnemyDirection);
                }
                if (PointDestination != null)
                {
                    Vector3 destination = PointDestination.Position;

                    if ((destination - Bot.Position).sqrMagnitude < 1f)
                    {
                        PointDestination = null;
                        return;
                    }
                    if (_recalcPathTime < Time.time)
                    {
                        bool sprint = true;

                        if (sprint && BotOwner.BotRun.Run(destination, false, SAINPlugin.LoadedPreset.GlobalSettings.General.SprintReachDistance))
                        {
                            Bot.Steering.LookToMovingDirection(true);
                            _recalcPathTime = Time.time + 1f;
                        }
                        else if (Bot.Mover.GoToPoint(destination, out _))
                        {
                            _recalcPathTime = Time.time + 1f;
                        }
                        else
                        {
                            _recalcPathTime = Time.time + 0.5f;
                        }
                    }
                }
            }
        }

        private float _recalcPathTime;
        private CoverPoint PointDestination;

        public override void Start()
        {
        }

        public override void Stop()
        {
        }
    }
}