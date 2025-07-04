using EFT;
using HarmonyLib;
using SAIN.Preset;
using SAIN.Preset.BotSettings.SAINSettings.Categories;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System;
using System.Reflection;
using System.Text;
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
                if (BotOwner.AimingManager.CurrentAiming != null && BotOwner.AimingManager.CurrentAiming is BotAimingClass aimClass)
                {
                    var status = aimClass.aimStatus_0;

                    //if (status != AimStatus.NoTarget &&
                    //    Bot.Enemy?.IsVisible == false &&
                    //    Bot.LastEnemy?.IsVisible == false)
                    //{
                    //    return AimStatus.NoTarget;
                    //}
                    return status;
                }
                else
                {
                    return AimStatus.NoTarget;
                }
            }
        }

        public override void ManualUpdate()
        {
            checkCanAim();
            CheckLoseTarget();
            base.ManualUpdate();
        }

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
            var aimData = BotOwner.AimingManager.CurrentAiming;
            if (aimData == null)
            {
                //return false;
            }
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