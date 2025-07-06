using UnityEngine;
using static RootMotion.FinalIK.FBIKChain;

namespace SAIN.Components.PlayerComponentSpace
{
    public class SmoothDampVector(float capDistance = -1)
    {
        public void Calculate(float deltaTime, float smoothing, float maxSpeed, float xCoef = 1, float yCoef = 1, float zCoef = 1)
        {
            Vector3 targetDir = Target;
            if (CapLengthDistance > 0 && Target.sqrMagnitude > 1)
            {
                targetDir.Normalize();
            }

            Current = new(
                Mathf.SmoothDamp(Current.x, targetDir.x, ref Velocity.x, smoothing * xCoef, maxSpeed, deltaTime),
                Mathf.SmoothDamp(Current.y, targetDir.y, ref Velocity.y, smoothing * yCoef, maxSpeed, deltaTime),
                Mathf.SmoothDamp(Current.z, targetDir.z, ref Velocity.z, smoothing * zCoef, maxSpeed, deltaTime)
                );
        }

        public Vector3 Current = Vector3.forward;
        public Vector3 Target = Vector3.forward;
        public Vector3 Velocity = Vector3.zero;
        public float CapLengthDistance = capDistance;
    }

    public class SmoothDampVectorDirectionNormal
    {
        public void Calculate(float deltaTime, float smoothing, float maxSpeed)
        {
            Vector3 targetDir = Target.normalized;

            // Convert target direction to angle
            float targetAngle = Mathf.Atan2(targetDir.z, targetDir.x) * Mathf.Rad2Deg;

            // Smooth damp the angle
            currentAngle = Mathf.SmoothDampAngle(currentAngle, targetAngle, ref angleVelocity, smoothing, maxSpeed, deltaTime);

            // Convert angle back to direction
            float rad = currentAngle * Mathf.Deg2Rad;
            Current = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)).normalized;
        }

        public Vector3 Current = Vector3.forward;
        public Vector3 Target = Vector3.forward;
        private float currentAngle;
        private float angleVelocity;
    }
}