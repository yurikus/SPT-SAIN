using EFT;
using SAIN.Components;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Components.RotationController;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.Classes
{
    public class PlayerMovementSmoothingClass
    {
        public Vector3 CurrentControlLookDirection => ControlLookDirection.Current;
        public SmoothDampVectorDirectionNormal ControlLookDirection { get; } = new();

        public void ManualUpdate(float currentTime, float deltaTime, Player player, BotOwner botOwner, BotComponent botComponent)
        {
            var settings = GlobalSettingsClass.Instance.Steering;
            TurnSettings turnSettings = GetTurnSettings(botOwner, botComponent);
            ControlLookDirection.Calculate(deltaTime, turnSettings.SmoothingValue, turnSettings.MaxTurnSpeed, settings.TURN_PITCH_MAX);
            player.CharacterController.SetSteerDirection(ControlLookDirection.Current);
        }

        private static TurnSettings GetTurnSettings(BotOwner bot, BotComponent botComponent)
        {
            TurnSettings turnSettings;
            var settings = GlobalSettingsClass.Instance.Steering;
            if (bot.AimingManager.CurrentAiming?.IsReady == true || bot.AimingManager.CurrentAiming is BotAimingClass aimclass && aimclass.aimStatus_0 != AimStatus.NoTarget)
            {
                if (settings.SMOOTHTURN_SETTINGS_BY_STATE.TryGetValue(EBotLookMode.Aiming, out turnSettings))
                {
                    return turnSettings;
                }
                return new TurnSettings(0.2f, 500f);
            }
            if (botComponent != null)
            {
                if (botComponent.SAINLayersActive && botComponent.Mover.PathFollower.Running)
                {
                    if (settings.SMOOTHTURN_SETTINGS_BY_STATE.TryGetValue(EBotLookMode.CombatSprint, out turnSettings))
                        return turnSettings;
                    return new TurnSettings(0.25f, 500f);
                }
                if (botComponent.Steering.CurrentSteerPriority == Models.Enums.ESteerPriority.RandomLook)
                {
                    if (settings.SMOOTHTURN_SETTINGS_BY_STATE.TryGetValue(EBotLookMode.RandomLook, out turnSettings))
                        return turnSettings;
                    return new TurnSettings(0.75f, 240f);
                }
                Enemy enemy = botComponent.CurrentTarget.CurrentTargetEnemy;
                if (enemy != null)
                {
                    if (enemy.IsVisible)
                    {
                        if (settings.SMOOTHTURN_SETTINGS_BY_STATE.TryGetValue(EBotLookMode.CombatVisibleEnemy, out turnSettings))
                            return turnSettings;
                        return new TurnSettings(0.4f, 500f);
                    }
                    if (settings.SMOOTHTURN_SETTINGS_BY_STATE.TryGetValue(EBotLookMode.Combat, out turnSettings))
                        return turnSettings;
                    return new TurnSettings(0.5f, 360f);
                }
            }
            else if (bot.Memory.GoalEnemy != null)
            {
                if (settings.SMOOTHTURN_SETTINGS_BY_STATE.TryGetValue(EBotLookMode.Combat, out turnSettings))
                {
                    return turnSettings;
                }
                return new TurnSettings(0.3f, 360f);
            }
            else if (bot.Mover.Sprinting)
            {
                    if (settings.SMOOTHTURN_SETTINGS_BY_STATE.TryGetValue(EBotLookMode.CombatSprint, out turnSettings))
                        return turnSettings;
                    return new TurnSettings(0.2f, 500f);
            }
            if (settings.SMOOTHTURN_SETTINGS_BY_STATE.TryGetValue(EBotLookMode.Peace, out turnSettings))
            {
                return turnSettings;
            }
            return new TurnSettings(0.65f, 360f);
        }

        public void SetTargetLookDirection(Vector3 targetDirection)
        {
            ControlLookDirection.Target = targetDirection;
        }

        public void SetTargetMoveDirection(Vector3 direction, Player player)
        {
            if (direction.sqrMagnitude > 0.0001f)
            {
                Vector2 moveDir = FindMoveDirection(direction, player.Rotation);
                if (moveDir.sqrMagnitude > 0.0001f)
                {
                    player.Move(moveDir);
                    player.AIData?.BotOwner?.AimingManager?.CurrentAiming?.Move(player.Speed);
                }
            }
        }

        public void SetTargetMovePoint(Vector3 point, Player player)
        {
            SetTargetMoveDirection(point - player.Position, player);
        }

        private static Vector2 FindMoveDirection(Vector3 direction, Vector2 playerRotation)
        {
            Vector3 vector = Quaternion.Euler(0f, 0f, playerRotation.x) * new Vector2(direction.x, direction.z);
            return MoveDirToVector2(vector);
        }

        private static Vector2 MoveDirToVector2(Vector3 direction)
        {
            return new Vector2(direction.x, direction.y);
        }
    }
}