using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SAIN.Components;
using SPT.Reflection.Patching;
using System.Reflection;

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
            return AccessTools.Method(typeof(BotItemTaker), nameof(BotItemTaker.RefreshClosestItems));
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotOwner ___botOwner_0)
        {
            return GenericHelpers.CheckNotNull(___botOwner_0);
        }
    }

    internal class RotateClampPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.Rotate));
        }

        [PatchPrefix]
        public static void PatchPrefix(Player __instance, ref bool ignoreClamp)
        {
            if (__instance?.IsAI == true && __instance.IsSprintEnabled && SAINEnableClass.IsBotInCombat(__instance))
            {
                ignoreClamp = true;
            }
        }
    }

    internal class BotGroupAddEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotsGroup), nameof(BotsGroup.AddEnemy));
        }

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
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotMemoryClass), nameof(BotMemoryClass.AddEnemy));
        }

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

    public class StopSetToNavMeshPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotMover), nameof(BotMover.method_9));
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotMover __instance, ref BotOwner ___botOwner_0)
        {
            if (SAINEnableClass.IsBotInCombat(__instance.botOwner_0))
            {
                __instance.PositionOnWayInner = ___botOwner_0.Position;
                ___botOwner_0.Mover.LocalAvoidance.DropOffset();
                return false;
            }
            return true;
        }
    }

    public class StopSetToNavMeshPatch2 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass478), nameof(GClass478.method_18));
        }

        [PatchPrefix]
        public static bool PatchPrefix(GClass478 __instance)
        {
            if (SAINEnableClass.IsBotInCombat(__instance.botOwner_0))
            {
                return false;
            }
            return true;
        }
    }
}