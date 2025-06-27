using EFT;
using EFT.InventoryLogic;
using SAIN.SAINComponent;
using System;
using System.Collections.Generic;
using UnityEngine;
using static EFT.InventoryLogic.Weapon;

namespace SAIN.Components.BotComponentSpace.Classes
{
    public class ReloadClass : BotBase, IBotClass
    {
        public bool TryReload()
        {
            if (!canReload())
            {
                return false;
            }
            if (tryCatchReload())
            {
                return true;
            }
            if (tryRefillAndReload())
            {
                return true;
            }
            checkChangeToMelee();
            return false;
        }

        private void checkChangeToMelee()
        {
            if (!BotOwner.WeaponManager.Selector.TryChangeWeapon(true) &&
                BotOwner.WeaponManager.Selector.CanChangeToMeleeWeapons)
            {
                var magWeapon = ActiveMagazineWeapon;
                if (magWeapon != null &&
                    magWeapon.FullMagazineCount == 0 &&
                    magWeapon.PartialMagazineCount == 0)
                {
                    BotOwner.WeaponManager.Selector.ChangeToMelee();
                }
                if (magWeapon == null &&
                    BotOwner.WeaponManager.Reload.BulletCount == 0 &&
                    Bot.Enemy.RealDistance < 10f)
                {
                    BotOwner.WeaponManager.Selector.ChangeToMelee();
                }
            }
        }

        private bool tryRefillAndReload()
        {
            var magWeapon = ActiveMagazineWeapon;
            if (magWeapon != null &&
                magWeapon.FullMagazineCount == 0 &&
                magWeapon.EmptyMagazineCount > 0 &&
                magWeapon.TryRefillAllMags() &&
                tryCatchReload())
            {
                magWeapon.BotReloaded();
                return true;
            }
            return false;
        }

