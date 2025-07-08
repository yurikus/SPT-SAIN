using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace.PersonClasses
{
    public class PersonVelocityClass : PersonSubClass
    {
        private const float TRANSFORM_UPDATE_VELOCITY_FREQ = 1f / 5f;
        private const float TRANSFORM_MIN_VELOCITY = 0.25f;
        private const float TRANSFORM_MAX_VELOCITY = 5f;

        public float MagnitudeNormal { get; private set; }
        public float Magnitude { get; private set; }
        public Vector3 Vector { get; private set; }

        public void Update()
        {
            updateVelocity();
        }

        private void updateVelocity()
        {
            if (_nextUpdateVelocityTime <= Time.time)
            {
                _nextUpdateVelocityTime = Time.time + TRANSFORM_UPDATE_VELOCITY_FREQ;
                Vector = Person.Player.MovementContext.Velocity;
                getPlayerVelocity(Vector.magnitude);
            }
        }

        private void getPlayerVelocity(float magnitude)
        {
            if (magnitude <= TRANSFORM_MIN_VELOCITY)
            {
                Magnitude = 0f;
                MagnitudeNormal = 0f;
                return;
            }
            if (magnitude >= TRANSFORM_MAX_VELOCITY)
            {
                Magnitude = TRANSFORM_MAX_VELOCITY;
                MagnitudeNormal = 1f;
                return;
            }
            Magnitude = magnitude;
            float num = TRANSFORM_MAX_VELOCITY - TRANSFORM_MIN_VELOCITY;
            float num2 = magnitude - TRANSFORM_MIN_VELOCITY;
            MagnitudeNormal = num2 / num;
        }

        public PersonVelocityClass(PersonClass person, PlayerData playerData) : base(person, playerData)
        {
        }

        private float _nextUpdateVelocityTime;
    }
}