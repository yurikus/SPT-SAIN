using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.Layers.Combat.Squad
{
    internal class SuppressAction(BotOwner bot) : BotAction(bot, nameof(SuppressAction)), IBotAction
    {
        public override void Update(CustomLayer.ActionData data)
        {
            var enemy = Bot.GoalEnemy;
            if (enemy != null)
            {
                if (Shoot.ShootAnyVisibleEnemies(enemy))
                {
                    Bot.Mover.Stop();
                    return;
                }

                if (Bot.Suppression.TrySuppressAnyEnemy(enemy, Bot.EnemyController.KnownEnemies, 0, 0, false))
                {
                    _manualShooting = true;
                    //Bot.Mover.Stop();

                    bool hasMachineGun = Bot.Info.WeaponInfo.EWeaponClass == EWeaponClass.machinegun;
                    if (hasMachineGun
                        && Bot.Mover.Prone.ShallProne(true))
                    {
                        Bot.Mover.Prone.SetProne(true);
                    }
                    enemy.Status.EnemyIsSuppressed = true;
                    float waitTime = hasMachineGun ? 0.1f : 0.5f;
                    _nextShotTime = Time.time + (waitTime * Random.Range(0.75f, 1.25f));
                    return;
                }

                Vector3? lastKnown = enemy.LastKnownPosition;
                if (lastKnown != null)
                {
                    Bot.Mover.WalkToPointByWay(enemy.Path.PathToEnemy);
                }
            }
            ResetManualShoot();
        }

        public override void OnSteeringTicked()
        {
            var enemy = Bot.GoalEnemy;
            if (enemy != null)
            {
                if (Shoot.ShootAnyVisibleEnemies(enemy))
                {
                    Bot.Mover.Stop();
                    return;
                }

                if (Bot.Suppression.TrySuppressAnyEnemy(enemy, Bot.EnemyController.KnownEnemies))
                {
                    _manualShooting = true;
                    Bot.Mover.Stop();

                    bool hasMachineGun = Bot.Info.WeaponInfo.EWeaponClass == EWeaponClass.machinegun;
                    if (hasMachineGun
                        && Bot.Mover.Prone.ShallProne(true))
                    {
                        Bot.Mover.Prone.SetProne(true);
                    }
                    enemy.Status.EnemyIsSuppressed = true;
                    float waitTime = hasMachineGun ? 0.1f : 0.5f;
                    _nextShotTime = Time.time + (waitTime * Random.Range(0.75f, 1.25f));
                    return;
                }

                Vector3? lastKnown = enemy.LastKnownPosition;
                if (lastKnown != null)
                {
                    Bot.Mover.WalkToPointByWay(enemy.Path.PathToEnemy);
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
                Bot.ManualShoot.Reset();
            }
        }

        private bool _manualShooting;

        private float _nextShotTime;

        private bool FindSuppressionTarget(out Vector3? pos)
        {
            pos = Bot.GoalEnemy?.SuppressionTarget;
            return pos != null;
        }

        public override void Start()
        {
            base.Start();   
        }

        public override void Stop()
        {
            base.Stop();
            ResetManualShoot();
        }
    }
}