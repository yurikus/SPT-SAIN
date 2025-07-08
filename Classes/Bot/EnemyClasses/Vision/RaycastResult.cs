using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class RaycastResult
    {
        private const float SIGHT_PERIOD_SEC = 0.25f;

        public bool InSight => TimeSinceSuccess <= SIGHT_PERIOD_SEC;
        public float TimeSinceChecked => Time.time - _lastCheckTime;
        public float TimeSinceSuccess => Time.time - _lastSuccessTime;

        public RaycastHit LastRaycastHit { get; private set; }
        public BodyPartCollider LastSuccessBodyPart { get; private set; }
        public Vector3? LastSuccessPoint { get; private set; }

        public void Update(Vector3 castPoint, BodyPartCollider bodyPartCollider, RaycastHit raycastHit, float time)
        {
            _lastCheckTime = time;
            LastRaycastHit = raycastHit;

            if (raycastHit.collider == null)
            {
                LastSuccessBodyPart = bodyPartCollider;
                LastSuccessPoint = castPoint;
                _lastSuccessTime = time;
            }
            else
            {
                LastSuccessBodyPart = null;
                LastSuccessPoint = null;
            }
        }

        private float _lastCheckTime;
        private float _lastSuccessTime;
    }
}