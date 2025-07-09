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
            if (settings.RANDOMSWAY_TOGGLE) RandomSwayOffset = CalcRandomSway(deltaTime) * CalcRandomSwayModifier(player, botOwner, botComponent);
            else RandomSwayOffset = Vector3.zero;
            TurnSettings turnSettings = GetTurnSettings(botOwner, botComponent);
            ControlLookDirection.Calculate(deltaTime, turnSettings.SmoothingValue, turnSettings.MaxTurnSpeed, settings.TURN_PITCH_MAX);
            player.CharacterController.SetSteerDirection(ControlLookDirection.Current);
        }

        private Vector3 RandomSwayOffset;

        private Vector3 CalcRandomSway(float deltaTime)
        {
            var settings = GlobalSettingsClass.Instance.Steering;
            loopTime += deltaTime;
            float t = (loopTime % settings.RANDOMSWAY_LOOPDURATION) / settings.RANDOMSWAY_LOOPDURATION; // 0 to 1 looping

            // Base circular loop
            float angle = t * Mathf.PI * 2f;
            float x = Mathf.Cos(angle);
            float z = Mathf.Sin(angle);

            // Add smooth looping wobble using extra sine waves
            float y = Mathf.Sin(angle * 2f) * 0.5f + Mathf.Sin(angle * 3.1f) * 0.3f;

            Vector3 basePos = new Vector3(x, y, z).normalized * settings.RANDOMSWAY_RADIUS;

            // Add small chaotic offset for more natural motion
            float wobble = Mathf.Sin(angle * 4f + Mathf.PI * 0.5f) * settings.RANDOMSWAY_SCALE;
            return basePos + new Vector3(wobble, -wobble, wobble * 0.5f);
        }

        private static float CalcRandomSwayModifier(Player player, BotOwner botOwner, BotComponent botComponent)
        {
            float result = 1f;
            bool sprinting = player.IsSprintEnabled;
            if (sprinting)
            {
                return 0.25f;
            }

            MovementContext movementContext = player.MovementContext;
            bool armsDamaged = movementContext.PhysicalConditionIs(EPhysicalCondition.LeftArmDamaged) || movementContext.PhysicalConditionIs(EPhysicalCondition.RightArmDamaged);
            bool noStamina = player.Physical.Stamina.NormalValue <= 0.1f;
            bool moving = botComponent?.Mover?.PathFollower?.Moving == true || botOwner.Mover?.IsMoving == true;
            bool aiming = botOwner.AimingManager.CurrentAiming is BotAimingClass aimClass && aimClass.aimStatus_0 != AimStatus.NoTarget;
            bool aimingDownSights = botOwner.WeaponManager.ShootController?.IsAiming == true;

            if (aiming) result *= 0.66f;
            if (aimingDownSights) result *= 0.66f;
            if (moving) result *= 2f;
            if (armsDamaged) result *= 1.5f;
            if (noStamina) result *= 1.5f;
            return Mathf.Clamp(result, 0.001f, 2.5f);
        }

        private float loopTime;

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
            ControlLookDirection.Target = targetDirection + RandomSwayOffset;
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