using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.Models.Enums;
using UnityEngine;

namespace SAIN.Layers.Combat.Squad
{
    internal class SuppressAction : CombatAction, ISAINAction
    {
        public SuppressAction(BotOwner bot) : base(bot, nameof(SuppressAction))
        {
        }

        public void Toggle(bool value)
        {
            ToggleAction(value);
        }

        public override void Update(CustomLayer.ActionData data)
        {
            var enemy = Bot.Enemy;
            if (enemy != null)
            {
                if (Shoot.ShootAnyVisibleEnemies(enemy))
                {
                    Bot.Mover.StopMove();
                    return;
                }

                if (Bot.Suppression.TrySuppressAnyEnemy(enemy, Bot.EnemyController.EnemyLists.KnownEnemies, 0f, 0))
                {
                    _manualShooting = true;
                    return;
                }

                Vector3? lastKnown = enemy.LastKnownPosition;
                if (lastKnown != null)
                {
                    Bot.Mover.GoToPoint(lastKnown.Value, out _, -1, false, false, false);
                }
            }

            ResetManualShoot();
            if (!Bot.Steering.SteerByPriority(enemy, false))
            {
                Bot.Steering.LookToLastKnownEnemyPosition(enemy);
            }
        }

        private void ResetManualShoot()
        {
            if (_manualShooting)
            {
                _manualShooting = false;
                Bot.Suppression.ResetSuppressing();
            }
        }

        private bool _manualShooting;

        public override void Start()
        {
            Toggle(true);
        }

        public override void Stop()
        {
            Toggle(false);
            ResetManualShoot();
        }
    }
}