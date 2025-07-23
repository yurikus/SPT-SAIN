using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using RootMotion.FinalIK;

using SAIN.Components;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using SAIN.Preset.BotSettings.SAINSettings.Categories;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using SPT.Reflection.Patching;
using System.Reflection;
using System.Text;
using UnityEngine;
using HitAffectClass = GClass583;

namespace SAIN.Patches.Shoot.Aim
{
    internal class WeaponMoAModificationPatch : ModulePatch
    {
        private const float MIN_START_MOA_AI = 4f;
        private const float MOA_FULLAUTO_COEF = 3f;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player.FirearmController), "method_54", null, null);
        }

        [PatchPostfix]
        public static void Patch(Player.FirearmController __instance, Player ____player, ref float __result)
        {
            // this method was lost due to a VS crash, so I grabbed from a decompiled build
            BotOwner botOwner = ____player.AIData?.BotOwner;
            if (botOwner == null)
            {
                return;
            }
            __result = 0;
            return;

            __result = Mathf.Min(__result, MIN_START_MOA_AI);
            __result *= botOwner.Settings.Current.CurrentScattering;
            if (__instance.Weapon?.FireMode?.FireMode == EFT.InventoryLogic.Weapon.EFireMode.fullauto)
            {
                __result *= MOA_FULLAUTO_COEF;
            }
            if (SAINEnableClass.GetSAIN(botOwner, out BotComponent botComponent))
            {
                Enemy lastShotEnemy = botComponent.Shoot.LastShotEnemy;
                if (lastShotEnemy != null)
                {
                    __result /= lastShotEnemy.Aim.AimAndScatterMultiplier;
                }
            }
        }
    }

    /// <summary>
    /// This method is usually called by NodeUpdate, we want to remove a few function calls from the original method.
    /// </summary>
    internal class BotAimSteerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotAimingClass), nameof(BotAimingClass.method_11));
        }

        [PatchPrefix]
        public static bool Patch(BotAimingClass __instance, Vector3 dir)
        {
            if (!SAINEnableClass.IsBotInCombat(__instance.botOwner_0)) return true;
            //__instance.botOwner_0.Steering.LookToDirection(dir, float.MaxValue);
            return false;
        }
    }
    
    /// <summary>
    /// In the original method it triggers a bot to set ADS, we control this in sain, and dont want it to override our settings.
    /// This is just the original method, but without setting ADS.
    /// </summary>
    internal class HardAimDisablePatch1 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotAimingClass), nameof(BotAimingClass.LoseTarget));
        }

        [PatchPrefix]
        public static bool Patch(BotAimingClass __instance)
        {
            if (SAINEnableClass.IsBotInCombat(__instance.botOwner_0))
            {
                __instance.Status = AimStatus.NoTarget;
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// In the original method it triggers a bot to set ADS, we control this in sain, and dont want it to override our settings.
    /// This is just the original method, but without setting ADS.
    /// </summary>
    internal class HardAimDisablePatch2 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.PropertySetter(typeof(BotAimingClass), nameof(BotAimingClass.Status));
        }

        [PatchPrefix]
        public static bool Patch(BotAimingClass __instance, AimStatus value)
        {
            if (__instance.aimStatus_0 == value) return false;

            BotOwner botOwner = __instance.botOwner_0;
            if (botOwner == null || botOwner.BotState != EBotState.Active) return false;
            if (!SAINEnableClass.IsBotInCombat(botOwner)) return true;
            __instance.aimStatus_0 = value;
            return false;
        }
    }

    internal class DisableMalfunctionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player.FirearmController), nameof(Player.FirearmController.GetMalfunctionState));
        }

        [PatchPostfix]
        public static void Patch(Player ____player, ref Weapon.EMalfunctionState __result)
        {
            if (____player.IsAI && __result != Weapon.EMalfunctionState.None)
            {
                //SAIN.Logger.LogError(__result);
                __result = Weapon.EMalfunctionState.None;
            }
        }
    }

    internal class PlayerHitReactionDisablePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HitReaction), nameof(HitReaction.Hit));
        }

        [PatchPrefix]
        public static bool Patch()
        {
            if (!GlobalSettingsClass.Instance.Aiming.HitEffects.HIT_REACTION_TOGGLE)
            {
                return true;
            }
            return false;
        }
    }

    internal class HitAffectApplyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HitAffectClass), nameof(HitAffectClass.Affect));
        }

        [PatchPrefix]
        public static bool Patch(BotOwner ___botOwner_0, ref Vector3 __result, Vector3 dir)
        {
            if (!GlobalSettingsClass.Instance.Aiming.HitEffects.HIT_REACTION_TOGGLE)
            {
                return true;
            }
            if (SAINEnableClass.GetSAIN(___botOwner_0, out var bot))
            {
                __result = bot.Medical.HitReaction.AimHitEffect.ApplyEffect(dir);
                return false;
            }
            return true;
        }
    }

    internal class DoHitAffectPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HitAffectClass), nameof(HitAffectClass.DoAffection));
        }

        [PatchPrefix]
        public static bool Patch(BotOwner ___botOwner_0)
        {
            if (!GlobalSettingsClass.Instance.Aiming.HitEffects.HIT_REACTION_TOGGLE)
            {
                return true;
            }
            if (SAINEnableClass.GetSAIN(___botOwner_0, out var bot))
            {
                return false;
            }
            return true;
        }
    }

    internal class AimOffsetPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotAimingClass), nameof(BotAimingClass.method_13));
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotAimingClass __instance)
        {
            if (!SAINEnableClass.IsBotInCombat(__instance.botOwner_0)) return true;

            // Applies aiming offset, recoil offset, and scatter offsets
            // Default Setup :: Vector3 finalTarget = __instance.RealTargetPoint + badShootOffset + (AimUpgradeByTime * (AimOffset + ___botOwner_0.RecoilData.RecoilOffset));

            float aimUpgradeByTime = __instance.float_13;
            Vector3 aimOffset = __instance.vector3_4;
            Vector3 realTargetPoint = __instance.RealTargetPoint;
            Vector3 result = realTargetPoint + (aimOffset * aimUpgradeByTime);

            __instance.EndTargetPoint = result;

            if (SAINPlugin.LoadedPreset.GlobalSettings.General.Debug.Gizmos.DebugDrawAimGizmos)
            {
                Vector3 weaponRoot = __instance.botOwner_0.WeaponRoot.position;
                DebugGizmos.DrawLine(weaponRoot, result, Color.red, 0.02f, 0.25f, true);
                DebugGizmos.DrawSphere(result, 0.025f, Color.red, 10f);
                DebugGizmos.DrawLine(result, realTargetPoint, Color.white, 0.02f, 0.25f, true);
            }
            return false;
        }
    }

    public class AimTimePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotAimingClass), nameof(BotAimingClass.method_7));
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotAimingClass __instance, float dist, float ang, ref float __result)
        {
            if (!SAINEnableClass.GetSAIN(__instance.botOwner_0, out var bot))
            {
                return true;
            }

            __result = CalculateAim(bot, dist, ang, __instance.bool_1, __instance.bool_0, __instance.float_10);
            bot.Aim.LastAimTime = __result;

            return false;
        }

        private static float CalculateAim(BotComponent botComponent, float distance, float angle, bool moving, bool panicing, float aimDelay)
        {
            BotOwner botOwner = botComponent.BotOwner;
            StringBuilder stringBuilder = SAINPlugin.LoadedPreset.GlobalSettings.General.Debug.Logs.DebugAimCalculations ? new StringBuilder() : null;
            stringBuilder?.AppendLine($"Aim Time Calculation for [{botOwner?.name} : {botOwner?.Profile?.Info?.Settings?.Role} : {botOwner?.Profile?.Info?.Settings?.BotDifficulty}]");

            SAINAimingSettings sainAimSettings = botComponent.Info.FileSettings.Aiming;
            BotSettingsComponents fileSettings = botOwner.Settings.FileSettings;

            float baseAimTime = fileSettings.Aiming.BOTTOM_COEF;
            stringBuilder?.AppendLine($"baseAimTime [{baseAimTime}]");
            baseAimTime = CalcCoverMod(baseAimTime, botOwner, botComponent, fileSettings, stringBuilder);
            BotCurvSettings curve = botOwner.Settings.Curv;
            float angleTime = CalcCurveOutput(curve.AimAngCoef, angle, sainAimSettings.AngleAimTimeMultiplier, stringBuilder, "Angle");
            float distanceTime = CalcCurveOutput(curve.AimTime2Dist, distance, sainAimSettings.DistanceAimTimeMultiplier, stringBuilder, "Distance");
            float calculatedAimTime = CalcAimTime(angleTime, distanceTime, botOwner, stringBuilder);
            calculatedAimTime = CalcPanic(panicing, calculatedAimTime, fileSettings, stringBuilder);

            float timeToAimResult = (baseAimTime + calculatedAimTime + aimDelay);
            stringBuilder?.AppendLine($"timeToAimResult [{timeToAimResult}] (baseAimTime + calculatedAimTime + aimDelay)");

            timeToAimResult = CalcMoveModifier(moving, timeToAimResult, fileSettings, stringBuilder);
            timeToAimResult = CalcADSModifier(botOwner.WeaponManager?.ShootController?.IsAiming == true, timeToAimResult, stringBuilder);
            timeToAimResult = ClampAimTime(timeToAimResult, fileSettings, stringBuilder);
            timeToAimResult = CalcFasterCQB(distance, timeToAimResult, sainAimSettings, stringBuilder);
            timeToAimResult = CalcAttachmentMod(botComponent, timeToAimResult, stringBuilder);

            if (stringBuilder != null &&
                botOwner?.Memory?.GoalEnemy?.Person?.IsYourPlayer == true)
            {
                Logger.LogDebug(stringBuilder.ToString());
            }
            return timeToAimResult;
        }

        private static float CalcAimTime(float angleTime, float distanceTime, BotOwner botOwner, StringBuilder stringBuilder)
        {
            float accuracySpeed = botOwner.Settings.Current.CurrentAccuratySpeed;
            stringBuilder?.AppendLine($"accuracySpeed [{accuracySpeed}]");

            float calculatedAimTime = angleTime * distanceTime * accuracySpeed;
            stringBuilder?.AppendLine($"calculatedAimTime [{calculatedAimTime}] (angleTime * distanceTime * accuracySpeed)");
            return calculatedAimTime;
        }

        private static float CalcCoverMod(float baseAimTime, BotOwner botOwner, BotComponent botComponent, BotSettingsComponents fileSettings, StringBuilder stringBuilder)
        {
            CoverPoint coverInUse = botComponent?.Cover.CoverInUse;
            bool inCover = botOwner.Memory.IsInCover || coverInUse?.BotInThisCover == true;
            if (inCover)
            {
                baseAimTime *= fileSettings.Aiming.COEF_FROM_COVER;
                stringBuilder?.AppendLine($"In Cover: [{baseAimTime}] : COEF_FROM_COVER [{fileSettings.Aiming.COEF_FROM_COVER}]");
            }
            return baseAimTime;
        }

        private static float CalcCurveOutput(AnimationCurve aimCurve, float input, float modifier, StringBuilder stringBuilder, string curveType)
        {
            float result = aimCurve.Evaluate(input);
            result *= modifier;
            stringBuilder?.AppendLine($"{curveType} Curve Output [{result}] : input [{input}] : Multiplier: [{modifier}]");
            return result;
        }

        private static float CalcMoveModifier(bool moving, float timeToAimResult, BotSettingsComponents fileSettings, StringBuilder stringBuilder)
        {
            if (moving)
            {
                timeToAimResult *= fileSettings.Aiming.COEF_IF_MOVE;
                stringBuilder?.AppendLine($"Moving [{timeToAimResult}] : Moving Coef [{fileSettings.Aiming.COEF_IF_MOVE}]");
            }
            return timeToAimResult;
        }

        private static float CalcADSModifier(bool aiming, float timeToAimResult, StringBuilder stringBuilder)
        {
            if (aiming)
            {
                float adsMulti = SAINPlugin.LoadedPreset.GlobalSettings.Aiming.AimDownSightsAimTimeMultiplier;
                timeToAimResult *= adsMulti;
                stringBuilder?.AppendLine($"Aiming Down Sights [{timeToAimResult}] : ADS Multiplier [{adsMulti}]");
            }
            return timeToAimResult;
        }

        private static float ClampAimTime(float timeToAimResult, BotSettingsComponents fileSettings, StringBuilder stringBuilder)
        {
            float clampedResult = Mathf.Clamp(timeToAimResult, 0f, fileSettings.Aiming.MAX_AIM_TIME);
            if (clampedResult != timeToAimResult)
            {
                stringBuilder?.AppendLine($"Clamped Aim Time [{clampedResult}] : MAX_AIM_TIME [{fileSettings.Aiming.MAX_AIM_TIME}]");
            }
            return clampedResult;
        }

        private static float CalcPanic(bool panicing, float calculatedAimTime, BotSettingsComponents fileSettings, StringBuilder stringBuilder)
        {
            if (panicing)
            {
                calculatedAimTime *= fileSettings.Aiming.PANIC_COEF;
                stringBuilder?.AppendLine($"Panicing [{calculatedAimTime}] : Panic Coef [{fileSettings.Aiming.PANIC_COEF}]");
            }
            return calculatedAimTime;
        }

        private static float CalcFasterCQB(float distance, float aimTimeResult, SAINAimingSettings aimSettings, StringBuilder stringBuilder)
        {
            if (!SAINPlugin.LoadedPreset.GlobalSettings.Aiming.FasterCQBReactionsGlobal)
            {
                return aimTimeResult;
            }
            if (aimSettings?.FasterCQBReactions == true &&
                distance <= aimSettings.FasterCQBReactionsDistance)
            {
                float ratio = distance / aimSettings.FasterCQBReactionsDistance;
                float fasterTime = aimTimeResult * ratio;
                fasterTime = Mathf.Clamp(fasterTime, aimSettings.FasterCQBReactionsMinimum, aimTimeResult);
                stringBuilder?.AppendLine($"Faster CQB Aim Time: Result [{fasterTime}] : Original [{aimTimeResult}] : At Distance [{distance}] with maxDist [{aimSettings.FasterCQBReactionsDistance}]");
                return fasterTime;
            }
            return aimTimeResult;
        }

        private static float CalcAttachmentMod(BotComponent bot, float aimTimeResult, StringBuilder stringBuilder)
        {
            Enemy enemy = bot?.GoalEnemy;
            if (enemy != null)
            {
                float modifier = enemy.Aim.AimAndScatterMultiplier;
                stringBuilder?.AppendLine($"Bot Attachment Mod: Result [{aimTimeResult / modifier}] : Original [{aimTimeResult}] : Modifier [{modifier}]");
                aimTimeResult /= modifier;
            }
            return aimTimeResult;
        }
    }

    public class SmoothTurnPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotSteering), nameof(BotSteering.Steering));
        }

        [PatchPrefix]
        public static bool Patch(BotSteering __instance)
        {
            BotOwner botOwner = __instance.botOwner_0;
            if (!GameWorldComponent.TryGetPlayerComponent(botOwner, out PlayerComponent playerComponent)) return true;
            if (playerComponent.BotComponent?.SAINLayersActive == true) return false;
            //return true;

            Vector3 newTargetLookDirection;
            if (botOwner.Mover.Sprinting && botOwner.Mover.HasPathAndNoComplete)
            {
                newTargetLookDirection = CalcLookPoint(botOwner, botOwner.Mover.RealDestPoint);
            }
            else
            {
                switch (__instance.SteeringMode)
                {
                    case EBotSteering.ToDestPoint:
                        if (botOwner.Destination != null)
                        {
                            newTargetLookDirection = CalcLookPoint(botOwner, botOwner.Destination.Value);
                            break;
                        }
                        newTargetLookDirection = __instance.LookDirection;
                        break;

                    case EBotSteering.ToMovingDirection:
                        if (!__instance.CanSteerToMovingDirection())
                        {
                            newTargetLookDirection = __instance._customDirection;
                            break;
                        }
                        newTargetLookDirection = CalcLookPoint(botOwner, botOwner.Mover.RealDestPoint);
                        break;

                    case EBotSteering.ToCustomPoint:
                        newTargetLookDirection = __instance._customPoint - botOwner.WeaponRoot.position;
                        break;

                    case EBotSteering.Direction:
                        newTargetLookDirection = __instance._customDirection;
                        break;

                    default:
                        newTargetLookDirection = __instance._customDirection;
                        break;
                }
            }
            if (Mathf.Abs(newTargetLookDirection.y) < 0.001f)
            {
                newTargetLookDirection.y = 0;
            }
            newTargetLookDirection.Normalize();
            playerComponent.CharacterController.SetTargetLookDirection(newTargetLookDirection, botOwner, playerComponent.BotComponent);
            __instance._lookDirection = playerComponent.CharacterController.CurrentControlLookDirection;
            return false;
        }

        private static Vector3 CalcLookPoint(BotOwner botOwner, Vector3 point)
        {
            Vector3 botPosition = botOwner.Position;
            Vector3 weaponRoot = botOwner.WeaponRoot.position;
            float rootOffset = weaponRoot.y - botPosition.y;
            point.y += rootOffset;
            return point - weaponRoot;
        }
    }

    internal class ForceNoHeadAimPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EnemyInfo), nameof(EnemyInfo.method_13));
        }

        [PatchPrefix]
        public static void PatchPrefix(ref bool withLegs, ref bool canBehead, EnemyInfo __instance)
        {
            if (!__instance.Person.IsAI)
            {
                var aim = GlobalSettingsClass.Instance.Aiming;
                canBehead = EFTMath.RandomBool(aim.PMCAimForHeadChance) && aim.PMCSAimForHead && IsPMC(__instance);
                withLegs = true;
            }
            else
            {
                canBehead = true;
                withLegs = true;
            }
        }

        private static bool IsPMC(EnemyInfo __instance)
        {
            return EnumValues.WildSpawn.IsPMC(__instance.Owner.Profile.Info.Settings.Role);
        }
    }
}