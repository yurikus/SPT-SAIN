using EFT;
using EFT.InventoryLogic;
using SAIN.Components;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.WeaponFunction
{
    public class AimClass : BotComponentClassBase, IBotClass
    {
        public AimClass(BotComponent sain) : base(sain)
        {
            TickRequirement = ESAINTickState.OnlyNoSleep;
        }

        public event Action<bool> OnAimAllowedOrBlocked;

        public bool CanAim { get; private set; }

        public float LastAimTime { get; set; }

        public AimStatus AimStatus {
            get
            {
                IBotAiming aim = BotOwner.AimingManager.CurrentAiming;
                if (aim is BotAimingClass aimClass)
                {
                    return aimClass.aimStatus_0;
                }
                if (aim != null && aim.IsReady)
                {
                    return AimStatus.AimComplete;
                }
                return AimStatus.NoTarget;
            }
            set
            {
                if (BotOwner?.AimingManager?.CurrentAiming is BotAimingClass aimClass)
                {
                    aimClass.aimStatus_0 = value;
                }
            }
        }

        public override void ManualUpdate()
        {
            checkCanAim();
            CheckLoseTarget();
            base.ManualUpdate();
        }

        public bool AimAtTarget(Vector3 shootPoint, Enemy enemy, out bool AimComplete, IBotAiming currentAiming, BotComponent bot)
        {
            BotOwner botOwner = bot.BotOwner;
            BotWeaponManager weaponManager = botOwner.WeaponManager;
            if (!weaponManager.HaveBullets || weaponManager.Reload.Reloading)
            {
                botOwner.ShootData.EndShoot();
                AimComplete = false;
                Bot.Aim.LoseAimTarget();
                return false;
            }
            currentAiming.SetTarget(shootPoint);
            if (!bot.FriendlyFire.UpdateFriendlyFireStatus(currentAiming.LastDist2Target, bot.Transform.WeaponData.FirePort, bot.Transform.WeaponData.PointDirection, bot))
            {
                botOwner.ShootData.EndShoot();
                AimComplete = false;
                Bot.Aim.LoseAimTarget();
                return false;
            }

            currentAiming.NodeUpdate();
            Bot.Steering.LookToPoint(currentAiming.EndTargetPoint);

            if (enemy != _lastAimEnemy)
            {
                TurningWeaponToAimPoint = true;
                _lastAimEnemy = enemy;
            }
            if (TurningWeaponToAimPoint)
            {
                const float STARTAIMANGLE = 20f;
                if (enemy.Vision.Angles.AngleToEnemy <= STARTAIMANGLE)
                {
                    TurningWeaponToAimPoint = false;
                }
            }

            CheckAimToEnemy(enemy);
            if (!Bot.Steering.IsLookingAtPoint(currentAiming.EndTargetPoint, out float dot, 0.75f))
            {
                AimComplete = false;
                return true;
            }
            AimComplete = currentAiming.IsReady;
            return true;
        }

        /// <summary>
        /// Make sure we are actually pointing our weapon at our target before we start ticking aim.
        /// </summary>
        /// <param name="shootPoint"></param>
        /// <param name="enemy"></param>
        private void CheckAimToEnemy(Enemy enemy)
        {
            if (enemy != _lastAimEnemy)
            {
                TurningWeaponToAimPoint = true;
                _lastAimEnemy = enemy;
            }
            if (TurningWeaponToAimPoint)
            {
                const float STARTAIMANGLE = 20f;
                if (enemy.Vision.Angles.AngleToEnemy <= STARTAIMANGLE)
                {
                    TurningWeaponToAimPoint = false;
                }
            }
        }

        private bool TurningWeaponToAimPoint;
        private Enemy _lastAimEnemy;

        private void checkCanAim()
        {
            bool couldAim = CanAim;
            CanAim = canAim();
            if (couldAim != CanAim)
            {
                OnAimAllowedOrBlocked?.Invoke(CanAim);
            }
        }

        private bool canAim()
        {
            if (Player.IsSprintEnabled)
            {
                return false;
            }
            if (BotOwner.WeaponManager.Reload.Reloading)
            {
                //return false;
            }
            if (!Bot.HasEnemy)
            {
                //return false;
            }
            return true;
        }

        public void LoseAimTarget()
        {
            if (BotOwner.AimingManager.CurrentAiming is BotAimingClass aimClass &&
                aimClass.aimStatus_0 != AimStatus.NoTarget)
            {
                aimClass.aimStatus_0 = AimStatus.NoTarget;
                TurningWeaponToAimPoint = false;
                _lastAimEnemy = null;
            }
        }

        private void CheckLoseTarget()
        {
            var weaponManager = BotOwner.WeaponManager;
            if (!CanAim || !weaponManager.HaveBullets || weaponManager.Reload.Reloading)
            {
                LoseAimTarget();
                BotOwner.ShootData?.EndShoot();
            }
        }
    }
}