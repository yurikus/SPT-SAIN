using SAIN.Preset.GlobalSettings;
using UnityEngine;

public class PredictiveLookSmoothing
{
    private Vector3 _lastTargetDirection;
    private Vector3 _targetVelocity;
    private bool _initialized;

    public float SmoothingFactor  => GlobalSettingsClass.Instance.Steering.SmoothingFactor;
    public float PredictionStrength  => GlobalSettingsClass.Instance.Steering.PredictionStrength;
    public float MaxAngularVelocity  => GlobalSettingsClass.Instance.Steering.MaxAngularVelocity;
    public float ConvergenceBoost => GlobalSettingsClass.Instance.Steering.ConvergenceBoost;

    /// <summary>
    /// Updates the smoothed look direction towards the target with predictive compensation.
    /// </summary>
    /// <param name="currentDirection">Current look direction (normalized)</param>
    /// <param name="targetDirection">Desired look direction (normalized)</param>
    /// <param name="deltaTime">Time since last update</param>
    /// <returns>New smoothed look direction (normalized)</returns>
    public Vector3 UpdateSmoothedDirection(Vector3 currentDirection, Vector3 targetDirection, float deltaTime)
    {
        if (!_initialized)
        {
            Initialize(targetDirection);
            return currentDirection;
        }

        if (deltaTime <= 0f) return currentDirection;

        // Calculate target angular velocity
        UpdateTargetVelocity(targetDirection, deltaTime);

        // Predict where the target will be to compensate for smoothing lag
        var predictedTarget = PredictTargetDirection(targetDirection, deltaTime);

        // Apply adaptive smoothing with convergence boost
        var adaptiveSmoothingFactor = CalculateAdaptiveSmoothingFactor(currentDirection, predictedTarget, deltaTime);
        
        // Perform the smoothed rotation
        return SmoothTowardsTarget(currentDirection, predictedTarget, adaptiveSmoothingFactor);
    }

    private void Initialize(Vector3 targetDirection)
    {
        _lastTargetDirection = targetDirection.normalized;
        _targetVelocity = Vector3.zero;
        _initialized = true;
    }

    private void UpdateTargetVelocity(Vector3 targetDirection, float deltaTime)
    {
        targetDirection = targetDirection.normalized;

        if (deltaTime > 0f)
        {
            // Calculate angular velocity using cross product and dot product
            var cross = Vector3.Cross(_lastTargetDirection, targetDirection);
            var dot = Vector3.Dot(_lastTargetDirection, targetDirection);
            
            // Calculate angular velocity in degrees per second
            var angularChange = Mathf.Atan2(cross.magnitude, dot) * Mathf.Rad2Deg;
            var angularVelocityVector = cross.magnitude > 0.001f ? cross.normalized * (angularChange / deltaTime) : Vector3.zero;

            // Smooth the velocity estimate to reduce noise
            var velocitySmoothingFactor = Mathf.Clamp01(deltaTime * 10f);
            _targetVelocity = Vector3.Lerp(_targetVelocity, angularVelocityVector, velocitySmoothingFactor);

            // Clamp to maximum angular velocity
            if (_targetVelocity.magnitude > MaxAngularVelocity)
            {
                _targetVelocity = _targetVelocity.normalized * MaxAngularVelocity;
            }
        }

        _lastTargetDirection = targetDirection;
    }

    private Vector3 PredictTargetDirection(Vector3 targetDirection, float deltaTime)
    {
        if (_targetVelocity.magnitude < 0.01f) return targetDirection;

        // Estimate lag time based on smoothing factor
        var estimatedLagTime = (1f - SmoothingFactor) / SmoothingFactor * deltaTime * PredictionStrength;

        // Predict future position using angular velocity
        var predictionAngle = _targetVelocity.magnitude * estimatedLagTime * Mathf.Deg2Rad;
        
        if (predictionAngle < 0.001f) return targetDirection;

        // Apply rotation using Rodriguez rotation formula
        var rotationAxis = _targetVelocity.normalized;
        var predictionRotation = Quaternion.AngleAxis(_targetVelocity.magnitude * estimatedLagTime, rotationAxis);
        
        return predictionRotation * targetDirection;
    }

    private float CalculateAdaptiveSmoothingFactor(Vector3 currentDirection, Vector3 targetDirection, float deltaTime)
    {
        // Calculate angular difference
        var angularDifference = Vector3.Angle(currentDirection, targetDirection);
        
        // Apply convergence boost when far from target
        var convergenceMultiplier = 1f;
        if (angularDifference > 20f) // If more than 30 degrees off
        {
            // The convergence boost is applied over a 180 degree range (inner range of 20 + 160)
            convergenceMultiplier = Mathf.Lerp(1f, ConvergenceBoost, (angularDifference - 20f) / 160f);
        }

        // Scale smoothing factor by delta time and convergence boost
        var adaptiveFactor = SmoothingFactor * convergenceMultiplier;
        
        // Ensure we don't overshoot with large delta times
        // TODO: Switch to FixedDeltaTime if possible! 
        return Mathf.Clamp01(adaptiveFactor * (deltaTime * 60f)); // Normalize for 60 FPS baseline
    }

    private static Vector3 SmoothTowardsTarget(Vector3 current, Vector3 target, float smoothingFactor)
    {
        // Use Quaternion.Slerp for smooth angular interpolation
        var currentRotation = Quaternion.LookRotation(current, Vector3.up);
        var targetRotation = Quaternion.LookRotation(target, Vector3.up);
        
        // We use Quaternion.Slerp because it's stable
        var smoothedRotation = Quaternion.Slerp(currentRotation, targetRotation, smoothingFactor);
        return smoothedRotation * Vector3.forward; // Already normalized due to quaternion math
    }

    /// <summary>
    /// Resets the smoothing state. Call when the AI loses track of target or switches targets.
    /// </summary>
    public void Reset()
    {
        _initialized = false;
        _targetVelocity = Vector3.zero;
    }

    /// <summary>
    /// Force sets the direction state. Useful for teleportation or instant direction changes.
    /// </summary>
    public void SetDirection(Vector3 direction)
    {
        _lastTargetDirection = direction.normalized;
        _targetVelocity = Vector3.zero;
        _initialized = true;
    }
}
