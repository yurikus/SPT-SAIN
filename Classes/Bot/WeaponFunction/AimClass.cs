using EFT;
using EFT.InventoryLogic;
using SAIN.Components;
using SAIN.SAINComponent.Classes.EnemyClasses;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.WeaponFunction
{
    public enum SBotAimState
    {
        None,
        Turning,
        Aiming,
        AimComplete,
    }

    public class SBotAimingClass : IBotAiming
    {
        public event Action<Vector3> OnSettingsTarget;

        public Enemy EnemyTarget { get; private set; }
        public SBotAimState AimStatus { get; private set; }
        public Vector3 EndTargetPoint { get; private set; }
        public Vector3 RealTargetPoint { get; private set; }

        public bool IsReady => AimStatus == SBotAimState.AimComplete;

        public bool AlwaysTurnOnLight { get; private set; }

        public float LastDist2Target { get; private set; }

        public bool HardAim { get; private set; }

        public void LoseTarget()
        {
        }

        public void SetTarget(Vector3 trg)
        {
        }

        public void SetTarget(Enemy enemy)
        {
        }

        public void SetNextAimingDelay(float nextAimingDelay)
        {
        }

        public void TriggerPressedDone()
        {
        }

        public void ShootDone(Weapon weapon)
        {
        }

        public void NodeUpdate()
        {
        }

        public void Activate()
        {
        }

        public void GetHit(DamageInfoStruct damageInfo)
        {
        }

        public void DrawGizmosSelected()
        {
        }

        public void ManualUpdate()
        {
        }

        public void RotateX(float angToRotate)
        {
        }

        public void RotateY(float deltaAngle)
        {
        }

        public void SetWeapon(Weapon weapon)
        {
        }

        public void SetTracers(bool isTracers)
        {
        }

        public void Move(float delta = 0f)
        {
        }

        public void NextShotMiss()
        {
        }

        public void OnDrawGizmos()
        {
        }

        public void DebugDraw()
        {
        }

        public void Dispose()
        {
        }
    }

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
            IBotAiming aim = BotOwner.AimingManager.CurrentAiming;
            if (aim != null)
            {
                return aim.EndTargetPoint;
            }
            return Vector3.zero;
        }

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
            CheckAimToEnemy(enemy);
            AimComplete = !TurningWeaponToAimPoint && currentAiming.IsReady;
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
                const float STARTAIMANGLE = 10f;
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