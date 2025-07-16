using EFT;
using EFT.InventoryLogic;
using SAIN.Components;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Decision
{
    public class SelfActionDecisionClass : BotBase
    {
        public SelfActionDecisionClass(BotComponent sain) : base(sain)
        {
            CanEverTick = false;
        }

        public ESelfDecision CurrentSelfAction => Bot.Decision.CurrentSelfDecision;

        public bool GetDecision(out ESelfDecision Decision, Enemy enemy)
        {
            if (enemy == null)
            {
                Decision = ESelfDecision.None;
                return false;
            }

            BotOwner botOwner = BotOwner;
            if (botOwner.WeaponManager?.Reload.Reloading == true)
            {
                _lastReloadTime = Time.time;
            }
            if (CheckContinueSelfAction(out Decision, enemy))
            {
                return true;
            }
            if (botOwner.ShootData.Shooting)
            {
                Decision = ESelfDecision.None;
                return false;
            }
            if (CheckDoReload(enemy, Bot))
            {
                botOwner.ShootData.BlockFor(0.7f);
                Decision = ESelfDecision.Reload;
                return true;
            }
            return StartBotHeal(ref Decision);
        }

        private float _lastReloadTime;

        private bool CheckDoReload(Enemy enemy, BotComponent bot)
        {
            const float RELOAD_AMMORATIO_MIN_PEACE = 0.7f;
            const float RELOAD_AMMORATIO_MAX = 0.8f;

            if (Time.time - _lastReloadTime < 1f)
                return false;

            BotOwner botOwner = bot.BotOwner;
            if (botOwner.Medecine?.Using == true) return false;
            if (botOwner.GetPlayer.HandsController?.IsInInteractionStrictCheck() == true)
                return false;

            var weaponManager = botOwner.WeaponManager;
            if (weaponManager == null) return false;

            if (weaponManager.IsMelee)
            {
                if (!weaponManager.Melee.ShallEndRun)
                {
                    return false;
                }
                //bool holsterRefilled = ReloadClass.RefillMagsInSlot(EquipmentSlot.Holster, bot, weaponManager, 1, true);
                //bool secondRefilled = ReloadClass.RefillMagsInSlot(EquipmentSlot.SecondPrimaryWeapon, bot, weaponManager, 1, true);
                //bool primaryRefilled = ReloadClass.RefillMagsInSlot(EquipmentSlot.FirstPrimaryWeapon, bot, weaponManager, 1, true);
                //if (!holsterRefilled &&
                //    !secondRefilled &&
                //    !primaryRefilled)
                //{
                //    Logger.LogWarning("Cant refill weapons!");
                //    return false;
                //}
                if (weaponManager.Reload?.Reloading != true) weaponManager.Selector.TryChangeWeapon(true);
                return false;
            }

            var reload = weaponManager.Reload;
            if (reload == null) return false;
            if (reload.Reloading) return false;

            if (!weaponManager.IsReady) return false;
            if (weaponManager.ShootController?.CanStartReload() != true) return false;

            if (botOwner.WeaponManager.Malfunctions.HaveMalfunction() && botOwner.WeaponManager.Malfunctions.MalfunctionType() != Weapon.EMalfunctionState.Misfire)
                return false;

            _nextGetRatioTime = Time.time + 0.025f;
            _ammoRatio = getAmmoRatio(reload);
            if (_ammoRatio <= 0)
                botOwner.ShootData.EndShoot();

            if (CheckReloadRatiosCanReload(enemy, RELOAD_AMMORATIO_MIN_PEACE, RELOAD_AMMORATIO_MAX, _ammoRatio))
            {
                if (reload.CanReload(true, out var MagazineItemClass, out var list))
                {
                    botOwner.ShootData.EndShoot();
                    reload.Reloading = true;
                    _lastReloadTime = Time.time;
                    if (MagazineItemClass != null)
                    {
                        reload.ReloadMagazine(MagazineItemClass);
                    }
                    else if (list != null && list.Count > 0)
                    {
                        reload.ReloadAmmo(list);
                    }
                    return true;
                }
                if (enemy != null && enemy.IsVisible && enemy.RealDistance < 10f && !weaponManager.Selector.TryChangeWeapon(true) && weaponManager.Selector.CanChangeToMeleeWeapons)
                {
                    weaponManager.Selector.ChangeToMelee();
                }
            }
            return false;
        }

        private static bool CheckReloadRatiosCanReload(Enemy enemy, float RELOAD_AMMORATIO_MIN_PEACE, float RELOAD_AMMORATIO_MAX, float ammoRatio)
        {
            if (ammoRatio > 0f)
            {
                if (ammoRatio >= RELOAD_AMMORATIO_MAX)
                {
                    return false;
                }
                if (enemy == null)
                {
                    if (ammoRatio < RELOAD_AMMORATIO_MIN_PEACE)
                    {
                        return true;
                    }
                    return false;
                }

                if (!CheckReloadByAmmoRemaining(enemy, ammoRatio))
                {
                    return false;
                }
                // TODO: Test to see if this is too much and makes bots not reload often enough
                //foreach (var knownEnemy in bot.EnemyController.EnemyLists.KnownEnemies)
                //{
                //    if (!CheckReloadByAmmoRemaining(knownEnemy, ammoRatio))
                //    {
                //        return false;
                //    }
                //}
            }
            return true;
        }

        private bool StartBotHeal(ref ESelfDecision Decision)
        {
            if (_nextCheckHealTime < Time.time)
            {
                // Solarint - Should fixed bots insta-healing when shot. TODO: Make these config variables.
                const float TimeSinceShotToBlockHeal = 0.5f;
                float TimeSinceShot = Bot.Medical.TimeSinceShot;
                if (TimeSinceShot < TimeSinceShotToBlockHeal)
                {
                    _nextCheckHealTime = Time.time + 0.2f;
                    return false;
                }
                _nextCheckHealTime = Time.time + 1f;
                if (startUseStims())
                {
                    Decision = ESelfDecision.Stims;
                    return true;
                }
                if (startFirstAid())
                {
                    Decision = ESelfDecision.FirstAid;
                    return true;
                }
                if (Bot.Medical.Surgery.CheckCanStartUsingKit())
                {
                    Decision = ESelfDecision.Surgery;
                    return true;
                }
            }
            return false;
        }

        private float _nextCheckHealTime;

        private bool CheckContinueSelfAction(out ESelfDecision Decision, Enemy enemy)
        {
            Decision = ESelfDecision.None;
            switch (CurrentSelfAction)
            {
                case ESelfDecision.FirstAid:
                    return checkContinueFirstAid(_timeSinceChangeDecision, out Decision, enemy);

                case ESelfDecision.Reload:
                    if (checkContinueReload(_timeSinceChangeDecision, enemy))
                    {
                        Decision = ESelfDecision.Reload;
                        return true;
                    }
                    return false;

                case ESelfDecision.Surgery:
                    return checkContinueSurgery(out Decision);

                case ESelfDecision.Stims:
                    return checkContinueStims(_timeSinceChangeDecision, out Decision);

                default:
                    Decision = ESelfDecision.None;
                    return false;
            }
        }

        private bool checkContinueReload(float timeSinceChange, Enemy enemy)
        {
            BotComponent bot = Bot;
            if (bot.Decision.CurrentSelfDecision != ESelfDecision.Reload)
                return false;
            BotWeaponManager weaponManager = bot.BotOwner.WeaponManager;
            if (weaponManager == null) return false;
            BotReload reload = weaponManager.Reload;
            if (reload == null) return false;
            bool reloading = reload.Reloading;
            if (!reloading) return false;
            Weapon currentWeapon = weaponManager.CurrentWeapon;
            if (currentWeapon == null) return false;

            const int BULLET_COUNT_TO_STOP_RELOAD = 3;
            if (currentWeapon.ReloadMode == Weapon.EReloadMode.InternalMagazine &&
                reload.BulletCount >= BULLET_COUNT_TO_STOP_RELOAD &&
                enemy != null &&
                enemy.IsVisible &&
                Time.time - enemy.Vision.VisibleStartTime > 0.25f)
            {
                reload.TryStopReload();
                //if (this._nextPossibleTryStopReload < Time.time)
                //{
                //    this._nextPossibleTryStopReload = Time.time + 1f;
                //    BotOwner.ShootData.Shoot();
                //}
                //return false;
            }

            reload.CheckReloadLongTime();

            if (timeSinceChange > 8f)
            {
                Bot.SelfActions.BotCancelReload();
                return false;
            }
            return true;
        }

        private bool checkContinueSurgery(out ESelfDecision Decision)
        {
            if (BotOwner?.Medecine == null)
            {
                Decision = ESelfDecision.None;
                return false;
            }
            var medical = Bot.Medical;
            medical.Surgery.CheckAreaClearForSurgery();
            if (medical.Surgery.AreaClearForSurgery &&
                !checkDecisionTooLong())
            {
                Decision = ESelfDecision.Surgery;
                return true;
            }
            Bot.Medical.TryCancelHeal();
            Decision = ESelfDecision.None;
            return false;
        }

        private bool checkContinueFirstAid(float timeSinceChange, out ESelfDecision Decision, Enemy enemy)
        {
            if (BotOwner?.Medecine == null)
            {
                Decision = ESelfDecision.None;
                return false;
            }
            if (timeSinceChange > 6f)
            {
                Bot.Medical.TryCancelHeal();
                Decision = ESelfDecision.None;
                //TryFixBusyHands();
                return false;
            }
            Decision = ESelfDecision.FirstAid;
            return true;
        }

        private bool checkContinueStims(float timeSinceChange, out ESelfDecision Decision)
        {
            if (BotOwner?.Medecine == null)
            {
                Decision = ESelfDecision.None;
                return false;
            }
            if (timeSinceChange > 3f)
            {
                Bot.Medical.TryCancelHeal();
                //TryFixBusyHands();
                Decision = ESelfDecision.None;
                return false;
            }
            Decision = ESelfDecision.Stims;
            return true;
        }

        private float _timeSinceChangeDecision => Time.time - Bot.Decision.ChangeDecisionTime;

        private bool checkDecisionTooLong()
        {
            return Time.time - Bot.Decision.ChangeDecisionTime > 60f;
        }

        public bool UsingMeds => BotOwner.Medecine?.Using == true && CurrentSelfAction != ESelfDecision.None;

        public bool CanUseStims {
            get
            {
                var stims = BotOwner.Medecine?.Stimulators;
                return stims?.HaveSmt == true && Time.time - stims.LastEndUseTime > 3f && stims?.CanUseNow() == true && !Bot.Memory.Health.Healthy;
            }
        }

        public bool CanUseFirstAid => BotOwner.Medecine?.FirstAid?.ShallStartUse() == true;

        public bool CanUseSurgery => BotOwner.Medecine?.SurgicalKit?.ShallStartUse() == true && BotOwner.Medecine?.FirstAid?.IsBleeding == false;

        public bool CanReload => BotOwner.WeaponManager?.IsReady == true && BotOwner.WeaponManager?.Reload.CanReload(false) == true;

        private bool startUseStims()
        {
            if (!CanUseStims)
            {
                return false;
            }
            if (!Bot.Memory.Health.Dying && !Bot.Memory.Health.BadlyInjured)
            {
                return false;
            }
            if (Bot.EnemyController.AtPeace)
            {
                return true;
            }
            if (Bot.Decision.RunningToCover)
            {
                return true;
            }
            foreach (Enemy enemy in Bot.EnemyController.KnownEnemies)
                if (!ShallUseStimsCheckEnemy(enemy))
                    return false;
            return true;
        }

        private bool startFirstAid()
        {
            if (Bot.Medical.TimeSinceShot < 0.25f)
            {
                return false;
            }
            if (!CanUseFirstAid)
            {
                return false;
            }
            if (Bot.Memory.Health.Healthy)
            {
                return false;
            }
            if (Bot.Decision.RunningToCover)
            {
                return true;
            }
            foreach (Enemy enemy in Bot.EnemyController.KnownEnemies)
                if (!ShallFirstAidCheckEnemy(enemy))
                    return false;

            return true;
        }

        //public struct BotCombatStimsSettings()
        //{
        //    bool MustBeSeen = true;
        //    bool MustBeHeard = true;
        //    bool MustBeSeenAndHeard = true;
        //    bool CanBeInLineOfSight = false;
        //    float TimeSinceKnownIfUnseenToUse = 3f;
        //    float PathVeryCloseDist =
        //
        //}

        private static bool ShallUseStimsCheckEnemy(Enemy enemy)
        {
            if (enemy == null)
            {
                return true;
            }
            if (!enemy.Seen && !enemy.Heard)
            {
                return true;
            }
            if (enemy.InLineOfSight)
            {
                return false;
            }

            float timeSinceLastKnownUpdated = enemy.TimeSinceLastKnownUpdated;
            if (!enemy.Seen && timeSinceLastKnownUpdated > 3f)
            {
                return true;
            }

            return enemy.EPathDistance switch {
                EPathDistance.VeryClose => timeSinceLastKnownUpdated > 6f,
                EPathDistance.Close => timeSinceLastKnownUpdated > 3f,
                EPathDistance.Mid => enemy.TimeSinceSeen > 2f,
                EPathDistance.Far => true,
                EPathDistance.VeryFar => true,
                _ => false,
            };
        }

        private bool ShallFirstAidCheckEnemy(Enemy enemy)
        {
            if (enemy == null || !enemy.CheckValid())
            {
                return true;
            }
            if (!enemy.Seen && !enemy.Heard)
            {
                return true;
            }
            if (enemy.InLineOfSight)
            {
                return false;
            }
            float timeSinceLastKnownUpdated = enemy.TimeSinceLastKnownUpdated;
            ETagStatus healthStatus = Bot.Memory.Health.HealthStatus;
            if (healthStatus != ETagStatus.BadlyInjured && healthStatus != ETagStatus.Dying && !enemy.Seen && timeSinceLastKnownUpdated > 8f)
            {
                return true;
            }

            return healthStatus switch {
                ETagStatus.Injured => enemy.EPathDistance switch {
                    EPathDistance.VeryClose => timeSinceLastKnownUpdated > 20f && (!enemy.Seen || enemy.TimeSinceSeen > 20f),
                    EPathDistance.Close => timeSinceLastKnownUpdated > 15f && (!enemy.Seen || enemy.TimeSinceSeen > 15f),
                    EPathDistance.Mid => enemy.TimeSinceSeen > 8f && (!enemy.Seen || enemy.TimeSinceSeen > 8f),
                    EPathDistance.Far => enemy.TimeSinceSeen > 5f && (!enemy.Seen || enemy.TimeSinceSeen > 5f),
                    EPathDistance.VeryFar => enemy.TimeSinceSeen > 3f && (!enemy.Seen || enemy.TimeSinceSeen > 3f),
                    _ => false,
                },
                ETagStatus.BadlyInjured => enemy.EPathDistance switch {
                    EPathDistance.VeryClose => timeSinceLastKnownUpdated > 18 && (!enemy.Seen || enemy.TimeSinceSeen > 18),
                    EPathDistance.Close => timeSinceLastKnownUpdated > 12 && (!enemy.Seen || enemy.TimeSinceSeen > 12),
                    EPathDistance.Mid => timeSinceLastKnownUpdated > 6 && (!enemy.Seen || enemy.TimeSinceSeen > 6),
                    EPathDistance.Far => timeSinceLastKnownUpdated > 4 && (!enemy.Seen || enemy.TimeSinceSeen > 4),
                    EPathDistance.VeryFar => timeSinceLastKnownUpdated > 2 && (!enemy.Seen || enemy.TimeSinceSeen > 2),
                    _ => false,
                },
                ETagStatus.Dying => enemy.EPathDistance switch {
                    EPathDistance.VeryClose => timeSinceLastKnownUpdated > 15 && (!enemy.Seen || enemy.TimeSinceSeen > 15),
                    EPathDistance.Close => timeSinceLastKnownUpdated > 10 && (!enemy.Seen || enemy.TimeSinceSeen > 10),
                    EPathDistance.Mid => timeSinceLastKnownUpdated > 4 && (!enemy.Seen || enemy.TimeSinceSeen > 4),
                    EPathDistance.Far => timeSinceLastKnownUpdated > 3 && (!enemy.Seen || enemy.TimeSinceSeen > 3),
                    EPathDistance.VeryFar => timeSinceLastKnownUpdated > 2 && (!enemy.Seen || enemy.TimeSinceSeen > 2),
                    _ => false,
                },
                _ => false,
            };
        }

        private static bool CheckReloadByAmmoRemaining(Enemy enemy, float ammoRatio)
        {
            const float RELOAD_AMMORATIO_UPPER = 0.66f;
            const float RELOAD_AMMORATIO_UPPER_TimeSinceSeen = 15f;
            const float RELOAD_AMMORATIO_UPPER_DIST_ENEMY_VeryClose = 90f;
            const float RELOAD_AMMORATIO_UPPER_DIST_ENEMY_Close = 12f;
            const float RELOAD_AMMORATIO_UPPER_DIST_ENEMY_Mid = 6f;
            const float RELOAD_AMMORATIO_UPPER_DIST_ENEMY_Far = 3f;
            const float RELOAD_AMMORATIO_UPPER_DIST_ENEMY_VeryFar = 3f;

            const float RELOAD_AMMORATIO_MID = 0.4f;
            const float RELOAD_AMMORATIO_MID_TimeSinceSeen = 20f;
            const float RELOAD_AMMORATIO_MID_DIST_ENEMY_VeryClose = 60;
            const float RELOAD_AMMORATIO_MID_DIST_ENEMY_Close = 8f;
            const float RELOAD_AMMORATIO_MID_DIST_ENEMY_Mid = 4f;
            const float RELOAD_AMMORATIO_MID_DIST_ENEMY_Far = 2f;
            const float RELOAD_AMMORATIO_MID_DIST_ENEMY_VeryFar = 2f;

            const float RELOAD_AMMORATIO_LOW = 0.2f;
            const float RELOAD_AMMORATIO_LOW_TimeSinceSeen = 4f;
            const float RELOAD_AMMORATIO_LOW_DIST_ENEMY_VeryClose = 2f;
            const float RELOAD_AMMORATIO_LOW_DIST_ENEMY_Close = 2f;
            const float RELOAD_AMMORATIO_LOW_DIST_ENEMY_Mid = 1f;
            const float RELOAD_AMMORATIO_LOW_DIST_ENEMY_Far = 1f;
            const float RELOAD_AMMORATIO_LOW_DIST_ENEMY_VeryFar = 1f;

            const float RELOAD_AMMORATIO_MINIMUM_TIMESINCESEEN = 2f;

            if (enemy.IsVisible && enemy.CanShoot)
            {
                return false;
            }
            float timeSinceSensed = enemy.TimeSinceLastKnownUpdated;
            if (timeSinceSensed < 0.2f)
            {
                return false;
            }

            EPathDistance distance = enemy.EPathDistance;

            if (ammoRatio > RELOAD_AMMORATIO_UPPER)
            {
                if (timeSinceSensed > RELOAD_AMMORATIO_UPPER_TimeSinceSeen)
                    return true;
                switch (distance)
                {
                    case EPathDistance.VeryClose:
                        if (timeSinceSensed > RELOAD_AMMORATIO_UPPER_DIST_ENEMY_VeryClose)
                            return true;
                        break;

                    case EPathDistance.Close:
                        if (timeSinceSensed > RELOAD_AMMORATIO_UPPER_DIST_ENEMY_Close)
                            return true;
                        break;

                    case EPathDistance.Mid:
                        if (timeSinceSensed > RELOAD_AMMORATIO_UPPER_DIST_ENEMY_Mid)
                            return true;
                        break;

                    case EPathDistance.Far:
                        if (timeSinceSensed > RELOAD_AMMORATIO_UPPER_DIST_ENEMY_Far)
                            return true;
                        break;

                    case EPathDistance.VeryFar:
                        if (timeSinceSensed > RELOAD_AMMORATIO_UPPER_DIST_ENEMY_VeryFar)
                            return true;
                        break;
                }
                return false;
            }
            if (ammoRatio > RELOAD_AMMORATIO_MID)
            {
                if (timeSinceSensed > RELOAD_AMMORATIO_MID_TimeSinceSeen)
                    return true;
                return distance switch {
                    EPathDistance.VeryClose => timeSinceSensed > RELOAD_AMMORATIO_MID_DIST_ENEMY_VeryClose,
                    EPathDistance.Close => timeSinceSensed > RELOAD_AMMORATIO_MID_DIST_ENEMY_Close,
                    EPathDistance.Mid => timeSinceSensed > RELOAD_AMMORATIO_MID_DIST_ENEMY_Mid,
                    EPathDistance.Far => timeSinceSensed > RELOAD_AMMORATIO_MID_DIST_ENEMY_Far,
                    EPathDistance.VeryFar => timeSinceSensed > RELOAD_AMMORATIO_MID_DIST_ENEMY_VeryFar,
                    _ => false,
                };
            }

            if (ammoRatio > RELOAD_AMMORATIO_LOW)
            {
                if (enemy.TimeSinceSeen > RELOAD_AMMORATIO_LOW_TimeSinceSeen)
                    return true;
                switch (distance)
                {
                    case EPathDistance.VeryClose:
                        if (timeSinceSensed > RELOAD_AMMORATIO_LOW_DIST_ENEMY_VeryClose)
                            return true;
                        break;

                    case EPathDistance.Close:
                        if (timeSinceSensed > RELOAD_AMMORATIO_LOW_DIST_ENEMY_Close)
                            return true;
                        break;

                    case EPathDistance.Mid:
                        if (timeSinceSensed > RELOAD_AMMORATIO_LOW_DIST_ENEMY_Mid)
                            return true;
                        break;

                    case EPathDistance.Far:
                        if (timeSinceSensed > RELOAD_AMMORATIO_LOW_DIST_ENEMY_Far)
                            return true;
                        break;

                    case EPathDistance.VeryFar:
                        if (timeSinceSensed > RELOAD_AMMORATIO_LOW_DIST_ENEMY_VeryFar)
                            return true;
                        break;
                }
                return false;
            }

            if (timeSinceSensed > RELOAD_AMMORATIO_MINIMUM_TIMESINCESEEN)
            {
                return true;
            }
            return false;
        }

        public bool LowOnAmmo(float ratio = 0.3f)
        {
            return AmmoRatio < ratio;
        }

        public float AmmoRatio {
            get
            {
                if (_nextGetRatioTime < Time.time)
                {
                    _nextGetRatioTime = Time.time + 0.025f;
                    _ammoRatio = getAmmoRatio(BotOwner.WeaponManager?.Reload);
                }
                return _ammoRatio;
            }
        }

        private float getAmmoRatio(BotReload reload)
        {
            float ratio = _ammoRatio;
            try
            {
                if (reload != null)
                {
                    int currentAmmo = reload.BulletCount;
                    int maxAmmo = reload.MaxBulletCount;
                    ratio = (float)currentAmmo / maxAmmo;
                }
            }
            catch
            {
                // I HATE THIS STUPID BUG
            }
            return ratio;
        }

        private float _ammoRatio;
        private float _nextGetRatioTime;
    }
}