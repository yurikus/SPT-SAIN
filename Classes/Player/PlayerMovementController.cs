using EFT;
using SAIN.Components;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Components.RotationController;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.Types.TurnSmoothing;
using System;
using UnityEngine;
using UnityEngine.UIElements;
using static GClass1943;

namespace SAIN.Classes
{
    public class PlayerMovementController
    {
        public Vector3 CurrentControlLookDirection => TurnData.CurrentLookDirection;

        public BotTurnData TurnData { get; set; } = new(Vector3.forward);

        public void TickBotSteering(float deltaTime, BotOwner botOwner, BotComponent botComponent, SmoothTurnConfig config)
        {
            BotTurnData turnData = TurnData;
            var settings = GlobalSettingsClass.Instance.Steering;
            turnData.Config = config;
            TurnData = PredictiveLookSmoothing.UpdateSmoothedDirection(turnData, deltaTime);

            UpdateRandomSway(deltaTime, botOwner.GetPlayer, botOwner, botComponent, settings);

            Vector3 dir = TurnData.CurrentLookDirection + RandomSwayOffset;
            SetXAngle(botOwner, dir);
            SetYAngle(CalcYByDir(dir), botOwner.GetPlayer, botOwner);
        }

        private void UpdateRandomSway(float deltaTime, Player player, BotOwner botOwner, BotComponent botComponent, SteeringSettings settings)
        {
            if (settings.RANDOMSWAY_TOGGLE) RandomSwayOffset = CalcRandomSway(deltaTime) * CalcRandomSwayModifier(player, botOwner, botComponent);
            else RandomSwayOffset = Vector3.zero;
        }