        private bool tryCatchReload()
        {
            bool result = false;
            try
            {
                var reload = BotOwner.WeaponManager.Reload;
                if (reload.CanReload(false, out var MagazineItemClass, out var list))
                {
                    if (MagazineItemClass != null)
                    {
                        reload.ReloadMagazine(MagazineItemClass);
                        result = true;
                        reload.Reloading = true;
                    }
                    else if (list != null && list.Count > 0)
                    {
                        reload.ReloadAmmo(list);
                        result = true;
                        reload.Reloading = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error Trying to get Bot to reload: {ex}");
            }

            return result;
        }

        private bool canReload()
        {
            if (BotOwner.WeaponManager.Reload.Reloading)
            {
                return false;
            }
            if (BotOwner.ShootData.Shooting)
            {
                return false;
            }
            if (BotOwner.Medecine.Using)
            {
                return false;
            }
            if (BotOwner.WeaponManager.Malfunctions.HaveMalfunction() &&
                BotOwner.WeaponManager.Malfunctions.MalfunctionType() != Weapon.EMalfunctionState.Misfire)
            {
                return false;
            }
            var magWeapon = ActiveMagazineWeapon;
            if (magWeapon != null)
            {
                var currentMag = magWeapon.Weapon.GetCurrentMagazine();
                if (currentMag != null && currentMag.Count == currentMag.MaxCount)
                {
                    return false;
                }
                if (magWeapon.FullMagazineCount == 0)
                {
                    magWeapon.TryRefillMags(1);
                }
            }

            return true;
        }

        public BotMagazineWeapon ActiveMagazineWeapon { get; private set; }
        public EquipmentSlot ActiveEquipmentSlot => _weaponManager.Selector.EquipmentSlot;
        public Weapon CurrentWeapon => _weaponManager?.CurrentWeapon;

        public readonly Dictionary<EquipmentSlot, BotMagazineWeapon> BotMagazineWeapons = new();

        public ReloadClass(BotComponent bot) : base(bot)
        {
            _weaponManager = Bot.BotOwner.WeaponManager;
            findWeapons();
        }

        private void findWeapons()
        {
            foreach (EquipmentSlot slot in _weapSlots)
            {
                Item item = Bot.Player.Equipment.GetSlot(slot).ContainedItem;
                if (item != null &&
                    item is Weapon weapon &&
                    isMagFed(weapon.ReloadMode))
                {
                    BotMagazineWeapons.Add(slot, new BotMagazineWeapon(weapon, Bot));
                }
            }

            foreach (var weapon in BotMagazineWeapons.Values)
            {
                weapon.Init(BotOwner);
            }
        }

        public void Init()
        {
            _weaponManager.Selector.OnActiveEquipmentSlotChanged += weaponChanged;
        }

        private void weaponChanged(EquipmentSlot slot)
        {
            _weaponChanged = true;
        }

        public void Update()
        {
            checkWeapChanged();
            foreach (var weapon in BotMagazineWeapons.Values)
            {
                weapon?.Update();
            }
            checkRefill();
        }

        private void checkRefill()
        {
            if (_nextCheckRefillTime > Time.time)
            {
                return;
            }
            _nextCheckRefillTime = Time.time + 5;
            if (BotOwner.WeaponManager.Reload.Reloading)
            {
                return;
            }
            if (!Bot.EnemyController.AtPeace)
            {
                return;
            }
            var weapon = ActiveMagazineWeapon;
            if (weapon == null)
            {
                return;
            }
            weapon.RefillRatioOfMags(0.5f);
        }

        private float _nextCheckRefillTime;

        private void checkWeapChanged()
        {
            if (!_weaponChanged)
            {
                return;
            }
            if (ActiveEquipmentSlot == EquipmentSlot.Scabbard)
            {
                _weaponChanged = false;
                return;
            }
            Weapon weapon = CurrentWeapon;
            if (weapon == null)
            {
                return;
            }
            ActiveMagazineWeapon = findOrCreateMagWeapon(weapon);
            _weaponChanged = false;
        }

        private static bool isMagFed(EReloadMode reloadMode)
        {
            switch (reloadMode)
            {
                case EReloadMode.ExternalMagazine:
                case EReloadMode.ExternalMagazineWithInternalReloadSupport:
                    return true;

                default:
                    return false;
            }
        }

        private BotMagazineWeapon findOrCreateMagWeapon(Weapon weapon)
        {
            if (!isMagFed(weapon.ReloadMode))
            {
                return null;
            }

            EquipmentSlot activeSlot = ActiveEquipmentSlot;
            if (!BotMagazineWeapons.TryGetValue(activeSlot, out BotMagazineWeapon magazineWeapon))
            {
                magazineWeapon = new BotMagazineWeapon(weapon, Bot);
                magazineWeapon.Init(BotOwner);
                BotMagazineWeapons.Add(activeSlot, magazineWeapon);
                return magazineWeapon;
            }

            if (magazineWeapon.Weapon != weapon)
            {
                magazineWeapon.Dispose(BotOwner);
                magazineWeapon = new BotMagazineWeapon(weapon, Bot);
                magazineWeapon.Init(BotOwner);
                BotMagazineWeapons[activeSlot] = magazineWeapon;
            }
            return magazineWeapon;
        }

        public void Dispose()
        {
            if (_weaponManager.Selector != null)
            {
                _weaponManager.Selector.OnActiveEquipmentSlotChanged -= weaponChanged;
            }
            if (BotOwner != null)
            {
                foreach (var weapon in BotMagazineWeapons.Values)
                {
                    weapon.Dispose(BotOwner);
                }
            }
            BotMagazineWeapons.Clear();
        }

        private static EquipmentSlot[] _weapSlots =
        [
            EquipmentSlot.FirstPrimaryWeapon,
            EquipmentSlot.SecondPrimaryWeapon,
            EquipmentSlot.Holster
        ];

        private bool _weaponChanged = true;
        private readonly BotWeaponManager _weaponManager;
    }
}
