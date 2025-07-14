using EFT;
using JetBrains.Annotations;
using SAIN.Classes.Transform;
using SAIN.Components;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Components.RotationController;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System;
using UnityEngine;

namespace SAIN.Classes
{
    public class PlayerMovementController
    {
        public bool Moving { get; private set; }
        public Vector3 CurrentControlLookDirection => ControlLookDirection.Current;
        public SmoothDampVectorDirectionNormal ControlLookDirection { get; } = new();

        public void UpdateBotMovement(float currentTime, float deltaTime, [NotNull] Player player, [NotNull] BotOwner botOwner, [CanBeNull] BotComponent botComponent)
        {
            var settings = GlobalSettingsClass.Instance.Steering;
            UpdateRandomSway(deltaTime, player, botOwner, botComponent, settings);
            UpdateTurnSmoothing(deltaTime, botOwner, botComponent, settings);
        }

        private void UpdateTurnSmoothing(float deltaTime, BotOwner botOwner, BotComponent botComponent, SteeringSettings settings)
        {
            TurnSettings turnSettings = GetTurnSettings(botOwner, botComponent);
            ControlLookDirection.Calculate(deltaTime, turnSettings.SmoothingValue, turnSettings.MaxTurnSpeed, settings.TURN_PITCH_MAX);
        }

        private void UpdateRandomSway(float deltaTime, Player player, BotOwner botOwner, BotComponent botComponent, SteeringSettings settings)
        {
            if (settings.RANDOMSWAY_TOGGLE) RandomSwayOffset = CalcRandomSway(deltaTime) * CalcRandomSwayModifier(player, botOwner, botComponent);
            else RandomSwayOffset = Vector3.zero;
        }

        private void SetPlayerSpeed(Player player, float magnitude, float playerSpeed, float START_SLOW_DIST, bool shallSprint)
        {
            const float SLOW_COEF = 7f;
            if (shallSprint)
            {
                ChangeSpeed(player, 1f - playerSpeed);
            }
            else
            {
                float targetSpeed = _targetMoveSpeed;
                if (magnitude <= START_SLOW_DIST)
                {
                    targetSpeed *= (magnitude / SLOW_COEF);
                }
                float speedDelta = targetSpeed - playerSpeed;
                ChangeSpeed(player, speedDelta);
            }
        }

        private static void ChangeSpeed(Player player, float delta)
        {
            if (Math.Abs(delta) >= 1E-45f) player.ChangeSpeed(delta);
        }

        private static bool CheckCanSprintByDistanceRemaining(float magnitude, float STOP_SPRINT_DIST)
        {
            bool canSprint = false;
            if (magnitude < STOP_SPRINT_DIST)
            {
                canSprint = false;
            }
            else if (magnitude >= STOP_SPRINT_DIST * 1.1f)
            {
                canSprint = true;
            }
            else
            {
                canSprint = false;
            }
            return canSprint;
        }

        public void SetWantToSprint(bool value)
        {
            _wantToSprint = value;
        }

        private bool _wantToSprint;

        /// <summary>
        /// Value between 0 and 1
        /// </summary>
        public void SetTargetMoveSpeed(float value)
        {
            _targetMoveSpeed = Mathf.Clamp01(value);
        }

        private float _targetMoveSpeed;

        private Vector3 RandomSwayOffset;

        private Vector3 CalcRandomSway(float deltaTime)
        {
            var settings = GlobalSettingsClass.Instance.Steering;
            loopTime += deltaTime;
            float t = (loopTime % settings.RANDOMSWAY_LOOP_DURATION) / settings.RANDOMSWAY_LOOP_DURATION; // 0 to 1 looping

            // Base circular loop
            float angle = t * Mathf.PI * 2f;
            float x = Mathf.Cos(angle);
            float z = Mathf.Sin(angle);

            // Add smooth looping wobble using extra sine waves
            float y = Mathf.Sin(angle * 2f) * 0.5f + Mathf.Sin(angle * 3.1f) * 0.3f;

            Vector3 basePos = new Vector3(x, y, z).normalized * settings.RANDOMSWAY_CIRCLE_RADIUS;

            // Add small chaotic offset for more natural motion
            float wobble = Mathf.Sin(angle * 4f + Mathf.PI * 0.5f) * settings.RANDOMSWAY_CIRCLE_SCALE;
            return basePos + new Vector3(wobble, -wobble, wobble * 0.5f);
        }

        private float loopTime;

        public void SetTargetLookDirection(Vector3 targetDirection)
        {
            ControlLookDirection.Target = targetDirection + RandomSwayOffset;
        }

        public void SetTargetMoveDirection(Vector3 direction, Vector3 finalMoveDestination, PlayerComponent playerComp)
        {
            //if (direction.sqrMagnitude < 0.001f) return;
            Player player = playerComp.Player;
            float playerSpeed = player.Speed;

            const float START_SLOW_DIST = 1f;
            const float STOP_SPRINT_DIST = START_SLOW_DIST * 1.1f;

            float destinationDistance = (finalMoveDestination - playerComp.Position).magnitude;
            bool canSprint = player.MovementContext.CanSprint && CheckCanSprintByDistanceRemaining(destinationDistance, STOP_SPRINT_DIST);
            bool shallSprint = canSprint && _wantToSprint;
            SetPlayerSpeed(player, destinationDistance, playerSpeed, START_SLOW_DIST, shallSprint);
            player.EnableSprint(shallSprint);

            direction.Normalize();
            player.CharacterController.SetSteerDirection(direction);
            Vector2 moveDir = FindMoveDirection(direction, player.Rotation);
            player.Move(moveDir);
            playerComp.BotOwner?.AimingManager?.CurrentAiming?.Move(player.Speed);
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
            bool moving = botComponent?.Mover?.Moving == true || botOwner.Mover?.IsMoving == true;
            bool aiming = botOwner.AimingManager.CurrentAiming is BotAimingClass aimClass && aimClass.aimStatus_0 != AimStatus.NoTarget;
            bool aimingDownSights = botOwner.WeaponManager.ShootController?.IsAiming == true;

            if (aiming) result *= 0.5f;
            if (aimingDownSights) result *= 0.5f;
            if (moving) result *= 2f;
            if (armsDamaged) result *= 1.5f;
            if (noStamina) result *= 1.5f;
            return Mathf.Clamp(result, 0.001f, 2.5f);
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
                if (botComponent.Mover.Running)
                {
                    if (settings.SMOOTHTURN_SETTINGS_BY_STATE.TryGetValue(EBotLookMode.CombatSprint, out turnSettings))
                        return turnSettings;
                    return new TurnSettings(0.2f, 500f);
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
    }
}