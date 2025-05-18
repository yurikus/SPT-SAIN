using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SAIN.Components;
using SPT.Reflection.Patching;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SAIN.Patches.Generic.Fixes
{
    internal class EnableVaultPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.InitVaultingComponent));
        }

        [PatchPrefix]
        public static void Patch(Player __instance, ref bool aiControlled)
        {
            if (__instance.UsedSimplifiedSkeleton)
                return;

            aiControlled = false;
        }
    }

    internal class FightShallReloadFixPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotReload), "FightShallReload");
        }

        [PatchPrefix]
        public static bool Patch(BotOwner ___botOwner_0, ref bool __result)
        {
            if (SAINPlugin.IsBotExluded(___botOwner_0))
            {
                return true;
            }
            if (___botOwner_0.Medecine?.Using == true)
            {
                __result = false;
                return false;
            }
            if (!___botOwner_0.WeaponManager.IsWeaponReady)
            {
                __result = false;
                return false;
            }
            if (___botOwner_0.WeaponManager.Malfunctions.HaveMalfunction() &&
                ___botOwner_0.WeaponManager.Malfunctions.MalfunctionType() != Weapon.EMalfunctionState.Misfire)
            {
                __result = false;
                return false;
            }
            __result = true;
            return false;
        }
    }

    internal class FixItemTakerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotItemTaker), nameof(BotItemTaker.method_12));
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotOwner ___botOwner_0)
        {
            return GenericHelpers.CheckNotNull(___botOwner_0);
        }
    }

    internal class FixItemTakerPatch2 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotItemTaker), "RefreshClosestItems");
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotOwner ___botOwner_0)
        {
            return GenericHelpers.CheckNotNull(___botOwner_0);
        }
    }

    internal class FixPatrolDataPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass483), "method_4");
        }

        [PatchPrefix]
        public static bool PatchPrefix(List<BotOwner> followers, ref bool __result)
        {
            using (List<BotOwner>.Enumerator enumerator = followers.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (!GenericHelpers.CheckNotNull(enumerator.Current) || enumerator.Current.BotFollower?.PatrolDataFollower?.HaveProblems == true)
                    {
                        __result = false;
                        return false;
                    }
                }
            }
            __result = true;
            return false;
        }
    }

    internal class HealCancelPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass455), "CancelCurrent");
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref BotOwner ___botOwner_0, ref bool ___bool_1, ref bool ___bool_0, ref float ___float_0)
        {
            if (___bool_1)
            {
                return false;
            }
            if (___bool_0)
            {
                if (___float_0 < Time.time + 3f)
                {
                    ___float_0 += 5f;
                }
                ___bool_1 = true;
                SAINBotController.Instance?.StartCoroutine(CancelHeal(___botOwner_0));
            }
            return false;
        }

        private static IEnumerator CancelHeal(BotOwner bot)
        {
            yield return new WaitForSeconds(0.25f);
            if (bot != null &&
                bot.GetPlayer != null &&
                bot.GetPlayer.HealthController.IsAlive &&
                bot.Medecine?.Using == true)
            {
                bot.WeaponManager?.Selector?.TakePrevWeapon();
            }
        }
    }

    internal class NoTeleportPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotMover), "GoToPoint", [typeof(Vector3), typeof(bool), typeof(float), typeof(bool), typeof(bool), typeof(bool)]);
        }

        [PatchPrefix]
        public static void PatchPrefix(ref bool mustHaveWay)
        {
            mustHaveWay = false;
        }
    }

    internal class RotateClampPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "Rotate");
        }

        [PatchPrefix]
        public static void PatchPrefix(ref Player __instance, ref bool ignoreClamp)
        {
            if (__instance?.IsAI == true
                && __instance.IsSprintEnabled)
            {
                ignoreClamp = true;
            }
        }
    }

    internal class BotGroupAddEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(BotsGroup).GetMethod("AddEnemy");

        [PatchPrefix]
        public static bool PatchPrefix(IPlayer person)
        {
            if (person == null || person.IsAI && person.AIData?.BotOwner?.GetPlayer == null)
            {
                return false;
            }

            return true;
        }
    }

    internal class BotMemoryAddEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(BotMemoryClass).GetMethod("AddEnemy");

        [PatchPrefix]
        public static bool PatchPrefix(IPlayer enemy)
        {
            if (enemy == null || enemy.IsAI && enemy.AIData?.BotOwner?.GetPlayer == null)
            {
                return false;
            }

            return true;
        }
    }

    internal class SkipLookForCoverPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(BotCoversData).GetMethod("GetClosestPoint");

        [PatchPrefix]
        public static bool PatchPrefix(BotOwner ___botOwner_0)
        {
            return SAINPlugin.IsBotExluded(___botOwner_0);
        }
    }

    public class StopSetToNavMeshPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotMover), "method_9");
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref BotOwner ___botOwner_0)
        {
            return SAINPlugin.IsBotExluded(___botOwner_0);
        }
    }
}