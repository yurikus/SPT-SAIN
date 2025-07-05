using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace
{
    public class SmoothDampVector(bool inNormalize = true, float capDistance = -1)
    {
        public void Calculate(float deltaTime, float smoothing, float maxSpeed, float xCoef = 1, float yCoef = 1, float zCoef = 1)
        {
            Vector3 targetDir = Target;
            if (CapLengthDistance > 0)
            {
                if (Target.sqrMagnitude > 1)
                {
                    targetDir.Normalize();
                }
            }
            else if (Normalize)
            {
                targetDir.Normalize();
            }

            Current = new(
                Mathf.SmoothDamp(Current.x, targetDir.x, ref Velocity.x, smoothing * xCoef, maxSpeed, deltaTime),
                Mathf.SmoothDamp(Current.y, targetDir.y, ref Velocity.y, smoothing * yCoef, maxSpeed, deltaTime),
                Mathf.SmoothDamp(Current.z, targetDir.z, ref Velocity.z, smoothing * zCoef, maxSpeed, deltaTime)
                );
            if (Normalize)
            {
                Current.Normalize();
            }
        }

        public Vector3 Current = Vector3.forward;
        public Vector3 Target = Vector3.forward;
        public Vector3 Velocity = Vector3.zero;
        public bool Normalize = inNormalize;
        public float CapLengthDistance = capDistance;
    }
}