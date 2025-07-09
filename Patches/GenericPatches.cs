using Comfort.Common;
using EFT;
using EFT.EnvironmentEffect;
using EFT.InventoryLogic;
using HarmonyLib;
using SAIN.Components;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using FoodAndMedsEquipCallbackType = GInterface176;
using QuickGrenadeUseCallbackType = GInterface179;
using SetInHandsMedsStruct = GStruct353<EBodyPart>;

namespace SAIN.Patches.Generic
{
    public static class GenericHelpers
    {
        public static bool CheckNotNull(BotOwner botOwner)
        {
            return botOwner != null &&
                botOwner.gameObject != null &&
                botOwner.gameObject.transform != null &&
                botOwner.Transform != null &&
                !botOwner.IsDead;
        }
    }

    namespace SetInHands
    {
        public static class Helpers
        {
            public static void SetItemEquiped(IPlayer Player, Item Item)
            {
                if (GameWorldComponent.TryGetPlayerComponent(Player, out PlayerComponent PlayerComponent))
                {
                    PlayerComponent.SetItemEquippedInHands(Item);
                }
            }
        }

        public class SetInHands_Empty : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(Player), nameof(Player.SetEmptyHands));
            }

            [PatchPostfix]
            public static void Patch(Player __instance)
            {
                Helpers.SetItemEquiped(__instance, null);
            }
        }

        public class SetInHands_Weapon_Patch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                System.Type[] Params = [typeof(Weapon), typeof(Callback<IFirearmHandsController>)];
                return AccessTools.Method(typeof(Player), nameof(Player.SetInHands), Params);
            }

            [PatchPostfix]
            public static void Patch(Player __instance, Weapon weapon)
            {
                Helpers.SetItemEquiped(__instance, weapon);
            }
        }

        public class SetInHands_Weapon_Stationary_Patch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(Player), nameof(Player.SetStationaryWeapon));
            }

            [PatchPostfix]
            public static void Patch(Player __instance, Weapon weapon)
            {
                Helpers.SetItemEquiped(__instance, weapon);
            }
        }

        public class SetInHands_Knife_Patch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                System.Type[] Params = [typeof(KnifeComponent), typeof(Callback<IKnifeController>)];
                return AccessTools.Method(typeof(Player), nameof(Player.SetInHands), Params);
            }

            [PatchPostfix]
            public static void Patch(Player __instance, KnifeComponent knife)
            {
                Helpers.SetItemEquiped(__instance, knife?.Item);
            }
        }

        public class SetInHands_Grenade_Patch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                System.Type[] Params = [typeof(ThrowWeapItemClass), typeof(Callback<IHandsThrowController>)];
                return AccessTools.Method(typeof(Player), nameof(Player.SetInHands), Params);
            }

            [PatchPrefix]
            public static void PatchPrefix(Player __instance, ThrowWeapItemClass throwWeap)
            {
                float range = SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_GrenadePinDraw;
                BotManagerComponent.Instance?.BotHearing.PlayAISound(__instance.ProfileId, SAINSoundType.GrenadeDraw, __instance.Position, range, 1f);
                Helpers.SetItemEquiped(__instance, throwWeap);
            }
        }

        public class SetInHands_Food_Patch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                System.Type[] Params = [typeof(FoodDrinkItemClass), typeof(float), typeof(int), typeof(Callback<FoodAndMedsEquipCallbackType>)];
                return AccessTools.Method(typeof(Player), nameof(Player.SetInHands), Params);
            }

            [PatchPrefix]
            public static void PatchPrefix(Player __instance, FoodDrinkItemClass foodDrink)
            {
                float range = SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_EatDrink;
                BotManagerComponent.Instance?.BotHearing.PlayAISound(__instance.ProfileId, SAINSoundType.Food, __instance.Position, range, 1f);
                Helpers.SetItemEquiped(__instance, foodDrink);
            }
        }

        public class SetInHands_Meds_Patch1 : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                System.Type[] Params = [typeof(MedsItemClass), typeof(EBodyPart), typeof(int), typeof(Callback<FoodAndMedsEquipCallbackType>)];
                return AccessTools.Method(typeof(Player), nameof(Player.SetInHands), Params);
            }

            [PatchPrefix]
            public static void PatchPrefix(MedsItemClass meds, Player __instance)
            {
                SAINSoundType soundType;
                float range;
                if (meds != null && meds.HealthEffectsComponent.AffectsAny([EDamageEffectType.DestroyedPart]))
                {
                    soundType = SAINSoundType.Surgery;
                    range = SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_Surgery;
                }
                else
                {
                    soundType = SAINSoundType.Heal;
                    range = SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_Healing;
                }
                BotManagerComponent.Instance?.BotHearing.PlayAISound(__instance.ProfileId, soundType, __instance.Position, range, 1f);
                Helpers.SetItemEquiped(__instance, meds);
            }
        }

        public class SetInHands_Meds_Patch2 : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                System.Type[] Params = [typeof(MedsItemClass), typeof(SetInHandsMedsStruct), typeof(int), typeof(Callback<FoodAndMedsEquipCallbackType>)];
                return AccessTools.Method(typeof(Player), nameof(Player.SetInHands), Params);
            }

            [PatchPrefix]
            public static void PatchPrefix(MedsItemClass meds, Player __instance)
            {
                SAINSoundType soundType;
                float range;
                if (meds != null && meds.HealthEffectsComponent.AffectsAny([EDamageEffectType.DestroyedPart]))
                {
                    soundType = SAINSoundType.Surgery;
                    range = SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_Surgery;
                }
                else
                {
                    soundType = SAINSoundType.Heal;
                    range = SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_Healing;
                }
                BotManagerComponent.Instance?.BotHearing.PlayAISound(__instance.ProfileId, soundType, __instance.Position, range, 1f);
                Helpers.SetItemEquiped(__instance, meds);
            }
        }

        public class SetInHands_QuickUse_Patch1 : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                System.Type[] Params = [typeof(Item), typeof(Callback<IOnHandsUseCallback>)];
                return AccessTools.Method(typeof(Player), nameof(Player.SetInHandsForQuickUse), Params);
            }

            [PatchPrefix]
            public static void PatchPrefix(Item quickUseItem, Player __instance)
            {
                Helpers.SetItemEquiped(__instance, quickUseItem);
            }
        }

        public class SetInHands_QuickUse_Patch2 : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                System.Type[] Params = [typeof(ThrowWeapItemClass), typeof(Callback<QuickGrenadeUseCallbackType>)];
                return AccessTools.Method(typeof(Player), nameof(Player.SetInHandsForQuickUse), Params);
            }

            [PatchPrefix]
            public static void PatchPrefix(ThrowWeapItemClass throwWeap, Player __instance)
            {
                Helpers.SetItemEquiped(__instance, throwWeap);
            }
        }
    }

    public class StopRefillMagsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotReload), nameof(BotReload.method_1));
        }

        [PatchPrefix]
        public static bool Patch(BotOwner ___botOwner_0)
        {
            return SAINPlugin.IsBotExluded(___botOwner_0);
        }
    }

    public class RefillMagazinePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotReload), nameof(BotReload.method_2));
        }

        [PatchPrefix]
        public static bool Patch(BotReload __instance, BotOwner ___botOwner_0, Weapon weapon, MagazineItemClass foundMag)
        {
            if (!___botOwner_0.GetPlayer.HealthController.IsAlive)
            {
                return false;
            }
            AmmoItemClass ammoItemClass = method_3(weapon, foundMag, ___botOwner_0.GetPlayer);
            if (ammoItemClass == null)
            {
                //__instance.NoAmmoForReloadCached = true;
                return false;
            }
            //__instance.NoAmmoForReloadCached = false;
            GStruct454 operationResult = foundMag.Apply(__instance.botOwner_0.GetPlayer.InventoryController, ammoItemClass, ammoItemClass.StackObjectsCount, true);
            if (operationResult.Failed)
            {
                Logger.LogDebug($"failed to fill mag [{operationResult.Error?.ToString()}]");
            }
            __instance.botOwner_0.GetPlayer.InventoryController.TryRunNetworkTransaction(operationResult, new Callback(BotReload.smethod_0));
            return false;
        }

        private static AmmoItemClass method_3(Weapon weapon, MagazineItemClass foundMag, Player player)
        {
            Slot slot = weapon.HasChambers ? weapon.Chambers[0] : null;
            _preallocatedAmmoList.Clear();
            player.InventoryController.GetAcceptableItemsNonAlloc<AmmoItemClass>(BotReload._availableEquipmentSlots, _preallocatedAmmoList, null, null);
            AmmoItemClass selectedAmmo = null;
            foreach (AmmoItemClass inventoryAmmo in _preallocatedAmmoList)
            {
                bool slotAccepts = slot != null && slot.CanAccept(inventoryAmmo);
                bool filterAccepts = foundMag.Cartridges.Filters.CheckItemFilter(inventoryAmmo) && (selectedAmmo == null || selectedAmmo.StackObjectsCount < inventoryAmmo.StackObjectsCount);
                if (slotAccepts || filterAccepts)
                {
                    Logger.LogDebug($"Ammo [{inventoryAmmo.Name}] Slot Accepts? [{slotAccepts}] filterAccepts? [{filterAccepts}]");
                    selectedAmmo = inventoryAmmo;
                }
            }
            Logger.LogDebug($"Ammo [{selectedAmmo?.Name}] Selected out of {_preallocatedAmmoList.Count}");
            _preallocatedAmmoList.Clear();
            return selectedAmmo;
        }

        private static readonly List<AmmoItemClass> _preallocatedAmmoList = new(100);
    }

    public class GetAmmoForRefillPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotReload), nameof(BotReload.method_3));
        }

        [PatchPrefix]
        public static bool Patch(BotOwner ___botOwner_0, Weapon weapon, MagazineItemClass foundMag, ref AmmoItemClass __result)
        {
            __result = method_3(weapon, foundMag, ___botOwner_0.GetPlayer);
            return false;
        }

        private static AmmoItemClass method_3(Weapon weapon, MagazineItemClass foundMag, Player player)
        {
            Slot slot = weapon.HasChambers ? weapon.Chambers[0] : null;
            _preallocatedAmmoList.Clear();
            player.InventoryController.GetAcceptableItemsNonAlloc<AmmoItemClass>(BotReload._availableEquipmentSlots, _preallocatedAmmoList, null, null);
            AmmoItemClass ammoItemClass = null;
            foreach (AmmoItemClass ammoItemClass2 in _preallocatedAmmoList)
            {
                bool slotAccepts = slot != null && slot.CanAccept(ammoItemClass2);
                bool filterAccepts = foundMag.Cartridges.Filters.CheckItemFilter(ammoItemClass2) && (ammoItemClass == null || ammoItemClass.StackObjectsCount < ammoItemClass2.StackObjectsCount);
                Logger.LogDebug($"Ammo [{ammoItemClass2.Name}] Slot Accepts? [{slotAccepts}] filterAccepts? [{filterAccepts}]");
                if (slotAccepts || filterAccepts)
                {
                    ammoItemClass = ammoItemClass2;
                }
            }
            Logger.LogDebug($"Ammo [{ammoItemClass?.Name}] Selected out of {_preallocatedAmmoList.Count}");
            _preallocatedAmmoList.Clear();
            return ammoItemClass;
        }

        private static readonly List<AmmoItemClass> _preallocatedAmmoList = new(100);
    }

    public class SetEnvironmentPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass567), nameof(GClass567.SetEnvironment));
        }

        [PatchPostfix]
        public static void Patch(GClass567 __instance, IndoorTrigger trigger)
        {
            BotManagerComponent.Instance?.PlayerEnviromentChanged(__instance?.Player?.ProfileId, trigger);
        }
    }

    public class AddPointToSearchPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotsGroup), nameof(BotsGroup.AddPointToSearch));
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotsGroup __instance, BotOwner owner)
        {
            return SAINPlugin.IsBotExluded(owner);
        }
    }

    public class SetPanicPointPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotMemoryClass), nameof(BotMemoryClass.SetPanicPoint));
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotOwner ___botOwner_0)
        {
            return SAINPlugin.IsBotExluded(___botOwner_0);
        }
    }

    public class HaveSeenEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.PropertyGetter(typeof(EnemyInfo), nameof(EnemyInfo.HaveSeen));
        }

        [PatchPostfix]
        public static void PatchPostfix(ref bool __result, EnemyInfo __instance)
        {
            if (__result == true)
            {
                return;
            }
            if (SAINEnableClass.GetSAIN(__instance.Owner, out var sain)
                //&& sain.Info.Profile.IsPMC
                && sain.EnemyController.CheckAddEnemy(__instance.Person)?.EnemyKnown == true)
            {
                __result = true;
            }
        }
    }

    public class TurnDamnLightOffPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotWeaponSelector), nameof(BotWeaponSelector.TryChangeToSlot));
        }

        [PatchPrefix]
        public static void PatchPrefix(ref BotOwner ___botOwner_0)
        {
            // Try to turn a gun's light off before swapping weapon.
            ___botOwner_0?.BotLight?.TurnOff(false, true);
        }
    }

    internal class ShallKnowEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EnemyInfo), nameof(EnemyInfo.ShallISuppress));
        }

        [PatchPostfix]
        public static void PatchPostfix(EnemyInfo __instance, ref bool __result)
        {
            if (!SAINEnableClass.GetSAIN(__instance.Owner, out var botComponent))
            {
                return;
            }
            var enemy = botComponent.EnemyController.CheckAddEnemy(__instance.Person);
            __result = enemy?.EnemyKnown == true;
        }
    }

    internal class ShallKnowEnemyLatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EnemyInfo), nameof(EnemyInfo.ShallKnowEnemyLate));
        }

        [PatchPostfix]
        public static void PatchPostfix(EnemyInfo __instance, ref bool __result)
        {
            if (!SAINEnableClass.GetSAIN(__instance.Owner, out var botComponent))
            {
                return;
            }
            var enemy = botComponent.EnemyController.CheckAddEnemy(__instance.Person);
            __result = enemy?.EnemyKnown == true;
        }
    }

    public class GrenadeThrownActionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotsController), nameof(BotsController.method_5));
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotsController __instance, Grenade grenade, Vector3 position, Vector3 force, float mass)
        {
            Vector3 danger = Vector.DangerPoint(position, force, mass);
            foreach (BotOwner bot in __instance.Bots.BotOwners)
            {
                if (SAINPlugin.IsBotExluded(bot))
                {
                    bot.BewareGrenade.AddGrenadeDanger(danger, grenade);
                }
            }
            return false;
        }
    }

    public class GrenadeExplosionActionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotsController), nameof(BotsController.method_3));
        }

        [PatchPrefix]
        public static bool PatchPrefix()
        {
            return false;
        }
    }

    public class AllowRequestPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotRequestController), nameof(BotRequestController.method_2));
        }

        [PatchPrefix]
        public static bool Patch(BotOwner ____owner)
        {
            BotRequest curRequest = ____owner.BotRequestController.CurRequest;
            if (curRequest == null)
            {
                return false;
            }
            if (!SAINEnableClass.GetSAIN(____owner, out BotComponent sain))
            {
                return true;
            }
            if (sain.HasEnemy && curRequest.Requester?.IsAI == true)
            {
                curRequest.Dispose();
                return false;
            }
            return true;
        }
    }

    public class FindRequestForMePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotGroupRequestController), nameof(BotGroupRequestController.FindForMe));
        }

        [PatchPrefix]
        public static bool Patch(BotOwner executer, List<BotRequest> ____listOfRequests)
        {
            // Copied original code in FindForMe, but add check to see if requester is AI or not if this bot currently has an active enemy.
            // START NEW //
            if (!SAINEnableClass.GetSAIN(executer, out BotComponent sain))
            {
                return true;
            }
            if (!sain.HasEnemy)
            {
                return true;
            }
            // END NEW //

            BotRequest botRequest = null;
            foreach (BotRequest botRequest2 in ____listOfRequests)
            {
                // START NEW //
                IPlayer requestor = botRequest2.Requester;
                if (requestor != null && requestor.IsAI)
                {
                    continue;
                }
                // END NEW //
                if ((botRequest2.CanExecuteByMyself || (Player)botRequest2.Requester != executer.GetPlayer) && (!executer.Boss.IamBoss || executer.Boss.AllowRequestSelf || executer.GetPlayer.Id != botRequest2.Requester.Id) && botRequest2.CanStartExecute(executer))
                {
                    botRequest = botRequest2;
                    break;
                }
            }
            if (botRequest != null)
            {
                botRequest.Take(executer);
                ____listOfRequests.Remove(botRequest);
            }
            return false;
        }
    }
}