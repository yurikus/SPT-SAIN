using EFT;
using EFT.InventoryLogic;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.Classes.Info;
using System;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class ShootDeciderClass : BotComponentClassBase
    {
        public event Action<Enemy> OnShootEnemy;

        public event Action OnEndShoot;

        public Enemy LastShotEnemy { get; private set; }

        public ShootDeciderClass(BotComponent bot) : base(bot)
        {
            TickRequirement = ESAINTickState.OnlyNoSleep;
        }

        public override void Init()
        {
            Bot.EnemyController.Events.OnEnemyRemoved += checkClearEnemy;
            base.Init();
        }

        public override void ManualUpdate()
        {
            checkEndShoot();
            base.ManualUpdate();
        }

        public override void Dispose()
        {
            Bot.EnemyController.Events.OnEnemyRemoved -= checkClearEnemy;
            base.Dispose();
        }

        private void checkEndShoot()
        {
            if (_shooting &&
                !BotOwner.ShootData.Shooting)
            {
                _shooting = false;
                OnEndShoot?.Invoke();
            }
        }

        private void checkClearEnemy(string profileId, Enemy enemy)
        {
            if (LastShotEnemy != null && LastShotEnemy.EnemyProfileId == profileId)
            {
                LastShotEnemy = null;
            }
        }

        public bool CheckAimAndFire(Enemy Enemy)
        {
            if (_changeAimTimer < Time.time)
            {
                _changeAimTimer = Time.time + 0.25f;
                Bot.AimDownSightsController.UpdateADSstatus(Enemy);
            }
            if (TryShoot(Enemy))
                return true;
            Bot.Aim.LoseAimTarget();
            return false;
        }

        private bool TryShoot(Enemy Enemy)
        {
            if (Enemy == null)
                return false;

            if (Enemy.Player?.HealthController?.IsAlive == false)
                return false;

            var weaponManager = BotOwner.WeaponManager;
            if (weaponManager == null)
                return false;

            if (!weaponManager.HaveBullets)
            {
                if (weaponManager.Selector.EquipmentSlot == EquipmentSlot.Holster && !weaponManager.Selector.TryChangeToMain())
                    selectWeapon(Enemy);

                return false;
            }

            if (!Bot.Aim.CanAim)
                return false;

            Vector3? target = GetShootTargetPosition(Enemy);
            //Bot.BotLight.HandleLightForEnemy(enemy);
            if (target != null &&
                Enemy != null)
            {
                Bot.BotLight.HandleLightForEnemy(Enemy);

                if (aimAtTarget(target.Value, out bool AimComplete))
                {
                    if (weaponManager.HaveBullets && AimComplete)
                    {
                        tryShoot(Enemy);
                    }
                    return true;
                }
            }
            return false;
        }

        private void selectWeapon(Enemy Enemy)
        {
            EquipmentSlot optimalSlot = findOptimalWeaponForDistance(Enemy.RealDistance);
            if (currentSlot != optimalSlot)
            {
                tryChangeWeapon(optimalSlot);
            }
        }

        private EquipmentSlot currentSlot => BotOwner.WeaponManager.Selector.EquipmentSlot;

        private void tryChangeWeapon(EquipmentSlot slot)
        {
            if (_nextChangeWeaponTime < Time.time)
            {
                var selector = BotOwner?.WeaponManager?.Selector;
                if (selector != null)
                {
                    _nextChangeWeaponTime = Time.time + 1f;
                    switch (slot)
                    {
                        case EquipmentSlot.FirstPrimaryWeapon:
                            selector.TryChangeToMain();
                            break;

                        case EquipmentSlot.SecondPrimaryWeapon:
                            selector.ChangeToSecond();
                            break;

                        case EquipmentSlot.Holster:
                            selector.TryChangeWeapon(true);
                            break;

                        default:
                            break;
                    }
                }
            }
        }

        private float getDistance()
        {
            if (_nextGetDistTime < Time.time)
            {
                _nextGetDistTime = Time.time + 0.5f;
                Vector3? target = Bot.CurrentTargetPosition;
                if (target != null)
                {
                    _lastDistance = Bot.CurrentTargetDistance;
                }
            }
            return _lastDistance;
        }

        private EquipmentSlot findOptimalWeaponForDistance(float distance)
        {
            if (_nextCheckOptimalTime < Time.time)
            {
                _nextCheckOptimalTime = Time.time + 0.5f;

                var equipment = Bot.PlayerComponent.Equipment;

                float? primaryEngageDist = null;
                var primary = equipment.PrimaryWeapon;
                if (isWeaponDurableEnough(primary))
                {
                    primaryEngageDist = primary.EngagementDistance;
                }

                float? secondaryEngageDist = null;
                var secondary = equipment.SecondaryWeapon;
                if (isWeaponDurableEnough(secondary))
                {
                    secondaryEngageDist = secondary.EngagementDistance;
                }

                float? holsterEngageDist = null;
                var holster = equipment.HolsterWeapon;
                if (isWeaponDurableEnough(holster))
                {
                    holsterEngageDist = holster.EngagementDistance;
                }

                float minDifference = Mathf.Abs(distance - primaryEngageDist ?? 0);
                optimalSlot = EquipmentSlot.FirstPrimaryWeapon;

                float difference = Mathf.Abs(distance - secondaryEngageDist ?? 0);
                if (difference < minDifference)
                {
                    minDifference = difference;
                    optimalSlot = EquipmentSlot.SecondPrimaryWeapon;
                }

                if (!BotOwner.WeaponManager.HaveBullets)
                {
                    difference = Mathf.Abs(distance - holsterEngageDist ?? 0);
                    if (difference < minDifference)
                    {
                        minDifference = difference;
                        optimalSlot = EquipmentSlot.Holster;
                    }
                }
            }
            return optimalSlot;
        }

        private bool isWeaponDurableEnough(WeaponInfo info, float min = 0.5f)
        {
            return info != null &&
                info.Durability > min &&
                info.Weapon.ChamberAmmoCount > 0;
        }

        private bool aimAtTarget(Vector3 target, out bool AimComplete)
        {
            var aimData = BotOwner.AimingManager.CurrentAiming;
            aimData.SetTarget(target);
            aimData.NodeUpdate();
            if (!Bot.FriendlyFire.CheckFriendlyFire(aimData.EndTargetPoint))
            {
                BotOwner.ShootData.EndShoot();
                AimComplete = false;
                return false;
            }
            if (Bot.NoBushESP.NoBushESPActive)
            {
                AimComplete = false;
                return false;
            }
            AimComplete = aimData.IsReady;
            return true;
        }

        private Vector3? GetShootTargetPosition(Enemy enemy)
        {
            Vector3? target = getAimTarget(enemy);
            if (target != null)
            {
                return target;
            }

            enemy = Bot.LastEnemy;
            target = getAimTarget(enemy);
            if (target != null)
            {
                return target;
            }

            enemy = null;
            return null;
        }

        private Vector3? getAimTarget(Enemy enemy)
        {
            if (enemy != null &&
                enemy.IsVisible &&
                enemy.CanShoot)
            {
                //Vector3? test = enemy.Shoot.Targets.GetPointToShoot();
                //if (test == null) {
                //    Logger.LogWarning($"cant get point to shoot with new system! oh no!");
                //}

                Vector3? centerMass = findCenterMassPoint(enemy);
                Vector3? partToShoot = getEnemyPartToShoot(enemy.EnemyInfo);
                Vector3? modifiedTarget = checkYValue(centerMass, partToShoot);
                Vector3? finalTarget = modifiedTarget ?? partToShoot ?? centerMass;

                return finalTarget;
            }
            return null;
        }

        private Vector3? checkYValue(Vector3? centerMass, Vector3? partTarget)
        {
            if (centerMass != null &&
                partTarget != null &&
                centerMass.Value.y < partTarget.Value.y)
            {
                Vector3 newTarget = partTarget.Value;
                newTarget.y = centerMass.Value.y;
                return new Vector3?(newTarget);
            }
            return null;
        }

        private Vector3? findCenterMassPoint(Enemy enemy)
        {
            if (enemy.IsAI)
            {
                return null;
            }
            if (!SAINPlugin.LoadedPreset.GlobalSettings.Aiming.AimCenterMassGlobal)
            {
                return null;
            }
            if (!Bot.Info.FileSettings.Aiming.AimCenterMass)
            {
                return null;
            }
            if (Bot.Info.Profile.IsPMC && GlobalSettingsClass.Instance.Aiming.PMCSAimForHead)
            {
                return null;
            }
            return enemy.CenterMass;
        }

        private Vector3? getEnemyPartToShoot(EnemyInfo enemy)
        {
            if (enemy != null)
            {
                Vector3 value;
                if (enemy.Distance < 6f)
                {
                    value = enemy.GetCenterPart();
                }
                else
                {
                    value = enemy.GetPartToShoot();
                }
                return new Vector3?(value);
            }
            return null;
        }

        private void tryShoot(Enemy enemy)
        {
            if (BotOwner.ShootData.Shoot())
            {
                OnShootEnemy?.Invoke(enemy);
                LastShotEnemy = enemy;
                enemy.EnemyInfo?.SetLastShootTime();
                _shooting = true;
            }
        }

        private bool _shooting;
        private EquipmentSlot optimalSlot;
        private float _nextCheckOptimalTime;
        private float _lastDistance;
        private float _nextGetDistTime;
        private float _nextChangeWeaponTime;
        private float _changeAimTimer;
    }
}