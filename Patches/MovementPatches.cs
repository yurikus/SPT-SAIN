using Comfort.Common;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using SAIN.Components;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Preset.GlobalSettings;
using SPT.Reflection.Patching;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace SAIN.Patches.Movement
{
    /// <summary>
    /// stops sideways sprinting
    /// </summary>
    public class SprintLookDirPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotMover), nameof(BotMover.Sprint));
        }

        [PatchPrefix]
        public static bool Patch(BotMover __instance, bool val)
        {
            if (__instance.Sprinting == val)
            {
                return false;
            }
            BotOwner botOwner = __instance.botOwner_0;
            if (SAINEnableClass.IsBotInCombat(botOwner)) return false;
            if (__instance.NoSprint)
            {
                __instance._player.EnableSprint(false);
                return false;
            }
            if (val && botOwner.Mover.HasPathAndNoComplete)
            {
                Vector3 destinationDirection = botOwner.Mover.RealDestPoint - botOwner.Position;
                destinationDirection.y = 0;
                destinationDirection.Normalize();
                Vector3 lookDirection = botOwner.LookDirection;
                lookDirection.y = 0;
                lookDirection.Normalize();
                if (Vector3.Angle(lookDirection, destinationDirection) > 20f)
                {
                    val = false;
                }
            }
            __instance.Sprinting = val;
            if (val)
            {
                botOwner.SetTargetMoveSpeed(1f);
            }
            __instance._player.EnableSprint(val);
            return false;
        }
    }

    /// <summary>
    /// for debug purposes
    /// </summary>
    public class PlayerEnableSprintPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BasePhysicalClass), nameof(BasePhysicalClass.Sprint));
        }

        [PatchPrefix]
        public static bool Sprint(bool target, BasePhysicalClass __instance)
        {
            if (target) return true;
            Logger.LogWarning("stop sprinting");
            if (target == __instance.bool_2) return true;
            IPlayer player = __instance.iobserverToPlayerBridge_0.iPlayer;
            if (player?.IsAI == true &&
                __instance.bool_2 &&
                GameWorldComponent.TryGetPlayerComponent(player, out PlayerComponent comp) &&
                comp.BotComponent?.Mover.Moving == true)
            {
                SAIN.Logger.LogError("WHO DID IT");
            }
            return true;
        }
    }

    /// <summary>
    /// Blocks pose changes when a player is sprinting for ai
    /// </summary>
    public class PlayerSetPosePatch : ModulePatch
    {
        private static readonly FieldInfo playerField = AccessTools.Field(typeof(MovementContext), "_player");

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(RunStateClass), nameof(RunStateClass.ChangePose));
        }

        [PatchPrefix]
        public static bool Patch(RunStateClass __instance)
        {
            if (playerField.GetValue(__instance.MovementContext) is Player player)
            {
                if (player.IsAI && __instance.MovementContext.IsSprintEnabled)
                {
                    __instance.MovementContext.SetPoseLevel(1f, true);
                    return false;
                }
                return true;
            }
            else
            {
                Logger.LogError("nope");
            }
            return true;
        }
    }

    /// <summary>
    /// Blocks speed changes when a player is sprinting for ai
    /// </summary>
    public class PlayerSetSpeedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.ChangeSpeed));
        }

        [PatchPrefix]
        public static void Patch(ref float speedDelta, Player __instance)
        {
        }
    }

    /// <summary>
    /// Disables the check for is ai in movement context. could break things in the future
    /// </summary>
    public class MovementContextIsAIPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.PropertyGetter(typeof(MovementContext), nameof(MovementContext.IsAI));
        }

        [PatchPrefix]
        public static bool Patch(ref bool __result)
        {
            __result = false;
            return false;
        }
    }

    /// <summary>
    /// Disables collision on doors when they open or close for a set amount of time for ai
    /// </summary>
    public class SetDoorCollisionPatch : ModulePatch
    {
        private const float NO_COLLISION_INTERVAL = 3.0f;
        private const float BOT_DISTANCE_TO_DISABLE_COLLISION = 20f;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Door), nameof(Door.SetDoorState));
        }

        [PatchPrefix]
        public static void Patch(Door __instance, EDoorState state)
        {
            switch (state)
            {
                case EDoorState.Open:
                case EDoorState.Shut:
                case EDoorState.Breaching:
                case EDoorState.Interacting:
                    break;

                default:
                    return;
            }
            if (__instance?.Collider != null)
            {
                for (int i = 0; i < _preAllocArray.Length; i++)
                    _preAllocArray[i] = null;
                Physics.OverlapSphereNonAlloc(__instance.transform.position, BOT_DISTANCE_TO_DISABLE_COLLISION, _preAllocArray, LayerMaskClass.PlayerMask);
                foreach (var collider in _preAllocArray)
                {
                    if (collider != null)
                    {
                        Player player = Singleton<GameWorld>.Instance?.GetPlayerByCollider(collider);
                        if (player != null && player.IsAI)
                        {
                            Collider playerCollider = player.CharacterController.GetCollider();
                            if (playerCollider != null)
                            {
                                player.StartCoroutine(SetDoorCollisionAfterDelay(player, playerCollider, __instance.Collider, NO_COLLISION_INTERVAL));
                            }
                        }
                    }
                }
            }
        }

        private static IEnumerator SetDoorCollisionAfterDelay(Player player, Collider playerCollider, Collider doorCollider, float delay)
        {
            if (playerCollider != null && doorCollider != null && player != null)
            {
                player.POM.IgnoreCollider(doorCollider, true);
                player.MovementContext.IgnoreInteractionCollision(doorCollider, true);
                EFTPhysicsClass.IgnoreCollision(playerCollider, doorCollider, true);
                //Logger.LogDebug("SetDoorCollisionPatch: player set to no collision with door");
            }

            yield return new WaitForSeconds(delay);

            if (playerCollider != null && doorCollider != null && player != null)
            {
                player.POM.IgnoreCollider(doorCollider, false);
                player.MovementContext.IgnoreInteractionCollision(doorCollider, false);
                EFTPhysicsClass.IgnoreCollision(playerCollider, doorCollider, false);
                //Logger.LogDebug("SetDoorCollisionPatch: reset");
            }
        }

        private static Collider[] _preAllocArray = new Collider[10];
    }

    /// <summary>
    /// Disables can be snapped for all players, its only used for door opening and ai
    /// </summary>
    public class CanBeSnappedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.PropertyGetter(typeof(Player), nameof(Player.CanBeSnapped));
        }

        [PatchPrefix]
        public static bool Patch(ref bool __result)
        {
            __result = false;
            return false;
        }
    }

    public class GlobalShootSettingsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotGlobalShootData), nameof(BotGlobalShootData.Update));
        }

        [PatchPostfix]
        public static void PatchPrefix(BotGlobalShootData __instance)
        {
            //__instance.CAN_STOP_SHOOT_CAUSE_ANIMATOR = true;
            __instance.MAX_DIST_COEF = 100f;
        }
    }

    public class StopShootCauseAnimatorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ShootData), nameof(ShootData.method_1));
        }

        [PatchPrefix]
        public static bool PatchPostfix(ShootData __instance)
        {
            __instance.CanShootByState = true;
            return false;
        }
    }

    public class PoseStaminaPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(PlayerPhysicalClass), nameof(PlayerPhysicalClass.ConsumePoseLevelChange));
        }

        [PatchPrefix]
        public static bool PatchPrefix(Player ___player_0)
        {
            if (___player_0.IsAI)
            {
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Disable specific functions in Manual Update that might be causing erratic movement in sain bots.
    /// </summary>
    public class BotMoverManualUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotMover), nameof(BotMover.ManualUpdate));
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotMover __instance, BotOwner ___botOwner_0)
        {
            if (!SAINEnableClass.IsBotInCombat(___botOwner_0))
            {
                return true;
            }
            __instance.LocalAvoidance.DropOffset();
            __instance.PositionOnWayInner = ___botOwner_0.Position;

            //__instance.method_16();
            //__instance.method_15();
            __instance.method_11();
            //__instance.LocalAvoidance.ManualUpdate();
            return false;
        }
    }

    /// <summary>
    /// Disable specific functions in Manual Update that might be causing erratic movement in sain bots if they are in combat.
    /// </summary>
    public class BotMoverManualFixedUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotMover), nameof(BotMover.ManualFixedUpdate));
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotMover __instance, BotOwner ___botOwner_0)
        {
            if (SAINEnableClass.IsBotInCombat(___botOwner_0))
            {
                return false;
            }
            return true;
        }
    }

    public class AimStaminaPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(PlayerPhysicalClass), nameof(PlayerPhysicalClass.Aim));
        }

        [PatchPrefix]
        public static bool PatchPrefix(Player ___player_0)
        {
            if (___player_0.IsAI)
            {
                return false;
            }
            return true;
        }
    }

    public class CrawlPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass485), nameof(GClass485.method_0));
        }

        [PatchPrefix]
        public static bool PatchPrefix(GClass485 __instance, BotOwner ___botOwner_0, Vector3 pos, bool slowAtTheEnd, bool getUpWithCheck, ref bool __result)
        {
            if (!SAINEnableClass.IsBotInCombat(___botOwner_0))
            {
                return true;
            }
            __result = false;
            return false;
            if (___botOwner_0.BotLay.IsLay &&
                getUpWithCheck)
            {
                Vector3 vector = pos - ___botOwner_0.Mover.PositionOnWay;
                if (vector.y < 0.5f)
                {
                    vector.y = 0f;
                }
                if (vector.sqrMagnitude > 0.2f)
                {
                    ___botOwner_0.BotLay.GetUp(getUpWithCheck);
                }
                if (___botOwner_0.BotLay.IsLay)
                {
                    return false;
                }
            }
            ___botOwner_0.WeaponManager.Stationary.StartMove();
            __instance.SlowAtTheEnd = slowAtTheEnd;
            return false;
        }
    }

    public class EncumberedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BasePhysicalClass), nameof(BasePhysicalClass.UpdateWeightLimits));
        }

        [PatchPrefix]
        public static bool PatchPrefix(bool ___bool_7, BasePhysicalClass.IObserverToPlayerBridge ___iobserverToPlayerBridge_0, BasePhysicalClass __instance)
        {
            //if (___bool_7)
            //{
            //    return true;
            //}
            IPlayer player = ___iobserverToPlayerBridge_0.iPlayer;
            if (player == null)
            {
                Logger.LogWarning($"Player is Null, can't set weight limits for AI.");
                return true;
            }
            if (!player.IsAI)
            {
                return true;
            }
            if (SAINPlugin.IsBotExluded(player.AIData.BotOwner))
            {
                return true;
            }

            // Copy Pasted from original EFT code, there is a check to not enable weight limits for AI
            BackendConfigSettingsClass.InertiaSettings inertia = Singleton<BackendConfigSettingsClass>.Instance.Inertia;
            Vector3 b2 = new(inertia.InertiaLimitsStep * (float)___iobserverToPlayerBridge_0.Skills.Strength.SummaryLevel, inertia.InertiaLimitsStep * (float)___iobserverToPlayerBridge_0.Skills.Strength.SummaryLevel, 0f);
            __instance.BaseInertiaLimits = inertia.InertiaLimits + b2;
            //__instance.WalkOverweightLimits = stamina.WalkOverweightLimits * d + b;
            //__instance.BaseOverweightLimits = stamina.BaseOverweightLimits * d + b;
            //__instance.SprintOverweightLimits = stamina.SprintOverweightLimits * d + b;
            //__instance.WalkSpeedOverweightLimits = stamina.WalkSpeedOverweightLimits * d + b;
            __instance.WalkOverweightLimits.Set(9000f, 10000f);
            __instance.BaseOverweightLimits.Set(9000f, 10000f);
            __instance.SprintOverweightLimits.Set(9000f, 10000f);
            __instance.WalkSpeedOverweightLimits.Set(9000f, 10000f);
            // End of CopyPaste

            return false;
        }
    }

    public class DoorOpenerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotDoorOpener), nameof(BotDoorOpener.Update));
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref BotOwner ____owner, ref bool __result)
        {
            var settings = GlobalSettingsClass.Instance.General.Doors;
            if (settings.DisableAllDoors && !ModDetection.ProjectFikaLoaded)
            {
                __result = false;
                return false;
            }
            if (SAINEnableClass.GetSAIN(____owner, out var botComponent) &&
                (botComponent.SAINLayersActive || botComponent.HasEnemy))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    public class DoorDisabledPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(WorldInteractiveObject), nameof(WorldInteractiveObject.method_4));
        }

        [PatchPrefix]
        public static bool PatchPrefix(WorldInteractiveObject __instance)
        {
            if (!__instance.enabled || !__instance.gameObject.activeInHierarchy)
            {
                return false;
            }
            return true;
        }
    }
}