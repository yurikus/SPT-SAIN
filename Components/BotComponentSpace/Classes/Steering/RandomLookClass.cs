using UnityEngine;
using Random = UnityEngine.Random;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class RandomLookClass : BotSubClass<SAINSteeringClass>
    {
        public RandomLookClass(SAINSteeringClass steeringClass) : base(steeringClass) { }

        public Vector3? UpdateRandomLook()
        {
            if (_randomLookTime < Time.time)
            {
                _lookRandomToggle = !_lookRandomToggle;
                _randomLookPoint = findRandomLookPos(out bool isRandom);
                if (_randomLookPoint == null)
                {
                    _randomLookTime = Time.time + 0.1f;
                }
                else
                {
                    float baseTime = isRandom ? 2f : 4f;
                    _randomLookTime = Time.time + baseTime * Random.Range(0.66f, 1.33f);
                }
            }
            return _randomLookPoint;
        }

        private Vector3? findRandomLookPos(out bool isRandomLook)
        {
            if (_lookRandomToggle)
            {
                Vector3 randomLookPosition = generateRandomLookPos();
                if (randomLookPosition != Vector3.zero)
                {
                    isRandomLook = true;
                    return randomLookPosition;
                }
            }
            isRandomLook = false;
            if (BaseClass.FindLastKnownTarget(Bot.Enemy, out Vector3 EnemyPosition))
            {
                return EnemyPosition;
            }
            return null;
        }

        private Vector3 generateRandomLookPos()
        {
            var Mask = LayerMaskClass.HighPolyWithTerrainMask;
            var headPos = Bot.Transform.HeadPosition;

            float pointDistance = 0f;
            Vector3 result = Vector3.zero;
            for (int i = 0; i < 5; i++)
            {
                var random = Random.onUnitSphere * 5f;
                random.y = 0f;
                if (!Physics.Raycast(headPos, random, out var hit, 8f, Mask))
                {
                    result = random + headPos;
                    break;
                }
                else if (hit.distance > pointDistance)
                {
                    pointDistance = hit.distance;
                    result = hit.point;
                }
            }
            return result;
        }

        private Vector3? _randomLookPoint;
        private float _randomLookTime = 0f;
        private bool _lookRandomToggle;
    }
}