using Comfort.Common;
using EFT;
using HarmonyLib;
using SAIN.Components;
using SAIN.Components.BotController;
using SAIN.SAINComponent;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using UnityEngine;

namespace SAIN.Patches.Components
{
    internal class AddBotComponentPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotOwner), nameof(BotOwner.PreActivate));
        }

        [PatchPostfix]
        public static void PatchPostfix(ref BotOwner __instance)
        {
            try
            {
                BotSpawnController.Instance.AddBot(__instance);
            }
            catch (Exception ex)
            {
                Logger.LogError($" SAIN Add Bot Error: {ex}");
            }
        }
    }

    internal class ActivateBotComponentPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotOwner), nameof(BotOwner.method_10));
        }

        [PatchPostfix]
        public static void PatchPostfix(ref BotOwner __instance)
        {
            __instance.GetComponent<BotComponent>()?.Activate(__instance);
        }
    }

    internal class AddGameWorldPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorldUnityTickListener), nameof(GameWorldUnityTickListener.Create));
        }

        [PatchPostfix]
        public static void PatchPostfix(GameObject gameObject, GameWorld gameWorld)
        {
            if (gameWorld is HideoutGameWorld)
                return;

            try
            {
                GameWorldHandler.Create(gameWorld);
            }
            catch (Exception ex)
            {
                Logger.LogError($" SAIN Init Gameworld Error: {ex}");
            }
        }
    }

    internal class GetBotController : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotsController), nameof(BotsController.Init));
        }

        [PatchPostfix]
        public static void Patch(BotsController __instance)
        {
            SAINBotController sainBotController = Singleton<GameWorld>.Instance?.GetComponent<SAINBotController>();
            if (sainBotController == null)
            {
                Logger.LogError("sainBotControllerNull");
                return;
            }
            sainBotController.DefaultController = __instance;
            sainBotController.BotSpawner = __instance.BotSpawner;
        }
    }

    internal class BotUpdateByUnityPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotsClass), nameof(BotsClass.UpdateByUnity));
        }

        [PatchPostfix]
        public static void Patch(BotsClass __instance)
        {
            SAINBotController.Instance?.ManualUpdate();
        }
    }
}