        private void SetPlayerSpeed(Player player, float magnitude, float playerSpeed, float START_SLOW_DIST, bool shallSprint)
        {
            const float SLOW_COEF = 10f;
            if (shallSprint || player.IsSprintEnabled)
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
            else if (magnitude >= STOP_SPRINT_DIST * 1.25f)
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

        public void SetTargetLookDirection(Vector3 targetDirection, BotOwner botOwner, BotComponent bot)
        {
            if (bot != null) targetDirection = bot.Info.WeaponInfo.Recoil.ApplyRecoil(targetDirection);

            BotTurnData turnData = TurnData;
            turnData.NewTargetLookDirection = targetDirection;
            TurnData = turnData;
        }

        public virtual void SetXAngle(BotOwner botOwner, Vector3 lookDirection)
        {
            float target;
            if (botOwner.LookedTransform != null)
            {
                Vector3 normalized = (botOwner.LookedTransform.position - botOwner.WeaponRoot.position).normalized;
                target = 57.29578f * Mathf.Atan2(normalized.x, normalized.z);
            }
            else
            {
                target = 57.29578f * Mathf.Atan2(lookDirection.x, lookDirection.z);
            }
            float num = Mathf.DeltaAngle(botOwner.GetPlayer.Rotation.x, target);
            //if (botOwner.BotLay.IsLay && num > botOwner.Settings.FileSettings.Look.ANGLE_FOR_GETUP)
            //{
            //    botOwner.BotLay.GetUp(true);
            //}
            botOwner.AimingManager.CurrentAiming.RotateX(num);
            botOwner.GetPlayer.Rotate(new Vector2(num, 0f), true);
        }

        public void SetYAngle(float angle, Player player, BotOwner botOwner)
        {
            float num = Mathf.DeltaAngle(player.Rotation.y, angle);
            botOwner.AimingManager.CurrentAiming.RotateY(num);
            player.Rotate(new Vector2(0f, num), true);
        }

        private static float CalcYByDir(Vector3 dir)
        {
            float magnitude = dir.magnitude;
            float num = -dir.y / magnitude;
            num = Mathf.Clamp(num, -1f, 1f);
            num = 57.29578f * Mathf.Asin(num);
            return -Mathf.Abs(num) * Mathf.Sign(dir.y);
        }

        public void SetTargetMoveDirection(Vector3 direction, Vector3 finalMoveDestination, PlayerComponent playerComp)
        {
            if (direction.sqrMagnitude < 0.001f) return;

            Player player = playerComp.Player;
            float playerSpeed = player.Speed;

            const float START_SLOW_DIST = 0.75f;
            const float STOP_SPRINT_DIST = 1f;

            float destinationDistance = (finalMoveDestination - playerComp.Position).magnitude;
            bool canSprint = player.MovementContext.CanSprint && CheckCanSprintByDistanceRemaining(destinationDistance, STOP_SPRINT_DIST);
            bool shallSprint = canSprint && _wantToSprint;
            SetPlayerSpeed(player, destinationDistance, playerSpeed, START_SLOW_DIST, shallSprint);
            player.EnableSprint(shallSprint);

            direction.Normalize();
            if (destinationDistance > 0.1f) player.CharacterController.SetSteerDirection(direction);
            Vector2 moveDir = FindMoveDirection(direction, player.Rotation);
            player.Move(moveDir);
            playerComp.BotOwner?.AimingManager?.CurrentAiming?.Move(player.Speed);
        }

        private static float CalcRandomSwayModifier(Player player, BotOwner botOwner, BotComponent botComponent)
        {
            float result = 1f;
            if (botComponent?.Mover?.Running == true)
            {
                return 0.01f;
            }
            bool sprinting = player.IsSprintEnabled;
            if (sprinting)
            {
                return 0.01f;
            }

            MovementContext movementContext = player.MovementContext;
            bool armsDamaged = movementContext.PhysicalConditionIs(EPhysicalCondition.LeftArmDamaged) || movementContext.PhysicalConditionIs(EPhysicalCondition.RightArmDamaged);
            bool noStamina = player.Physical.Stamina.NormalValue <= 0.1f;
            bool moving = botComponent?.Mover?.Moving == true || botOwner.Mover?.IsMoving == true;
            bool aiming = botOwner.AimingManager.CurrentAiming is BotAimingClass aimClass && aimClass.aimStatus_0 != AimStatus.NoTarget;
            bool aimingDownSights = botOwner.WeaponManager.ShootController?.IsAiming == true;

            if (aiming) result *= 0.5f;
            if (aimingDownSights) result *= 0.15f;
            if (moving) result *= 1.25f;
            if (armsDamaged) result *= 1.5f;
            if (noStamina) result *= 1.5f;
            return Mathf.Clamp(result, 0.01f, 2.5f);
        }

        private static Vector2 FindMoveDirection(Vector3 direction, Vector2 playerRotation)
        {
            Vector3 vector = Quaternion.Euler(0f, 0f, playerRotation.x) * new Vector2(direction.x, direction.z);
            return new Vector2(vector.x, vector.y);
        }

        private static TurnSettings GetTurnSettings(BotOwner bot, BotComponent botComponent)
        {
            TurnSettings turnSettings;
            //var settings = GlobalSettingsClass.Instance.Steering;
            //if (bot.AimingManager.CurrentAiming?.IsReady == true || bot.AimingManager.CurrentAiming is BotAimingClass aimclass && aimclass.aimStatus_0 != AimStatus.NoTarget)
            //{
            //    if (settings.SMOOTHTURN_SETTINGS.TryGetValue(EBotLookMode.Aiming, out turnSettings))
            //    {
            //        return turnSettings;
            //    }
            //    return new TurnSettings(0.15f, 500f);
            //}
            //if (botComponent != null)
            //{
            //    if (botComponent.Mover.Running)
            //    {
            //        if (settings.SMOOTHTURN_SETTINGS.TryGetValue(EBotLookMode.CombatSprint, out turnSettings))
            //            return turnSettings;
            //        return new TurnSettings(0.2f, 500f);
            //    }
            //    if (botComponent.Steering.CurrentSteerPriority == Models.Enums.ESteerPriority.RandomLook)
            //    {
            //        if (settings.SMOOTHTURN_SETTINGS.TryGetValue(EBotLookMode.RandomLook, out turnSettings))
            //            return turnSettings;
            //        return new TurnSettings(0.75f, 240f);
            //    }
            //    Enemy enemy = botComponent.GoalEnemy;
            //    if (enemy != null)
            //    {
            //        if (enemy.IsVisible)
            //        {
            //            if (settings.SMOOTHTURN_SETTINGS.TryGetValue(EBotLookMode.CombatVisibleEnemy, out turnSettings))
            //                return turnSettings;
            //            return new TurnSettings(0.4f, 500f);
            //        }
            //        if (settings.SMOOTHTURN_SETTINGS.TryGetValue(EBotLookMode.Combat, out turnSettings))
            //            return turnSettings;
            //        return new TurnSettings(0.5f, 360f);
            //    }
            //}
            //else if (bot.Memory.GoalEnemy != null)
            //{
            //    if (settings.SMOOTHTURN_SETTINGS.TryGetValue(EBotLookMode.Combat, out turnSettings))
            //    {
            //        return turnSettings;
            //    }
            //    return new TurnSettings(0.3f, 360f);
            //}
            //else if (bot.Mover.Sprinting)
            //{
            //    if (settings.SMOOTHTURN_SETTINGS.TryGetValue(EBotLookMode.CombatSprint, out turnSettings))
            //        return turnSettings;
            //    return new TurnSettings(0.2f, 500f);
            //}
            //if (settings.SMOOTHTURN_SETTINGS.TryGetValue(EBotLookMode.Peace, out turnSettings))
            //{
            //    return turnSettings;
            //}
            return new TurnSettings(0.65f, 360f);
        }

        public static class Util
        {
            public static Vector3 CalculateBallisticOffset(Vector3 weaponRoot, Vector3 targetPosition, Vector3 playerVelocity, float muzzleVelocity, float gravity = 9.81f)
            {
                // Basic vector from shooter to target
                Vector3 displacement = targetPosition - weaponRoot;
                float horizontalDistance = new Vector3(displacement.x, 0, displacement.z).magnitude;

                // Target is directly above/below, no ballistic correction needed
                Vector3 bulletDropOffset = horizontalDistance < 0.01f ? Vector3.zero : CalculateSimpleBallisticOffset(displacement, muzzleVelocity, gravity);
                Vector3 leadOffset = playerVelocity * (displacement.magnitude / muzzleVelocity);
                //DebugGizmos.DrawLine(targetPosition, targetPosition + playerVelocity, Color.blue, 0.015f, 5f);
                //DebugGizmos.DrawLine(targetPosition, targetPosition + leadOffset, Color.green, 0.015f, 5f);
                DebugGizmos.DrawLine(targetPosition, targetPosition + leadOffset + bulletDropOffset, Color.green, 0.015f, 5f);
                return bulletDropOffset + leadOffset;
            }

            private static Vector3 CalculateSimpleBallisticOffset(Vector3 displacement, float muzzleVelocity, float gravity)
            {
                var horizontalDistance = new Vector3(displacement.x, 0, displacement.z).magnitude;
                var heightDifference = displacement.y;

                // Calculate time of flight using quadratic formula
                var timeOfFlight = CalculateTimeOfFlight(horizontalDistance, heightDifference, muzzleVelocity, gravity);

                if (timeOfFlight <= 0)
                {
                    // No valid solution (target too far or velocity too low)
                    return Vector3.zero;
                }

                // Calculate vertical drop
                var verticalDrop = 0.5f * gravity * timeOfFlight * timeOfFlight;

                // Return the upward offset needed to compensate for drop
                return new Vector3(0, verticalDrop, 0);
            }

            private static float CalculateTimeOfFlight(
                float horizontalDistance,
                float heightDifference,
                float muzzleVelocity,
                float gravity)
            {
                // For a given horizontal distance and height difference,
                // solve for the time using the kinematic equation

                // We assume optimal launch angle for maximum range
                var discriminant = muzzleVelocity * muzzleVelocity * muzzleVelocity * muzzleVelocity -
                                   gravity * (gravity * horizontalDistance * horizontalDistance + 2 * heightDifference * muzzleVelocity * muzzleVelocity);

                if (discriminant < 0)
                {
                    // No real solution - target is unreachable
                    return -1f;
                }

                // Calculate the optimal launch angle
                var tanTheta = (muzzleVelocity * muzzleVelocity - Mathf.Sqrt(discriminant)) / (gravity * horizontalDistance);
                var launchAngle = Mathf.Atan(tanTheta);

                // Calculate horizontal velocity component
                var horizontalVelocity = muzzleVelocity * Mathf.Cos(launchAngle);

                // Time of flight
                return horizontalDistance / horizontalVelocity;
            }
        }
    }
}