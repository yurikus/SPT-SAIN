using EFT;
using EFT.InventoryLogic;
using SAIN.SAINComponent;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.Components.BotComponentSpace.Classes
{
    public class BotMagazineWeapon
    {
        public BotMagazineWeapon(Weapon weapon, BotComponent bot)
        {
            Weapon = weapon;
            _inventoryController = bot.Player.InventoryController;
            _weaponManager = bot.BotOwner.WeaponManager;
            findMags();
            TryRefillAllMags();
        }

        public void Init(BotOwner botOwner)
        {
            if (botOwner == null)
                return;

            if (botOwner.ItemTaker != null)
                botOwner.ItemTaker.OnItemTaken += checkItemTaken;

            if (botOwner.ItemDropper != null)
                botOwner.ItemDropper.OnItemDrop += checkItemDropped;
        }

        public void Dispose(BotOwner botOwner)
        {
            Magazines.Clear();

            if (botOwner == null)
                return;

            if (botOwner.ItemTaker != null)
                botOwner.ItemTaker.OnItemTaken -= checkItemTaken;

            if (botOwner.ItemDropper != null)
                botOwner.ItemDropper.OnItemDrop -= checkItemDropped;
        }

        private void checkItemTaken(Item item, IPlayer player)
        {
            if (item == null) return;
            if (item is MagazineItemClass mag &&
                _refill.magazineSlot?.CanAccept(mag) == true)
            {
                _needToRecheck = true;
            }
        }

        private void checkItemDropped(Item item)
        {
            if (item == null) return;
            if (item is MagazineItemClass mag &&
                Magazines.Contains(mag))
            {
                _needToRecheck = true;
            }
        }

        private void findMags()
        {
            Weapon weapon = Weapon;
            if (weapon == null)
            {
                //Logger.LogDebug("Weapon Null");
                _magsFound = false;
                return;
            }
            _magsFound = getNonActiveMagazines() > 0;
        }

        public void RecheckMagazines()
        {
            _needToRecheck = false;
            findMags();
            checkMagAmmoStatus();
        }

        public bool RefillRatioOfMags(float ratio)
        {
            RecheckMagazines();
            int magCount = Magazines.Count;
            if (magCount == 0)
            {
                return false;
            }
            int fullCount = FullMagazineCount;
            float fullRatio = (float)fullCount / (float)magCount;
            if (fullRatio >= ratio)
            {
                return true;
            }

            int countToReload = Mathf.RoundToInt((float)magCount / 2f);
            if (countToReload < 1)
                countToReload = 1;

            return TryRefillMags(countToReload);
        }

        public void BotReloaded()
        {
            _needToRecheck = true;
        }

        private bool _magsFound;
        private bool _needToRecheck = true;

        public void Update()
        {
            if (_needToRecheck &&
                !_weaponManager.Reload.Reloading)
            {
                RecheckMagazines();
            }
        }

        private int getNonActiveMagazines()
        {
            if (getActiveMagazine() == null)
            {
                //Logger.LogDebug("Active Magazine is Null!");
                return 0;
            }
            Magazines.Clear();
            _inventoryController.GetReachableItemsOfTypeNonAlloc<MagazineItemClass>(Magazines, _refill.canAccept);
            return Magazines.Count;
        }

        public bool TryRefillAllMags()
        {
            return TryRefillMags(-1);
        }

        public bool TryRefillMags(int count)
        {
            if (!_magsFound)
            {
                //Logger.LogDebug($"no magazines found!");
                return false;
            }
            Weapon weapon = Weapon;
            if (weapon == null)
            {
                //Logger.LogDebug("Weapon Null");
                return false;
            }
            return refillMagsInList(Magazines, weapon, count);
        }

        private Slot getActiveMagazine()
        {
            Slot slot = Weapon?.GetMagazineSlot();
            _refill.magazineSlot = slot;
            return slot;
        }

        private bool refillMagsInList(List<MagazineItemClass> list, Weapon weapon, int numberToRefill = -1)
        {
            int refilled = 0;
            int full = 0;
            foreach (MagazineItemClass mag in list)
            {
                if (mag == null) continue;
                int capacity = mag.MaxCount;
                if (mag.Count == capacity)
                {
                    full++;
                    continue;
                }
                _weaponManager.Reload.method_2(weapon, mag);
                if (mag.Count == capacity) refilled++;
                if (numberToRefill < 0) continue;
                if (refilled >= numberToRefill) break;
            }
            //Logger.LogDebug($"Refilled [{refilled}] magazines. Full Mags: [{full}]");
            return refilled > 0 || full > 0;
        }

        private void checkMagAmmoStatus()
        {
            FullMagazineCount = 0;
            PartialMagazineCount = 0;
            EmptyMagazineCount = 0;
            foreach (MagazineItemClass mag in Magazines)
            {
                if (mag == null) continue;
                float ratio = getAmmoRatio(mag);
                if (ratio <= 0)
                {
                    EmptyMagazineCount++;
                    continue;
                }
                if (ratio < 1f)
                {
                    PartialMagazineCount++;
                    continue;
                }
                FullMagazineCount++;
            }
        }

        public bool CheckAnyMagHasAmmo(float ratioToCheck)
        {
            foreach (MagazineItemClass mag in Magazines)
            {
                float ammoRatio = getAmmoRatio(mag);
                if (ammoRatio >= ratioToCheck)
                {
                    return true;
                }
            }
            return false;
        }

        private float getAmmoRatio(MagazineItemClass magazine)
        {
            if (magazine == null) return 0.0f;

            return (float)magazine.Count / (float)magazine.MaxCount;
        }

        public int FullMagazineCount { get; private set; }
        public int PartialMagazineCount { get; private set; }
        public int EmptyMagazineCount { get; private set; }

        public readonly Weapon Weapon;
        public readonly List<MagazineItemClass> Magazines = new();
        private readonly BotWeaponManager _weaponManager;
        private readonly InventoryController _inventoryController;
        private MagRefillClass _refill = new();
    }
}