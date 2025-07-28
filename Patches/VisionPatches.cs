using EFT;
using HarmonyLib;
using SAIN.Components;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SAIN.Patches.Vision
{
    public class UpdateLightEnablePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotLight), nameof(BotLight.UpdateLightEnable));
        }

        [PatchPrefix]
        public static bool PatchPrefix(
            BotOwner ___botOwner_0,
            float curLightDist,
            ref float __result,
            bool ____haveLight,
            ref float ____curLightDist,
            ref bool ____canUseNow,
            BotLight __instance)
        {
            __result = curLightDist;
            if (___botOwner_0.FlashGrenade.IsFlashed)
            {
                return false;
            }
            if (!____haveLight)
            {
                return false;
            }
            ____curLightDist = curLightDist;

            float timeModifier = BotManagerComponent.Instance.TimeVision.TimeVisionDistanceModifier;
            var lookSettings = GlobalSettingsClass.Instance.Look.Light;
            float turnOnRatio = lookSettings.LightOnRatio;
            float turnOffRatio = lookSettings.LightOffRatio;

            bool isOn = __instance.IsEnable;
            bool wantOn = !isOn && timeModifier <= turnOnRatio && ___botOwner_0.Memory.IsPeace;
            bool wantOff = isOn && timeModifier >= turnOffRatio;
            ____canUseNow = timeModifier < turnOffRatio;

            if (wantOn)
            {
                try
                {
                    __instance.TurnOn(true);
                }
                catch { }
            }
            if (wantOff)
            {
                try
                {
                    __instance.TurnOff(true, true);
                }
                catch { }
#if DEBUG
                try
                {
                    __instance.TurnOff(true, true);
                }
                catch (Exception e)
                {
                    if (SAINPlugin.DebugMode)
                    {
                        Logger.LogError(e);
                    }
                }
#endif

                if (__instance.IsEnable)
                {
                    var gameworld = GameWorldComponent.Instance;
                    if (gameworld == null)
                    {
#if DEBUG
                        Logger.LogError($"GameWorldComponent is null, cannot check if bot has flashlight on!");
#endif
                        return false;
                    }
                    PlayerComponent playerComponent = gameworld.PlayerTracker.GetPlayerComponent(___botOwner_0.ProfileId);
                    if (playerComponent == null)
                    {
#if DEBUG
                        Logger.LogError($"Player Component is null, cannot check if bot has flashlight on!");
#endif
                        return false;
                    }
                    if (playerComponent.Flashlight.WhiteLight ||
                        (___botOwner_0.NightVision.UsingNow && playerComponent.Flashlight.IRLight))
                    {
                        float min = ___botOwner_0.Settings.FileSettings.Look.VISIBLE_DISNACE_WITH_LIGHT;
                        __result = Mathf.Clamp(curLightDist, min, float.MaxValue);
                    }
                }
            }
            return false;
        }
    }

    public class UpdateLightEnablePatch2 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotLight), nameof(BotLight.method_0));
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotLight __instance)
        {
            if (!__instance.IsEnable)
            {
                return false;
            }
            float timeModifier = BotManagerComponent.Instance.TimeVision.TimeVisionDistanceModifier;
            float turnOffRatio = GlobalSettingsClass.Instance.Look.Light.LightOffRatio;
            bool wantOff = timeModifier >= turnOffRatio;
            if (wantOff)
            {
                __instance.TurnOff(true, true);
            }
            return false;
        }
    }

    public class ToggleNightVisionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotNightVisionData), nameof(BotNightVisionData.method_0));
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotOwner ___botOwner_0, bool ____nightVisionAtPocket, BotNightVisionData __instance)
        {
            if (___botOwner_0.FlashGrenade.IsFlashed)
            {
                return false;
            }

            float timeModifier = BotManagerComponent.Instance.TimeVision.TimeVisionDistanceModifier;
            var lookSettings = GlobalSettingsClass.Instance.Look.Light;
            float turnOnRatio = lookSettings.NightVisionOnRatio;
            float turnOffRatio = lookSettings.NightVisionOffRatio;

            if (____nightVisionAtPocket)
            {
                if (timeModifier < turnOnRatio)
                {
                    __instance.method_4();
                    return false;
                }
            }
            else
            {
                if (timeModifier < turnOnRatio)
                {
                    __instance.method_5();
                }
                if (timeModifier >= turnOffRatio)
                {
                    __instance.method_1();
                }
            }
            return false;
        }
    }

    public class SetPartPriorityPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EnemyInfo), nameof(EnemyInfo.method_1));
        }

        [PatchPrefix]
        public static bool PatchPrefix(EnemyInfo __instance)
        {
            bool isAI = __instance.Person?.IsAI == true;

            if (isAI)
            {
                if (!__instance.HaveSeenPersonal || Time.time - __instance.TimeLastSeenReal > 5f)
                {
                    __instance.SetFarParts();
                }
                else
                {
                    __instance.SetMiddleParts();
                }
                return false;
            }

            if (!isAI &&
                SAINEnableClass.GetSAIN(__instance.Owner.ProfileId, out BotComponent botComponent))
            {
                Enemy enemy = botComponent.EnemyController.CheckAddEnemy(__instance.Person);
                if (enemy != null)
                {
                    if (enemy.IsCurrentEnemy)
                    {
                        __instance.SetCloseParts();
                        return false;
                    }
                    if (enemy.Status.ShotAtMeRecently ||
                        enemy.Status.PositionalFlareEnabled)
                    {
                        __instance.SetCloseParts();
                        return false;
                    }
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Disable the ai task registration of SAIN bots for vision updates.
    /// </summary>
    public class DisableLookUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(LookSensor), nameof(LookSensor.Activate));
        }

        [PatchPrefix]
        public static bool Patch(LookSensor __instance)
        {
            if (SAINEnableClass.IsBotExcluded(__instance._botOwner)) return true;
            __instance.method_2();
            return false;
        }
    }

    public class GlobalLookSettingsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotGlobalLookData), nameof(BotGlobalLookData.Update));
        }

        [PatchPostfix]
        public static void Patch(BotGlobalLookData __instance)
        {
            __instance.CHECK_HEAD_ANY_DIST = true;
            __instance.MIDDLE_DIST_CAN_SHOOT_HEAD = true;
            __instance.SHOOT_FROM_EYES = false;
        }
    }

    public class WeatherTimeVisibleDistancePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(LookSensor), nameof(LookSensor.method_2));
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotOwner ____botOwner, ref float ____nextUpdateVisibleDist)
        {
            if (____nextUpdateVisibleDist < Time.time)
            {
                if (SAINEnableClass.IsBotExcluded(____botOwner))
                {
                    return true;
                }
                ____nextUpdateVisibleDist = float.MaxValue;
                return false;
            }
            return false;
        }
    }

    public class NoAIESPPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotOwner).GetMethod(nameof(BotOwner.IsEnemyLookingAtMe), BindingFlags.Instance | BindingFlags.Public, null, [typeof(IPlayer)], null);
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }

    public class BotLightTurnOnPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotLight), nameof(BotLight.TurnOn));
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotOwner ___botOwner_0, ref bool ____isInDarkPlace)
        {
            if (____isInDarkPlace
                && !SAINPlugin.LoadedPreset.GlobalSettings.General.Flashlight.AllowLightOnForDarkBuildings)
            {
                ____isInDarkPlace = false;
            }
            if (____isInDarkPlace || ___botOwner_0.Memory.GoalEnemy != null)
            {
                return true;
            }
            if (!ShallTurnLightOff(___botOwner_0.Profile.Info.Settings.Role))
            {
                return true;
            }
            ___botOwner_0.BotLight.TurnOff(false, true);
            return false;
        }

        private static bool ShallTurnLightOff(WildSpawnType wildSpawnType)
        {
            FlashlightSettings settings = SAINPlugin.LoadedPreset.GlobalSettings.General.Flashlight;
            if (EnumValues.WildSpawn.IsScav(wildSpawnType))
            {
                return settings.TurnLightOffNoEnemySCAV;
            }
            if (EnumValues.WildSpawn.IsPMC(wildSpawnType))
            {
                return settings.TurnLightOffNoEnemyPMC;
            }
            if (EnumValues.WildSpawn.IsGoons(wildSpawnType))
            {
                return settings.TurnLightOffNoEnemyGOONS;
            }
            if (EnumValues.WildSpawn.IsBoss(wildSpawnType))
            {
                return settings.TurnLightOffNoEnemyBOSS;
            }
            if (EnumValues.WildSpawn.IsFollower(wildSpawnType))
            {
                return settings.TurnLightOffNoEnemyFOLLOWER;
            }
            return settings.TurnLightOffNoEnemyRAIDERROGUE;
        }
    }

    public class VisionSpeedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EnemyInfo), nameof(EnemyInfo.method_7));
        }

        [PatchPostfix]
        public static void PatchPostfix(ref float __result, EnemyInfo __instance)
        {
            if (SAINEnableClass.GetSAIN(__instance.Owner.ProfileId, out var sain))
            {
                Enemy enemy = sain.EnemyController.GetEnemy(__instance.ProfileId, false);
                enemy ??= sain.EnemyController.CheckAddEnemy(__instance.Person);
                if (enemy != null)
                {
                    float sainMod = EnemyGainSightClass.GetGainSightModifier(enemy);
                    __result /= sainMod;
                    enemy.Vision.LastGainSightResult = __result;
                }
            }
        }
    }

    public class WeatherVisionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EnemyInfo), nameof(EnemyInfo.method_8));
        }

        [PatchPrefix]
        public static bool PatchPrefix(EnemyInfo __instance, ref float __result)
        {
            if (SAINEnableClass.IsBotExcluded(__instance.Owner))
            {
                return true;
            }

            __result = 1f;
            return false;
        }
    }


    //public class CheckPartLineOfSightPatch : ModulePatch
    //{
    //    protected override MethodBase GetTargetMethod()
    //    {
    //        return AccessTools.Method(typeof(EnemyInfo), nameof(EnemyInfo.CheckPartLineOfSight));
    //    }
    //
    //    [PatchPrefix]
    //    public static bool PatchPrefix(EnemyInfo __instance, ref bool __result, KeyValuePair<EnemyPart, EnemyPartData> part, LayerMask lookSensorMask, float addSensorDistance, ref float visibilityChangeSpeedK)
    //    {
    //        if (SAINEnableClass.GetSAIN(__instance.Owner, out var sain))
    //        {
    //            Enemy enemy = sain.EnemyController.GetEnemy(__instance.ProfileId, true);
    //            if (enemy != null)
    //            {
    //                __result = enemy.Vision.Angles.CanBeSeen;
    //                return false;
    //            }
    //        }
    //        return true;
    //    }
    //}

    public class IsPointInVisibleSectorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EnemyInfo), nameof(EnemyInfo.IsPointInVisibleSector));
        }

        [PatchPrefix]
        public static bool PatchPrefix(EnemyInfo __instance, ref bool __result)
        {
            if (SAINEnableClass.GetSAIN(__instance.Owner.ProfileId, out var sain))
            {
                Enemy enemy = sain.EnemyController.GetEnemy(__instance.ProfileId, false);
                if (enemy != null)
                {
                    __result = enemy.Vision.Angles.CanBeSeen && enemy.Vision.EnemyParts.CanBeSeen;
                    return false;
                }
            }
            return true;
        }
    }

    public class VisionDistancePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EnemyInfo), nameof(EnemyInfo.method_0));
        }

        [PatchPostfix]
        public static void PatchPrefix(ref float __result, EnemyInfo __instance)
        {
            if (SAINEnableClass.GetSAIN(__instance.Owner.ProfileId, out var sain))
            {
                Enemy enemy = sain.EnemyController.GetEnemy(__instance.ProfileId, false);
                if (enemy != null)
                {
                    __result += enemy.Vision._visionDistance.Value;
                }
            }
        }
    }

    public class CheckFlashlightPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player.FirearmController), nameof(Player.FirearmController.SetLightsState));
        }

        [PatchPostfix]
        public static void PatchPostfix(Player ____player)
        {
            PlayerComponent playerComponent = GameWorldComponent.Instance?.PlayerTracker.GetPlayerComponent(____player?.ProfileId);
            if (playerComponent != null)
            {
                BotManagerComponent.Instance.BotHearing.PlayAISound(playerComponent, SAINSoundType.GearSound, playerComponent.Player.WeaponRoot.position, 35f, 1f, true);
                var flashLight = playerComponent.Flashlight;
                flashLight.CheckDevice();

                if (!flashLight.WhiteLight && !flashLight.Laser)
                {
                    (____player.AIData as GClass567).UsingLight = false;
                }
            }
        }
    }
}