using SAIN.Models.Structs;
using UnityEngine;

namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public class ColliderData
    {
        public ColliderData(SAINHardColliderData hardData, TargetData targetDirs)
        {
            Vector3 colliderPos = hardData.Position;
            Vector3 dirToTarget = targetDirs.TargetPosition - colliderPos;
            dirColliderToTarget = dirToTarget;
            dirColliderToTargetNormal = dirToTarget.normalized;
            ColliderToTargetMagnitude = dirToTarget.magnitude;

            dirTargetToCollider = -dirColliderToTarget;
            dirTargetToColliderNormal = -dirColliderToTargetNormal;

            Vector3 dirToCollider = colliderPos - targetDirs.BotPosition;
            dirBotToCollider = dirToCollider;
            dirBotToColliderNormal = dirToCollider.normalized;
            ColliderDistanceToBot = dirToCollider.magnitude;
        }

        public Vector3 dirColliderToTarget;
        public Vector3 dirColliderToTargetNormal;
        public float ColliderToTargetMagnitude;

        public Vector3 dirTargetToCollider;
        public Vector3 dirTargetToColliderNormal;

        public Vector3 dirBotToCollider;
        public Vector3 dirBotToColliderNormal;
        public float ColliderDistanceToBot;
    }
}