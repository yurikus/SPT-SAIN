using EFT;
using EFT.InventoryLogic;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Decision
{
    public class SelfActionDecisionClass : BotBase, IBotClass
    {
        public SelfActionDecisionClass(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
            base.SubscribeToPreset(null);
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }

        public ESelfDecision CurrentSelfAction => Bot.Decision.CurrentSelfDecision;

        public bool GetDecision(out ESelfDecision Decision)
        {
            if (Bot.Enemy == null)
            {
                Decision = ESelfDecision.None;
                return false;
            }

            if (CheckContinueSelfAction(out Decision))
            {
                return true;
            }

            if (StartBotReload())
            {
                Decision = ESelfDecision.Reload;
                return true;
            }

            if (_nextCheckHealTime < Time.time)
            {
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
                if (Bot.Medical.Surgery.AreaClearForSurgery)
                {
                    Decision = ESelfDecision.Surgery;
                    return true;
                }
            }

            return false;
        }

        private float _nextCheckHealTime;

        private void TryFixBusyHands()
        {
            if (BusyHandsTimer > Time.time)
            {
                return;
            }
            BusyHandsTimer = Time.time + 1f;

            var selector = BotOwner.WeaponManager?.Selector;
            if (selector == null)
            {
                return;
            }
            if (selector.TryChangeWeapon(true))
            {
                return;
            }
            if (selector.TakePrevWeapon())
            {
                return;
            }
            if (selector.TryChangeToMain())
            {
                return;
            }
            if (selector.CanChangeToSecondWeapons)
            {
                selector.ChangeToSecond();
                return;
            }
        }

        private float BusyHandsTimer;
        private float _nextCheckReloadTime;

        private bool CheckContinueSelfAction(out ESelfDecision Decision)
        {
            switch (CurrentSelfAction)
            {
                case ESelfDecision.FirstAid:
                    return checkContinueFirstAid(_timeSinceChangeDecision, out Decision);

                case ESelfDecision.Reload:
                    return checkContinueReload(_timeSinceChangeDecision, out Decision);

                case ESelfDecision.Surgery:
                    return checkContinueSurgery(out Decision);

                case ESelfDecision.Stims:
                    return checkContinueStims(_timeSinceChangeDecision, out Decision);

                default:
                    Decision = ESelfDecision.None;
                    return false;
            }
        }

        private bool checkContinueReload(float timeSinceChange, out ESelfDecision Decision)
        {
            bool reloading = BotOwner.WeaponManager.Reload?.Reloading == true;
            if (!StartBotReload())
            {
                if (reloading)
                {
                    Bot.SelfActions.BotCancelReload();
                }
                Decision = ESelfDecision.None;
                return false;
            }

            if (reloading || timeSinceChange < 0.5f)
            {
                if (timeSinceChange > 5f)
                {
                    Bot.SelfActions.BotCancelReload();
                    Decision = ESelfDecision.None;
                    return false;
                }
                Decision = ESelfDecision.Reload;
                return true;
            }

            Decision = ESelfDecision.None;
            return false;
        }

        private bool checkContinueSurgery(out ESelfDecision Decision)
        {
            if (BotOwner?.Medecine == null)
            {
                Decision = ESelfDecision.None;
                return false;
            }
            if (Bot.Medical.Surgery.AreaClearForSurgery &&
                !checkDecisionTooLong())
            {
                Decision = ESelfDecision.Surgery;
                return true;
            }
            Bot.Medical.TryCancelHeal();
            Decision = ESelfDecision.None;
            return false;
        }

        private bool checkContinueFirstAid(float timeSinceChange, out ESelfDecision Decision)
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
                TryFixBusyHands();
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
                TryFixBusyHands();
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

        private bool ContinueReload => BotOwner.WeaponManager.Reload?.Reloading == true && CurrentSelfAction == ESelfDecision.Reload; //  && !StartCancelReload()

        public bool CanUseStims
        {
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
            foreach (Enemy enemy in Bot.EnemyController.EnemyLists.KnownEnemies)
                if (!shallUseStimsCheckEnemy(enemy))
                    return false;
            return true;
        }

        private bool startFirstAid()
        {
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
            foreach (Enemy enemy in Bot.EnemyController.EnemyLists.KnownEnemies)
                if (!shallFirstAidCheckEnemy(enemy))
                    return false;

            return true;
        }

        private bool shallHealInLineOfSight(Enemy enemy, out bool enemyVisible)
        {
            if (enemy == null)
            {
                enemyVisible = false;
                return false;
            }
            if (!enemy.IsVisible && !enemy.InLineOfSight)
            {
                enemyVisible = false;
                return false;
            }
            enemyVisible = true;
            if (Bot.Decision.RunningToCover)
            {
                return Bot.Memory.Health.Dying || Bot.Memory.Health.BadlyInjured;
            }
            return false;
        }

        private bool shallUseStimsCheckEnemy(Enemy enemy)
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

            switch (enemy.EPathDistance)
            {
                case EPathDistance.VeryClose:
                    return timeSinceLastKnownUpdated > 6f;

                case EPathDistance.Close:
                    return timeSinceLastKnownUpdated > 3f;

                case EPathDistance.Mid:
                    return enemy.TimeSinceSeen > 2f;

                case EPathDistance.Far:
                    return true;

                case EPathDistance.VeryFar:
                    return true;

                default:
                    return false;
            }
        }

        private bool shallFirstAidCheckEnemy(Enemy enemy)
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
            if (!enemy.Seen && timeSinceLastKnownUpdated > 8f)
            {
                return true;
            }

            switch (Bot.Memory.Health.HealthStatus)
            {
                default:
                    return false;

                case ETagStatus.Injured:

                    switch (enemy.EPathDistance)
                    {
                        case EPathDistance.VeryClose:
                            return timeSinceLastKnownUpdated > 20f;

                        case EPathDistance.Close:
                            return timeSinceLastKnownUpdated > 15f;

                        case EPathDistance.Mid:
                            return enemy.TimeSinceSeen > 8f;

                        case EPathDistance.Far:
                            return enemy.TimeSinceSeen > 5f;

                        case EPathDistance.VeryFar:
                            return enemy.TimeSinceSeen > 3f;

                        default:
                            return false;
                    }

                case ETagStatus.BadlyInjured:

                    switch (enemy.EPathDistance)
                    {
                        case EPathDistance.VeryClose:
                            return timeSinceLastKnownUpdated > 18;

                        case EPathDistance.Close:
                            return timeSinceLastKnownUpdated > 12;

                        case EPathDistance.Mid:
                            return enemy.TimeSinceSeen > 6;

                        case EPathDistance.Far:
                            return enemy.TimeSinceSeen > 4;

                        case EPathDistance.VeryFar:
                            return enemy.TimeSinceSeen > 2;

                        default:
                            return false;
                    }

                case ETagStatus.Dying:

                    switch (enemy.EPathDistance)
                    {
                        case EPathDistance.VeryClose:
                            return timeSinceLastKnownUpdated > 15;

                        case EPathDistance.Close:
                            return timeSinceLastKnownUpdated > 10;

                        case EPathDistance.Mid:
                            return enemy.TimeSinceSeen > 4;

                        case EPathDistance.Far:
                            return enemy.TimeSinceSeen > 3;

                        case EPathDistance.VeryFar:
                            return enemy.TimeSinceSeen > 2;

                        default:
                            return false;
                    }
            }
        }

        public bool StartCancelReload()
        {
            if (!BotOwner.WeaponManager?.IsReady == true || BotOwner.WeaponManager.Reload.BulletCount == 0 || BotOwner.WeaponManager.CurrentWeapon.ReloadMode == EFT.InventoryLogic.Weapon.EReloadMode.ExternalMagazine)
            {
                return false;
            }

            var enemy = Bot.Enemy;
            if (enemy != null && BotOwner.WeaponManager.Reload.Reloading && Bot.Enemy != null)
            {
                var pathStatus = enemy.EPathDistance;
                bool SeenRecent = Time.time - enemy.TimeSinceSeen > 3f;

                if (SeenRecent && Vector3.Distance(BotOwner.Position, enemy.EnemyIPlayer.Position) < 8f)
                {
                    return true;
                }

                if (!LowOnAmmo(0.15f) && enemy.IsVisible)
                {
                    return true;
                }
                if (pathStatus == EPathDistance.VeryClose)
                {
                    return true;
                }
                if (BotOwner.WeaponManager.Reload.BulletCount > 1 && pathStatus == EPathDistance.Close)
                {
                    return true;
                }
            }

            return false;
        }

        private bool StartBotReload()
        {
            if (BotOwner.WeaponManager?.Reload?.Reloading == true)
            {
                if (Bot.Enemy?.IsVisible == true && BotOwner.WeaponManager.Reload.BulletCount > 3)
                {
                    TryStopReload();
                    return false;
                }
                BotOwner.WeaponManager.Reload.CheckReloadLongTime();
                _nextCheckReloadTime = Time.time + 0.5f;
                _needToReload = false;
                return Bot.Decision.CurrentSelfDecision == ESelfDecision.Reload;
            }

            // Only allow reloading every 1 seconds to avoid spamming reload when the weapon data is bad
            if (_nextCheckReloadTime < Time.time)
            {
                _nextCheckReloadTime = Time.time + 0.5f;
                _needToReload = checkNeedToReload();
            }
            return _needToReload;
        }

        public void TryStopReload()
        {
            if (this._nextPossibleTryStopReload < Time.time)
            {
                this._nextPossibleTryStopReload = Time.time + 1f;
                BotOwner.ShootData.Shoot();
            }
        }

        private float _nextPossibleTryStopReload;

        private bool checkNeedToReload()
        {
            if (BotOwner.WeaponManager?.IsReady == false)
            {
                return false;
            }

            if (BotOwner.Medecine?.Using == true)
            {
                return false;
            }

            if (BotOwner.WeaponManager.Malfunctions.HaveMalfunction() &&
                BotOwner.WeaponManager.Malfunctions.MalfunctionType() != Weapon.EMalfunctionState.Misfire)
            {
                return false;
            }

            var currentMagazine = BotOwner.WeaponManager.CurrentWeapon.GetCurrentMagazine();
            if (currentMagazine != null && currentMagazine.MaxCount == BotOwner.WeaponManager.CurrentWeapon.GetCurrentMagazineCount())
            {
                return false;
            }

            if (!BotOwner.WeaponManager.Reload.CanReload(true))
            {
                return false;
            }

            float ammoRatio = AmmoRatio;
            if (ammoRatio >= 0.85f)
            {
                return false;
            }
            if (ammoRatio <= 0)
            {
                return true;
            }

            var enemy = Bot.Enemy;
            if (enemy == null)
            {
                return ammoRatio < 0.8f;
            }

            EPathDistance distance = enemy.EPathDistance;

            if (ammoRatio > 0.66f)
            {
                if (enemy.TimeSinceSeen > 15f)
                {
                    return true;
                }
                switch (distance)
                {
                    case EPathDistance.VeryClose:
                        break;

                    case EPathDistance.Close:
                        if (enemyNotSeenFor(enemy, 12f))
                        {
                            return true;
                        }
                        break;

                    case EPathDistance.Mid:
                        if (enemyNotSeenFor(enemy, 6f))
                        {
                            return true;
                        }
                        break;

                    case EPathDistance.Far:
                    case EPathDistance.VeryFar:
                        if (enemyNotSeenFor(enemy, 3f))
                        {
                            return true;
                        }
                        break;
                }
                return false;
            }

            if (ammoRatio > 0.4f)
            {
                if (enemy.TimeSinceSeen > 10f)
                {
                    return true;
                }
                switch (distance)
                {
                    case EPathDistance.VeryClose:
                        break;

                    case EPathDistance.Close:
                        if (enemyNotSeenFor(enemy, 8f))
                        {
                            return true;
                        }
                        break;

                    case EPathDistance.Mid:
                        if (enemyNotSeenFor(enemy, 4f))
                        {
                            return true;
                        }
                        break;

                    case EPathDistance.Far:
                    case EPathDistance.VeryFar:
                        if (enemyNotSeenFor(enemy, 2f))
                        {
                            return true;
                        }
                        break;
                }
                return false;
            }

            if (ammoRatio > 0.2f)
            {
                if (enemy.TimeSinceSeen > 4f)
                {
                    return true;
                }
                switch (distance)
                {
                    case EPathDistance.VeryClose:
                    case EPathDistance.Close:
                        if (enemyNotSeenFor(enemy, 2f))
                        {
                            return true;
                        }
                        break;

                    case EPathDistance.Mid:
                    case EPathDistance.Far:
                    case EPathDistance.VeryFar:
                        if (enemyNotSeenFor(enemy, 1f))
                        {
                            return true;
                        }
                        break;
                }
                return false;
            }

            return enemy.TimeSinceSeen > 2f;
        }

        private bool enemyNotSeenFor(Enemy enemy, float time)
        {
            return enemy != null &&
                !enemy.IsVisible &&
                enemy.TimeSinceSeen > time;
        }

        private bool _needToReload;

        public bool LowOnAmmo(float ratio = 0.3f)
        {
            return AmmoRatio < ratio;
        }

        public float AmmoRatio
        {
            get
            {
                if (_nextGetRatioTime < Time.time)
                {
                    _nextGetRatioTime = Time.time + 0.1f;
                    _ammoRatio = getAmmoRatio();
                }
                return _ammoRatio;
            }
        }

        private float getAmmoRatio()
        {
            float ratio = _ammoRatio;
            try
            {
                int currentAmmo = BotOwner.WeaponManager.Reload.BulletCount;
                int maxAmmo = BotOwner.WeaponManager.Reload.MaxBulletCount;
                ratio = (float)currentAmmo / maxAmmo;
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