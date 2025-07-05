using SAIN.Preset.GlobalSettings;
using UnityEngine;

namespace SAIN.Components.RotationController
{
    public static class RotationTypes
    {
        public static void CalcSmoothDampAngleTurn(ref BotRotationManagerComponent.CalcRotationInput Data, out BotRotationManagerComponent.CalcRotationOutput Result, float maxTurnSpeed, float deltaTime)
        {
            var settings = GlobalSettingsClass.Instance.Steering;

            Data.TargetLookDirection.Normalize();
            Vector3 smoothDirection = new(
                Mathf.SmoothDampAngle(Data.LookDirection.x, Data.TargetLookDirection.x, ref Data.LookVelocity.x, settings.SmoothTurn_Smoothing * settings.SmoothTurn_X_Coef, settings.SmoothTurn_MaxTurnSpeed, deltaTime),
                Mathf.SmoothDampAngle(Data.LookDirection.y, Data.TargetLookDirection.y, ref Data.LookVelocity.y, settings.SmoothTurn_Smoothing * settings.SmoothTurn_Y_Coef, settings.SmoothTurn_MaxTurnSpeed, deltaTime),
                Mathf.SmoothDampAngle(Data.LookDirection.z, Data.TargetLookDirection.z, ref Data.LookVelocity.z, settings.SmoothTurn_Smoothing * settings.SmoothTurn_Z_Coef, settings.SmoothTurn_MaxTurnSpeed, deltaTime)
                );

            Result = new BotRotationManagerComponent.CalcRotationOutput() {
                CalculatedLookDirection = smoothDirection,
                LookVelocity = Data.LookVelocity,
            };
        }

        public static void CalcSmoothDampAngleTurn(ref Vector3 current, ref Vector3 velocity, Vector3 target, float deltaTime)
        {
            var settings = GlobalSettingsClass.Instance.Steering;
            current = new(
                Mathf.SmoothDampAngle(current.x, target.x, ref velocity.x, settings.SmoothTurn_Smoothing * settings.SmoothTurn_X_Coef, settings.SmoothTurn_MaxTurnSpeed, deltaTime),
                Mathf.SmoothDampAngle(current.y, target.y, ref velocity.y, settings.SmoothTurn_Smoothing * settings.SmoothTurn_Y_Coef, settings.SmoothTurn_MaxTurnSpeed, deltaTime),
                Mathf.SmoothDampAngle(current.z, target.z, ref velocity.z, settings.SmoothTurn_Smoothing * settings.SmoothTurn_Z_Coef, settings.SmoothTurn_MaxTurnSpeed, deltaTime)
                );
            current.Normalize();
        }
    }
}