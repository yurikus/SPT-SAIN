using EFT;
using SAIN.SAINComponent.Classes.EnemyClasses;
using Sirenix.Serialization;
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

        public Vector3 EndTargetPoint()
        {
            if (BotOwner.AimingManager.CurrentAiming is BotAimingClass aimClass)
            {
                return aimClass.EndTargetPoint;
            }
            return Vector3.zero;
        }

        public Vector3 RealTargetPoint()
        {
            if (BotOwner.AimingManager.CurrentAiming is BotAimingClass aimClass)
            {
                return aimClass.RealTargetPoint;
            }
            return Vector3.zero;
        }

        public AimStatus AimStatus {
            get
            {
                if (BotOwner?.AimingManager?.CurrentAiming is BotAimingClass aimClass)
                {
                    return aimClass.aimStatus_0;
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
            currentAiming.SetTarget(shootPoint);
            BotOwner botOwner = bot.BotOwner;
            BotWeaponManager weaponManager = botOwner.WeaponManager;
            if (!weaponManager.HaveBullets || weaponManager.Reload.Reloading)
            {
                botOwner.ShootData.EndShoot();
                AimComplete = false;
                return false;
            }
            if (!bot.FriendlyFire.UpdateFriendlyFireStatus(currentAiming.LastDist2Target, bot.Transform.WeaponFirePort, bot.Transform.WeaponPointDirection, bot))
            {
                botOwner.ShootData.EndShoot();
                AimComplete = false;
                return false;
            }
            if (bot.NoBushESP.NoBushESPActive)
            {
                AimComplete = false;
                return false;
            }

            CheckAimToEnemy(shootPoint, enemy);
            if (TurningWeaponToAimPoint)
            {
                AimComplete = false;
                // return true because we want to aim at this enemy, but haven't turned to do so yet.
                return true;
            }

            // Tick aim
            currentAiming.NodeUpdate();
            AimComplete = currentAiming.IsReady;
            return true;
        }

        /// <summary>
        /// Make sure we are actually pointing our weapon at our target before we start ticking aim.
        /// </summary>
        /// <param name="shootPoint"></param>
        /// <param name="enemy"></param>
        private void CheckAimToEnemy(Vector3 shootPoint, Enemy enemy)
        {
            if (enemy != _lastAimEnemy)
            {
                TurningWeaponToAimPoint = true;
                _lastAimEnemy = enemy;
            }
            if (TurningWeaponToAimPoint)
            {
                const float ANGLE_TO_START_AIM = 5f;
                Vector3 weaponFirePort = Person.Transform.WeaponFirePort;
                Vector3 weaponPointDir = Person.Transform.WeaponPointDirection;
                Vector3 shootPointDir = (shootPoint - weaponFirePort).normalized;
                if (Vector3.Angle(shootPointDir, weaponPointDir) < ANGLE_TO_START_AIM)
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
                // Should prevent bots jerking their look point when losing aiming status
                Bot.Steering.LookToDirection(Bot.LookDirection);

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